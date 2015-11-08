//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : CommentTextTagger.cs
// Authors : Noah Richards, Roman Golovin, Michael Lehenbauer, Eric Woodruff
// Updated : 10/29/2015
// Note    : Copyright 2010-2015, Microsoft Corporation, All rights reserved
//           Portions Copyright 2013-2015, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a class used to provide tags for source code files of any type
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
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
using System.Linq;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

using VisualStudio.SpellChecker.ProjectSpellCheck;
using VisualStudio.SpellChecker.Tagging.CSharp;

namespace VisualStudio.SpellChecker.Tagging
{
    /// <summary>
    /// This class provides tags for source code files of any type
    /// </summary>
    internal class CommentTextTagger : ITagger<NaturalTextTag>, IDisposable
    {
        #region Private data members
        //=====================================================================

        private ITextBuffer buffer;
        private IClassifier classifier;
        private IEnumerable<string> ignoredXmlElements, spellCheckedXmlAttributes;
        #endregion

        #region MEF Imports / Exports
        //=====================================================================

        /// <summary>
        /// Comment text tagger provider
        /// </summary>
        [Export(typeof(ITaggerProvider)), ContentType("code"), TagType(typeof(NaturalTextTag))]
        internal class CommentTextTaggerProvider : ITaggerProvider
        {
            [Import]
            private IClassifierAggregatorService classifierAggregatorService = null;

            [Import]
            private SpellingServiceFactory spellingService = null;

