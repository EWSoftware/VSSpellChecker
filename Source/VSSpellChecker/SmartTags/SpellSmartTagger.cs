//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : SpellSmartTagger.cs
// Authors : Noah Richards, Roman Golovin, Michael Lehenbauer, Eric Woodruff, Franz Alex Gaisie-Essilfie
// Updated : 08/25/2015
// Note    : Copyright 2010-2015, Microsoft Corporation, All rights reserved
//           Portions Copyright 2013-2015, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a class used to implement the tagger for spelling smart tags
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who   Comments
// ==============================================================================================================
// 04/14/2013  EFW   Imported the code into the project
// 04/26/2013  EFW   Added support for disabling spell checking as you type
// 05/02/2013  EFW   Added support for Replace All
// 05/31/2013  EFW   Added support for Ignore Once
// 06/06/2014  EFW   Added support for doubled word smart tags
// 06/20/2014  EFW   Added support for use in VS 2013 Peek Definition windows
// 02/28/2015  EFW   Added support for code analysis dictionary options
// 07/28/2015  EFW   Added support for culture information in the spelling suggestions
// 08/01/2015  EFW   Added support for multiple dictionary languages
// 08/18/2015  FAGE  Grouping of multi-language suggestions by word
//===============================================================================================================

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;

using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

using VisualStudio.SpellChecker.Definitions;
using VisualStudio.SpellChecker.Tagging;

namespace VisualStudio.SpellChecker.SmartTags
{
    /// <summary>
    /// Tagger for Spelling smart tags.
    /// </summary>
    internal class SpellSmartTagger : ITagger<SpellSmartTag>, IDisposable
    {
        #region Private data members
        //=====================================================================

        private ITextBuffer buffer;
        private SpellingDictionary dictionary;
        private ITagAggregator<MisspellingTag> misspellingAggregator;
        private bool disposed;

        internal const string SpellingErrorType = "Spelling Error Smart Tag";

        #endregion

        #region MEF Imports / Exports
        //=====================================================================

        /// <summary>
        /// Spelling smart tagger provider
        /// </summary>
        [Export(typeof(IViewTaggerProvider)), ContentType("any"),
          TagType(typeof(Microsoft.VisualStudio.Language.Intellisense.SmartTag))]
        internal class SpellSmartTaggerProvider : IViewTaggerProvider
        {
            [Import]
            private SpellingServiceFactory spellingService = null;

            [Import]
            private IViewTagAggregatorFactoryService tagAggregatorFactory = null;

            /// <summary>
            /// Creates a tag provider for the specified view and buffer
            /// </summary>
            /// <typeparam name="T">The tag type</typeparam>
            /// <param name="textView">The text view</param>
            /// <param name="buffer">The text buffer</param>
            /// <returns>The tag provider for the specified view and buffer or null if the buffer is not editable
            /// or spell checking as you type is disabled.</returns>
            public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
            {
                // If this view isn't editable, then there isn't a good reason to be showing these.  Also,
                // make sure we are only tagging the top buffer.
                if(textView == null || buffer == null || spellingService == null ||
                  textView.TextBuffer != buffer || !textView.Roles.Contains(PredefinedTextViewRoles.Editable) ||
                  (!textView.Roles.Contains(PredefinedTextViewRoles.PrimaryDocument) &&
                  !textView.Roles.Contains(Utility.EmbeddedPeekTextView)))
                {
                    return null;
                }

                // Getting the dictionary determines if spell checking is enabled for this file
                var dictionary = spellingService.GetDictionary(buffer);

                if(dictionary == null)
                    return null;

                return new SpellSmartTagger(buffer, dictionary,
                    tagAggregatorFactory.CreateTagAggregator<MisspellingTag>(textView)) as ITagger<T>;
            }
        }
        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="buffer">The text buffer</param>
        /// <param name="dictionary">The spelling dictionary</param>
        /// <param name="misspellingAggregator">The misspelling aggregator</param>
        public SpellSmartTagger(ITextBuffer buffer, SpellingDictionary dictionary,
          ITagAggregator<MisspellingTag> misspellingAggregator)
        {
            this.buffer = buffer;
            this.dictionary = dictionary;
            this.misspellingAggregator = misspellingAggregator;

            this.misspellingAggregator.TagsChanged += (sender, args) =>
            {
                if(!this.disposed)
                    foreach(var span in args.Span.GetSpans(this.buffer))
                        RaiseTagsChangedEvent(span);
            };
        }
        #endregion

        #region ITagger<SpellSmartTag> Members
        //=====================================================================

        /// <summary>
        /// Returns tags on demand
        /// </summary>
        /// <param name="spans">Spans collection for which to get tags</param>
        /// <returns>Squiggle tags in the provided spans</returns>
        public IEnumerable<ITagSpan<SpellSmartTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if(spans.Count == 0 || disposed)
                yield break;

            ITextSnapshot snapshot = spans[0].Snapshot;

            foreach(var misspelling in misspellingAggregator.GetTags(spans))
            {
                var misspellingSpans = misspelling.Span.GetSpans(snapshot);

                if(misspellingSpans.Count != 1)
                    continue;

                SnapshotSpan errorSpan = misspellingSpans[0];

                if(misspelling.Tag.MisspellingType != MisspellingType.DoubledWord)
                {
                    yield return new TagSpan<SpellSmartTag>(errorSpan, new SpellSmartTag(
                        GetMisspellingSmartTagActions(errorSpan, misspelling.Tag.MisspellingType,
                            misspelling.Tag.Suggestions)));
                }
                else
                {
                    yield return new TagSpan<SpellSmartTag>(errorSpan,
                        new SpellSmartTag(GetDoubledWordSmartTagActions(errorSpan, misspelling.Tag.DeleteWordSpan)));
                }
            }
        }

