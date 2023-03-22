//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : SpellingTagger.cs
// Authors : Noah Richards, Roman Golovin, Michael Lehenbauer, Eric Woodruff
// Updated : 03/15/2023
// Note    : Copyright 2010-2023, Microsoft Corporation, All rights reserved
//           Portions Copyright 2013-2023, Eric Woodruff, All rights reserved
//
// This file contains a class that implements the spelling tagger
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 04/14/2013  EFW  Imported the code into the project.
// 04/14/2013  EFW  Added code to skip escape sequences so that they don't include extra characters in words or
//                  accidentally cause them to be excluded.  Added code to ignore XML entities, leading and
//                  trailing apostrophes, and words less than two characters in length.  Added support for
//                  NHunspell.
// 04/26/2013  EFW  Added support for new configuration options
// 05/02/2013  EFW  Added support for Replace All
// 05/11/2013  EFW  Added support for ignoring XML elements in the spell checked text
// 05/28/2013  EFW  Added properties to allow for interactive spell checking via a tool window
// 05/31/2013  EFW  Added support for Ignore Once
// 06/03/2014  EFW  Merged changes from David Ruhmann to ignore escaped words and enhance word breaking code.
//                  Added code to ignore .NET and C-style format string specifiers.
// 02/28/2015  EFW  Added support for code analysis dictionary options
// 07/28/2015  EFW  Added support for culture information in the spelling suggestions
// 08/20/2018  EFW  Added support for the inline ignore spelling directive
//===============================================================================================================

// Ignore Spelling: Versicherungs Vertrag

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Threading;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Threading;
using VisualStudio.SpellChecker.Configuration;
using VisualStudio.SpellChecker.Definitions;
using VisualStudio.SpellChecker.ProjectSpellCheck;
using VisualStudio.SpellChecker.Tagging;

namespace VisualStudio.SpellChecker
{
    /// <summary>
    /// This class implements the spelling tagger
    /// </summary>
    internal sealed class SpellingTagger : ITagger<MisspellingTag>
    {
        #region Ignored word class
        //=====================================================================

        /// <summary>
        /// This represents a word ignored once within the document
        /// </summary>
        private class IgnoredOnceWord
        {
            /// <summary>
            /// The word's position
            /// </summary>
            public int Position { get; }

            /// <summary>
            /// The span start
            /// </summary>
            public int StartIndex { get; }

            /// <summary>
            /// The span end
            /// </summary>
            public int EndIndex { get; }

            /// <summary>
            /// The word at the span location when it was ignored
            /// </summary>
            public string Word { get; }

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="span">The span containing the word's location</param>
            public IgnoredOnceWord(string word, int position, int startIndex, int endIndex)
            {
                this.Word = word;
                this.Position = position;
                this.StartIndex = startIndex;
                this.EndIndex = endIndex;
            }
        }
        #endregion

        #region Private data members
        //=====================================================================

        private readonly ITextBuffer buffer;
        private readonly ITagAggregator<INaturalTextTag> naturalTextAggregator;
        private readonly ITagAggregator<IUrlTag> urlAggregator;

        private readonly SpellCheckerConfiguration configuration;
        private readonly WordSplitter wordSplitter;

        private volatile List<MisspellingTag> misspellings;

        private readonly ConcurrentQueue<SnapshotSpan> dirtySpans;
        private readonly ConcurrentQueue<IgnoredOnceWord> wordsIgnoredOnce;
        private readonly List<InlineIgnoredWord> inlineIgnoredWords;

        private bool isClosed, spellCheckInProgress;
        private readonly bool unescapeApostrophes;

        private DispatcherTimer timer;
        private static bool disabledInSession;

        #endregion

        #region Properties
        //=====================================================================

        /// <summary>
        /// This is used to get or set whether or not interactive spell checking is enabled for the session
        /// </summary>
        public static bool DisabledInSession
        {
            get => disabledInSession;
            set
            {
                if(disabledInSession != value)
                {
                    disabledInSession = value;

                    GlobalDictionary.NotifyChangeOfStatus();
                }
            }
        }

        /// <summary>
        /// This read-only property returns an enumerable list of the current misspellings
        /// </summary>
        public IEnumerable<MisspellingTag> CurrentMisspellings
        {
            get
            {
                var currentMisspellings = misspellings;

                return currentMisspellings;
            }
        }

