//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : HtmlTextTagger.cs
// Authors : Noah Richards, Roman Golovin, Michael Lehenbauer, Eric Woodruff
// Updated : 06/06/2014
// Note    : Copyright 2010-2014, Microsoft Corporation, All rights reserved
//           Portions Copyright 2013-2014, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a class used to provide tags for HTML files
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 04/14/2013  EFW  Imported the code into the project
// 04/26/2013  EFW  Added support for disabling spell checking as you type
// 02/23/2014  EFW  Added support for project files using the derived HTML type "htmlx"
// 06/06/2014  EFW  Added support for excluding from spell checking by filename extension
//===============================================================================================================

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

using VisualStudio.SpellChecker;

namespace VisualStudio.SpellChecker.NaturalTextTaggers
{
    /// <summary>
    /// This class provides tags for HTML files
    /// </summary>
    internal class HtmlTextTagger : ITagger<NaturalTextTag>
    {
        #region Private data members
        //=====================================================================

        private ITextBuffer _buffer;
        private IClassifier _classifier;
        #endregion

        #region MEF Imports / Exports
        //=====================================================================

        /// <summary>
        /// HTML text tagger provider
        /// </summary>
        /// <remarks>The content types for this provider are "html" which covers regular HTML and "htmlx" which
        /// covers derived types such as HTML files in Razor projects.</remarks>
        [Export(typeof(ITaggerProvider)), ContentType("html"), ContentType("htmlx"), TagType(typeof(NaturalTextTag))]
        internal class HtmlTextTaggerProvider : ITaggerProvider
        {
            /// <summary>
            /// This is used to get or set the classifier aggregator service
            /// </summary>
            /// <remarks>The Import attribute causes the composition container to assign a value to this when an
            /// instance is created.  It is not assigned to within this class.</remarks>
            [Import]
            IClassifierAggregatorService ClassifierAggregatorService { get; set; }

            /// <summary>
            /// Creates a tag provider for the specified buffer
            /// </summary>
            /// <typeparam name="T">The tag type</typeparam>
            /// <param name="buffer">The text buffer</param>
            /// <returns>The tag provider for the specified buffer or null if the buffer is null or spell
            /// checking as you type is disabled.</returns>
            public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
            {
                if(buffer == null || !SpellCheckerConfiguration.SpellCheckAsYouType ||
                  SpellCheckerConfiguration.IsExcludedByExtension(buffer.GetFilenameExtension()))
                    return null;

                return new HtmlTextTagger(buffer, ClassifierAggregatorService.GetClassifier(buffer)) as ITagger<T>;
            }
        }
        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="buffer">The text buffer</param>
        /// <param name="classifier">The classifier</param>
        public HtmlTextTagger(ITextBuffer buffer, IClassifier classifier)
        {
            _buffer = buffer;
            _classifier = classifier;
        }
        #endregion

        #region ITagger<INaturalTextTag> Members
        //=====================================================================

        /// <inheritdoc />
        public IEnumerable<ITagSpan<NaturalTextTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            NormalizedSnapshotSpanCollection classifiedSpans = new NormalizedSnapshotSpanCollection(
                spans.SelectMany(span => _classifier.GetClassificationSpans(span)).Select(c => c.Span));

            NormalizedSnapshotSpanCollection plainSpans = NormalizedSnapshotSpanCollection.Difference(spans,
                classifiedSpans);

            foreach(var span in plainSpans)
                yield return new TagSpan<NaturalTextTag>(span, new NaturalTextTag());
        }

#pragma warning disable 67
        /// <inheritdoc />
        /// <remarks>This event is not used by this tagger</remarks>
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
#pragma warning restore 67

        #endregion
    }
}
