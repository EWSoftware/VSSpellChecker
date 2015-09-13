//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : XmlClassifier.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 09/13/2015
// Note    : Copyright 2015, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a class used to classify XML file content
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
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

using Microsoft.VisualStudio.Text;

using VisualStudio.SpellChecker.Configuration;

namespace VisualStudio.SpellChecker.ProjectSpellCheck
{
    /// <summary>
    /// This class is used to classify XML file content
    /// </summary>
    internal class XmlClassifier : TextClassifier
    {
        #region Private data members
        //=====================================================================

        private static Regex reXmlEncoding = new Regex("^<\\?xml.*?encoding\\s*=\\s*\"(?<Encoding>.*?)\".*?\\?>");

        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="filename">The filename to load</param>
        /// <param name="spellCheckConfiguration">The spell checker configuration for the file</param>
        public XmlClassifier(string filename, SpellCheckerConfiguration spellCheckConfiguration) :
          base(filename, spellCheckConfiguration)
        {
            try
            {
                // If an encoding is specified, re-read it using the correct encoding
                Match m = reXmlEncoding.Match(this.Text);

                if(m.Success)
                {
                    var encoding = Encoding.GetEncoding(m.Groups["Encoding"].Value);

                    if(encoding != Encoding.Default)
                    {
                        using(StreamReader sr = new StreamReader(filename, encoding, true))
                        {
                            this.SetText(sr.ReadToEnd());
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                // Ignore errors for invalid encodings.  We'll just use the default
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }
        #endregion

        #region Methods
        //=====================================================================

        /// <inheritdoc />
        public override IEnumerable<SpellCheckSpan> Parse()
        {
            List<SpellCheckSpan> spans = new List<SpellCheckSpan>();
            XmlReaderSettings rs = new XmlReaderSettings { DtdProcessing = DtdProcessing.Parse };
            Stack<string> elementNames = new Stack<string>();
            string elementName = String.Empty, value;

            // For parsing, convert carriage returns to spaces to maintain the correct offsets.  The XML reader
            // converts CR/LF pairs to a single LF which throws off the positions otherwise.
            using(var stringStream = new StringReader(this.Text.Replace('\r', ' ')))
            using(var reader = XmlReader.Create(stringStream, rs))
            {
                IXmlLineInfo lineInfo = (IXmlLineInfo)reader;
                
                try
                {
                    reader.MoveToContent();

                    while(!reader.EOF)
                    {
                        switch(reader.NodeType)
                        {
                            case XmlNodeType.Element:
                                if(reader.HasAttributes)
                                {
                                    for(int idx = 0; idx < reader.AttributeCount; idx++)
                                    {
                                        reader.MoveToAttribute(idx);

                                        if(this.SpellCheckConfiguration.SpellCheckedXmlAttributes.Contains(reader.LocalName))
                                        {
                                            // Set the approximate position of the value assuming the format:
                                            // attrName="value".  The value may need to be encoded to get an
                                            // accurate position (quotes excluded).
                                            value = reader.Value;

                                            if(!value.Equals(this.Text.Substring(this.GetOffset(lineInfo.LineNumber,
                                              lineInfo.LinePosition + reader.Settings.LinePositionOffset +
                                              reader.Name.Length + 2)), StringComparison.Ordinal))
                                            {
                                                value = WebUtility.HtmlEncode(value).Replace("&quot;", "\"").Replace(
                                                    "&#39;", "'");
                                            }

                                            spans.Add(new SpellCheckSpan
                                            {
                                                Span = new Span(this.AdjustedOffset(this.GetOffset(lineInfo.LineNumber,
                                                    lineInfo.LinePosition + reader.Settings.LinePositionOffset +
                                                    reader.Name.Length + 2), value), value.Length),
                                                Text = value,
                                                Classification = RangeClassification.AttributeValue
                                            });
                                        }
                                    }

                                    reader.MoveToElement();
                                }

                                if(!reader.IsEmptyElement)
                                {
                                    elementNames.Push(elementName);
                                    elementName = reader.LocalName;
                                }

                                // Is it an element in which to skip the content?
                                if(this.SpellCheckConfiguration.IgnoredXmlElements.Contains(reader.LocalName) ||
                                  this.ShouldSkipElement(reader))
                                {
                                    reader.Skip();
                                    continue;
                                }
                                break;

                            case XmlNodeType.EndElement:
                                if(elementNames.Count != 0)
                                    elementName = elementNames.Pop();
                                else
                                    elementName = String.Empty;
                                break;

                            case XmlNodeType.Comment:
                                // Apply adjustments to the comments if necessary
                                value = this.AdjustCommentText(reader.Value);

                                spans.Add(new SpellCheckSpan
                                {
                                    Span = new Span(this.GetOffset(lineInfo.LineNumber, lineInfo.LinePosition),
                                        value.Length),
                                    Text = value,
                                    Classification = RangeClassification.XmlFileComment
                                });
                                break;

                            case XmlNodeType.CDATA:
                                spans.Add(new SpellCheckSpan
                                {
                                    Span = new Span(this.GetOffset(lineInfo.LineNumber, lineInfo.LinePosition),
                                        reader.Value.Length),
                                    Text = reader.Value,
                                    Classification = RangeClassification.XmlFileCData
                                });
                                break;

                            case XmlNodeType.Text:
                                // The value may need to be encoded to get an accurate position (quotes excluded)
                                value = reader.Value;

                                if(!value.Equals(this.Text.Substring(this.GetOffset(lineInfo.LineNumber,
                                  lineInfo.LinePosition), value.Length).Replace('\r', ' '), StringComparison.Ordinal))
                                {
                                    value = WebUtility.HtmlEncode(value).Replace("&quot;", "\"").Replace(
                                        "&#39;", "'");
                                }

                                spans.AddRange(this.ClassifyText(elementName, value, this.GetOffset(
                                    lineInfo.LineNumber, lineInfo.LinePosition)));
                                break;

                            default:
                                break;
                        }

                        reader.Read();
                    }
                }
                catch(Exception ex)
                {
                    // Ignore exceptions.  Probably ill-formed content or an unknown entity.
                    System.Diagnostics.Debug.WriteLine(ex);
                }

                return spans;
            }
        }

        /// <summary>
        /// This can be overridden to adjust the comment text based on rules specific to a given file type such
        /// as removing text that should not be spell checked to prevent false reports.
        /// </summary>
        /// <param name="comments">The comments to adjust</param>
        /// <returns>The comments with any necessary adjustments made.  Note that the overall length of the text
        /// and the positions of any remaining words should stay the same.  The default implementation simply
        /// returns the comment text unmodified.</returns>
        protected virtual string AdjustCommentText(string comments)
        {
            return comments;
        }

        /// <summary>
        /// This can be overridden to determine whether or not to skip an element not otherwise excluded in the
        /// spell checker configuration settings.
        /// </summary>
        /// <param name="reader">The XML reader used to determine whether or not the element should be skipped</param>
        /// <returns>True if it should, false if not.  The base implementation always returns false.</returns>
        protected virtual bool ShouldSkipElement(XmlReader reader)
        {
            return false;
        }

        /// <summary>
        /// This can be overridden to further classify or limit the spans spell checked within an XML elements
        /// inner text.
        /// </summary>
        /// <param name="elementName">The name of the element containing the text</param>
        /// <param name="text">The text to classify</param>
        /// <param name="offset">The starting offset of the text within the file</param>
        /// <returns>An enumerable list of spans to spell check.  By default, this returns the entire range as a
        /// single text span.</returns>
        protected virtual IEnumerable<SpellCheckSpan> ClassifyText(string elementName, string text, int offset)
        {
            yield return new SpellCheckSpan
            {
                Span = new Span(offset, text.Length),
                Text = text,
                Classification = RangeClassification.InnerText
            };
        }
        #endregion
    }
}
