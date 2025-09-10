//===============================================================================================================
// System  : Spell Check My Code Package
// File    : CSharpSpellCheckHandler.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 04/23/2023
// Note    : Copyright 2023, Eric Woodruff, All rights reserved
//
// This file contains the class used to implement the C# spell check handler
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

using VisualStudio.SpellChecker.Common.Configuration;

namespace VisualStudio.SpellChecker.CodeAnalyzer
{
    /// <summary>
    /// This is used to implement the C# spell check handler
    /// </summary>
    internal class CSharpSpellCheckHandler : SpellCheckHandler
    {
        /// <inheritdoc />
        public override char IdentifierKeywordPrefixCharacter => '@';

        /// <inheritdoc />
        public override string DelimitedCommentCharacters => "/*";

        /// <inheritdoc />
        public override string QuadSlashCommentCharacters => "////";

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="configuration">The spell checker configuration to use</param>
        public CSharpSpellCheckHandler(SpellCheckerConfiguration configuration) : base(configuration)
        {
        }
    }
}