        /// <summary>
        /// This event is raised when the tags change
        /// </summary>
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        #endregion

        #region IDisposable
        //=====================================================================

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if(!disposed)
            {
                if(disposing)
                {
                    misspellingAggregator.Dispose();
                    misspellingAggregator = null;
                }

                disposed = true;
            }
        }

        #endregion

        #region Helper methods
        //=====================================================================

        /// <summary>
        /// Get the smart tag actions for misspelled words
        /// </summary>
        /// <param name="errorSpan">The error span for the misspelled word</param>
        /// <param name="misspellingType">The misspelling type</param>
        /// <param name="suggestions">The suggestions to use as the replacement</param>
        /// <returns>A read-only collection of smart tag action sets</returns>
        private ReadOnlyCollection<SmartTagActionSet> GetMisspellingSmartTagActions(SnapshotSpan errorSpan,
          MisspellingType misspellingType, IEnumerable<ISpellingSuggestion> suggestions)
        {
            List<SmartTagActionSet> smartTagSets = new List<SmartTagActionSet>();
            List<ISmartTagAction> actions = new List<ISmartTagAction>();
            ITrackingSpan trackingSpan = errorSpan.Snapshot.CreateTrackingSpan(errorSpan,
                SpanTrackingMode.EdgeExclusive);

            if(dictionary.DictionaryCount > 1)
            {
                // Merge the same words from different dictionaries
                var words = suggestions.GroupBy(w => w.Suggestion).Select(g =>
                    new MultiLanguageSpellSmartTagAction(trackingSpan, g.First(),
                        g.Select(w => w.Culture), dictionary));

                actions.AddRange(words);
            }
            else
                actions.AddRange(suggestions.Select(word => new SpellSmartTagAction(trackingSpan, word, dictionary)));

            if(actions.Count != 0)
                smartTagSets.Add(new SmartTagActionSet(actions.AsReadOnly()));

            if(smartTagSets.Count != 0)
            {
                // This acts as a place holder to tell the user to hold the Ctrl key down to replace all
                // occurrences of the selected word.
                actions = new List<ISmartTagAction>();
                actions.Add(new LabelSmartTagAction("Hold Ctrl to replace all"));

                smartTagSets.Insert(0, new SmartTagActionSet(actions.AsReadOnly()));
            }

            // Add Dictionary operations
            actions = new List<ISmartTagAction>();

            actions.Add(new SpellDictionarySmartTagAction(trackingSpan, dictionary, "Ignore Once",
                DictionaryAction.IgnoreOnce, null));
            actions.Add(new SpellDictionarySmartTagAction(trackingSpan, dictionary, "Ignore All",
                DictionaryAction.IgnoreAll, null));

            smartTagSets.Add(new SmartTagActionSet(actions.AsReadOnly()));

            if(misspellingType == MisspellingType.MisspelledWord)
            {
                actions = new List<ISmartTagAction>();

                foreach(var d in dictionary.Dictionaries)
                    actions.Add(new SpellDictionarySmartTagAction(trackingSpan, dictionary,
                        "Add to Dictionary", DictionaryAction.AddWord, d.Culture));

                smartTagSets.Add(new SmartTagActionSet(actions.AsReadOnly()));
            }

            return smartTagSets.AsReadOnly();
        }

        /// <summary>
        /// Get the smart tag actions for doubled words
        /// </summary>
        /// <param name="errorSpan">The error span for the misspelled word</param>
        /// <param name="suggestions">The suggestions to use as the replacement</param>
        /// <returns>A read-only collection of smart tag action sets</returns>
        private ReadOnlyCollection<SmartTagActionSet> GetDoubledWordSmartTagActions(SnapshotSpan errorSpan,
          ITrackingSpan deleteWordSpan)
        {
            List<SmartTagActionSet> smartTagSets = new List<SmartTagActionSet>();

            ITrackingSpan trackingSpan = errorSpan.Snapshot.CreateTrackingSpan(errorSpan,
                SpanTrackingMode.EdgeExclusive);

            // Add Dictionary operations (ignore all)
            List<ISmartTagAction> doubledWordActions = new List<ISmartTagAction>();

            doubledWordActions.Add(new DoubledWordSmartTagAction(deleteWordSpan));

            doubledWordActions.Add(new SpellDictionarySmartTagAction(trackingSpan, dictionary, "Ignore Once",
                DictionaryAction.IgnoreOnce, null));
            smartTagSets.Add(new SmartTagActionSet(doubledWordActions.AsReadOnly()));

            return smartTagSets.AsReadOnly();
        }
        #endregion

        #region Event handlers
        //=====================================================================

        /// <summary>
        /// Raise the <see cref="TagsChanged"/> event
        /// </summary>
        /// <param name="subjectSpan">The snapshot span to use for the event arguments</param>
        private void RaiseTagsChangedEvent(SnapshotSpan subjectSpan)
        {
            EventHandler<SnapshotSpanEventArgs> handler = this.TagsChanged;

            if(handler != null)
                handler(this, new SnapshotSpanEventArgs(subjectSpan));
        }
        #endregion
    }
}
