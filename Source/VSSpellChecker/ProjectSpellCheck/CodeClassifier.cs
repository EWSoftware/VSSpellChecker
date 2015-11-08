//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : CodeClassifier.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 10/29/2015
// Note    : Copyright 2015, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a class used to classify source code file content using a set of regular expressions
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 08/26/2015  EFW  Created the code
//===============================================================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using HtmlAgilityPack;

using Microsoft.VisualStudio.Text;

using VisualStudio.SpellChecker.Configuration;

namespace VisualStudio.SpellChecker.ProjectSpellCheck
{
    /// <summary>
    /// This class is used to classify source code file content using a set of regular expressions.  In addition,
    /// it further classifies literal string types and XML documentation comments.
    /// </summary>
    /// <remarks>This should work for pretty much any type of source code.  The configuration options can be
    /// varied based on the language.</remarks>
    internal class CodeClassifier : RegexClassifier
    {
        #region Private data members
        //=====================================================================

        private string xmlDocCommentDelimiter, quadSlashDelimiter, oldStyleDocCommentDelimiter;
        private bool isCSharp, isCStyleCode;

        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="filename">The filename to load</param>
        /// <param name="spellCheckConfiguration">The spell checker configuration for the file</param>
        /// <param name="classifierConfiguration">The configuration element containing the classification
        /// expressions and their range types.</param>
        public CodeClassifier(string filename, SpellCheckerConfiguration spellCheckConfiguration,
          XElement classifierConfiguration) : base(filename, spellCheckConfiguration, classifierConfiguration)
        {
            xmlDocCommentDelimiter = (string)classifierConfiguration.Attribute("XmlDocCommentDelimiter");
            quadSlashDelimiter = (string)classifierConfiguration.Attribute("QuadSlashDelimiter");
            oldStyleDocCommentDelimiter = (string)classifierConfiguration.Attribute("OldStyleDocCommentDelimiter");

            isCSharp = filename.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);
            isCStyleCode = (spellCheckConfiguration.CSharpOptions.ApplyToAllCStyleLanguages &&
                ClassifierFactory.IsCStyleCode(filename));
        }
        #endregion

        #region Method overrides
        //=====================================================================

        /// <inheritdoc />
        /// <remarks>This classifier will ignore elements excluded by the C# options in C# files and, if wanted,
        /// all C-style code.  It will also classify XML documentation comments to eliminated things that
        /// shouldn't be spell checked within them.</remarks>
        public override IEnumerable<SpellCheckSpan> Parse()
        {
            int line, column;

            var spans = base.Parse();

            foreach(var span in spans)
            {
                // Apply the C# options to C# code and, if wanted, all C-style code
                if(isCSharp || isCStyleCode)
                {
                    var opts = this.SpellCheckConfiguration.CSharpOptions;
                    var classification = span.Classification;

                    if((classification == RangeClassification.XmlDocComments && opts.IgnoreXmlDocComments) ||
                      (classification == RangeClassification.DelimitedComments && opts.IgnoreDelimitedComments) ||
                      (classification == RangeClassification.SingleLineComment && opts.IgnoreStandardSingleLineComments) ||
                      (classification == RangeClassification.QuadSlashComment && opts.IgnoreQuadrupleSlashComments) ||
                      (classification == RangeClassification.NormalStringLiteral && opts.IgnoreNormalStrings) ||
                      (classification == RangeClassification.VerbatimStringLiteral && opts.IgnoreVerbatimStrings) ||
                      (classification == RangeClassification.InterpolatedStringLiteral && opts.IgnoreInterpolatedStrings))
                    {
                        continue;
                    }
                }

                if(span.Classification != RangeClassification.XmlDocComments)
                    yield return span;
                else
                {
                    // Parse XML documentation comments using HtmlDocument as XML comments may be ill-formed or
                    // may not actually be XML documentation comments due to an incorrect expression match.  In
                    // addition, we don't have to deal with the comment delimiters which may appear within an
                    // element between attributes when it spans more than one line which the XML reader doesn't
                    // like.
                    HtmlDocument doc = new HtmlDocument();
                    doc.LoadHtml(span.Text);

                    var docSpans = new List<SpellCheckSpan>();

                    this.GetPosition(span.Span.Start, out line, out column);

                    this.LineOffset = line;
                    this.ColumnOffset = column;

                    this.ParseNode(doc.DocumentNode, docSpans);

                    this.LineOffset = this.ColumnOffset = 0;

                    foreach(var s in docSpans)
                        yield return s;
                }
            }
        }

