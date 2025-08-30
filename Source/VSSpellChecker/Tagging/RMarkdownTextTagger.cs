//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : RMarkdownTextTagger.cs
// Authors : Eric Woodruff
// Updated : 08/30/2025
// Note    : Copyright 2017-2025, Eric Woodruff, All rights reserved
//
// This file contains a class used to provide tags for R markdown files when the R Tools for Visual Studio
// package is installed (part of the Data Science and Analytical Applications workload).
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 08/11/2017  EFW  Created the code
// 08/17/2018  EFW  Added support for tracking and excluding classifications using the classification cache
//===============================================================================================================

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace VisualStudio.SpellChecker.Tagging
{
    /// <summary>
    /// This class provides tags for R markdown files when the R Tools for Visual Studio package is installed
    /// (part of the Data Science and Analytical Applications workload).
    /// </summary>
    internal class RMarkdownTextTagger : ITagger<NaturalTextTag>, IDisposable
    {
        #region Private data members
        //=====================================================================

        private readonly ITextBuffer buffer;
        private readonly IEnumerable<string> ignoredClassifications;
        private readonly ClassificationCache classificationCache;
        private IClassifier classifier;

        #endregion

        #region MEF Imports/Exports
        //=====================================================================

        /// <summary>
        /// This class provides tags for PHP files when PHP Tools for Visual Studio are installed
        /// </summary>
        [Export(typeof(IViewTaggerProvider)), ContentType("R Markdown"), ContentType("RDoc"), TagType(typeof(NaturalTextTag))]
        internal class RMarkdownTextTaggerProvider : IViewTaggerProvider
        {
            [Import]
            private readonly IViewClassifierAggregatorService classifierAggregatorService = null;

            /// <inheritdoc />
            public ITagger<T> CreateTagger<T>(ITextView view, ITextBuffer buffer) where T : ITag
            {
                if(view == null || buffer == null)
                    return null;

                var classifier = classifierAggregatorService.GetClassifier(view);
#pragma warning disable VSTHRD010
                var config = SpellingServiceProxy.GetConfiguration(buffer);
#pragma warning restore VSTHRD010

                return new RMarkdownTextTagger(buffer, classifier,
                    config?.IgnoredClassificationsFor(buffer.ContentType.TypeName)) as ITagger<T>;
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
        /// <param name="ignoredClassifications">An optional enumerable list of ignored classifications for
        /// the buffer's content type</param>
        public RMarkdownTextTagger(ITextBuffer buffer, IClassifier classifier,
          IEnumerable<string> ignoredClassifications)
        {
            classificationCache = ClassificationCache.CacheFor(buffer.ContentType.TypeName);

            this.buffer = buffer;
            this.classifier = classifier;
            this.classifier.ClassificationChanged += this.ClassificationChanged;

            this.ignoredClassifications = (ignoredClassifications ?? []);
        }
        #endregion

        #region ITagger<NaturalTextTag> Members
        //=====================================================================

        /// <inheritdoc />
        public IEnumerable<ITagSpan<NaturalTextTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            List<SnapshotSpan> ignoredSpans = [];
            string text;
            int start, end;

            if(classifier == null || spans == null || spans.Count == 0)
                yield break;

            foreach(var snapshotSpan in spans)
            {
                Debug.Assert(snapshotSpan.Snapshot.TextBuffer == buffer);

                ignoredSpans.Clear();

                // The classifier for this one doesn't return natural language spans so we'll get those below.
                // First, get a list of stuff we can handle directly and/or ignore below.
                foreach(ClassificationSpan classificationSpan in classifier.GetClassificationSpans(snapshotSpan))
                {
                    string name = classificationSpan.ClassificationType.Classification.ToLowerInvariant();

                    switch(name)
                    {
                        case "markdown monospace":      // Typically code keywords, etc.
                        case "keyword":                 // RDoc stuff
                        case "number":
                        case "punctuation":
                        case "rd braces":
                            ignoredSpans.Add(classificationSpan.Span);
                            break;

                        case "markdown italic text":
                            // Italics may be denoted with underscores so we'll need to trim them off of the
                            // span or it will not spell check the text if the "treat underscore as separator"
                            // option is turned off.
                            text = classificationSpan.Span.GetText();
                            start = 0;

                            while(start < text.Length && text[start] == '_')
                                start++;

                            end = text.Length - 1;

                            while(end > start && text[end] == '_')
                                end--;

                            end++;

                            if(end - start > 1)
                            {
                                SnapshotSpan s = new(classificationSpan.Span.Start + start, end - start);
                                ignoredSpans.Add(s);

                                classificationCache.Add(name);

                                if(!ignoredClassifications.Contains(name))
                                    yield return new TagSpan<NaturalTextTag>(s, new NaturalTextTag());
                            }
                            break;

                        default:
                            classificationCache.Add(name);

                            if(ignoredClassifications.Contains(name))
                                ignoredSpans.Add(classificationSpan.Span);
                            break;
                    }
                }

                // Now return the spans we didn't ignore or handle above
                start = snapshotSpan.Start;

                foreach(var ignored in ignoredSpans.OrderBy(s => s.Start))
                {
                    if(ignored.Start > start)
                        yield return new TagSpan<NaturalTextTag>(new SnapshotSpan(snapshotSpan.Snapshot, start,
                            ignored.Start.Position - start), new NaturalTextTag());

                    start = ignored.Start + ignored.Length;
                }

                if(start < snapshotSpan.End)
                {
                    yield return new TagSpan<NaturalTextTag>(new SnapshotSpan(snapshotSpan.Snapshot, start,
                        snapshotSpan.End.Position - start), new NaturalTextTag());
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
                classifier.ClassificationChanged -= this.ClassificationChanged;
                classifier = null;
            }

            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
