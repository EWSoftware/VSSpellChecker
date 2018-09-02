//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : MarkdownTextTagger.cs
// Authors : Eric Woodruff
// Updated : 08/21/2018
// Note    : Copyright 2016-2018, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a class used to provide tags for markdown files when the Markdown Editor extension by Mads
// Kristensen is installed (https://github.com/madskristensen/MarkdownEditor).
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 07/26/2016  EFW  Created the code
// 08/17/2018  EFW  Added support for tracking and excluding classifications using the classification cache
//===============================================================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;

namespace VisualStudio.SpellChecker.Tagging
{
    /// <summary>
    /// This class provides tags for markdown files when the Markdown Editor extension is installed
    /// </summary>
    internal class MarkdownTextTagger : ITagger<NaturalTextTag>, IDisposable
    {
        #region Private data members
        //=====================================================================

        private readonly ITextBuffer buffer;
        private readonly IEnumerable<string> ignoredClassifications;
        private readonly ClassificationCache classificationCache;
        private IClassifier classifier;

        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="buffer">The text buffer</param>
        /// <param name="classifier">The classifier</param>
        /// <param name="ignoredClassifications">An optional enumerable list of ignored classifications for
        /// the buffer's content type</param>
        public MarkdownTextTagger(ITextBuffer buffer, IClassifier classifier, IEnumerable<string> ignoredClassifications)
        {
            classificationCache = ClassificationCache.CacheFor(buffer.ContentType.TypeName);

            this.buffer = buffer;
            this.classifier = classifier;
            this.ignoredClassifications = (ignoredClassifications ?? Enumerable.Empty<string>());

            this.classifier.ClassificationChanged += ClassificationChanged;
        }
        #endregion

        #region ITagger<NaturalTextTag> Members
        //=====================================================================

        /// <inheritdoc />
        public IEnumerable<ITagSpan<NaturalTextTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if(classifier == null || spans == null || spans.Count == 0)
                yield break;

            ITextSnapshot snapshot = spans[0].Snapshot;

            foreach(var snapshotSpan in spans)
            {
                Debug.Assert(snapshotSpan.Snapshot.TextBuffer == buffer);

                foreach(ClassificationSpan classificationSpan in classifier.GetClassificationSpans(snapshotSpan))
                {
                    string name = classificationSpan.ClassificationType.Classification.ToLowerInvariant();

                    switch(name)
                    {
                        case "md_bold":     // Markers only, these contain nothing that can be spell checked
                        case "md_header":
                        case "md_html":
                        case "md_italic":
                        case "md_quote":
                        case "keyword":
                            break;

                        default:
                            // "md_code" will most likely contain a fair number of false reports but the
                            // classification can be excluded if necessary through the configuration or
                            // specific unwanted words through the Ignore Spelling directive.
                            classificationCache.Add(name);

                            if(!ignoredClassifications.Contains(name))
                                yield return new TagSpan<NaturalTextTag>(classificationSpan.Span, new NaturalTextTag());
                            break;
                    }
                }
            }
        }

        /// <inheritdoc />
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        /// <summary>
        /// This is used to raise the <see cref="TagsChanged"/> event
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void ClassificationChanged(object sender, ClassificationChangedEventArgs e)
        {
            this.TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(e.ChangeSpan));
        }
        #endregion

        #region IDisposable implementation
        //=====================================================================

        /// <inheritdoc />
        public void Dispose()
        {
            if(classifier != null)
            {
                classifier.ClassificationChanged -= ClassificationChanged;
                classifier = null;
            }
        }
        #endregion
    }
}
