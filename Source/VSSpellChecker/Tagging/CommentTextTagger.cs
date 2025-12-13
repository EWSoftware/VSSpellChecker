//===============================================================================================================
// System  : Spell Check My Code Package
// File    : CommentTextTagger.cs
// Authors : Noah Richards, Roman Golovin, Michael Lehenbauer, Eric Woodruff
// Updated : 12/07/2025
// Note    : Copyright 2010-2025, Microsoft Corporation, All rights reserved
//           Portions Copyright 2013-2025, Eric Woodruff, All rights reserved
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
// 08/15/2018  EFW  Added support for tracking and excluding classifications using the classification cache
//===============================================================================================================

// Ignore spelling: sql cppstringdelimitercharacter

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

using VisualStudio.SpellChecker.ProjectSpellCheck;
using VisualStudio.SpellChecker.Tagging.CSharp;

namespace VisualStudio.SpellChecker.Tagging;

/// <summary>
/// This class provides tags for source code files of any type
/// </summary>
internal class CommentTextTagger : ITagger<NaturalTextTag>, IDisposable
{
    #region Private data members
    //=====================================================================

    private readonly ITextBuffer buffer;
    private IClassifier classifier;
    private readonly IEnumerable<string> ignoredXmlElements, spellCheckedXmlAttributes, ignoredClassifications;
    private readonly ClassificationCache classificationCache;

    #endregion

    #region MEF Imports / Exports
    //=====================================================================

    /// <summary>
    /// Comment view tagger provider
    /// </summary>
    [Export(typeof(IViewTaggerProvider)), ContentType("code"), TagType(typeof(NaturalTextTag))]
    internal class CommentTextTaggerProvider : IViewTaggerProvider
    {
        [Import]
        private readonly IViewClassifierAggregatorService classifierAggregatorService = null;

