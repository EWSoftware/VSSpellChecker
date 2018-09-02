//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : SpellingTagger.cs
// Authors : Noah Richards, Roman Golovin, Michael Lehenbauer, Eric Woodruff
// Updated : 09/02/2018
// Note    : Copyright 2010-2018, Microsoft Corporation, All rights reserved
//           Portions Copyright 2013-2018, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Threading;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

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
            /// The span containing the word's location
            /// </summary>
            public ITrackingSpan Span { get; }

            /// <summary>
            /// Get the starting point of the span
            /// </summary>
            public SnapshotPoint StartPoint => this.Span.GetStartPoint(this.Span.TextBuffer.CurrentSnapshot);

            /// <summary>
            /// The word at the span location when it was ignored
            /// </summary>
            public string Word { get; }

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="span">The span containing the word's location</param>
            public IgnoredOnceWord(ITrackingSpan span)
            {
                this.Span = span;
                this.Word = span.GetText(span.TextBuffer.CurrentSnapshot);
            }
        }
        #endregion

        #region Private data members
        //=====================================================================

        private ITextBuffer buffer;
        private ITagAggregator<INaturalTextTag> naturalTextAggregator;
        private ITagAggregator<IUrlTag> urlAggregator;
        private readonly Dispatcher dispatcher;

        private SpellCheckerConfiguration configuration;
        private WordSplitter wordSplitter;

        private List<SnapshotSpan> dirtySpans;

        private readonly object dirtySpanLock = new object();

        // TODO: Would concurrent collections be more appropriate than using volatile on these?
        private volatile List<MisspellingTag> misspellings;
        private volatile List<IgnoredOnceWord> wordsIgnoredOnce;

        private readonly List<InlineIgnoredWord> inlineIgnoredWords;

        private Thread updateThread;
        private DispatcherTimer timer;

        private bool isClosed, unescapeApostrophes;

        #endregion

        #region Properties
        //=====================================================================

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
        public IEnumerable<Span> IgnoredOnceSpans
        {
            get
            {
                List<Span> spans = new List<Span>();

                foreach(var w in wordsIgnoredOnce)
                    spans.Add(w.Span.GetSpan(w.Span.TextBuffer.CurrentSnapshot));

                return spans;
            }
        }
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
            this.dispatcher = Dispatcher.CurrentDispatcher;
            this.configuration = configuration;
            this.Dictionary = dictionary;

            dirtySpans = new List<SnapshotSpan>();
            misspellings = new List<MisspellingTag>();
            wordsIgnoredOnce = new List<IgnoredOnceWord>();
            inlineIgnoredWords = new List<InlineIgnoredWord>();

            string filename = buffer.GetFilename();

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

            foreach(var line in snapshot.Lines)
                AddDirtySpan(line.Extent);
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

            if(timer != null)
                timer.Stop();

            if(buffer != null)
                buffer.Changed -= BufferChanged;

            if(naturalTextAggregator != null)
                naturalTextAggregator.Dispose();

            if(urlAggregator != null)
                urlAggregator.Dispose();

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
            if(isClosed)
                return;

            NormalizedSnapshotSpanCollection dirty = e.Span.GetSpans(buffer.CurrentSnapshot);

            if(dirty.Count != 0)
                foreach(var span in dirty)
                    this.AddDirtySpan(span);
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
                foreach(var line in snapshot.Lines)
                    AddDirtySpan(line.Extent);

                return;
            }

            List<MisspellingTag> currentMisspellings = misspellings;

            foreach(var misspelling in currentMisspellings)
            {
                SnapshotSpan span = misspelling.Span.GetSpan(snapshot);

                if(span.GetText().Equals(e.Word, StringComparison.OrdinalIgnoreCase))
                    AddDirtySpan(span);
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
                var newIgnoredWords = new List<IgnoredOnceWord>(wordsIgnoredOnce) { new IgnoredOnceWord(e.Span) };
                var currentMisspellings = misspellings;

                // Raise the TagsChanged event to get rid of the tags on the ignored word
                foreach(var misspelling in currentMisspellings)
                    if(misspelling.Span.GetStartPoint(misspelling.Span.TextBuffer.CurrentSnapshot) ==
                      e.Span.GetStartPoint(e.Span.TextBuffer.CurrentSnapshot))
                    {
                        this.AddDirtySpan(misspelling.Span.GetSpan(misspelling.Span.TextBuffer.CurrentSnapshot));

                        this.TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(misspelling.Span.GetSpan(
                                misspelling.Span.TextBuffer.CurrentSnapshot)));

                        break;
                    }

                wordsIgnoredOnce = newIgnoredWords;
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

                AddDirtySpan(new SnapshotSpan(startLine.Start, endLine.End));
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
            if(span.IsEmpty)
                return;

            lock(dirtySpanLock)
            {
                dirtySpans.Add(span);
                ScheduleUpdate();
            }
        }

        /// <summary>
        /// Schedule a spell checking update
        /// </summary>
        private void ScheduleUpdate()
        {
            if(isClosed)
                return;

            if(timer == null)
            {
                timer = new DispatcherTimer(DispatcherPriority.ApplicationIdle, dispatcher)
                {
                    Interval = TimeSpan.FromMilliseconds(500)
                };

                timer.Tick += GuardedStartUpdateThread;
            }

            timer.Stop();
            timer.Start();
        }

        /// <summary>
        /// Start the update thread with exception checking
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void GuardedStartUpdateThread(object sender, EventArgs e)
        {
            try
            {
                StartUpdateThread(sender, e);
            }
            catch(Exception ex)
            {
                // If anything fails during the handling of a dispatcher tick, just ignore it.  If we don't guard
                // against those exceptions, the user will see a crash.
                Debug.WriteLine(ex);

                Debug.Fail("Exception!" + ex.Message);
            }
        }

        // TODO: This should probably be rewritten as an sync task so that we can use async throughout and get
        // rid of the calls to ThreadHelper.JoinableTaskFactory.Run.
        // TODO: Would a concurrent bag be better for _dirtySpans  than List<T>?  It would probably simplify usage
        // and would not require the dirtySpanLock anymore.
        /// <summary>
        /// Start the update thread
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void StartUpdateThread(object sender, EventArgs e)
        {
            // If an update is currently running, wait until the next timer tick
            if(isClosed || updateThread != null && updateThread.IsAlive)
                return;

            timer.Stop();

            List<SnapshotSpan> dirty;
            lock(dirtySpanLock)
            {
                dirty = new List<SnapshotSpan>(dirtySpans);
                dirtySpans = new List<SnapshotSpan>();

                if(dirty.Count == 0)
                    return;
            }

            // Normalize the dirty spans
            ITextSnapshot snapshot = buffer.CurrentSnapshot;

            var normalizedSpans = new NormalizedSnapshotSpanCollection(dirty.Select(
                s => s.TranslateTo(snapshot, SpanTrackingMode.EdgeInclusive)));

            updateThread = new Thread(GuardedCheckSpellings)
            {
                Name = "Spell Check",
                Priority = ThreadPriority.BelowNormal
            };

            // TODO: I don't think this is true anymore.  I'm not using the WPF text box to spell check and it
            // switches to the main UI thread when necessary.
            if(!updateThread.TrySetApartmentState(ApartmentState.STA))
                Debug.Fail("Unable to set thread apartment state to STA, things *will* break.");

            updateThread.Start(normalizedSpans);
        }

        /// <summary>
        /// Check for spelling mistakes with exception checking
        /// </summary>
        /// <param name="dirtySpansObject"></param>
        private void GuardedCheckSpellings(object dirtySpansObject)
        {
            if(isClosed)
                return;

            try
            {
                if(!(dirtySpansObject is IEnumerable<SnapshotSpan> dirty))
                {
                    Debug.Fail("Being asked to check a null list of dirty spans.  What gives?");
                    return;
                }

                CheckSpellings(dirty);
            }
            catch(Exception ex)
            {
                // If anything fails in the background thread, just ignore it.  It's possible that the background
                // thread will run on VS shutdown, at which point calls into WPF throw exceptions.  If we don't
                // guard against those exceptions, the user will see a crash on exit.
                Debug.WriteLine(ex);

                Debug.Fail("Exception!" + ex.Message);
            }
        }

        /// <summary>
        /// Check for misspellings in the given set of dirty spans
        /// </summary>
        /// <param name="dirtySpansToCheck">The enumerable list of dirty spans to check for misspellings</param>
        private void CheckSpellings(IEnumerable<SnapshotSpan> dirtySpansToCheck)
        {
            ITextSnapshot snapshot = buffer.CurrentSnapshot;

            foreach(var dirtySpan in dirtySpansToCheck)
            {
                if(isClosed)
                    return;

                var dirty = dirtySpan.TranslateTo(snapshot, SpanTrackingMode.EdgeInclusive);

                // We have to go back to the UI thread to get natural text spans
                var naturalTextSpans = ThreadHelper.JoinableTaskFactory.Run<IEnumerable<SnapshotSpan>> (async delegate
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                    return GetNaturalLanguageSpansForDirtySpan(dirty);
                });

                var naturalText = new NormalizedSnapshotSpanCollection(
                    naturalTextSpans.Select(span => span.TranslateTo(snapshot, SpanTrackingMode.EdgeInclusive)));

                List<MisspellingTag> currentMisspellings = new List<MisspellingTag>(misspellings);
                List<MisspellingTag> newMisspellings = new List<MisspellingTag>();

                int removed = currentMisspellings.RemoveAll(tag => tag.ToTagSpan(snapshot).Span.OverlapsWith(dirty));

                newMisspellings.AddRange(GetMisspellingsInSpans(naturalText));

                // Also remove empties
                removed += currentMisspellings.RemoveAll(tag => tag.ToTagSpan(snapshot).Span.IsEmpty);

                // If anything has been updated, we need to send out a change event
                if(newMisspellings.Count != 0 || removed != 0)
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
                    ThreadHelper.JoinableTaskFactory.Run(async delegate
                    {
                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                        if(!isClosed)
                        {
                            misspellings = currentMisspellings;
                            this.TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(dirty));
                        }
                    });
                }
            }

            lock(dirtySpanLock)
            {
                if(!isClosed)
                {
                    // Clear out any inline ignored word spans that don't exist anymore (i.e. the containing line
                    // was deleted).
                    int removed = inlineIgnoredWords.RemoveAll(s => s.Span.GetSpan(snapshot).IsEmpty);

                    // If any new inline ignored words were seen or removed, rescan the whole file
                    var newInlineIgnored = inlineIgnoredWords.Where(i => i.IsNew);

                    if(newInlineIgnored.Any() || removed != 0)
                    {
                        foreach(var i in newInlineIgnored)
                            i.IsNew = false;

                        dirtySpans.Clear();

                        foreach(var line in snapshot.Lines)
                            if(!line.Extent.IsEmpty)
                                dirtySpans.Add(line.Extent);
                    }

                    if(dirtySpans.Count != 0)
                    {
                        // We have to go back to the UI thread to schedule another update
                        ThreadHelper.JoinableTaskFactory.Run(async delegate
                        {
                            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                            if(!isClosed)
                                this.ScheduleUpdate();
                        });
                    }
                }
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
            Microsoft.VisualStudio.Text.Span lastWord;
            string textToSplit, actualWord, textToCheck;
            int mnemonicPos;
            var ignoredWords = wordsIgnoredOnce;

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

                lastWord = new Microsoft.VisualStudio.Text.Span();

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

                        // Check for a doubled word.  This isn't perfect as it won't detected doubled words
                        // across a line break.
                        if(configuration.DetectDoubledWords && lastWord.Length != 0 &&
                          textToSplit.Substring(lastWord.Start, lastWord.Length).Equals(actualWord,
                          StringComparison.OrdinalIgnoreCase) && String.IsNullOrWhiteSpace(textToSplit.Substring(
                          lastWord.Start + lastWord.Length, word.Start - lastWord.Start - lastWord.Length)))
                        {
                            // If the doubled word is not being ignored at the current location, return it
                            if(!ignoredWords.Any(w => w.StartPoint == errorSpan.Start && w.Word.Equals(actualWord,
                              StringComparison.OrdinalIgnoreCase)))
                            {
                                // Delete the whitespace ahead of it too
                                deleteWordSpan = new SnapshotSpan(span.Start + lastWord.Start + lastWord.Length,
                                    word.Length + word.Start - lastWord.Start - lastWord.Length);

                                yield return new MisspellingTag(errorSpan, deleteWordSpan);

                                lastWord = word;
                                continue;
                            }
                        }

                        lastWord = word;

                        // If the word is not being ignored, perform the other checks
                        if(!this.Dictionary.ShouldIgnoreWord(textToCheck) && !ignoredWords.Any(
                          w => w.StartPoint == errorSpan.Start && w.Word.Equals(actualWord,
                          StringComparison.OrdinalIgnoreCase)))
                        {
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

                                    if(this.Dictionary.ShouldIgnoreWord(textToCheck) ||
                                      this.Dictionary.IsSpelledCorrectly(textToCheck))
                                        continue;

                                    textToCheck += aposEss;
                                }

                                // Some dictionaries include a trailing period on certain words such as "etc." which
                                // we don't include.  If the word is followed by a period, try it with the period to
                                // see if we get a match.  If so, consider it valid.
                                if(word.Start + word.Length < textToSplit.Length && textToSplit[word.Start + word.Length] == '.')
                                {
                                    if(this.Dictionary.ShouldIgnoreWord(textToCheck + ".") ||
                                      this.Dictionary.IsSpelledCorrectly(textToCheck + "."))
                                        continue;
                                }

                                yield return new MisspellingTag(errorSpan) { EscapeApostrophes = unescapeApostrophes };
                            }
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