        /// <summary>
        /// This read-only property returns the spelling dictionary instance
        /// </summary>
        public SpellingDictionary Dictionary { get; }

        /// <summary>
        /// This is used to get an enumerable list of ignore once word spans
        /// </summary>
        public IEnumerable<Span> IgnoredOnceSpans => wordsIgnoredOnce.Select(
            s => Span.FromBounds(s.StartIndex, s.EndIndex)).ToList();

        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="buffer">The text buffer</param>
        /// <param name="view">The text view</param>
        /// <param name="naturalTextAggregator">The tag aggregator</param>
        /// <param name="urlAggregator">The URL aggregator</param>
        /// <param name="configuration">The spell checker configuration to use</param>
        /// <param name="dictionary">The spelling dictionary to use</param>
        public SpellingTagger(ITextBuffer buffer, ITextView view,
          ITagAggregator<INaturalTextTag> naturalTextAggregator, ITagAggregator<IUrlTag> urlAggregator,
          SpellCheckerConfiguration configuration, SpellingDictionary dictionary)
        {
            this.buffer = buffer;
            this.naturalTextAggregator = naturalTextAggregator;
            this.urlAggregator = urlAggregator;
            this.configuration = configuration;
            this.Dictionary = dictionary;

            dirtySpans = new ConcurrentQueue<SnapshotSpan>();
            wordsIgnoredOnce = new ConcurrentQueue<IgnoredOnceWord>();
            misspellings = new List<MisspellingTag>();
            inlineIgnoredWords = new List<InlineIgnoredWord>();

#pragma warning disable VSTHRD010
            string filename = buffer.GetFilename();
#pragma warning restore VSTHRD010

            wordSplitter = new WordSplitter
            {
                Configuration = configuration,
                Mnemonic = ClassifierFactory.GetMnemonic(filename),
                IsCStyleCode = ClassifierFactory.IsCStyleCode(filename)
            };

            this.buffer.Changed += BufferChanged;
            this.naturalTextAggregator.TagsChanged += AggregatorTagsChanged;
            this.urlAggregator.TagsChanged += AggregatorTagsChanged;
            this.Dictionary.DictionaryUpdated += DictionaryUpdated;
            this.Dictionary.ReplaceAll += ReplaceAll;
            this.Dictionary.IgnoreOnce += IgnoreOnce;

            view.Closed += ViewClosed;

            // Strings in SQL script can contain escaped single quotes which are apostrophes.  Unescape them
            // so that they are spell checked correctly.
            unescapeApostrophes = buffer.ContentType.IsOfType("SQL Server Tools");

            // To start with, the entire buffer is dirty.  Split this into chunks so we update pieces at a time.
            ITextSnapshot snapshot = this.buffer.CurrentSnapshot;

            this.AddDirtySpans(snapshot.Lines.Where(l => !l.Extent.IsEmpty).Select(l => l.Extent));
        }
        #endregion

        #region Event handlers
        //=====================================================================

        /// <summary>
        /// When closed, disconnect event handlers and dispose of resources
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void ViewClosed(object sender, EventArgs e)
        {
            isClosed = true;

            timer?.Stop();

            if(buffer != null)
                buffer.Changed -= BufferChanged;

            naturalTextAggregator?.Dispose();
            urlAggregator?.Dispose();

            if(this.Dictionary != null)
            {
                this.Dictionary.DictionaryUpdated -= DictionaryUpdated;
                this.Dictionary.ReplaceAll -= ReplaceAll;
                this.Dictionary.IgnoreOnce -= IgnoreOnce;
            }
        }

        /// <summary>
        /// Recheck the changed spans when tags change
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void AggregatorTagsChanged(object sender, TagsChangedEventArgs e)
        {
            if(!isClosed)
                this.AddDirtySpans(e.Span.GetSpans(buffer.CurrentSnapshot));
        }

        /// <summary>
        /// Recheck the affected spans when the dictionary is updated
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void DictionaryUpdated(object sender, SpellingEventArgs e)
        {
            if(isClosed)
                return;

            ITextSnapshot snapshot = buffer.CurrentSnapshot;

            // If the word is null, it means the entire dictionary was updated and we need to reparse the entire
            // file.
            if(e.Word == null)
            {
                this.AddDirtySpans(snapshot.Lines.Where(l => !l.Extent.IsEmpty).Select(l => l.Extent));
                return;
            }

            List<MisspellingTag> currentMisspellings = misspellings;

            foreach(var misspelling in currentMisspellings)
            {
                SnapshotSpan span = misspelling.Span.GetSpan(snapshot);

                if(span.GetText().Equals(e.Word, StringComparison.OrdinalIgnoreCase))
                    this.AddDirtySpan(span);
            }
        }