        /// <summary>
        /// Creates a tag provider for the specified view and buffer
        /// </summary>
        /// <typeparam name="T">The tag type</typeparam>
        /// <param name="view">The text view</param>
        /// <param name="buffer">The text buffer</param>
        /// <returns>The tag provider for the specified buffer or null if the buffer is null or the spelling
        /// service is unavailable.</returns>
        public ITagger<T> CreateTagger<T>(ITextView view, ITextBuffer buffer) where T : ITag
        {
            if(view == null || buffer == null || buffer.ContentType.IsOfType("R Markdown"))
                return null;

#pragma warning disable VSTHRD010
            var config = SpellingServiceProxy.GetConfiguration(buffer);

            if(config == null)
                return null;

            // Markdown has its own tagger
            if(buffer.ContentType.IsOfType("Markdown") || buffer.ContentType.IsOfType("code++.Markdown"))
            {
                return new MarkdownTextTagger(buffer, classifierAggregatorService.GetClassifier(view),
                    config.IgnoredClassificationsFor(buffer.ContentType.TypeName)) as ITagger<T>;
            }

            // Due to an issue with the built-in C# classifier, we avoid using it.  This also lets us provide
            // configuration options to exclude certain elements from being spell checked if not wanted.
            // Through the configuration options, we can also specify this tagger be used for all C-style
            // code.  Not all configuration options will apply but the structure is similar enough to make
            // most of them relevant.
            string filename = buffer.GetFilename();
#pragma warning restore VSTHRD010

            if(buffer.ContentType.IsOfType("csharp") || (config.CodeAnalyzerOptions.ApplyToAllCStyleLanguages &&
              ClassifierFactory.IsCStyleCode(filename)))
            {
                // The C# options are passed to the tagger for local use since it tracks the state of the
                // lines in the buffer.  Changing the global options will require that any open editors be
                // closed and reopened for the changes to take effect.
                return new CSharpCommentTextTagger(buffer)
                {
                    SupportsOldStyleXmlDocComments = ClassifierFactory.SupportsOldStyleXmlDocComments(filename),
                    IgnoreXmlDocComments = config.CodeAnalyzerOptions.IgnoreXmlDocComments,
                    IgnoreDelimitedComments = config.CodeAnalyzerOptions.IgnoreDelimitedComments,
                    IgnoreStandardSingleLineComments = config.CodeAnalyzerOptions.IgnoreStandardSingleLineComments,
                    IgnoreQuadrupleSlashComments = config.CodeAnalyzerOptions.IgnoreQuadrupleSlashComments,
                    IgnoreNormalStrings = config.CodeAnalyzerOptions.IgnoreNormalStrings,
                    IgnoreVerbatimStrings = config.CodeAnalyzerOptions.IgnoreVerbatimStrings,
                    IgnoreInterpolatedStrings = config.CodeAnalyzerOptions.IgnoreInterpolatedStrings,
                    IgnoredXmlElements = config.IgnoredXmlElements,
                    SpellCheckedAttributes = config.SpellCheckedXmlAttributes

                } as ITagger<T>;
            }

            return new CommentTextTagger(buffer, classifierAggregatorService.GetClassifier(view),
                config.IgnoredXmlElements, config.SpellCheckedXmlAttributes,
                config.IgnoredClassificationsFor(buffer.ContentType.TypeName)) as ITagger<T>;
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
    /// <param name="ignoredClassifications">An optional enumerable list of ignored classifications for
    /// the buffer's content type</param>
    public CommentTextTagger(ITextBuffer buffer, IClassifier classifier, IEnumerable<string> ignoredXmlElements,
      IEnumerable<string> spellCheckedXmlAttributes, IEnumerable<string> ignoredClassifications)
    {
        classificationCache = ClassificationCache.CacheFor(buffer.ContentType.TypeName);

        this.buffer = buffer;
        this.classifier = classifier;
        this.classifier.ClassificationChanged += ClassificationChanged;

        this.ignoredXmlElements = ignoredXmlElements ?? [];
        this.spellCheckedXmlAttributes = spellCheckedXmlAttributes ?? [];
        this.ignoredClassifications = ignoredClassifications ?? [];
    }
    #endregion

    #region ITagger<NaturalTextTag> Members
    //=====================================================================

    /// <inheritdoc />
    public IEnumerable<ITagSpan<NaturalTextTag>> GetTags(NormalizedSnapshotSpanCollection spans)
    {
        bool preprocessorKeywordSeen = false, delimiterSeen = false, skipAttributeValueParts = false;
        string elementName = null, attributeName = null, text;
        int pos;

        if(classifier == null || spans == null || spans.Count == 0)
            yield break;

        foreach(var snapshotSpan in spans)
        {
            Debug.Assert(snapshotSpan.Snapshot.TextBuffer == buffer);

            foreach(ClassificationSpan classificationSpan in classifier.GetClassificationSpans(snapshotSpan))
            {
                string name = classificationSpan.ClassificationType.Classification.ToLowerInvariant(),
                    originalName = name;

                // Do some conversion to make things simpler below
                switch(name)
                {
                    case "sql string":
                        // Skip the leading Unicode indicator if present
                        var span = classificationSpan.Span;

                        if(span.Length > 2 && span.GetText()[0] == 'N')
                            span = new SnapshotSpan(span.Snapshot, span.Start + 1, span.Length - 1);

                        classificationCache.Add(name);

                        if(!ignoredClassifications.Contains(name))
                            yield return new TagSpan<NaturalTextTag>(span, new NaturalTextTag());
                        continue;

                    case "vb xml doc attribute":
                        name = "attribute value";
                        break;

                    // VS2015 is much more specific in classifying XML doc comment parts
                    case "xml doc comment - delimiter":
                        name = "xml delimiter";
                        break;

                    case "xml doc comment - name":
                    case "xml literal - name":
                        name = "xml name";
                        break;

                    case "xml literal - text":
                        name = "xml text";
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
                        if(name == "identifier" || name.StartsWith("xml doc comment - ", StringComparison.Ordinal) ||
                          name.StartsWith("cppstringdelimitercharacter", StringComparison.Ordinal) ||
                          (name.Contains("attribute value") && skipAttributeValueParts))
                        {
                            continue;
                        }
                        break;
                }

                skipAttributeValueParts = false;

                // As long as the opening and closing XML tags appear on the same line as the content, we
                // can skip spell checking of unwanted elements.
                if(name == "xml delimiter" || name == "xaml delimiter" || name == "vb xml doc tag" ||
                  name == "punctuation" || name.StartsWith("vb xml delimiter", StringComparison.Ordinal) ||
                  name.Contains("html tag delimiter"))
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
                  name.StartsWith("vb xml name", StringComparison.Ordinal) || name.Contains("html element name")))
                {
                    elementName = classificationSpan.Span.GetText().Trim();

                    // Ignore any namespace prefix
                    if(elementName.IndexOf(':') != -1)
                        elementName = elementName.Substring(elementName.IndexOf(':') + 1);
                }

                // As long as the attribute value appears on the same line as the attribute name, we can
                // spell check attribute values if wanted.  Note that the Razor classifier works rather
                // oddly and may classify the element names separately from the attributes thus resulting in
                // unwanted attribute values being spell checked.  There doesn't appear to be anything we
                // can do about that.
                if(name.EndsWith(" attribute", StringComparison.Ordinal) || name.Contains("attribute name") ||
                  name == "parameter name")
                {
                    // XAML attribute names may include leading and trailing white space
                    attributeName = classificationSpan.Span.GetText().Trim();

                    // Ignore any namespace prefix
                    if(attributeName.IndexOf(':') != -1)
                        attributeName = attributeName.Substring(attributeName.IndexOf(':') + 1);
                }

                if((name.Contains("comment") || name.Contains("string") || name.Contains("xml text") ||
                  name.Contains("xaml text") || name.Contains("attribute value") ||
                  name.Equals("text", StringComparison.OrdinalIgnoreCase)) && !name.Contains("xml doc tag"))
                {
                    // If it's not a wanted attribute name, don't spell check its value
                    if(attributeName != null && (name.Contains("attribute value") || name == "string") &&
                      !spellCheckedXmlAttributes.Contains(attributeName))
                    {
                        // Some taggers return the opening and closing quotes and each word within as
                        // separate attribute value spans.  Set a flag to skip any subsequent parts as well.
                        skipAttributeValueParts = true;

                        attributeName = null;
                        continue;
                    }

                    // If it's an unwanted element, don't spell check its XML text
                    if(elementName != null && attributeName == null && ignoredXmlElements.Contains(elementName))
                        continue;

                    attributeName = null;

                    // Include files in C/C++ are tagged as a string but we don't want to spell check them
                    if(preprocessorKeywordSeen && name == "string" &&
                      classificationSpan.Span.Snapshot.ContentType.IsOfType("C/C++"))
                    {
                        preprocessorKeywordSeen = false;
                        continue;
                    }

                    preprocessorKeywordSeen = false;

                    // The Python classifier tends to tag general code elements, variables, etc. as text.
                    // Ignore them as we don't want to flag that stuff.  I'm not sure if this will stop it
                    // spell checking stuff that it really should.  Guess we'll have to wait and see.
                    if(name == "text" && classificationSpan.Span.Snapshot.ContentType.IsOfType("Python"))
                        continue;

                    // Track and ignore classifications by original name to allow the user to be more
                    // selective if necessary.
                    classificationCache.Add(originalName);

                    if(ignoredClassifications.Contains(originalName))
                        continue;

                    yield return new TagSpan<NaturalTextTag>(classificationSpan.Span, new NaturalTextTag());
                }
                else
                {
                    if(name == "preprocessor keyword")
                        preprocessorKeywordSeen = true;
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

        GC.SuppressFinalize(this);
    }
    #endregion
}
