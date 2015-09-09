//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : RegexClassification.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 08/29/2015
// Note    : Copyright 2015, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a class used to define a regular expression classification
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 08/29/2015  EFW  Created the code
//===============================================================================================================

using System.Text.RegularExpressions;

namespace VisualStudio.SpellChecker.ProjectSpellCheck
{
    /// <summary>
    /// This class is used to define a regular expression classification
    /// </summary>
    internal class RegexClassification
    {
        /// <summary>
        /// This read-only property returns the expression used to classify text
        /// </summary>
        public Regex Expression { get; private set; }

        /// <summary>
        /// This read-only property returns the classification to use for matched text
        /// </summary>
        public RangeClassification Classification { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="expression">The regular expression to use</param>
        /// <param name="classification">The classification to assign matched text</param>
        public RegexClassification(Regex expression, RangeClassification classification)
        {
            this.Expression = expression;
            this.Classification = classification;
        }
    }
}
