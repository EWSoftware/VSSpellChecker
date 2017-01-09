//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : MarkdownTextTagger.cs
// Authors : Eric Woodruff
// Updated : 01/08/2017
// Note    : Copyright 2016-2017, Eric Woodruff, All rights reserved
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
//===============================================================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;

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

        private ITextBuffer buffer;
        private IClassifier classifier;

        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="buffer">The text buffer</param>
        /// <param name="classifier">The classifier</param>
        public MarkdownTextTagger(ITextBuffer buffer, IClassifier classifier)
        {
            this.buffer = buffer;
            this.classifier = classifier;

            this.classifier.ClassificationChanged += ClassificationChanged;
        }
        #endregion

        #region ITagger<NaturalTextTag> Members
        //=====================================================================

        /// <inheritdoc />
        public IEnumerable<ITagSpan<NaturalTextTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            string text;
            int start, end;

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
                        case "comment":
                        case "md_bold":
                        case "md_header":
                        case "md_quote":
                        case "natural language":
                            yield return new TagSpan<NaturalTextTag>(classificationSpan.Span, new NaturalTextTag());
                            break;

                        case "md_italic":
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
                                yield return new TagSpan<NaturalTextTag>(new SnapshotSpan(
                                    classificationSpan.Span.Start + start, end - start), new NaturalTextTag());
                            break;

                        case "keyword":
                            // An image in the form: ![Alternate text](URL "Title")
                            // A URL link in the form: [Inner text](URL "Title)
                            text = classificationSpan.Span.GetText();

                            start = text.IndexOf('[');
                            end = text.IndexOf(']');

                            if(start != -1 && end != -1 && end > start)
                                yield return new TagSpan<NaturalTextTag>(new SnapshotSpan(
                                    classificationSpan.Span.Start + start, end - start), new NaturalTextTag());

                            start = text.IndexOf('"');
                            end = text.IndexOf('"', start + 1);

                            if(start == -1 || end == -1)
                            {
                                start = text.IndexOf('\'');
                                end = text.IndexOf('\'', start + 1);
                            }

                            if(start != -1 && end != -1 && end > start)
                                yield return new TagSpan<NaturalTextTag>(new SnapshotSpan(
                                    classificationSpan.Span.Start + start, end - start), new NaturalTextTag());
                            break;

                        default:
                            // All other classifications such as code and HTML are ignored.  While there may be
                            // text within such spans that needs spell checking, it could prove rather difficult
                            // to extract so we'll ignore it.
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
            var handler = TagsChanged;

            if(handler != null)
                handler(this, new SnapshotSpanEventArgs(e.ChangeSpan));
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