        /// <summary>
        /// Replace all occurrences of the specified word with its correct spelling
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void ReplaceAll(object sender, SpellingEventArgs e)
        {
            if(isClosed)
                return;

            var snapshot = buffer.CurrentSnapshot;
            var replacedWords = new List<MisspellingTag>();

            // Do all replacements in one edit
            using(var edit = snapshot.TextBuffer.CreateEdit())
            {
                var currentMisspellings = misspellings;

                foreach(var misspelling in currentMisspellings)
                {
                    var span = misspelling.Span.GetSpan(snapshot);

                    if(span.GetText().Equals(e.Word, StringComparison.OrdinalIgnoreCase))
                    {
                        replacedWords.Add(misspelling);

                        string currentWord = misspelling.Span.GetText(snapshot);
                        string replacementWord = e.ReplacementWord;

                        var language = e.Culture ?? CultureInfo.CurrentCulture;

                        // Match the case of the first letter if necessary
                        if(replacementWord.Length > 1 &&
                          (Char.IsUpper(replacementWord[0]) != Char.IsUpper(replacementWord[1]) ||
                          (Char.IsLower(replacementWord[0]) && Char.IsLower(replacementWord[1]))))
                            if(Char.IsUpper(currentWord[0]) && !Char.IsUpper(replacementWord[0]))
                            {
                                replacementWord = replacementWord.Substring(0, 1).ToUpper(language) +
                                    replacementWord.Substring(1);
                            }
                            else
                                if(Char.IsLower(currentWord[0]) && !Char.IsLower(replacementWord[0]))
                                    replacementWord = replacementWord.Substring(0, 1).ToLower(language) +
                                        replacementWord.Substring(1);

                        edit.Replace(span, replacementWord);

                        this.AddDirtySpan(span);
                    }
                }

                edit.Apply();
            }

            // Raise the TagsChanged event to get rid of the tags on the replaced words
            var tagsChanged = TagsChanged;

            if(tagsChanged != null)
            {
                snapshot = buffer.CurrentSnapshot;

                foreach(var misspelling in replacedWords)
                    tagsChanged(this, new SnapshotSpanEventArgs(misspelling.Span.GetSpan(snapshot)));
            }
        }

