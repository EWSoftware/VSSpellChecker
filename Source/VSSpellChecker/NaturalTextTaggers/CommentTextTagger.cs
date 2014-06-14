//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : CommentTextTagger.cs
// Authors : Noah Richards, Roman Golovin, Michael Lehenbauer, Eric Woodruff
// Updated : 06/13/2014
// Note    : Copyright 2010-2014, Microsoft Corporation, All rights reserved
//           Portions Copyright 2013-2014, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a class used to provide tags for source code files of any type
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code.  It can also be found at the project website: http://VSSpellChecker.CodePlex.com.
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 04/14/2013  EFW  Imported the code into the project
// 04/14/2013  EFW  Added a condition to include "XML Text" elements so that it spell checks XML element inner
//                  text.  Added code to ignore strings following C/C++ preprocessor keywords so as not to spell
//                  check stuff like include file directives.
// 04/26/2013  EFW  Added condition to exclude the content of named XML elements from spell checking.  Added
//                  support for disabling spell checking as you type.
// 05/23/2013  EFW  Added conditions to exclude XAML elements and include XAML attributes
// 06/06/2014  EFW  Added support for excluding from spell checking by filename extension
//===============================================================================================================

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

using VisualStudio.SpellChecker;
using VisualStudio.SpellChecker.NaturalTextTaggers.CSharp;

namespace VisualStudio.SpellChecker.NaturalTextTaggers
{
    /// <summary>
    /// This class provides tags for source code files of any type
    /// </summary>
    internal class CommentTextTagger : ITagger<NaturalTextTag>, IDisposable
    {
        #region Private data members
        //=====================================================================

        private ITextBuffer _buffer;
        private IClassifier _classifier;
        private HashSet<string> ignoredXmlElements, spellCheckedXmlAttributes;
        #endregion

        #region MEF Imports / Exports
        //=====================================================================

        /// <summary>
        /// Comment text tagger provider
        /// </summary>
        [Export(typeof(ITaggerProvider)), ContentType("code"), TagType(typeof(NaturalTextTag))]
        internal class CommentTextTaggerProvider : ITaggerProvider
        {
            /// <summary>
            /// This is used to get or set the classifier aggregator service
            /// </summary>
            /// <remarks>The Import attribute causes the composition container to assign a value to this when an
            /// instance is created.  It is not assigned to within this class.</remarks>
            [Import]
            internal IClassifierAggregatorService ClassifierAggregatorService { get; set; }

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

                // Due to an issue with the built-in C# classifier, we avoid using it.  This also lets us provide
                // configuration options to exclude certain elements from being spell checked if not wanted.
                if(buffer.ContentType.IsOfType("csharp"))
                {
                    // The C# options are passed to the tagger for local use since it tracks the state of the
                    // lines in the buffer.  Changing the global options will require that any open editors be
                    // closed and reopened for the changes to take effect.
                    return new CSharpCommentTextTagger(buffer)
                    {
                        IgnoreXmlDocComments = SpellCheckerConfiguration.IgnoreXmlDocComments,
                        IgnoreDelimitedComments = SpellCheckerConfiguration.IgnoreDelimitedComments,
                        IgnoreStandardSingleLineComments = SpellCheckerConfiguration.IgnoreStandardSingleLineComments,
                        IgnoreQuadrupleSlashComments = SpellCheckerConfiguration.IgnoreQuadrupleSlashComments,
                        IgnoreNormalStrings = SpellCheckerConfiguration.IgnoreNormalStrings,
                        IgnoreVerbatimStrings = SpellCheckerConfiguration.IgnoreVerbatimStrings

                    } as ITagger<T>;
                }

                var tagger = new CommentTextTagger(buffer, ClassifierAggregatorService.GetClassifier(buffer));

                // Add the XML elements in which to ignore content and the XML attributes that will have their
                // content spell checked.
                foreach(string element in SpellCheckerConfiguration.IgnoredXmlElements)
                    tagger.IgnoredElementNames.Add(element);

                foreach(string attr in SpellCheckerConfiguration.SpellCheckedXmlAttributes)
                    tagger.SpellCheckAttributeNames.Add(attr);

                return tagger as ITagger<T>;
            }
        }
        #endregion

        #region Properties
        //=====================================================================

        /// <summary>
        /// This read-only property is used to get the hash set of ignored element names
        /// </summary>
        public HashSet<string> IgnoredElementNames
        {
            get { return ignoredXmlElements; }
        }

