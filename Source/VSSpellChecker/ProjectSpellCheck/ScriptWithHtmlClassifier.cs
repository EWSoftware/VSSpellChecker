//===============================================================================================================
// System  : Spell Check My Code Package
// File    : ScriptWithHtmlClassifier.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 03/22/2023
// Note    : Copyright 2021-2023, Eric Woodruff, All rights reserved
//
// This file contains a class used to classify script files with a mix of code and HTML
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 12/29/2021  EFW  Created the code
//===============================================================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using HtmlAgilityPack;

using Microsoft.VisualStudio.Text;

using VisualStudio.SpellChecker.Common.Configuration;

namespace VisualStudio.SpellChecker.ProjectSpellCheck
{
    /// <summary>
    /// This class is used to classify source code file content using a set of regular expressions.  In addition,
    /// it further classifies literal string types and XML documentation comments.
    /// </summary>
    /// <remarks>This should work for pretty much any type of source code.  The configuration options can be
    /// varied based on the language.</remarks>
    internal class ScriptWithHtmlClassifier : CodeClassifier
    {
        #region Private data members
        //=====================================================================

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
        public ScriptWithHtmlClassifier(string filename, SpellCheckerConfiguration spellCheckConfiguration,
          XElement classifierConfiguration) : base(filename, spellCheckConfiguration, classifierConfiguration)
        {
        }
        #endregion

        #region Method overrides
        //=====================================================================

        /// <inheritdoc />
        /// <remarks>This classifier will ignore elements classified as undefined and append those elements
        /// classified as HTML that are wanted.</remarks>
        public override IEnumerable<SpellCheckSpan> Parse()
        {
            var spans = base.Parse().ToList();

            // Parse as HTML and exclude the parts we don't want based on the results
            HtmlDocument doc = new();
            doc.LoadHtml(this.Text);

            var htmlSpans = new List<SpellCheckSpan>();

            this.ParseNode(doc.DocumentNode, htmlSpans);

            // If an HTML span is classified as undefined, exclude any code spans that fall within it.  If an
            // HTML span is not undefined but is covered by a code span, keep the code span and exclude the HTML
            // span.
            foreach(var html in htmlSpans)
            {
                foreach(var code in spans)
                {
                    if(code.Span.IntersectsWith(html.Span))
                    {
                        if(html.Classification == RangeClassification.Undefined)
                            code.Classification = RangeClassification.Undefined;
                        else
                            html.Classification = RangeClassification.Undefined;
                    }
                }
            }

            return spans.Concat(htmlSpans).Where(s => s.Classification != RangeClassification.Undefined);
        }
        #endregion

        #region Helper methods
        //=====================================================================

        /// <summary>
        /// Parse HTML in the file and return spans based on the settings.  These spans may be ignored or
        /// included in the results.
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

                    // Only spell check actual comments.  Stuff like "<!DOCTYPE>" is also classified as a comment
                    // by the node parser but we don't want to include them.
                    if(commentNode.OuterHtml.StartsWith("<!--", StringComparison.Ordinal))
                    {
#if DEBUG
                        // The parser offsets changed in an update to the HtmlAgilityPack.  Catch any further
                        // changes as we'll need to fix our computed offsets again.
                        if(!this.Text.Substring(this.GetOffset(commentNode.Line, commentNode.LinePosition + 1),
                          commentNode.OuterHtml.Length).Equals(commentNode.OuterHtml, StringComparison.OrdinalIgnoreCase))
                        {
                            System.Diagnostics.Debugger.Break();
                        }
#endif
                        spans.Add(new SpellCheckSpan
                        {
                            Span = new Span(this.AdjustedOffset(this.GetOffset(commentNode.Line,
                                commentNode.LinePosition + 1), commentNode.OuterHtml), commentNode.OuterHtml.Length),
                            Text = commentNode.OuterHtml,
                            Classification = RangeClassification.XmlFileComment
                        });
                    }
                    break;

                case HtmlNodeType.Text:
                    var textNode = (HtmlTextNode)node;

                    if(textNode.ParentNode.Name != "#document" &&
                      !HtmlNode.IsOverlappedClosingElement(textNode.Text) && textNode.Text.Trim().Length != 0)
                    {
#if DEBUG
                        // See above
                        if(!this.Text.Substring(this.GetOffset(textNode.Line, textNode.LinePosition + 1),
                            textNode.Text.Length).Equals(textNode.Text, StringComparison.OrdinalIgnoreCase))
                        {
                            System.Diagnostics.Debugger.Break();
                        }
#endif
                        spans.Add(new SpellCheckSpan
                        {
                            Span = new Span(this.AdjustedOffset(this.GetOffset(textNode.Line,
                                textNode.LinePosition + 1), textNode.Text), textNode.Text.Length),
                            Text = textNode.Text,
                            Classification = RangeClassification.InnerText
                        });
                    }
                    break;

                case HtmlNodeType.Element:
                    foreach(var attribute in node.Attributes)
                    {
                        if(this.SpellCheckConfiguration.SpellCheckedXmlAttributes.Contains(attribute.Name) &&
                          !String.IsNullOrWhiteSpace(attribute.Value))
                        {
#if DEBUG
                            // See above
                            if(!this.Text.Substring(this.GetOffset(attribute.Line, attribute.LinePosition +
                              attribute.Name.Length + 3), attribute.Value.Length).Equals(attribute.Value,
                              StringComparison.OrdinalIgnoreCase))
                            {
                                System.Diagnostics.Debugger.Break();
                            }
#endif
                            spans.Add(new SpellCheckSpan
                            {
                                Span = new Span(this.AdjustedOffset(this.GetOffset(attribute.Line,
                                    attribute.LinePosition + attribute.Name.Length + 3), attribute.Value),
                                    attribute.Value.Length),
                                Text = attribute.Value,
                                Classification = RangeClassification.AttributeValue
                            });
                        }
                        else
                        {
                            // Ignored attribute value
                            spans.Add(new SpellCheckSpan
                            {
                                Span = new Span(this.AdjustedOffset(this.GetOffset(attribute.Line,
                                    attribute.LinePosition + attribute.Name.Length + 3), attribute.Value),
                                    attribute.Value.Length),
                                Text = attribute.Value,
                                Classification = RangeClassification.Undefined
                            });
                        }
                    }

                    if(node.HasChildNodes)
                    {
                        if(!this.SpellCheckConfiguration.IgnoredXmlElements.Contains(node.Name))
                        {
                            foreach(HtmlNode subnode in node.ChildNodes)
                                this.ParseNode(subnode, spans);
                        }
                        else
                        {
                            // Ignored XML element
                            spans.Add(new SpellCheckSpan
                            {
                                Span = new Span(this.AdjustedOffset(this.GetOffset(node.Line,
                                    node.LinePosition + 1), node.InnerText), node.InnerText.Length),
                                Text = node.InnerText,
                                Classification = RangeClassification.Undefined
                            });
                        }
                    }
                    break;
            }
        }
        #endregion
    }
}
