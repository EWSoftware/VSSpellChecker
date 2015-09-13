//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : HtmlClassifier.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 09/13/2015
// Note    : Copyright 2015, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a class used to classify HTML file content
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 08/31/2015  EFW  Created the code
//===============================================================================================================

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.Text;

using HtmlAgilityPack;

using VisualStudio.SpellChecker.Configuration;

namespace VisualStudio.SpellChecker.ProjectSpellCheck
{
    /// <summary>
    /// This class is used to classify HTML file content
    /// </summary>
    internal class HtmlClassifier : TextClassifier
    {
        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="filename">The filename to load</param>
        /// <param name="spellCheckConfiguration">The spell checker configuration for the file</param>
        public HtmlClassifier(string filename, SpellCheckerConfiguration spellCheckConfiguration) :
          base(filename, spellCheckConfiguration)
        {
        }
        #endregion

        #region Method overrides
        //=====================================================================

        /// <inheritdoc />
        public override IEnumerable<SpellCheckSpan> Parse()
        {
            List<SpellCheckSpan> spans = new List<SpellCheckSpan>();

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(this.Text);

            this.ParseNode(doc.DocumentNode, spans);

            return spans;
        }
        #endregion

        #region Helper methods
        //=====================================================================

        /// <summary>
        /// This is used to parse HTML nodes
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
                    // TODO: Ignore PHP script for now.  Need a separate parser for PHP files to return just
                    // comments and string literals within PHP script blocks.  The agility pack doesn't know
                    // about them so it doesn't extract them properly.
                    if(node.Name != "<?")
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

                    // TODO: Parse script for comments and literal strings?  May need a separate parser for
                    // ASP.NET and Razor inline code elements.
                    if(node.Name != "script" && !this.SpellCheckConfiguration.IgnoredXmlElements.Contains(node.Name) &&
                      node.HasChildNodes)
                        foreach(HtmlNode subnode in node.ChildNodes)
                            this.ParseNode(subnode, spans);
                    break;
            }
        }
        #endregion

    }
}
