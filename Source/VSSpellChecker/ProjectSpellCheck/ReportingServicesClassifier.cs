//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : ReportingServicesClassifier.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 09/10/2015
// Note    : Copyright 2015, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a class used to classify Reporting Services report file content
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 09/10/2015  EFW  Created the code
//===============================================================================================================

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;

using Microsoft.VisualStudio.Text;

using VisualStudio.SpellChecker.Configuration;

namespace VisualStudio.SpellChecker.ProjectSpellCheck
{
    /// <summary>
    /// This classifier is used to parse Reporting Services report (.rdl and .rdlc) files and exclude elements
    /// that should not be spell checked and to classify inner text values to reduce the number of false
    /// misspelling reports in expressions.
    /// </summary>
    internal class ReportingServicesClassifier : XmlClassifier
    {
        #region Private data members
        //=====================================================================

        private static Regex reComments = new Regex(@"\s*('.*?|Rem(\t| ).*?|Rem)([\r\n]{1,2}|$)",
            RegexOptions.Multiline | RegexOptions.IgnoreCase);
        private static Regex reStringLiterals = new Regex("\"(.|\"\")*?\"");

        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="filename">The filename to load</param>
        /// <param name="spellCheckConfiguration">The spell checker configuration for the file</param>
        public ReportingServicesClassifier(string filename, SpellCheckerConfiguration spellCheckConfiguration) :
          base(filename, spellCheckConfiguration)
        {
        }
        #endregion

        #region Method overrides
        //=====================================================================

        /// <inheritdoc />
        /// <remarks>This classifier removes various elements that contain information that should never be
        /// spell checked.</remarks>
        protected override bool ShouldSkipElement(XmlReader reader)
        {
            bool result = false;

            switch(reader.LocalName)
            {
                case "DataSets":
                case "DataSources":
                case "Format":
                case "ImageData":
                case "MIMEType":
                case "ReportID":
                    result = true;
                    break;

                default:
                    result = (reader.LocalName.EndsWith("Color", StringComparison.Ordinal));
                    break;
            }

            return result;
        }

        /// <inheritdoc />
        /// <remarks>This classifiers returns a single span for the inner text except in the case of <c>Code</c>
        /// elements or inner text that look like an expression.  For expressions, it only returns spans that are
        /// string literals.  For <c>Code</c> elements, it only returns comment and string literal spans.</remarks>
        protected override IEnumerable<SpellCheckSpan> ClassifyText(string elementName, string text, int offset)
        {
            if(elementName != "Code" && (text.TrimStart().Length < 2 || text.TrimStart()[0] != '='))
            {
                yield return new SpellCheckSpan
                {
                    Span = new Span(offset, text.Length),
                    Text = text,
                    Classification = RangeClassification.InnerText
                };
            }
            else
                if(elementName == "Code")
                {
                    // This doesn't merge contiguous ranges so it may miss doubled words.  It may not be worth
                    // the effort so we'll ignore it for now.
                    foreach(Match match in reComments.Matches(text))
                        yield return new SpellCheckSpan
                        {
                            Span = new Span(offset + match.Index, match.Value.Length),
                            Text = match.Value,
                            Classification = RangeClassification.SingleLineComment
                        };

                    // There's a chance here that a literal appears within a comment.  We could filter them out
                    // but it may not be worth the effort so we'll ignore it for now.  Worst case it will report
                    // a duplicate issue for the overlapping range.
                    foreach(Match match in reStringLiterals.Matches(text))
                        yield return new SpellCheckSpan
                        {
                            Span = new Span(offset + match.Index, match.Value.Length),
                            Text = match.Value,
                            Classification = RangeClassification.NormalStringLiteral
                        };
                }
                else
                {
                    foreach(Match match in reStringLiterals.Matches(text))
                        yield return new SpellCheckSpan
                        {
                            Span = new Span(offset + match.Index, match.Value.Length),
                            Text = match.Value,
                            Classification = RangeClassification.NormalStringLiteral
                        };
                }
        }
        #endregion
    }
}
