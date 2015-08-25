//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : SpellCheckSPan.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 08/29/2015
// Note    : Copyright 2015, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a class used to define a range of text that can be spell checked
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

using Microsoft.VisualStudio.Text;

namespace VisualStudio.SpellChecker.ProjectSpellCheck
{
    /// <summary>
    /// This class defines a range of text that can be spell checked
    /// </summary>
    internal class SpellCheckSpan
    {
        /// <summary>
        /// This is used to get or set the range of the text within the file
        /// </summary>
        public Span Span { get; set; }

        /// <summary>
        /// This is used to get or set the text within the range
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// This is used to get or set the range classification
        /// </summary>
        public RangeClassification Classification { get; set; }
    }
}
