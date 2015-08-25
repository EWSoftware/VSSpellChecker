//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : XmlClassifier.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 09/08/2015
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

        private bool isResxFile;

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
            isResxFile = Path.GetExtension(filename).Equals(".resx", StringComparison.OrdinalIgnoreCase);
        }
        #endregion

        /// <inheritdoc />
        /// <remarks>For resource files (.resx), certain common comment words are automatically excluded and
        /// <c>data</c> and <c>metadata</c> elements with a <c>type</c> attribute are automatically ignored.</remarks>
        public override IEnumerable<SpellCheckSpan> Parse()
        {
            List<SpellCheckSpan> spans = new List<SpellCheckSpan>();
            XmlReaderSettings rs = new XmlReaderSettings { DtdProcessing = DtdProcessing.Parse };
            string value;

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
                                            // attrName="value".  The value must be encoded to get an accurate
                                            // position (quotes excluded).
                                            value = WebUtility.HtmlEncode(reader.Value).Replace("&quot;", "\"").Replace(
                                                "&#39;", "'");

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

                                // Is it an element in which to skip the content?
                                if(this.SpellCheckConfiguration.IgnoredXmlElements.Contains(reader.LocalName) ||
                                  (isResxFile && (reader.LocalName == "data" || reader.LocalName == "metadata") &&
                                  reader.GetAttribute("type") != null))
                                {
                                    reader.Skip();
                                    continue;
                                }
                                break;

                            case XmlNodeType.Comment:
                                value = reader.Value;

                                if(isResxFile)
                                    value = value.Replace("mimetype", "        ").Replace(
                                        "resheader", "         ").Replace("microsoft-resx", "              ");

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
                                // The value must be encoded to get an accurate position (quotes excluded)
                                value = WebUtility.HtmlEncode(reader.Value).Replace("&quot;", "\"").Replace(
                                    "&#39;", "'");

                                spans.Add(new SpellCheckSpan
                                {
                                    Span = new Span(this.GetOffset(lineInfo.LineNumber, lineInfo.LinePosition),
                                        value.Length),
                                    Text = value,
                                    Classification = RangeClassification.InnerText
                                });
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
    }
}
