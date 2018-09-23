//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : SpellSuggestedActionSource.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 08/02/2018
// Note    : Copyright 2016-2018, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a class used to implement the suggestion source for spelling light bulbs
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who   Comments
// ==============================================================================================================
// 12/05/2016  EFW   Created the code
//===============================================================================================================

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

using VisualStudio.SpellChecker.Definitions;
using VisualStudio.SpellChecker.Tagging;

namespace VisualStudio.SpellChecker.SuggestedActions
{
    /// <summary>
    /// This implements the suggestion source for spelling light bulbs
    /// </summary>
    internal class SpellSuggestedActionSource : ISuggestedActionsSource, IDisposable
    {
        #region Private data members
        //=====================================================================

        private SpellingDictionary dictionary;
        private ITagAggregator<MisspellingTag> misspellingAggregator;
        private bool disposed;

        #endregion

        #region MEF Imports / Exports
        //=====================================================================

        /// <summary>
        /// Spelling smart tagger provider
        /// </summary>
        [Export(typeof(ISuggestedActionsSourceProvider)), ContentType("any"), Name("VSSpellChecker Suggested Actions")]
        internal class SpellSmartTaggerProvider : ISuggestedActionsSourceProvider
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
            public ISuggestedActionsSource CreateSuggestedActionsSource(ITextView textView, ITextBuffer textBuffer)
            {
                // If this view isn't editable, then there isn't a good reason to be showing these
                if(textView == null || textBuffer == null || spellingService == null ||
                  !textView.Roles.Contains(PredefinedTextViewRoles.Editable) ||
                  (!textView.Roles.Contains(PredefinedTextViewRoles.PrimaryDocument) &&
                  !textView.Roles.Contains(Utility.EmbeddedPeekTextView)))
                {
                    return null;
                }

#pragma warning disable VSTHRD010
                // Getting the dictionary determines if spell checking is enabled for this file
                var dictionary = spellingService.GetDictionary(textBuffer);
#pragma warning restore VSTHRD010

                if(dictionary == null)
                    return null;

                return new SpellSuggestedActionSource(dictionary,
                    tagAggregatorFactory.CreateTagAggregator<MisspellingTag>(textView));
            }
        }
        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dictionary">The spelling dictionary</param>
        /// <param name="misspellingAggregator">The misspelling aggregator</param>
        public SpellSuggestedActionSource(SpellingDictionary dictionary,
          ITagAggregator<MisspellingTag> misspellingAggregator)
        {
            this.dictionary = dictionary;
            this.misspellingAggregator = misspellingAggregator;

            this.misspellingAggregator.TagsChanged += (sender, args) =>
            {
                if(!this.disposed)
                    this.SuggestedActionsChanged?.Invoke(this, EventArgs.Empty);
            };
        }
        #endregion

        #region ISuggestedActionsSource Members
        //=====================================================================

        /// <inheritdoc />
        public event EventHandler<EventArgs> SuggestedActionsChanged;

        /// <inheritdoc />
        public IEnumerable<SuggestedActionSet> GetSuggestedActions(ISuggestedActionCategorySet requestedActionCategories,
          SnapshotSpan range, CancellationToken cancellationToken)
        {
            if(range.IsEmpty || disposed)
                yield break;

            ITextSnapshot snapshot = range.Snapshot;

            var allMisspellings = misspellingAggregator.GetTags(range);

            if(allMisspellings.Count() == 1)
            {
                var misspelling = allMisspellings.First();

                SnapshotSpan errorSpan = misspelling.Span.GetSpans(snapshot)[0];

                if(misspelling.Tag.MisspellingType != MisspellingType.DoubledWord)
                {
                    foreach(var set in GetMisspellingSuggestedActions(errorSpan, misspelling.Tag))
                        yield return set;
                }
                else
                    foreach(var set in GetDoubledWordSuggestedActions(errorSpan, misspelling.Tag.DeleteWordSpan))
                        yield return set;
            }
            else
            {
                // If there are multiple misspellings (i.e. the margin light bulb is used), show the options for
                // each one in a submenu so that the main context menu is more compact and better organized.
                foreach(var misspelling in allMisspellings)
                {
                    SnapshotSpan errorSpan = misspelling.Span.GetSpans(snapshot)[0];

                    // The column is approximate.  It may not be accurate if tabs are present.  I couldn't find
                    // a better way to get it.
                    int column = errorSpan.Start.Position - errorSpan.Start.GetContainingLine().Start.Position + 1;

                    string displayText = String.Format("{0} ({1})", errorSpan.GetText(), column);

                    if(misspelling.Tag.MisspellingType != MisspellingType.DoubledWord)
                    {
                        yield return new SuggestedActionSet(null, new[] { new SuggestedActionSubmenu(displayText,
                            GetMisspellingSuggestedActions(errorSpan, misspelling.Tag)) }, null,
                            SuggestedActionSetPriority.Low);
                    }
                    else
                        yield return new SuggestedActionSet(null, new[] { new SuggestedActionSubmenu(displayText,
                            GetDoubledWordSuggestedActions(errorSpan, misspelling.Tag.DeleteWordSpan)) }, null,
                            SuggestedActionSetPriority.Low);
                }
            }
        }

