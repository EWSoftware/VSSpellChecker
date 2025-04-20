//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : CodeAnalyzerIdentifierSplitter.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 04/23/2023
// Note    : Copyright 2023, Eric Woodruff, All rights reserved
//
// This file contains the class used to split identifiers for the code analyzer
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 02/26/2023  EFW  Created the code
//===============================================================================================================

using Microsoft.CodeAnalysis.Text;

using VisualStudio.SpellChecker.Common;

namespace VisualStudio.SpellChecker.CodeAnalyzer
{
    /// <summary>
    /// This is used to split identifiers for the code analyzer
    /// </summary>
    internal class CodeAnalyzerIdentifierSplitter : IdentifierSplitter<TextSpan>
    {
        /// <inheritdoc />
        public override TextSpan CreateSpan(int start, int end)
        {
            return TextSpan.FromBounds(start, end);
        }
    }
}