        /// <inheritdoc />
        /// <remarks>For string literals, this further classifies them by type of literal (normal, verbatim,
        /// interpolated).  For single line comments, this further classifies them by type: //, ///, //// etc.
        /// Delimited comments are reclassified as XML documentation comments if they start with '/**'.</remarks>
        protected override void AdjustClassifications(IEnumerable<SpellCheckSpan> spans)
        {
            string comment;

            foreach(var span in spans)
                switch(span.Classification)
                {
                    case RangeClassification.NormalStringLiteral:
                        if(span.Text[0] == '@')
                            span.Classification = RangeClassification.VerbatimStringLiteral;
                        else
                            if(span.Text[0] == '$')
                                span.Classification = RangeClassification.InterpolatedStringLiteral;
                        break;

                    case RangeClassification.SingleLineComment:
                        comment = span.Text.Trim();

                        if(quadSlashDelimiter != null && comment.Length >= quadSlashDelimiter.Length &&
                          comment.Substring(0, quadSlashDelimiter.Length) == quadSlashDelimiter)
                            span.Classification = RangeClassification.QuadSlashComment;
                        else
                            if(xmlDocCommentDelimiter != null && comment.Length >= xmlDocCommentDelimiter.Length &&
                              comment.Substring(0, xmlDocCommentDelimiter.Length) == xmlDocCommentDelimiter)
                                span.Classification = RangeClassification.XmlDocComments;
                        break;

                    case RangeClassification.DelimitedComments:
                        comment = span.Text.Trim();

                        if(oldStyleDocCommentDelimiter != null && comment.Length >= oldStyleDocCommentDelimiter.Length &&
                          comment.Substring(0, oldStyleDocCommentDelimiter.Length) == oldStyleDocCommentDelimiter)
                            span.Classification = RangeClassification.XmlDocComments;
                        break;

                    default:
                        break;
                }
        }
        #endregion

        #region Helper methods
        //=====================================================================

        /// <summary>
        /// Parse XML comment nodes from an XML documentation comments span
        /// </summary>
        /// <param name="node">The starting node</param>
        /// <param name="spans">The list to which spell check spans are added</param>
        private void ParseNode(HtmlNode node, List<SpellCheckSpan> spans)
        {
            switch(node.NodeType)
            {
                case HtmlNodeType.Document:
                    foreach(HtmlNode subnode in node.ChildNodes)
                        this.ParseNode(subnode, spans);
                    break;

                case HtmlNodeType.Comment:
                    var commentNode = (HtmlCommentNode)node;

                    if(commentNode.OuterHtml.StartsWith("<!--", StringComparison.Ordinal))
                        spans.Add(new SpellCheckSpan
                        {
                            Span = new Span(this.AdjustedOffset(this.GetOffset(commentNode.Line,
                                commentNode.LinePosition) - 1, commentNode.OuterHtml),
                                commentNode.OuterHtml.Length),
                            Text = commentNode.OuterHtml,
                            Classification = RangeClassification.XmlFileComment
                        });
                    break;

                case HtmlNodeType.Text:
                    var textNode = (HtmlTextNode)node;

                    if(!HtmlNode.IsOverlappedClosingElement(textNode.Text) && textNode.Text.Trim().Length != 0)
                        spans.Add(new SpellCheckSpan
                        {
                            Span = new Span(this.GetOffset(textNode.Line, textNode.LinePosition),
                                textNode.Text.Length),
                            Text = textNode.Text,
                            Classification = RangeClassification.InnerText
                        });
                    break;

                case HtmlNodeType.Element:
                    foreach(var attribute in node.Attributes)
                        if(this.SpellCheckConfiguration.SpellCheckedXmlAttributes.Contains(attribute.Name) &&
                          !String.IsNullOrWhiteSpace(attribute.Value))
                            spans.Add(new SpellCheckSpan
                            {
                                Span = new Span(this.AdjustedOffset(this.GetOffset(attribute.Line,
                                    attribute.LinePosition + attribute.Name.Length + 1), attribute.Value),
                                    attribute.Value.Length),
                                Text = attribute.Value,
                                Classification = RangeClassification.AttributeValue
                            });

                    if(!this.SpellCheckConfiguration.IgnoredXmlElements.Contains(node.Name) && node.HasChildNodes)
                        foreach(HtmlNode subnode in node.ChildNodes)
                            this.ParseNode(subnode, spans);
                    break;
            }
        }
        #endregion
    }
}