        /// <inheritdoc />
        public Task<bool> HasSuggestedActionsAsync(ISuggestedActionCategorySet requestedActionCategories,
          SnapshotSpan range, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                if(range.IsEmpty || disposed)
                    return false;

                return misspellingAggregator.GetTags(range).Any();
            });
        }

        /// <inheritdoc />
        /// <returns>This provider does not participate in telemetry logging and alway returns false</returns>
        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }
        #endregion

        #region IDisposable
        //=====================================================================

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
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
        /// Get the suggested actions for misspelled words
        /// </summary>
        /// <param name="errorSpan">The error span for the misspelled word</param>
        /// <param name="misspellingType">The misspelling type</param>
        /// <param name="suggestions">The suggestions to use as the replacement</param>
        /// <returns>A enumerable list of suggested action sets</returns>
        private IEnumerable<SuggestedActionSet> GetMisspellingSuggestedActions(SnapshotSpan errorSpan,
          MisspellingTag misspelling)
        {
            List<SuggestedActionSet> actionSets = new List<SuggestedActionSet>();
            List<ISuggestedAction> actions = new List<ISuggestedAction>();
            ITrackingSpan trackingSpan = errorSpan.Snapshot.CreateTrackingSpan(errorSpan,
                SpanTrackingMode.EdgeExclusive);

            if(dictionary.DictionaryCount > 1)
            {
                // Merge the same words from different dictionaries
                var words = misspelling.Suggestions.GroupBy(w => w.Suggestion).Select(g =>
                    new MultiLanguageSpellSuggestedAction(trackingSpan, g.First(), misspelling.EscapeApostrophes,
                        g.Select(w => w.Culture), dictionary));

                actions.AddRange(words);
            }
            else
                actions.AddRange(misspelling.Suggestions.Select(word => new SpellSuggestedAction(trackingSpan, 
                    word, misspelling.EscapeApostrophes, dictionary)));

            if(actions.Count != 0)
                actionSets.Add(new SuggestedActionSet(null, actions, null, SuggestedActionSetPriority.Low));

            // Add Dictionary operations
            actions = new List<ISuggestedAction>
            {
                new SpellDictionarySuggestedAction(trackingSpan, dictionary, "Ignore Once",
                    DictionaryAction.IgnoreOnce, null),
                new SpellDictionarySuggestedAction(trackingSpan, dictionary, "Ignore All",
                    DictionaryAction.IgnoreAll, null)
            };

            actionSets.Add(new SuggestedActionSet(null, actions, null, SuggestedActionSetPriority.Low));

            if(misspelling.MisspellingType == MisspellingType.MisspelledWord)
            {
                actions = new List<ISuggestedAction>();

                if(dictionary.Dictionaries.Count() == 1)
                {
                    actions.Add(new SpellDictionarySuggestedAction(trackingSpan, dictionary,
                        "Add to Dictionary", DictionaryAction.AddWord, dictionary.Dictionaries.First().Culture));

                    actionSets.Add(new SuggestedActionSet(null, actions, null, SuggestedActionSetPriority.Low));
                }
                else
                {
                    // If there are multiple dictionaries, put them in a submenu
                    foreach(var d in dictionary.Dictionaries)
                        actions.Add(new SpellDictionarySuggestedAction(trackingSpan, dictionary,
                            String.Empty, DictionaryAction.AddWord, d.Culture));

                    actionSets.Add(new SuggestedActionSet(null, new[] { new SuggestedActionSubmenu("Add to Dictionary",
                        new[] {  new SuggestedActionSet(null, actions) }) }, null, SuggestedActionSetPriority.Low));
                }
            }

            return actionSets;
        }

        /// <summary>
        /// Get the suggested actions for doubled words
        /// </summary>
        /// <param name="errorSpan">The error span for the misspelled word</param>
        /// <param name="suggestions">The suggestions to use as the replacement</param>
        /// <returns>An enumerable list of suggested action sets</returns>
        private IEnumerable<SuggestedActionSet> GetDoubledWordSuggestedActions(SnapshotSpan errorSpan,
          ITrackingSpan deleteWordSpan)
        {
            List<SuggestedActionSet> actionSets = new List<SuggestedActionSet>();

            ITrackingSpan trackingSpan = errorSpan.Snapshot.CreateTrackingSpan(errorSpan,
                SpanTrackingMode.EdgeExclusive);

            // Add Dictionary operations (ignore all)
            List<ISuggestedAction> doubledWordActions = new List<ISuggestedAction>
            {
                new DoubledWordSuggestedAction(deleteWordSpan),
                new SpellDictionarySuggestedAction(trackingSpan, dictionary, "Ignore Once",
                    DictionaryAction.IgnoreOnce, null)
            };

            actionSets.Add(new SuggestedActionSet(null, doubledWordActions, null, SuggestedActionSetPriority.Low));

            return actionSets;
        }
        #endregion
    }
}