        /// <summary>
        /// Ignore a word once by remembering the span and ignoring it in subsequent updates
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void IgnoreOnce(object sender, SpellingEventArgs e)
        {
            if(!isClosed)
            {
                wordsIgnoredOnce.Enqueue(new IgnoredOnceWord(e.Word, e.Position, e.StartIndex, e.EndIndex));

                var currentMisspellings = misspellings;

                // Raise the TagsChanged event to get rid of the tags on the ignored word
                foreach(var misspelling in currentMisspellings)
                {
                    if(misspelling.Span.GetStartPoint(misspelling.Span.TextBuffer.CurrentSnapshot).Position == e.Position)
                    {
                        this.AddDirtySpan(misspelling.Span.GetSpan(misspelling.Span.TextBuffer.CurrentSnapshot));

                        this.TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(misspelling.Span.GetSpan(
                                misspelling.Span.TextBuffer.CurrentSnapshot)));

                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Recheck the affected spans when the buffer changes
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void BufferChanged(object sender, TextContentChangedEventArgs e)
        {
            if(isClosed)
                return;

            ITextSnapshot snapshot = e.After;

            foreach(var change in e.Changes)
            {
                SnapshotSpan changedSpan = new SnapshotSpan(snapshot, change.NewSpan);

                var startLine = changedSpan.Start.GetContainingLine();
                var endLine = (startLine.EndIncludingLineBreak < changedSpan.End) ?
                    changedSpan.End.GetContainingLine() : startLine;

                this.AddDirtySpan(new SnapshotSpan(startLine.Start, endLine.End));
            }
        }
        #endregion

        #region Helper methods
        //=====================================================================

        /// <summary>
        /// Get natural language spans for the given dirty span
        /// </summary>
        /// <param name="dirtySpan">The dirty span for which to get natural language spans</param>
        /// <returns>A normalized snapshot span collection containing natural language spans</returns>
        private NormalizedSnapshotSpanCollection GetNaturalLanguageSpansForDirtySpan(SnapshotSpan dirtySpan)
        {
            if(isClosed || dirtySpan.IsEmpty)
                return new NormalizedSnapshotSpanCollection();

            ITextSnapshot snapshot = dirtySpan.Snapshot;

            var spans = new NormalizedSnapshotSpanCollection(
                naturalTextAggregator.GetTags(dirtySpan)
                                      .SelectMany(tag => tag.Span.GetSpans(snapshot))
                                      .Select(s => s.Intersection(dirtySpan))
                                      .Where(s => s.HasValue && !s.Value.IsEmpty)
                                      .Select(s => s.Value));

            // Now, subtract out IUrlTag spans, since we never want to spell check URLs
            var urlSpans = new NormalizedSnapshotSpanCollection(urlAggregator.GetTags(spans).SelectMany(
                tagSpan => tagSpan.Span.GetSpans(snapshot)));

            return NormalizedSnapshotSpanCollection.Difference(spans, urlSpans);
        }

        /// <summary>
        /// Add a dirty span that needs to be checked
        /// </summary>
        /// <param name="span">The span to add</param>
        private void AddDirtySpan(SnapshotSpan span)
        {
            if(!span.IsEmpty)
            {
                dirtySpans.Enqueue(span);
                this.ScheduleUpdate();
            }
        }

        /// <summary>
        /// Add a range of dirty span that needs to be checked
        /// </summary>
        /// <param name="spans">The spans to add</param>
        private void AddDirtySpans(IEnumerable<SnapshotSpan> spans)
        {
            foreach(var span in spans.Where(s => !s.IsEmpty))
                dirtySpans.Enqueue(span);

            this.ScheduleUpdate();
        }

        /// <summary>
        /// Schedule a spell checking update
        /// </summary>
        private void ScheduleUpdate()
        {
            if(!isClosed && !dirtySpans.IsEmpty)
            {
                if(timer == null)
                {
                    timer = new DispatcherTimer(TimeSpan.FromMilliseconds(500), DispatcherPriority.ApplicationIdle,
                        StartUpdateTask, Dispatcher.CurrentDispatcher);
                }

                timer.Stop();
                timer.Start();
            }
        }

        /// <summary>
        /// Start the spell checking task
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void StartUpdateTask(object sender, EventArgs e)
        {
            if(!isClosed && !spellCheckInProgress && this.Dictionary.IsReadyForUse)
            {
                Debug.WriteLine("SpellingTagger: Spell checking text");

                timer.Stop();

                if(!dirtySpans.IsEmpty)
                {
                    // Empty the queue and normalize the dirty spans
                    ITextSnapshot snapshot = buffer.CurrentSnapshot;
                    List<SnapshotSpan> spans = new List<SnapshotSpan>();

                    while(dirtySpans.TryDequeue(out SnapshotSpan s))
                        spans.Add(s.TranslateTo(snapshot, SpanTrackingMode.EdgeInclusive));

                    var normalizedSpans = new NormalizedSnapshotSpanCollection(spans);

                    spellCheckInProgress = true;

                    // Fire and forget
                    System.Threading.Tasks.Task.Run(() => this.CheckSpellings(normalizedSpans)).Forget();
                }
            }
#if DEBUG
            else
                if(!isClosed && !spellCheckInProgress && !this.Dictionary.IsReadyForUse)
                    Debug.WriteLine("SpellingTagger: Dictionaries not ready for use.  Waiting...");
#endif
        }

        /// <summary>
        /// Check for misspellings in the given set of dirty spans
        /// </summary>
        /// <param name="snapshot">The current snapshot of the buffer</param>
        /// <param name="spansToCheck">The enumerable list of spans to check for misspellings</param>
#pragma warning disable VSTHRD100
        private async void CheckSpellings(IEnumerable<SnapshotSpan> spansToCheck)
#pragma warning restore VSTHRD100
        {
            bool isDisabled = DisabledInSession;

            try
            {
                foreach(var dirtySpan in spansToCheck)
                {
                    if(isClosed)
                        return;

                    var snapshot = dirtySpan.Snapshot;
                    var dirty = dirtySpan;

                    // We have to go back to the UI thread to get natural text spans
                    var naturalTextSpans = await System.Threading.Tasks.Task.Run(async () =>
                    {
                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                        // Get the current snapshot and translate the dirty span to it as the text may have
                        // changed.
                        snapshot = buffer.CurrentSnapshot;
                        dirty = dirty.TranslateTo(snapshot, SpanTrackingMode.EdgeInclusive);

                        return GetNaturalLanguageSpansForDirtySpan(dirty);
                    }).ConfigureAwait(true);

                    var currentMisspellings = new List<MisspellingTag>(misspellings);
                    var newMisspellings = new List<MisspellingTag>();

                    int removed = currentMisspellings.RemoveAll(tag => tag.ToTagSpan(snapshot).Span.OverlapsWith(dirty));

                    if(!isDisabled)
                        newMisspellings.AddRange(GetMisspellingsInSpans(naturalTextSpans));

                    // Also remove empties
                    removed += currentMisspellings.RemoveAll(tag => tag.ToTagSpan(snapshot).Span.IsEmpty);

                    // If anything has been updated, we need to send out a change event
                    if(removed != 0 || newMisspellings.Count != 0)
                    {
                        foreach(var g in newMisspellings.Where(
                          w => w.MisspellingType == MisspellingType.MisspelledWord).GroupBy(w => w.Word))
                        {
                            var suggestions = this.Dictionary.SuggestCorrections(g.Key);

                            foreach(var m in g)
                                m.Suggestions = suggestions;
                        }

                        currentMisspellings.AddRange(newMisspellings);

                        // We have to go back to the UI thread to update the misspellings
                        await System.Threading.Tasks.Task.Run(async () =>
                        {
                            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                            if(!isClosed)
                            {
                                misspellings = currentMisspellings;
                                this.TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(dirty));
                            }
                        }).ConfigureAwait(true);
                    }
                }

                if(!isClosed)
                {
                    var snapshot = buffer.CurrentSnapshot;

                    // Clear out any inline ignored word spans that don't exist anymore (i.e. the containing line
                    // was deleted).
                    int removed = inlineIgnoredWords.RemoveAll(s => s.Span.GetSpan(snapshot).IsEmpty);

                    // If any new inline ignored words were seen or removed, rescan the whole file
                    var newInlineIgnored = inlineIgnoredWords.Where(i => i.IsNew);

                    if(removed != 0 || newInlineIgnored.Any())
                    {
                        foreach(var i in newInlineIgnored)
                            i.IsNew = false;

                        foreach(var line in snapshot.Lines.Where(l => !l.Extent.IsEmpty))
                            dirtySpans.Enqueue(line.Extent);

                        // We have to go back to the UI thread to schedule another update
                        await System.Threading.Tasks.Task.Run(async () =>
                        {
                            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                            this.ScheduleUpdate();
                        }).ConfigureAwait(true);
                    }
                }
            }
            catch(Exception ex)
            {
                // Ignore exceptions that occur during spell checking except when debugging
                Debug.WriteLine(ex);
                Debugger.Break();
            }
            finally
            {
                spellCheckInProgress = false;
            }
        }

        /// <summary>
        /// Get misspelled words in the given set of spans
        /// </summary>
        /// <param name="spans">The spans to check</param>
        /// <returns>An enumerable list of misspelling tags</returns>
        private IEnumerable<MisspellingTag> GetMisspellingsInSpans(NormalizedSnapshotSpanCollection spans)
        {
            List<Match> rangeExclusions = new List<Match>();
            SnapshotSpan errorSpan, deleteWordSpan;
            Span lastWord;
            string textToSplit, actualWord, textToCheck;
            int mnemonicPos;

            // **************************************************************************************************
            // NOTE: If anything changes here, update the related solution/project spell checking code in
            // ToolWindows\SolutionProjectSpellCheckControl.xaml.cs\GetMisspellingsInSpans().
            // **************************************************************************************************
            foreach(var span in spans)
            {
                textToSplit = span.GetText();

                rangeExclusions.Clear();

                // Note the location of all XML elements if needed
                if(configuration.IgnoreXmlElementsInText)
                    rangeExclusions.AddRange(WordSplitter.XmlElement.Matches(textToSplit).Cast<Match>());

                // Add exclusions from the configuration if any
                foreach(var exclude in configuration.ExclusionExpressions)
                    try
                    {
                        rangeExclusions.AddRange(exclude.Matches(textToSplit).Cast<Match>());
                    }
                    catch(RegexMatchTimeoutException ex)
                    {
                        // Ignore expression timeouts
                        Debug.WriteLine(ex);
                    }

                // Get any ignored words specified inline within the span
                foreach(Match m in InlineIgnoredWord.reIgnoreSpelling.Matches(textToSplit))
                {
                    string ignored = m.Groups["IgnoredWords"].Value;
                    bool caseSensitive = !String.IsNullOrWhiteSpace(m.Groups["CaseSensitive"].Value);
                    int start = m.Groups["IgnoredWords"].Index;

                    foreach(var ignoreSpan in wordSplitter.GetWordsInText(ignored))
                    {
                        var ss = new SnapshotSpan(span.Snapshot, span.Start + start + ignoreSpan.Start,
                            ignoreSpan.Length);
                        var match = inlineIgnoredWords.FirstOrDefault(i => i.Span.GetSpan(span.Snapshot).OverlapsWith(ss));

                        if(match != null)
                        {
                            // If the span is already there, ignore it
                            if(match.Word == ss.GetText() && match.CaseSensitive == caseSensitive)
                                continue;

                            // If different, replace it
                            inlineIgnoredWords.Remove(match);
                        }

                        var ts = span.Snapshot.CreateTrackingSpan(ss, SpanTrackingMode.EdgeExclusive);

                        inlineIgnoredWords.Add(new InlineIgnoredWord
                        {
                            Word = ignored.Substring(ignoreSpan.Start, ignoreSpan.Length),
                            CaseSensitive = caseSensitive,
                            Span = ts,
                            IsNew = true
                        });
                    }
                }

                lastWord = new Span();

                foreach(var word in wordSplitter.GetWordsInText(textToSplit))
                {
                    if(isClosed)
                        yield break;

                    actualWord = textToSplit.Substring(word.Start, word.Length);

                    if(inlineIgnoredWords.Any(w => w.IsMatch(actualWord)))
                        continue;

                    mnemonicPos = actualWord.IndexOf(wordSplitter.Mnemonic);

                    if(mnemonicPos == -1)
                        textToCheck = actualWord;
                    else
                        textToCheck = actualWord.Substring(0, mnemonicPos) + actualWord.Substring(mnemonicPos + 1);

                    if(unescapeApostrophes && textToCheck.IndexOf("''", StringComparison.Ordinal) != -1)
                        textToCheck = textToCheck.Replace("''", "'");

                    // Spell check the word if it looks like one and is not ignored
                    if(wordSplitter.IsProbablyARealWord(textToCheck) && (rangeExclusions.Count == 0 ||
                      !rangeExclusions.Any(match => word.Start >= match.Index &&
                      word.Start <= match.Index + match.Length - 1)))
                    {
                        errorSpan = new SnapshotSpan(span.Start + word.Start, word.Length);

                        // TODO: Check configuration.ShouldIgnoreWord to ignore if in ignored keywords
                        // If the word is being ignored either in the ignored word lists or in the current
                        // location, skip it.  This goes for doubled words as well.
                        if(this.Dictionary.ShouldIgnoreWord(textToCheck) || wordsIgnoredOnce.Any(
                          w => w.Position == errorSpan.Start.Position && w.Word.Equals(actualWord,
                          StringComparison.OrdinalIgnoreCase)))
                        {
                            lastWord = word;
                            continue;
                        }

                        // Check for a doubled word.  This isn't perfect as it won't detected doubled words
                        // across a line break.
                        if(configuration.DetectDoubledWords && lastWord.Length != 0 &&
                          textToSplit.Substring(lastWord.Start, lastWord.Length).Equals(actualWord,
                          StringComparison.OrdinalIgnoreCase) && String.IsNullOrWhiteSpace(textToSplit.Substring(
                          lastWord.Start + lastWord.Length, word.Start - lastWord.Start - lastWord.Length)))
                        {
                            // Delete the whitespace ahead of it too
                            deleteWordSpan = new SnapshotSpan(span.Start + lastWord.Start + lastWord.Length,
                                word.Length + word.Start - lastWord.Start - lastWord.Length);

                            yield return new MisspellingTag(errorSpan, deleteWordSpan);

                            lastWord = word;
                            continue;
                        }

                        lastWord = word;

                        // Handle code analysis dictionary checks first as they may be not be recognized as
                        // correctly spelled words but have alternate handling.
                        if(configuration.CadOptions.TreatDeprecatedTermsAsMisspelled &&
                          configuration.DeprecatedTerms.TryGetValue(textToCheck, out string preferredTerm))
                        {
                            yield return new MisspellingTag(MisspellingType.DeprecatedTerm, errorSpan,
                                new[] { new SpellingSuggestion(null, preferredTerm) });
                            continue;
                        }

                        if(configuration.CadOptions.TreatCompoundTermsAsMisspelled &&
                          configuration.CompoundTerms.TryGetValue(textToCheck, out preferredTerm))
                        {
                            yield return new MisspellingTag(MisspellingType.CompoundTerm, errorSpan,
                                new[] { new SpellingSuggestion(null, preferredTerm) });
                            continue;
                        }

                        if(configuration.CadOptions.TreatUnrecognizedWordsAsMisspelled &&
                          configuration.UnrecognizedWords.TryGetValue(textToCheck, out IList<string> spellingAlternates))
                        {
                            yield return new MisspellingTag(MisspellingType.UnrecognizedWord, errorSpan,
                                spellingAlternates.Select(a => new SpellingSuggestion(null, a)));
                            continue;
                        }

                        if(!this.Dictionary.IsSpelledCorrectly(textToCheck))
                        {
                            // Sometimes it flags a word as misspelled if it ends with "'s".  Try checking the
                            // word without the "'s".  If ignored or correct without it, don't flag it.  This
                            // appears to be caused by the definitions in the dictionary rather than Hunspell.
                            if(textToCheck.EndsWith("'s", StringComparison.OrdinalIgnoreCase) ||
                              textToCheck.EndsWith("\u2019s", StringComparison.OrdinalIgnoreCase))
                            {
                                string aposEss = textToCheck.Substring(textToCheck.Length - 2);

                                textToCheck = textToCheck.Substring(0, textToCheck.Length - 2);

                                // TODO: Check configuration.ShouldIgnoreWord to ignore if in ignored keywords
                                if(this.Dictionary.ShouldIgnoreWord(textToCheck) ||
                                  this.Dictionary.IsSpelledCorrectly(textToCheck))
                                {
                                    continue;
                                }

                                textToCheck += aposEss;
                            }

                            // Some dictionaries include a trailing period on certain words such as "etc." which
                            // we don't include.  If the word is followed by a period, try it with the period to
                            // see if we get a match.  If so, consider it valid.  Similarly, some languages like
                            // German may include a trailing hyphen on certain words which we split on that makes
                            // them correct if included (Versicherungs-Vertrag).
                            if(word.Start + word.Length < textToSplit.Length)
                            {
                                char trailingChar = textToSplit[word.Start + word.Length];

                                if(trailingChar == '.' || trailingChar == '-')
                                {
                                    string withTrailingChar = textToCheck + trailingChar;

                                    if(this.Dictionary.ShouldIgnoreWord(withTrailingChar) ||
                                      this.Dictionary.IsSpelledCorrectly(withTrailingChar))
                                    {
                                        continue;
                                    }
                                }
                            }

                            yield return new MisspellingTag(errorSpan) { EscapeApostrophes = unescapeApostrophes };
                        }
                    }
                }
            }
        }
        #endregion

        #region Tagging implementation
        //=====================================================================

        /// <inheritdoc />
        public IEnumerable<ITagSpan<MisspellingTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if(isClosed || spans.Count == 0)
                yield break;

            List<MisspellingTag> currentMisspellings = misspellings;

            if(currentMisspellings.Count == 0)
                yield break;

            ITextSnapshot snapshot = spans[0].Snapshot;

            foreach(var misspelling in currentMisspellings)
            {
                var tagSpan = misspelling.ToTagSpan(snapshot);

                if(tagSpan.Span.Length == 0)
                    continue;

                if(spans.IntersectsWith(new NormalizedSnapshotSpanCollection(tagSpan.Span)))
                    yield return tagSpan;
            }
        }

        /// <inheritdoc />
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        #endregion
    }
}