        /// <summary>
        /// This read-only property is used to get the hash set of attribute names that should have their
        /// values spell checked.
        /// </summary>
        public HashSet<string> SpellCheckAttributeNames
        {
            get { return spellCheckedXmlAttributes; }
        }
        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="buffer">The text buffer</param>
        /// <param name="classifier">The classifier</param>
        public CommentTextTagger(ITextBuffer buffer, IClassifier classifier)
        {
            _buffer = buffer;
            _classifier = classifier;

            classifier.ClassificationChanged += ClassificationChanged;

            ignoredXmlElements = new HashSet<string>();
            spellCheckedXmlAttributes = new HashSet<string>();
        }
        #endregion

        #region ITagger<INaturalTextTag> Members
        //=====================================================================

        /// <inheritdoc />
        public IEnumerable<ITagSpan<NaturalTextTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            bool preprocessorKeywordSeen = false, delimiterSeen = false;
            string elementName = null, attributeName = null;

            if(_classifier == null || spans == null || spans.Count == 0)
                yield break;

            ITextSnapshot snapshot = spans[0].Snapshot;

            foreach(var snapshotSpan in spans)
            {
                Debug.Assert(snapshotSpan.Snapshot.TextBuffer == _buffer);

                foreach(ClassificationSpan classificationSpan in _classifier.GetClassificationSpans(snapshotSpan))
                {
                    string name = classificationSpan.ClassificationType.Classification.ToLowerInvariant();

                    // As long as the opening and closing XML tags appear on the same line as the content, we
                    // can skip spell checking of unwanted elements.
                    if(name == "xml delimiter" || name == "xaml delimiter" || name.StartsWith("vb xml delimiter",
                      StringComparison.Ordinal))
                    {
                        if(classificationSpan.Span.GetText().IndexOf('/') != -1)
                        {
                            elementName = null;
                            delimiterSeen = false;
                        }
                        else
                            if(classificationSpan.Span.GetText().IndexOf('<') != -1)
                                delimiterSeen = true;
                    }

                    if(delimiterSeen && (name == "xml name" || name == "xaml name" ||
                      name.StartsWith("vb xml name", StringComparison.Ordinal)))
                    {
                        elementName = classificationSpan.Span.GetText();

                        // Ignore any namespace prefix
                        if(elementName.IndexOf(':') != -1)
                            elementName = elementName.Substring(elementName.IndexOf(':') + 1);
                    }

                    // As long as the attribute value appears on the same line as the attribute name, we can
                    // spell check attribute values if wanted.
                    if(name == "xml attribute" || name == "xaml attribute" ||
                      name.StartsWith("vb xml attribute name", StringComparison.Ordinal))
                    {
                        // XAML attribute names may include leading and trailing white space
                        attributeName = classificationSpan.Span.GetText().Trim();

                        // Ignore any namespace prefix
                        if(attributeName.IndexOf(':') != -1)
                            attributeName = attributeName.Substring(attributeName.IndexOf(':') + 1);
                    }
                    else
                        if(name == "xml attribute value" || name == "xaml attribute value" ||
                          name == "vb xml attribute value")
                        {
                            // If the name matches one to be spell checked, treat the value as XML text
                            if(spellCheckedXmlAttributes.Contains(attributeName))
                                name = "xml text";

                            attributeName = null;
                        }

                    if((name.Contains("comment") || name.Contains("string") || name.Contains("xml text")) &&
                      !name.Contains("xml doc tag"))
                    {
                        // If it's an unwanted element, don't spell check its XML text
                        if(elementName != null && name.Contains("xml text") && ignoredXmlElements.Contains(elementName))
                            continue;

                        // Include files in C/C++ are tagged as a string but we don't want to spell check them
                        if(preprocessorKeywordSeen && name == "string" &&
                          classificationSpan.Span.Snapshot.ContentType.IsOfType("C/C++"))
                            continue;

                        preprocessorKeywordSeen = false;

                        yield return new TagSpan<NaturalTextTag>(classificationSpan.Span, new NaturalTextTag());
                    }
                    else
                        if(name == "preprocessor keyword")
                            preprocessorKeywordSeen = true;
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
            if(_classifier != null)
                _classifier.ClassificationChanged -= ClassificationChanged;
        }
        #endregion
    }
}
