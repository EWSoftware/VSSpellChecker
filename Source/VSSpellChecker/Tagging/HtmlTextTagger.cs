//===============================================================================================================
// System  : Spell Check My Code Package
// File    : HtmlTextTagger.cs
// Authors : Noah Richards, Roman Golovin, Michael Lehenbauer, Eric Woodruff
// Updated : 06/03/2024
// Note    : Copyright 2010-2024, Microsoft Corporation, All rights reserved
//           Portions Copyright 2013-2024, Eric Woodruff, All rights reserved
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

// Ignore Spelling: cshtml

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace VisualStudio.SpellChecker.Tagging
{
    /// <summary>
    /// This class provides tags for HTML files
    /// </summary>
    internal class HtmlTextTagger : ITagger<NaturalTextTag>
    {
        #region Private data members
        //=====================================================================

        private readonly IClassifier classifier;

        #endregion

        #region MEF Imports / Exports
        //=====================================================================

        /// <summary>
        /// HTML text tagger provider
        /// </summary>
        /// <remarks>The content types for this provider are "html" which covers regular HTML, "htmlx" which
        /// covers derived HTML content types in certain other web project types, "code++.PHP" and "code++.Markdown"
        /// which cover PHP and markdown files when no extension is present that handles them, "Razor" which
        /// covers cshtml files, and "WebForms" which handles files opened with the Web Forms editor.</remarks>
        [Export(typeof(IViewTaggerProvider)), ContentType("html"), ContentType("htmlx"), ContentType("code++.PHP"),
          ContentType("Markdown"), ContentType("code++.Markdown"), ContentType("Razor"), ContentType("WebForms"),
          TagType(typeof(NaturalTextTag))]
        internal class HtmlTextTaggerProvider : IViewTaggerProvider
        {
            [Import]
            private IViewClassifierAggregatorService classifierAggregatorService = null;

            /// <summary>
            /// Creates a tag provider for the specified view and buffer
            /// </summary>
            /// <typeparam name="T">The tag type</typeparam>
            /// <param name="view">The text view</param>
            /// <param name="buffer">The text buffer</param>
            /// <returns>The tag provider for the specified buffer or null if the buffer is null</returns>
            public ITagger<T> CreateTagger<T>(ITextView view, ITextBuffer buffer) where T : ITag
            {
                if(view == null || buffer == null)
                    return null;

                // Since VS 2019 PHP Tools for Visual Studio have PHPProjection content type based on HTLMX
                // Return null here and let PhpTextTaggerProvider deal with it
                if(buffer.ContentType.IsOfType("PHPProjection"))
                    return null;

                return new HtmlTextTagger(classifierAggregatorService.GetClassifier(view)) as ITagger<T>;
            }
        }
        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="classifier">The classifier</param>
        public HtmlTextTagger(IClassifier classifier)
        {
            this.classifier = classifier;
        }
        #endregion

        #region ITagger<NaturalTextTag> Members
        //=====================================================================

        /// <inheritdoc />
        public IEnumerable<ITagSpan<NaturalTextTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            NormalizedSnapshotSpanCollection classifiedSpans = new(
                spans.SelectMany(span => classifier.GetClassificationSpans(span)).Select(c => c.Span));

            NormalizedSnapshotSpanCollection plainSpans = NormalizedSnapshotSpanCollection.Difference(spans,
                classifiedSpans);

            // NOTE: The Razor classifier works rather oddly.  It seems to classify parts of the file but
            // not others and then goes back and does the bits it missed.  As such, you may see code, attributes,
            // and attribute values here but they go away on the next pass.  On occasion, it seems to not
            // classify everything when the file is opened and those noted elements aren't syntax highlighted
            // correctly and may show up as misspellings.  However, if you wait a few seconds, it catches up,
            // reclassifies everything properly and the incorrect misspellings go away.
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
