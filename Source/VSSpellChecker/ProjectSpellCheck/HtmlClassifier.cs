//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : HtmlClassifier.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 08/12/2018
// Note    : Copyright 2015-2018, Eric Woodruff, All rights reserved
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
using System.Text.RegularExpressions;

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
        #region Private data members
        //=====================================================================

        private string pageLanguage;

        private static readonly Regex rePageLanguage = new Regex("<%@.*Language=\"(?<Language>.*?)\".*?%>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
        private static readonly Regex reScript = new Regex(@"<(%|\?).*?(\1)>", RegexOptions.Singleline);
        private static readonly Regex reScriptElement = new Regex(@"<script.*?(?<!(%|\?))>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
        private static readonly Regex reScriptLanguage = new Regex(@"type=(?<Language>[^\s]*)",
            RegexOptions.IgnoreCase);

        private Dictionary<string, TextClassifier> classifiers;

        #endregion

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
            classifiers = new Dictionary<string, TextClassifier>(StringComparer.OrdinalIgnoreCase);
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

                    if(commentNode.OuterHtml.StartsWith("<!--", StringComparison.Ordinal) &&
                      !this.SpellCheckConfiguration.IgnoreHtmlComments)
                    {
                        spans.Add(new SpellCheckSpan
                        {
                            Span = new Span(this.AdjustedOffset(this.GetOffset(commentNode.Line,
                                commentNode.LinePosition) - 1, commentNode.OuterHtml),
                                commentNode.OuterHtml.Length),
                            Text = commentNode.OuterHtml,
                            Classification = RangeClassification.XmlFileComment
                        });
                    }
                    break;

                case HtmlNodeType.Text:
                    var textNode = (HtmlTextNode)node;

                    if(!HtmlNode.IsOverlappedClosingElement(textNode.Text) && textNode.Text.Trim().Length != 0)
                    {
                        if(!this.DeterminePageLanguage(textNode.Text, this.GetOffset(textNode.Line, textNode.LinePosition), spans))
                            this.ParseScriptBlocks(textNode.Text, this.GetOffset(textNode.Line, textNode.LinePosition), spans);
                    }
                    break;

                case HtmlNodeType.Element:
                    foreach(var attribute in node.Attributes)
                        if(this.SpellCheckConfiguration.SpellCheckedXmlAttributes.Contains(attribute.Name) &&
                          !String.IsNullOrWhiteSpace(attribute.Value))
                        {
                            spans.Add(new SpellCheckSpan
                            {
                                Span = new Span(this.AdjustedOffset(this.GetOffset(attribute.Line,
                                    attribute.LinePosition + attribute.Name.Length + 1), attribute.Value),
                                    attribute.Value.Length),
                                Text = attribute.Value,
                                Classification = RangeClassification.AttributeValue
                            });
                        }

                    if(!this.SpellCheckConfiguration.IgnoredXmlElements.Contains(node.Name) && node.HasChildNodes)
                    {
                        if(node.Name == "script")
                            this.ParseScriptElement(node.OuterHtml, this.GetOffset(node.Line, node.LinePosition), spans);
                        else
                            foreach(HtmlNode subnode in node.ChildNodes)
                                this.ParseNode(subnode, spans);
                    }
                    break;
            }
        }

        /// <summary>
        /// Check for directives that specify the page language.  We'll use this for script elements that don't
        /// specify a language.
        /// </summary>
        /// <param name="text">The text to check</param>
        /// <param name="offset">The starting offset of the text within the file content</param>
        /// <param name="spans">The span collection used to contain literal text before and after the
        /// directives.</param>
        /// <returns>True if the page language was found, false if not</returns>
        private bool DeterminePageLanguage(string text, int offset, List<SpellCheckSpan> spans)
        {
            Match m = rePageLanguage.Match(text);

            if(m.Success)
            {
                pageLanguage = m.Groups["Language"].Value;

                // Add any text before and after the match as inner text excluding other script blocks
                if(m.Index != 0)
                    this.ParseScriptBlocks(text.Substring(0, m.Index), offset, spans);

                if(m.Index + m.Length < text.Length)
                    this.ParseScriptBlocks(text.Substring(m.Index + m.Length), offset + m.Index + m.Length, spans);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Find script blocks within the text and parse any that are found.  If there are none or literal text
        /// is found before, after, or between blocks, it is added as spell check spans.
        /// </summary>
        /// <param name="text">The text to check</param>
        /// <param name="offset">The starting offset of the text within the file content</param>
        /// <param name="spans">The span collection used to contain literal text between script blocks</param>
        private void ParseScriptBlocks(string text, int offset, List<SpellCheckSpan> spans)
        {
            int lastPos = 0;

            foreach(Match m in reScript.Matches(text))
            {
                // Add any literal text before the script
                if(m.Index != lastPos)
                {
                    spans.Add(new SpellCheckSpan
                    {
                        Span = new Span(offset, m.Index - lastPos),
                        Text = text.Substring(lastPos, m.Index - lastPos),
                        Classification = RangeClassification.InnerText
                    });

                    offset += m.Index - lastPos;
                }

                // Ignore ASP.NET directives
                if(!m.Value.StartsWith("<%@", StringComparison.Ordinal))
                {
                    var classifier = this.GetClassifier(pageLanguage);

                    classifier.SetText(m.Value);

                    foreach(var s in classifier.Parse())
                    {
                        // Adjust the span to set the position relative to the start of the block within this file
                        s.Span = new Span(s.Span.Start + offset, s.Span.Length);

                        System.Diagnostics.Debug.Assert(this.Text.Substring(s.Span.Start, s.Span.Length) == s.Text);

                        spans.Add(s);
                    }
                }

                offset += m.Length;
                lastPos = m.Index + m.Length;
            }

            // Add any trailing literal text
            if(lastPos < text.Length)
                spans.Add(new SpellCheckSpan
                {
                    Span = new Span(offset, text.Length - lastPos),
                    Text = text.Substring(lastPos),
                    Classification = RangeClassification.InnerText
                });
        }

        /// <summary>
        /// This is used to parse the script in a <c>script</c> element using the appropriate classifier based
        /// on the determined script language.
        /// </summary>
        /// <param name="script">The script to parse</param>
        /// <param name="offset">The starting offset of the text within the file content</param>
        /// <param name="spans">The span collection used to contain text to spell check extracted from the
        /// script.</param>
        private void ParseScriptElement(string script, int offset, List<SpellCheckSpan> spans)
        {
            Match scriptElement = reScriptElement.Match(script);

            if(scriptElement.Success)
            {
                Match language = reScriptLanguage.Match(scriptElement.Value);

                var classifier = this.GetClassifier(language.Success ? language.Groups["Language"].Value : pageLanguage);

                classifier.SetText(script.Substring(scriptElement.Index + scriptElement.Length));

                foreach(var s in classifier.Parse())
                {
                    // Adjust the span to set the position relative to the start of the block within this file
                    s.Span = new Span(s.Span.Start + offset + scriptElement.Index + scriptElement.Length, s.Span.Length);

                    System.Diagnostics.Debug.Assert(this.Text.Substring(s.Span.Start, s.Span.Length) == s.Text);

                    spans.Add(s);
                }
            }
        }

        /// <summary>
        /// This is used to get a classifier based on the script language
        /// </summary>
        /// <param name="language">The script language</param>
        /// <returns>A classifier based on the script language</returns>
        private TextClassifier GetClassifier(string language)
        {
            TextClassifier c;

            // Cache the script classifiers as there may be more than one script block on the page
            if(!classifiers.TryGetValue(language ?? String.Empty, out c))
            {
                if(!String.IsNullOrWhiteSpace(language) && language.ToLowerInvariant().IndexOf("vb",
                  StringComparison.OrdinalIgnoreCase) != -1)
                {
                    c = ClassifierFactory.GetClassifier("~~.vb", this.SpellCheckConfiguration);
                }
                else
                {
                    // Anything else is considered a C-style script language (C#, PHP, JavaScript, etc.).
                    c = ClassifierFactory.GetClassifier("~~.cs", this.SpellCheckConfiguration);
                }

                classifiers.Add(language ?? String.Empty, c);
            }

            return c;
        }
        #endregion
    }
}
