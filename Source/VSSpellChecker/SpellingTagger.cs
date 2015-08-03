//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : SpellingTagger.cs
// Authors : Noah Richards, Roman Golovin, Michael Lehenbauer, Eric Woodruff
// Updated : 08/02/2015
// Note    : Copyright 2010-2015, Microsoft Corporation, All rights reserved
//           Portions Copyright 2013-2015, Eric Woodruff, All rights reserved
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
//===============================================================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Documents;
using System.Windows.Threading;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

using VisualStudio.SpellChecker.Configuration;
using VisualStudio.SpellChecker.Definitions;
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
        private class IgnoredWord
        {
            /// <summary>
            /// The span containing the word's location
            /// </summary>
            public ITrackingSpan Span { get; private set; }

            /// <summary>
            /// Get the starting point of the span
            /// </summary>
            public SnapshotPoint StartPoint
            {
                get { return this.Span.GetStartPoint(this.Span.TextBuffer.CurrentSnapshot); }
            }

            /// <summary>
            /// The word at the span location when it was ignored
            /// </summary>
            public string Word { get; private set; }

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="span">The span containing the word's location</param>
            public IgnoredWord(ITrackingSpan span)
            {
                this.Span = span;
                this.Word = span.GetText(span.TextBuffer.CurrentSnapshot);
            }
        }
        #endregion

        #region Private data members
        //=====================================================================

        // Word break characters (\u201C/\u201D = Unicode quotes, \u2026 = Ellipsis character).
        // Specifically excludes: _ . ' @
        private const string wordBreakChars = " \t!\"#$%&()*+,-/:;<=>?[\\]^`{|}~\u201C\u201D\u2026";

        private ITextBuffer _buffer;
        private ITagAggregator<INaturalTextTag> _naturalTextAggregator;
        private ITagAggregator<IUrlTag> _urlAggregator;
        private Dispatcher _dispatcher;

        private SpellCheckerConfiguration configuration;
        private SpellingDictionary _dictionary;

        private List<SnapshotSpan> _dirtySpans;

        private object _dirtySpanLock = new object();
        private volatile List<MisspellingTag> _misspellings;
        private volatile List<IgnoredWord> wordsIgnoredOnce;

        private Thread _updateThread;
        private DispatcherTimer _timer;

        private bool _isClosed;

        // Regular expressions used to find things that look like XML elements
        private static Regex reXml = new Regex(@"<[A-Za-z/]+?.*?>");
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
                var currentMisspellings = _misspellings;

                return currentMisspellings;
            }
        }

        /// <summary>
        /// This read-only property returns the spelling dictionary instance
        /// </summary>
        public SpellingDictionary Dictionary
        {
            get { return _dictionary; }
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
            _isClosed = false;
            _buffer = buffer;
            _naturalTextAggregator = naturalTextAggregator;
            _urlAggregator = urlAggregator;
            _dispatcher = Dispatcher.CurrentDispatcher;
            this.configuration = configuration;
            _dictionary = dictionary;

            _dirtySpans = new List<SnapshotSpan>();
            _misspellings = new List<MisspellingTag>();
            wordsIgnoredOnce = new List<IgnoredWord>();

            _buffer.Changed += BufferChanged;
            _naturalTextAggregator.TagsChanged += AggregatorTagsChanged;
            _urlAggregator.TagsChanged += AggregatorTagsChanged;
            _dictionary.DictionaryUpdated += DictionaryUpdated;
            _dictionary.ReplaceAll += ReplaceAll;
            _dictionary.IgnoreOnce += IgnoreOnce;

            view.Closed += ViewClosed;

            // To start with, the entire buffer is dirty.  Split this into chunks so we update pieces at a time.
            ITextSnapshot snapshot = _buffer.CurrentSnapshot;

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
            _isClosed = true;

            if(_timer != null)
                _timer.Stop();

            if(_buffer != null)
                _buffer.Changed -= BufferChanged;

            if(_naturalTextAggregator != null)
                _naturalTextAggregator.Dispose();

            if(_urlAggregator != null)
                _urlAggregator.Dispose();

            if(_dictionary != null)
            {
                _dictionary.DictionaryUpdated -= DictionaryUpdated;
                _dictionary.ReplaceAll -= ReplaceAll;
                _dictionary.IgnoreOnce -= IgnoreOnce;
            }
        }

        /// <summary>
        /// Recheck the changed spans when tags change
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void AggregatorTagsChanged(object sender, TagsChangedEventArgs e)
        {
            if(_isClosed)
                return;

            NormalizedSnapshotSpanCollection dirtySpans = e.Span.GetSpans(_buffer.CurrentSnapshot);

            if(dirtySpans.Count == 0)
                return;

            foreach(var span in dirtySpans)
                AddDirtySpan(span);
        }

        /// <summary>
        /// Recheck the affected spans when the dictionary is updated
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void DictionaryUpdated(object sender, SpellingEventArgs e)
        {
            if(_isClosed)
                return;

            ITextSnapshot snapshot = _buffer.CurrentSnapshot;

            // If the word is null, it means the entire dictionary was updated and we need to reparse the entire
            // file.
            if(e.Word == null)
            {
                foreach(var line in snapshot.Lines)
                    AddDirtySpan(line.Extent);

                return;
            }

            List<MisspellingTag> currentMisspellings = _misspellings;

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
            if(_isClosed)
                return;

            var snapshot = _buffer.CurrentSnapshot;
            var replacedWords = new List<MisspellingTag>();

            // Do all replacements in one edit
            using(var edit = snapshot.TextBuffer.CreateEdit())
            {
                var currentMisspellings = _misspellings;

                foreach(var misspelling in currentMisspellings)
                {
                    var span = misspelling.Span.GetSpan(snapshot);

                    if(span.GetText().Equals(e.Word, StringComparison.OrdinalIgnoreCase))
                    {
                        replacedWords.Add(misspelling);

                        string currentWord = misspelling.Span.GetText(snapshot);
                        string replacementWord = e.ReplacementWord;

                        var language = e.Culture ?? CultureInfo.CurrentUICulture;

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
                snapshot = _buffer.CurrentSnapshot;

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
            if(!_isClosed)
            {
                var newIgnoredWords = new List<IgnoredWord>(wordsIgnoredOnce);

                newIgnoredWords.Add(new IgnoredWord(e.Span));

                var currentMisspellings = _misspellings;

                // Raise the TagsChanged event to get rid of the tags on the ignored word
                foreach(var misspelling in currentMisspellings)
                    if(misspelling.Span.GetStartPoint(misspelling.Span.TextBuffer.CurrentSnapshot) ==
                      e.Span.GetStartPoint(e.Span.TextBuffer.CurrentSnapshot))
                    {
                        this.AddDirtySpan(misspelling.Span.GetSpan(misspelling.Span.TextBuffer.CurrentSnapshot));

                        var tagsChanged = TagsChanged;

                        if(tagsChanged != null)
                            tagsChanged(this, new SnapshotSpanEventArgs(misspelling.Span.GetSpan(
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
            if(_isClosed)
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
            if(_isClosed || dirtySpan.IsEmpty)
                return new NormalizedSnapshotSpanCollection();

            ITextSnapshot snapshot = dirtySpan.Snapshot;

            var spans = new NormalizedSnapshotSpanCollection(
                _naturalTextAggregator.GetTags(dirtySpan)
                                      .SelectMany(tag => tag.Span.GetSpans(snapshot))
                                      .Select(s => s.Intersection(dirtySpan))
                                      .Where(s => s.HasValue && !s.Value.IsEmpty)
                                      .Select(s => s.Value));

            // Now, subtract out IUrlTag spans, since we never want to spell check URLs
            var urlSpans = new NormalizedSnapshotSpanCollection(_urlAggregator.GetTags(spans).SelectMany(
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

            lock(_dirtySpanLock)
            {
                _dirtySpans.Add(span);
                ScheduleUpdate();
            }
        }

        /// <summary>
        /// Schedule a spell checking update
        /// </summary>
        private void ScheduleUpdate()
        {
            if(_isClosed)
                return;

            if(_timer == null)
            {
                _timer = new DispatcherTimer(DispatcherPriority.ApplicationIdle, _dispatcher)
                {
                    Interval = TimeSpan.FromMilliseconds(500)
                };

                _timer.Tick += GuardedStartUpdateThread;
            }

            _timer.Stop();
            _timer.Start();
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
                System.Diagnostics.Debug.WriteLine(ex);

                Debug.Fail("Exception!" + ex.Message);
            }
        }

        /// <summary>
        /// Start the update thread
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void StartUpdateThread(object sender, EventArgs e)
        {
            // If an update is currently running, wait until the next timer tick
            if(_isClosed || _updateThread != null && _updateThread.IsAlive)
                return;

            _timer.Stop();

            List<SnapshotSpan> dirtySpans;
            lock(_dirtySpanLock)
            {
                dirtySpans = new List<SnapshotSpan>(_dirtySpans);
                _dirtySpans = new List<SnapshotSpan>();

                if(dirtySpans.Count == 0)
                    return;
            }

            // Normalize the dirty spans
            ITextSnapshot snapshot = _buffer.CurrentSnapshot;

            var normalizedSpans = new NormalizedSnapshotSpanCollection(dirtySpans.Select(
                s => s.TranslateTo(snapshot, SpanTrackingMode.EdgeInclusive)));

            _updateThread = new Thread(GuardedCheckSpellings)
            {
                Name = "Spell Check",
                Priority = ThreadPriority.BelowNormal
            };

            if(!_updateThread.TrySetApartmentState(ApartmentState.STA))
                Debug.Fail("Unable to set thread apartment state to STA, things *will* break.");

            _updateThread.Start(normalizedSpans);
        }

        /// <summary>
        /// Check for spelling mistakes with exception checking
        /// </summary>
        /// <param name="dirtySpansObject"></param>
        private void GuardedCheckSpellings(object dirtySpansObject)
        {
            if(_isClosed)
                return;

            try
            {
                IEnumerable<SnapshotSpan> dirtySpans = dirtySpansObject as IEnumerable<SnapshotSpan>;

                if(dirtySpans == null)
                {
                    Debug.Fail("Being asked to check a null list of dirty spans.  What gives?");
                    return;
                }

                CheckSpellings(dirtySpans);
            }
            catch(Exception ex)
            {
                // If anything fails in the background thread, just ignore it.  It's possible that the background
                // thread will run on VS shutdown, at which point calls into WPF throw exceptions.  If we don't
                // guard against those exceptions, the user will see a crash on exit.
                System.Diagnostics.Debug.WriteLine(ex);

                Debug.Fail("Exception!" + ex.Message);
            }
        }

        /// <summary>
        /// Check for misspellings in the given set of dirty spans
        /// </summary>
        /// <param name="dirtySpans">The enumerable list of dirty spans to check for misspellings</param>
        private void CheckSpellings(IEnumerable<SnapshotSpan> dirtySpans)
        {
            ITextSnapshot snapshot = _buffer.CurrentSnapshot;

            foreach(var dirtySpan in dirtySpans)
            {
                if(_isClosed)
                    return;

                var dirty = dirtySpan.TranslateTo(snapshot, SpanTrackingMode.EdgeInclusive);

                // We have to go back to the UI thread to get natural text spans
                List<SnapshotSpan> naturalTextSpans = new List<SnapshotSpan>();
                OnForegroundThread(() => naturalTextSpans = GetNaturalLanguageSpansForDirtySpan(dirty).ToList());

                var naturalText = new NormalizedSnapshotSpanCollection(
                    naturalTextSpans.Select(span => span.TranslateTo(snapshot, SpanTrackingMode.EdgeInclusive)));

                List<MisspellingTag> currentMisspellings = new List<MisspellingTag>(_misspellings);
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
                        var suggestions = _dictionary.SuggestCorrections(g.Key);

                        foreach(var m in g)
                            m.Suggestions = suggestions;
                    }

                    currentMisspellings.AddRange(newMisspellings);

                    _dispatcher.Invoke(new Action(() =>
                    {
                        if(_isClosed)
                            return;

                        _misspellings = currentMisspellings;

                        var temp = TagsChanged;

                        if(temp != null)
                            temp(this, new SnapshotSpanEventArgs(dirty));
                    }));
                }
            }

            lock(_dirtySpanLock)
            {
                if(!_isClosed && _dirtySpans.Count != 0)
                    _dispatcher.BeginInvoke(new Action(() => ScheduleUpdate()));
            }
        }

        /// <summary>
        /// Get misspelled words in the given set of spans
        /// </summary>
        /// <param name="spans">The spans to check</param>
        /// <returns>An enumerable list of misspelling tags</returns>
        private IEnumerable<MisspellingTag> GetMisspellingsInSpans(NormalizedSnapshotSpanCollection spans)
        {
            List<Match> xmlTags = null;
            IList<string> spellingAlternates;
            SnapshotSpan errorSpan, deleteWordSpan;
            Microsoft.VisualStudio.Text.Span lastWord;
            string text, textToParse, preferredTerm;
            var ignoredWords = wordsIgnoredOnce;

            foreach(var span in spans)
            {
                text = span.GetText();

                // Note the location of all XML elements if needed
                if(configuration.IgnoreXmlElementsInText)
                    xmlTags = reXml.Matches(text).OfType<Match>().ToList();

                lastWord = new Microsoft.VisualStudio.Text.Span();

                foreach(var word in GetWordsInText(text))
                {
                    if(_isClosed)
                        yield break;

                    textToParse = text.Substring(word.Start, word.Length);

                    // Spell check the word if it looks like one and is not ignored
                    if(IsProbablyARealWord(textToParse) && (xmlTags == null || xmlTags.Count == 0 ||
                      !xmlTags.Any(match => word.Start >= match.Index &&
                      word.Start <= match.Index + match.Length - 1)))
                    {
                        errorSpan = new SnapshotSpan(span.Start + word.Start, word.Length);

                        // Check for a doubled word.  This isn't perfect as it won't detected doubled words
                        // across a line break.
                        if(lastWord.Length != 0 && text.Substring(lastWord.Start, lastWord.Length).Equals(
                          textToParse, StringComparison.OrdinalIgnoreCase) && String.IsNullOrWhiteSpace(
                          text.Substring(lastWord.Start + lastWord.Length, word.Start - lastWord.Start - lastWord.Length)))
                        {
                            // If the doubled word is not being ignored at the current location, return it
                            if(!ignoredWords.Any(w => w.StartPoint == errorSpan.Start && w.Word.Equals(textToParse,
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
                        if(!_dictionary.ShouldIgnoreWord(textToParse) && !ignoredWords.Any(
                          w => w.StartPoint == errorSpan.Start && w.Word.Equals(textToParse,
                          StringComparison.OrdinalIgnoreCase)))
                        {
                            // Handle code analysis dictionary checks first as they may be not be recognized as
                            // correctly spelled words but have alternate handling.
                            if(configuration.CadOptions.TreatDeprecatedTermsAsMisspelled &&
                              configuration.DeprecatedTerms.TryGetValue(textToParse, out preferredTerm))
                            {
                                yield return new MisspellingTag(MisspellingType.DeprecatedTerm, errorSpan,
                                    new[] { new SpellingSuggestion(null, preferredTerm) });
                                continue;
                            }

                            if(configuration.CadOptions.TreatCompoundTermsAsMisspelled &&
                              configuration.CompoundTerms.TryGetValue(textToParse, out preferredTerm))
                            {
                                yield return new MisspellingTag(MisspellingType.CompoundTerm, errorSpan,
                                    new[] { new SpellingSuggestion(null, preferredTerm) });
                                continue;
                            }

                            if(configuration.CadOptions.TreatUnrecognizedWordsAsMisspelled &&
                              configuration.UnrecognizedWords.TryGetValue(textToParse, out spellingAlternates))
                            {
                                yield return new MisspellingTag(MisspellingType.UnrecognizedWord, errorSpan,
                                    spellingAlternates.Select(a => new SpellingSuggestion(null, a)));
                                continue;
                            }

                            if(!_dictionary.IsSpelledCorrectly(textToParse))
                            {
                                // Sometimes it flags a word as misspelled if it ends with "'s".  Try checking the
                                // word without the "'s".  If ignored or correct without it, don't flag it.  This
                                // appears to be caused by the definitions in the dictionary rather than Hunspell.
                                if(textToParse.EndsWith("'s", StringComparison.OrdinalIgnoreCase))
                                {
                                    textToParse = textToParse.Substring(0, textToParse.Length - 2);

                                    if(_dictionary.ShouldIgnoreWord(textToParse) ||
                                      _dictionary.IsSpelledCorrectly(textToParse))
                                        continue;

                                    textToParse += "'s";
                                }

                                // Some dictionaries include a trailing period on certain words such as "etc." which
                                // we don't include.  If the word is followed by a period, try it with the period to
                                // see if we get a match.  If so, consider it valid.
                                if(word.Start + word.Length < text.Length && text[word.Start + word.Length] == '.')
                                {
                                    if(_dictionary.ShouldIgnoreWord(textToParse + ".") ||
                                      _dictionary.IsSpelledCorrectly(textToParse + "."))
                                        continue;
                                }

                                yield return new MisspellingTag(errorSpan);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Determine if a word is probably a real word
        /// </summary>
        /// <param name="word">The word to check</param>
        /// <returns>True if it appears to be a real word or false if any of the following conditions are met:
        /// 
        /// <list type="bullet">
        ///     <description>The word contains a period or an at-sign (it looks like a filename or an e-mail
        /// address) and those words are being ignored.  We may miss a few real misspellings in this case due
        /// to a missed space after a period, but that's acceptable.</description>
        ///     <description>The word contains an underscore and underscores are not being treated as
        /// separators.</description>
        ///     <description>The word contains a digit and words with digits are being ignored.</description>
        ///     <description>The word is composed entirely of digits when words with digits are not being
        /// ignored.</description>
        ///     <description>The word is in all uppercase and words in all uppercase are being ignored.</description>
        ///     <description>The word is camel cased.</description>
        /// </list>
        /// </returns>
        internal bool IsProbablyARealWord(string word)
        {
            if(String.IsNullOrWhiteSpace(word))
                return false;

            word = word.Trim();

            // Check for a period or an at-sign in the word (things that look like filenames and e-mail addresses)
            if(word.IndexOfAny(new[] { '.', '@' }) >= 0)
                return false;

            // Check for underscores and digits
            if(word.Any(c => c == '_' || (Char.IsDigit(c) && configuration.IgnoreWordsWithDigits)))
                return false;

            // Ignore if all digits (this only happens if the Ignore Words With Digits option is false)
            if(!word.Any(c => Char.IsLetter(c)))
                return false;

            // Ignore if all uppercase, accounting for apostrophes and digits
            if(word.All(c => Char.IsUpper(c) || !Char.IsLetter(c)))
                return !configuration.IgnoreWordsInAllUppercase;

            // Ignore if camel cased
            if(Char.IsLetter(word[0]) && word.Skip(1).Any(c => Char.IsUpper(c)))
            {
                // An exception is if it appears in the code analysis dictionary options.  These may be camel
                // cased but the user wants them replaced with something else.
                if((configuration.CadOptions.TreatDeprecatedTermsAsMisspelled &&
                  configuration.DeprecatedTerms.ContainsKey(word)) ||
                  (configuration.CadOptions.TreatCompoundTermsAsMisspelled &&
                  configuration.CompoundTerms.ContainsKey(word)))
                    return true;

                return false;
            }

            // Ignore by character class.  A rather simplistic way to ignore some foreign language words in files
            // with mixed English/non-English text.
            if(configuration.IgnoreCharacterClass != IgnoredCharacterClass.None)
            {
                if(configuration.IgnoreCharacterClass == IgnoredCharacterClass.NonAscii && word.Any(c => c > '\x07F'))
                    return false;

                if(configuration.IgnoreCharacterClass == IgnoredCharacterClass.NonLatin && word.Any(c => c > '\x0FF'))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Get all words in the specified text string
        /// </summary>
        /// <param name="text">The text to break into words</param>
        /// <returns>An enumerable list of word spans</returns>
        internal IEnumerable<Microsoft.VisualStudio.Text.Span> GetWordsInText(string text)
        {
            if(String.IsNullOrWhiteSpace(text))
                yield break;

            for(int i = 0, end = 0; i < text.Length; i++)
            {
                // Skip escape sequences.  If not, they can end up as part of the word or cause words to be
                // missed.  For example, "This\r\nis\ta\ttest \x22missing\x22" would incorrectly yield "nis",
                // "ta", and "ttest" and incorrectly exclude "missing".  This can cause the occasional false
                // positive in file paths (i.e. \Folder\transform\File.txt flags "ransform" as a misspelled word
                // because of the lowercase "t" following the backslash) but I can live with that.  If they are
                // common enough, they can be added to the configuration's ignored word list as an escaped word.
                if(text[i] == '\\')
                {
                    end = i + 1;

                    if(end < text.Length)
                    {
                        // Skip escaped words.  Only need to check the escape sequence letters.
                        switch(text[end])
                        {
                            case 'a':   // BEL
                            case 'b':   // BS
                            case 'f':   // FF
                            case 'n':   // LF
                            case 'r':   // CR
                            case 't':   // TAB
                            case 'v':   // VT
                            case 'x':   // Hex value
                            case 'u':   // Unicode value
                            case 'U':
                            {
                                // Find the end of the word
                                int wordEnd = end;

                                while(++wordEnd < text.Length && !IsWordBreakCharacter(text[wordEnd]))
                                    ;

                                if(configuration.ShouldIgnoreWord(text.Substring(end - 1, --wordEnd - i + 1)))
                                {
                                    i = wordEnd;
                                    continue;
                                }

                                break;
                            }
                        }

                        // Escape sequences
                        switch(text[end])
                        {
                            case '\'':
                            case '\"':
                            case '\\':
                            case '?':   // Anti-Trigraph
                            case '0':   // NUL or Octal
                            case 'a':   // BEL
                            case 'b':   // BS
                            case 'f':   // FF
                            case 'n':   // LF
                            case 'r':   // CR
                            case 't':   // TAB
                            case 'v':   // VT
                                i++;
                                break;

                            case 'x':   // xh[h[h[h]]] or xhh[hh]
                                while(++end < text.Length && (end - i) < 6 && (Char.IsDigit(text[end]) ||
                                  (Char.ToLower(text[end]) >= 'a' && Char.ToLower(text[end]) <= 'f')))
                                    ;

                                i = --end;
                                break;

                            case 'u':   // uhhhh
                                while(++end < text.Length && (end - i) < 6 && (Char.IsDigit(text[end]) ||
                                  (Char.ToLower(text[end]) >= 'a' && Char.ToLower(text[end]) <= 'f')))
                                    ;

                                if((--end - i) == 5)
                                    i = end;
                                break;

                            case 'U':   // Uhhhhhhhh
                                while(++end < text.Length && (end - i) < 10 && (Char.IsDigit(text[end]) ||
                                  (Char.ToLower(text[end]) >= 'a' && Char.ToLower(text[end]) <= 'f')))
                                    ;

                                if((--end - i) == 9)
                                    i = end;
                                break;

                            default:
                                break;
                        }
                    }

                    continue;
                }

                // Skip XML entities
                if(text[i] == '&')
                {
                    end = i + 1;

                    if(end < text.Length && text[end] == '#')
                    {
                        // Numeric Reference &#n[n][n][n];
                        while(++end < text.Length && (end - i) < 7 && Char.IsDigit(text[end]))
                            ;

                        // Hexadecimal Reference &#xh[h][h][h];
                        if(end < text.Length && text[end] == 'x')
                        {
                            while(++end < text.Length && (end - i) < 8 && (Char.IsDigit(text[end]) ||
                              (Char.ToLower(text[end]) >= 'a' && Char.ToLower(text[end]) <= 'f')))
                                ;
                        }

                        // Check for entity closer
                        if(end < text.Length && text[end] == ';')
                            i = end;
                    }

                    continue;
                }

                // Skip .NET format string specifiers if so indicated.  This ignores stuff like date formats
                // such as "{0:MM/dd/yyyy hh:nn tt}".
                if(text[i] == '{' && configuration.IgnoreFormatSpecifiers)
                {
                    end = i + 1;

                    if(i > 0 && text.Length > 2 && text[0] == '$' && text[1] == '"')
                    {
                        // C# 6 string format: $"{Property}".  Find the end accounting for escaped braces
                        while(++end < text.Length)
                            if(text[end] == '}')
                            {
                                if(end + 1 == text.Length || text[end + 1] != '}')
                                    break;

                                end++;
                            }
                    }
                    else
                        while(end < text.Length && Char.IsDigit(text[end]))
                            end++;

                    if(end < text.Length && text[end] == ':')
                    {
                        // Find the end accounting for escaped braces
                        while(++end < text.Length)
                            if(text[end] == '}')
                            {
                                if(end + 1 == text.Length || text[end + 1] != '}')
                                    break;

                                end++;
                            }
                    }

                    if(end < text.Length && text[end] == '}')
                        i = end;

                    continue;
                }

                // Skip C-style format string specifiers if so indicated.  These can cause spelling errors in
                // cases where there are multiple characters such as "%ld".  My C/C++ skills are very rusty but
                // this should cover it.
                if(text[i] == '%' && configuration.IgnoreFormatSpecifiers)
                {
                    end = i + 1;

                    if(end < text.Length)
                    {
                        // Flags
                        switch(text[end])
                        {
                            // NOTE: A space is also a valid flag character but we can't tell if it's part of
                            // the format or just a percentage followed by a word without some lookahead which
                            // probably isn't worth the effort (i.e. "% i" vs "100% stuff").  As such, the space
                            // flag character is not included here.
                            case '-':
                            case '+':
                            case '#':
                            case '0':
                                end++;
                                break;

                            default:
                                break;
                        }

                        // Width and precision not accounting for validity to keep it simple
                        while(end < text.Length && (Char.IsDigit(text[end]) || text[end] == '.' || text[end] == '*'))
                            end++;

                        if(end < text.Length)
                        {
                            // Length
                            switch(text[end])
                            {
                                case 'h':
                                case 'l':
                                    end++;

                                    // Check for "hh" and "ll"
                                    if(end < text.Length && text[end] == text[end - 1])
                                        end++;
                                    break;

                                case 'j':
                                case 'z':
                                case 't':
                                case 'L':
                                    end++;
                                    break;

                                default:
                                    break;
                            }

                            if(end < text.Length)
                            {
                                // And finally, the specifier
                                switch(text[end])
                                {
                                    case 'd':
                                    case 'i':
                                    case 'u':
                                    case 'o':
                                    case 'x':
                                    case 'X':
                                    case 'f':
                                    case 'F':
                                    case 'e':
                                    case 'E':
                                    case 'g':
                                    case 'G':
                                    case 'a':
                                    case 'A':
                                    case 'c':
                                    case 's':
                                    case 'p':
                                    case 'n':
                                        i = end;
                                        break;

                                    default:
                                        break;
                                }
                            }
                        }
                    }

                    continue;
                }

                // Skip word separator
                if(IsWordBreakCharacter(text[i]))
                    continue;

                // Find the end of the word
                end = i;

                while(++end < text.Length && !IsWordBreakCharacter(text[end]))
                    ;

                // Skip XML entity reference &[name];
                if(end < text.Length && i > 0 && text[i - 1] == '&' && text[end] == ';')
                {
                    i = end;
                    continue;
                }

                // Skip leading apostrophes
                while(i < end && text[i] == '\'')
                    i++;

                // Skip trailing apostrophes, periods, and at-signs
                while(--end > i && (text[end] == '\'' || text[end] == '.' || text[end] == '@'))
                    ;

                end++;    // Move back to last match

                // Ignore anything less than two characters
                if(end - i > 1)
                    yield return Microsoft.VisualStudio.Text.Span.FromBounds(i, end);

                i = --end;
            }
        }

        /// <summary>
        /// See if the specified character is a word break character
        /// </summary>
        /// <param name="c">The character to check</param>
        /// <returns>True if the character is a word break, false if not</returns>
        private bool IsWordBreakCharacter(char c)
        {
            return wordBreakChars.Contains(c) || Char.IsWhiteSpace(c) ||
                (c == '_' && configuration.TreatUnderscoreAsSeparator) ||
                ((c == '.' || c == '@') && !configuration.IgnoreFilenamesAndEMailAddresses);
        }

        /// <summary>
        /// Execute an action on the foreground thread
        /// </summary>
        /// <param name="action">The action to execute</param>
        /// <param name="priority">The priority to use for the action</param>
        private void OnForegroundThread(Action action, DispatcherPriority priority = DispatcherPriority.ApplicationIdle)
        {
            _dispatcher.Invoke(action, priority);
        }
        #endregion

        #region Tagging implementation
        //=====================================================================

        /// <inheritdoc />
        public IEnumerable<ITagSpan<MisspellingTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if(_isClosed || spans.Count == 0)
                yield break;

            List<MisspellingTag> currentMisspellings = _misspellings;

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
