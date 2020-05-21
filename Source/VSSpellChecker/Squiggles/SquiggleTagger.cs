//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : SquiggleTagger.cs
// Authors : Noah Richards, Roman Golovin, Michael Lehenbauer
// Updated : 04/16/2015
// Note    : Copyright 2010-2015, Microsoft Corporation, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains the tagger class for spelling squiggles
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 04/14/2013  EFW  Imported the code into the project
// 06/20/2014  EFW  Added support for use in VS 2013 Peek Definition windows
//===============================================================================================================

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows.Media;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

using VisualStudio.SpellChecker.Tagging;

namespace VisualStudio.SpellChecker.Squiggles
{
    /// <summary>
    /// Tagger for Spelling squiggles.
    /// </summary>
    internal class SquiggleTagger : ITagger<IErrorTag>, IDisposable
    {
        #region Private Fields
        //=====================================================================

        private ITextBuffer buffer;
        private ITagAggregator<MisspellingTag> misspellingAggregator;
        private bool disposed;

        internal const string SpellingErrorType = "Spelling Error";
        #endregion

        #region MEF Imports / Exports
        //=====================================================================

        /// <summary>
        /// Defines colors for the spelling squiggles.
        /// </summary>
        [Export(typeof(EditorFormatDefinition))]
        [Name(SquiggleTagger.SpellingErrorType)]
        [Order(After = Priority.High)]
        [UserVisible(true)]
        internal class SpellingErrorClassificationFormatDefinition : EditorFormatDefinition
        {
            public SpellingErrorClassificationFormatDefinition()
            {
                this.ForegroundColor = Colors.Magenta;
                this.BackgroundCustomizable = false;
                this.DisplayName = "Spelling Error";
            }
        }

#pragma warning disable 414
        [Export(typeof(ErrorTypeDefinition))]
        [Name(SquiggleTagger.SpellingErrorType)]
        private readonly ErrorTypeDefinition SpellingErrorTypeDefinition = null;
#pragma warning restore 414

        /// <summary>
        /// MEF connector for the Spell checker squiggles.
        /// </summary>
        [Export(typeof(IViewTaggerProvider))]
        [ContentType("any")]
        [TagType(typeof(SpellSquiggleTag))]
        internal class SquiggleTaggerProvider : IViewTaggerProvider
        {
            [Import]
            internal IViewTagAggregatorFactoryService TagAggregatorFactory { get; set; }

            #region ITaggerProvider
            public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
            {
                // If this view isn't editable, then there isn't a good reason to be showing these
                if(!textView.Roles.Contains(PredefinedTextViewRoles.Editable) || (
                  !textView.Roles.Contains(PredefinedTextViewRoles.PrimaryDocument) &&
                  !textView.Roles.Contains(Utility.EmbeddedPeekTextView)))
                    return null;

                // Make sure we are only tagging the top buffer
                if(buffer != textView.TextBuffer)
                    return null;

                return textView.Properties.GetOrCreateSingletonProperty(() =>
                    new SquiggleTagger(buffer, TagAggregatorFactory.CreateTagAggregator<MisspellingTag>(textView)))
                    as ITagger<T>;
            }
            #endregion
        }
        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="buffer">The buffer to use</param>
        /// <param name="misspellingAggregator">The misspelling aggregator to use</param>
        public SquiggleTagger(ITextBuffer buffer, ITagAggregator<MisspellingTag> misspellingAggregator)
        {
            this.buffer = buffer;
            this.misspellingAggregator = misspellingAggregator;

            this.misspellingAggregator.TagsChanged += (sender, args) =>
            {
                if(!this.disposed)
                    foreach(var span in args.Span.GetSpans(this.buffer))
                        RaiseTagsChangedEvent(span);
            };
        }
        #endregion

        #region ITagger<SpellSquiggleTag> Members
        //=====================================================================

        /// <summary>
        /// Returns tags on demand.
        /// </summary>
        /// <param name="spans">Spans collection to get tags for.</param>
        /// <returns>Squiggle tags in provided spans.</returns>
        public IEnumerable<ITagSpan<IErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
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

                yield return new TagSpan<IErrorTag>(errorSpan, new SpellSquiggleTag(SquiggleTagger.SpellingErrorType));
            }
        }

        /// <inheritdoc />
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

        #region Helpers
        //=====================================================================

        /// <summary>
        /// Raise the <see cref="TagsChanged"/> event
        /// </summary>
        /// <param name="subjectSpan">The snapshot span to use for the event arguments</param>
        private void RaiseTagsChangedEvent(SnapshotSpan subjectSpan)
        {
            this.TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(subjectSpan));
        }
        #endregion
    }
}
