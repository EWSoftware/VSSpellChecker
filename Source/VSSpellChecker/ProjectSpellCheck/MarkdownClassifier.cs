//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : MarkdownClassifier.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 08/24/2017
// Note    : Copyright 2017, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
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

using VisualStudio.SpellChecker.Configuration;

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

        private static Regex reInlineCode = new Regex(@"`[^`\r\n]+?`");
        private static Regex reFencedCode = new Regex("^```.+?^```", RegexOptions.Singleline | RegexOptions.Multiline);
        private static Regex reLatexCode = new Regex(@"^\$\$.+?^\$\$", RegexOptions.Singleline | RegexOptions.Multiline);

        private static MatchEvaluator matchReplacement = new MatchEvaluator(m => new String(' ', m.Length));

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
        /// <remarks>This is overridden to replace code elements with blank spaces.  This works around an issue
        /// where angle brackets in code spans mess up the HTML parser.  We want to ignore such spans anyway as
        /// we don't want to spell check code which can generate a lot of false reports.</remarks>
        public override void SetText(string text)
        {
            text = reInlineCode.Replace(text, matchReplacement);
            text = reFencedCode.Replace(text, matchReplacement);
            text = reLatexCode.Replace(text, matchReplacement);

            base.SetText(text);
        }
        #endregion
    }
}