            /// <summary>
            /// Creates a tag provider for the specified buffer
            /// </summary>
            /// <typeparam name="T">The tag type</typeparam>
            /// <param name="buffer">The text buffer</param>
            /// <returns>The tag provider for the specified buffer or null if the buffer is null or the spelling
            /// service is unavailable.</returns>
            public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
            {
                if(buffer == null || spellingService == null)
                    return null;

                var config = spellingService.GetConfiguration(buffer);

                if(config == null)
                    return null;

                // Due to an issue with the built-in C# classifier, we avoid using it.  This also lets us provide
                // configuration options to exclude certain elements from being spell checked if not wanted.
                // Through the configuration options, we can also specify this tagger be used for all C-style
                // code.  Not all configuration options will apply but the structure is similar enough to make
                // most of them relevant.
                if(buffer.ContentType.IsOfType("csharp") || (config.CSharpOptions.ApplyToAllCStyleLanguages &&
                  ClassifierFactory.IsCStyleCode(buffer.GetFilename())))
                {
                    // The C# options are passed to the tagger for local use since it tracks the state of the
                    // lines in the buffer.  Changing the global options will require that any open editors be
                    // closed and reopened for the changes to take effect.
                    return new CSharpCommentTextTagger(buffer)
                    {
                        IgnoreXmlDocComments = config.CSharpOptions.IgnoreXmlDocComments,
                        IgnoreDelimitedComments = config.CSharpOptions.IgnoreDelimitedComments,
                        IgnoreStandardSingleLineComments = config.CSharpOptions.IgnoreStandardSingleLineComments,
                        IgnoreQuadrupleSlashComments = config.CSharpOptions.IgnoreQuadrupleSlashComments,
                        IgnoreNormalStrings = config.CSharpOptions.IgnoreNormalStrings,
                        IgnoreVerbatimStrings = config.CSharpOptions.IgnoreVerbatimStrings,
                        IgnoreInterpolatedStrings = config.CSharpOptions.IgnoreInterpolatedStrings,
                        IgnoredXmlElements = config.IgnoredXmlElements,
                        SpellCheckedAttributes = config.SpellCheckedXmlAttributes

                    } as ITagger<T>;
                }

                return new CommentTextTagger(buffer, classifierAggregatorService.GetClassifier(buffer),
                    config.IgnoredXmlElements, config.SpellCheckedXmlAttributes) as ITagger<T>;
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
        /// <param name="ignoredXmlElements">An optional enumerable list of ignored XML elements</param>
        /// <param name="spellCheckedXmlAttributes">An optional enumerable list of spell checked XML attributes</param>
        public CommentTextTagger(ITextBuffer buffer, IClassifier classifier, IEnumerable<string> ignoredXmlElements,
          IEnumerable<string> spellCheckedXmlAttributes)
        {
            this.buffer = buffer;
            this.classifier = classifier;

            this.classifier.ClassificationChanged += ClassificationChanged;

            this.ignoredXmlElements = (ignoredXmlElements ?? Enumerable.Empty<string>());
            this.spellCheckedXmlAttributes = (spellCheckedXmlAttributes ?? Enumerable.Empty<string>());
        }
        #endregion

        #region ITagger<NaturalTextTag> Members
        //=====================================================================

        /// <inheritdoc />
        public IEnumerable<ITagSpan<NaturalTextTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            bool preprocessorKeywordSeen = false, delimiterSeen = false;
            string elementName = null, attributeName = null, text;
            int pos;

            if(classifier == null || spans == null || spans.Count == 0)
                yield break;

            ITextSnapshot snapshot = spans[0].Snapshot;

            foreach(var snapshotSpan in spans)
            {
                Debug.Assert(snapshotSpan.Snapshot.TextBuffer == buffer);

                foreach(ClassificationSpan classificationSpan in classifier.GetClassificationSpans(snapshotSpan))
                {
                    string name = classificationSpan.ClassificationType.Classification.ToLowerInvariant();

                    // Do some conversion to make things simpler below
                    switch(name)
                    {
                        case "vb xml doc attribute":
                            name = "attribute value";
                            break;

                        // VS2015 is much more specific in classifying XML doc comment parts
                        case "xml doc comment - delimiter":
                            name = "xml delimiter";
                            break;

                        case "xml doc comment - name":
                            name = "xml name";
                            break;

                        case "xml doc comment - attribute name":
                            name = "xml attribute";
                            break;

                        case "xml doc comment - attribute value":
                            name = "attribute value";
                            break;

                        case "xml doc comment - text":
                            break;

                        default:
                            if(name == "identifier" || name.StartsWith("xml doc comment - ", StringComparison.Ordinal))
                                continue;

                            break;
                    }

                    // As long as the opening and closing XML tags appear on the same line as the content, we
                    // can skip spell checking of unwanted elements.
                    if(name == "xml delimiter" || name == "xaml delimiter" || name == "vb xml doc tag" ||
                      name.StartsWith("vb xml delimiter", StringComparison.Ordinal))
                    {
                        text = classificationSpan.Span.GetText();

                        if(text.IndexOf('/') != -1)
                        {
                            elementName = null;
                            delimiterSeen = false;
                        }
                        else
                            if(text.IndexOf('<') != -1)
                                delimiterSeen = true;

                        if(name == "vb xml doc tag" && delimiterSeen)
                        {
                            if(text.Length > 1 && text[0] == '<')
                            {
                                pos = text.IndexOf(' ');

                                if(pos != -1)
                                    elementName = text.Substring(1, pos - 1);
                                else
                                    elementName = text.Substring(1, text.Length - 2);
                            }

                            if(text.Length > 1 && text[text.Length - 1] == '=')
                            {
                                pos = text.IndexOf(' ');

                                if(pos != -1)
                                    attributeName = text.Substring(pos + 1, text.Length - pos - 2);
                                else
                                    attributeName = text.Substring(0, text.Length - 1);
                            }
                        }
                    }

                    if(delimiterSeen && (name == "xml name" || name == "xaml name" ||
                      name.StartsWith("vb xml name", StringComparison.Ordinal)))
                    {
                        elementName = classificationSpan.Span.GetText().Trim();

                        // Ignore any namespace prefix
                        if(elementName.IndexOf(':') != -1)
                            elementName = elementName.Substring(elementName.IndexOf(':') + 1);
                    }

                    // As long as the attribute value appears on the same line as the attribute name, we can
                    // spell check attribute values if wanted.
                    if(name == "xml attribute" || name == "xaml attribute" || name.Contains("attribute name"))
                    {
                        // XAML attribute names may include leading and trailing white space
                        attributeName = classificationSpan.Span.GetText().Trim();

                        // Ignore any namespace prefix
                        if(attributeName.IndexOf(':') != -1)
                            attributeName = attributeName.Substring(attributeName.IndexOf(':') + 1);
                    }

                    if((name.Contains("comment") || name.Contains("string") || name.Contains("xml text") ||
                      name.Contains("xaml text") || name.Contains("attribute value")) &&
                      !name.Contains("xml doc tag"))
                    {
                        // If it's not a wanted attribute name, don't spell check its value
                        if(attributeName != null && name.Contains("attribute value") &&
                          !spellCheckedXmlAttributes.Contains(attributeName))
                        {
                            attributeName = null;
                            continue;
                        }

                        attributeName = null;

                        // If it's an unwanted element, don't spell check its XML text
                        if(elementName != null && !name.Contains("attribute value") && ignoredXmlElements.Contains(elementName))
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
            if(classifier != null)
            {
                classifier.ClassificationChanged -= ClassificationChanged;
                classifier = null;
            }
        }
        #endregion
    }
}
