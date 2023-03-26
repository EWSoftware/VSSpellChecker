//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : MarkdownClassifier.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 03/22/2023
// Note    : Copyright 2017-2023, Eric Woodruff, All rights reserved
//
// This file contains a class used to classify markdown file content
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 08/24/2017  EFW  Created the code
//===============================================================================================================

using System;
using System.Text.RegularExpressions;

using VisualStudio.SpellChecker.Common.Configuration;

namespace VisualStudio.SpellChecker.ProjectSpellCheck
{
    /// <summary>
    /// This class is used to classify markdown file content
    /// </summary>
    /// <remarks>This is identical to the HTML classifier but it excludes inline code, fenced code blocks,
    /// and LaTeX blocks.</remarks>
    internal class MarkdownClassifier : HtmlClassifier
    {
        #region Private data members
        //=====================================================================

        private static readonly Regex reCode = new Regex(@"(`[^`\r\n]+?`)|(^```.+?^```)|(^\$\$.+?^\$\$)",
            RegexOptions.Singleline | RegexOptions.Multiline);
        private static readonly MatchEvaluator matchReplacement = new MatchEvaluator(ReplaceAngleBrackets);

        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="filename">The filename to load</param>
        /// <param name="spellCheckConfiguration">The spell checker configuration for the file</param>
        public MarkdownClassifier(string filename, SpellCheckerConfiguration spellCheckConfiguration) :
          base(filename, spellCheckConfiguration)
        {
        }
        #endregion

        #region Method overrides
        //=====================================================================

        /// <inheritdoc />
        /// <remarks>This is overridden to replace angle brackets in code elements with blank spaces as needed</remarks>
        public override void SetText(string text)
        {
            base.SetText(reCode.Replace(text, matchReplacement));
        }

        /// <summary>
        /// This match evaluator handles replacing angle brackets in the text as needed
        /// </summary>
        /// <param name="m">The match to evaluate</param>
        /// <returns>The modified text</returns>
        /// <remarks>This works around an issue where angle brackets in code spans mess up the HTML parser</remarks>
        private static string ReplaceAngleBrackets(Match m)
        {
            int lt = m.Value.IndexOf('<'), gt = m.Value.IndexOf('>');

            // If one but not the other is present or if both are present but it doesn't look like any closing
            // elements are present, replace them with spaces.
            if((lt != -1 && gt == -1) || (lt == -1 && gt != -1) || (lt != -1 && gt != -1 &&
              m.Value.IndexOf("/>", StringComparison.Ordinal) == -1 &&
              m.Value.IndexOf("</", StringComparison.Ordinal) == -1))
            {
                return m.Value.Replace('<', ' ').Replace('>', ' ');
            }

            return m.Value;
        }
        #endregion
    }
}
