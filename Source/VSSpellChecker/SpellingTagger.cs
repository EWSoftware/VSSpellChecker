//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : SpellingTagger.cs
// Author  : Noah Richards, Roman Golovin, Michael Lehenbauer
// Updated : 10/23/2013
// Note    : Copyright 2010-2013, Microsoft Corporation, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a class that implements the spelling tagger
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code.  It can also be found at the project website: http://VSSpellChecker.CodePlex.com.
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
// Version     Date     Who  Comments
//===============================================================================================================
// 1.0.0.0  04/14/2013  EFW  Imported the code into the project.
//
// Change History
// 04/14/2013 - EFW - Added code to skip escape sequences so that they don't include extra characters in words
// or accidentally cause them to be excluded.  Added code to ignore XML entities, leading and trailing
// apostrophes, and words less than two characters in length.  Added support for NHunspell.
// 04/26/2013 - EFW - Added support for new configuration options
// 05/02/2013 - EFW - Added support for Replace All
// 05/11/2013 - EFW - Added support for ignoring XML elements in the spell checked text
// 05/28/2013 - EFW - Added properties to allow for interactive spell checking via a tool window
// 05/31/2013 - EFW - Added support for Ignore Once
//===============================================================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Documents;
using System.Windows.Threading;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

using VisualStudio.SpellChecker.Definitions;

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

        // Word break characters.  Specifically excludes: _ . ' @
        private const string wordBreakChars = ",/<>?;:\"[]\\{}|-=+~!#$%^&*() \t";

        private ITextBuffer _buffer;
        private ITagAggregator<INaturalTextTag> _naturalTextAggregator;
        private ITagAggregator<IUrlTag> _urlAggregator;
        private Dispatcher _dispatcher;
        private ISpellingDictionary _dictionary;

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
        public ISpellingDictionary Dictionary
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
        /// <param name="dictionary">The spelling dictionary</param>
        public SpellingTagger(ITextBuffer buffer, ITextView view,
          ITagAggregator<INaturalTextTag> naturalTextAggregator, ITagAggregator<IUrlTag> urlAggregator,
          ISpellingDictionary dictionary)
        {
            _isClosed = false;
            _buffer = buffer;
            _naturalTextAggregator = naturalTextAggregator;
            _urlAggregator = urlAggregator;
            _dispatcher = Dispatcher.CurrentDispatcher;
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

                        // TODO: Should probably add the language to the misspelling tag somehow so that it
                        // uses the proper language.  It may not be the same in the global config anymore at
                        // this point if it was changed.

                        // Match the case of the first letter if necessary
                        if(replacementWord.Length > 1 &&
                          (Char.IsUpper(replacementWord[0]) != Char.IsUpper(replacementWord[1]) ||
                          (Char.IsLower(replacementWord[0]) && Char.IsLower(replacementWord[1]))))
                            if(Char.IsUpper(currentWord[0]) && !Char.IsUpper(replacementWord[0]))
                            {
                                replacementWord = replacementWord.Substring(0, 1).ToUpper(
                                    SpellCheckerConfiguration.DefaultLanguage) + replacementWord.Substring(1);
                            }
                            else
                                if(Char.IsLower(currentWord[0]) && !Char.IsLower(replacementWord[0]))
                                    replacementWord = replacementWord.Substring(0, 1).ToLower(
                                        SpellCheckerConfiguration.DefaultLanguage) + replacementWord.Substring(1);

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
            var urlSpans = new NormalizedSnapshotSpanCollection(
                _urlAggregator.GetTags(spans)
                              .SelectMany(tagSpan => tagSpan.Span.GetSpans(snapshot)));

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
                if(_isClosed)
                    return;

                if(_dirtySpans.Count != 0)
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
            var ignoredWords = wordsIgnoredOnce;

            foreach(var span in spans)
            {
                string text = span.GetText();

                // Note the location of all XML elements if needed
                if(SpellCheckerConfiguration.IgnoreXmlElementsInText)
                    xmlTags = reXml.Matches(text).OfType<Match>().ToList();

                foreach(var word in GetWordsInText(text))
                {
                    string textToParse = text.Substring(word.Start, word.Length);

                    // Spell check the word if it looks like one and is not ignored
                    if(IsProbablyARealWord(textToParse) && (xmlTags == null || xmlTags.Count == 0 ||
                      !xmlTags.Any(match => word.Start >= match.Index &&
                      word.Start <= match.Index + match.Length - 1)) &&
                      !_dictionary.ShouldIgnoreWord(textToParse) && !_dictionary.IsSpelledCorrectly(textToParse))
                    {
                        // Sometimes it flags a word as misspelled if it ends with "'s".  Try checking the word
                        // without the "'s".  If ignored or correct without it, don't flag it.  This appears to
                        // be caused by the definitions in the dictionary rather than Hunspell.
                        if(textToParse.EndsWith("'s", StringComparison.OrdinalIgnoreCase))
                        {
                            textToParse = textToParse.Substring(0, textToParse.Length - 2);

                            if(_dictionary.ShouldIgnoreWord(textToParse) ||
                              _dictionary.IsSpelledCorrectly(textToParse))
                                continue;

                            textToParse += "'s";
                        }

                        // Some dictionaries include a trailing period on certain words such as "etc." which
                        // we don't include.  If the word is followed by a period, try it with the period to see
                        // if we get a match.  If so, consider it valid.
                        if(word.Start + word.Length < text.Length && text[word.Start + word.Length] == '.')
                        {
                            if(_dictionary.ShouldIgnoreWord(textToParse + ".") ||
                              _dictionary.IsSpelledCorrectly(textToParse + "."))
                                continue;
                        }

                        SnapshotSpan errorSpan = new SnapshotSpan(span.Start + word.Start, word.Length);

                        // Return the misspelled word and correction suggestions
                        var misspellingTag = new MisspellingTag(errorSpan, _dictionary.SuggestCorrections(textToParse));

                        // If the word is not being ignored at the current location, return it
                        if(!ignoredWords.Any(w => w.StartPoint == errorSpan.Start && w.Word.Equals(textToParse,
                          StringComparison.OrdinalIgnoreCase)))
                        {
                            yield return misspellingTag;
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
        internal static bool IsProbablyARealWord(string word)
        {
            if(String.IsNullOrWhiteSpace(word))
                return false;

            word = word.Trim();

            // Check for a period or an at-sign in the word (things that look like filenames and e-mail addresses)
            if(word.IndexOfAny(new[] { '.', '@' }) >= 0)
                return false;

            // Check for underscores and digits
            if(word.Any(c => c == '_' || (Char.IsDigit(c) && SpellCheckerConfiguration.IgnoreWordsWithDigits)))
                return false;

            // Ignore if all digits (this only happens if the Ignore Words With Digits option is false)
            if(!word.Any(c => Char.IsLetter(c)))
                return false;

            // Ignore if all uppercase, accounting for apostrophes and digits
            if(word.All(c => Char.IsUpper(c) || !Char.IsLetter(c)))
                return !SpellCheckerConfiguration.IgnoreWordsInAllUppercase;

            // Ignore if camel cased
            if(Char.IsLetter(word[0]) && word.Skip(1).Any(c => Char.IsUpper(c)))
                return false;

            return true;
        }

        /// <summary>
        /// Get all words in the specified text string
        /// </summary>
        /// <param name="text">The text to break into words</param>
        /// <returns>An enumerable list of word spans</returns>
        internal static IEnumerable<Microsoft.VisualStudio.Text.Span> GetWordsInText(string text)
        {
            int len, start, end;

            if(String.IsNullOrWhiteSpace(text))
                yield break;

            for(int i = 0; i < text.Length; i++)
            {
                if(IsWordBreakCharacter(text[i]))
                {
                    // Skip escape sequences.  If not, they can end up as part of the word or cause words to be
                    // missed.  For example, "This\r\nis\ta\ttest \x22missing\x22" would incorrectly yield "r",
                    // "nis", "ta", and "ttest" and incorrectly exclude "missing").  This can cause the
                    // occasional false positive in file paths (i.e. \Folder\transform\File.txt flags "ransform"
                    // as a misspelled word because of the lowercase "t" following the backslash) but I can live
                    // with that.
                    if(text[i] == '\\' && i + 1 < text.Length)
                        switch(text[i + 1])
                        {
                            case '\'':
                            case '\"':
                            case '\\':
                            case '0':
                            case 'a':
                            case 'b':
                            case 'f':
                            case 'n':
                            case 'r':
                            case 't':
                            case 'v':
                                i++;
                                break;

                            case 'u':
                            case 'U':
                            case 'x':
                                // Special handling for \x, \u, and \U.  Skip the hex digits too.
                                if(i + 2 < text.Length)
                                {
                                    start = i;
                                    i++;
                                    len = 0;

                                    do
                                    {
                                        i++;
                                        len++;

                                    } while(len < 5 && i < text.Length && (Char.IsDigit(text[i]) ||
                                      (Char.ToLower(text[i]) >= 'a' && Char.ToLower(text[i]) <= 'f')));

                                    i--;

                                    // Ignore invalid hex and Unicode escape sequences
                                    if(len == 1 || (text[start + 1] != 'x' && len != 5))
                                        i = start;
                                }
                                break;

                            default:
                                break;
                        }

                    continue;
                }

                end = i;

                for(; end < text.Length; end++)
                    if(IsWordBreakCharacter(text[end]))
                        break;

                // If it looks like an XML entity, ignore it
                if(i == 0 || end >= text.Length || text[i - 1] != '&' || text[end] != ';')
                {
                    // Ignore leading apostrophes
                    while(i < end && text[i] == '\'')
                        i++;

                    // Ignore trailing apostrophes, periods, and at-signs
                    while(end > i && (text[end - 1] == '\'' || text[end - 1] == '.' || text[end - 1] == '@'))
                        end--;

                    // Ignore anything less than two characters
                    if(end <= i)
                        end++;
                    else
                        if(end - i > 1)
                            yield return Microsoft.VisualStudio.Text.Span.FromBounds(i, end);
                }

                i = end - 1;
            }
        }

        /// <summary>
        /// See if the specified character is a word break character
        /// </summary>
        /// <param name="c">The character to check</param>
        /// <returns>True if the character is a word break, false if not</returns>
        private static bool IsWordBreakCharacter(char c)
        {
            return wordBreakChars.Contains(c) || Char.IsWhiteSpace(c) ||
                (c == '_' && SpellCheckerConfiguration.TreatUnderscoreAsSeparator) ||
                ((c == '.' || c == '@') && !SpellCheckerConfiguration.IgnoreFilenamesAndEMailAddresses);
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
