//===============================================================================================================
// System  : Spell Check My Code Package
// File    : CodeAnalyzerWordSplitter.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 04/23/2023
// Note    : Copyright 2023, Eric Woodruff, All rights reserved
//
// This file contains the class used to split words for the code analyzer
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
    /// This is used to split words for the code analyzer
    /// </summary>
    internal class CodeAnalyzerWordSplitter : WordSplitter<TextSpan>
    {
        /// <summary>
        /// The spell check type for the current range of text being spell checked
        /// </summary>
        /// <remarks>This is used during solution/project spell checking to change how certain parts of the
        /// word splitting process are handled.</remarks>
        public SpellCheckType SpellCheckType { get; set; }

        /// <inheritdoc />
        public override bool CanContainEscapedCharacters => this.SpellCheckType != SpellCheckType.None &&
            (this.SpellCheckType & (SpellCheckType.Identifier | SpellCheckType.AttributeValue |
            SpellCheckType.TypeParameter | SpellCheckType.VerbatimString | SpellCheckType.RawString)) == 0;

        /// <inheritdoc />
        public override bool IsStringLiteral => (this.SpellCheckType & SpellCheckType.StringLiteral) != 0;

        /// <inheritdoc />
        public override TextSpan CreateSpan(int start, int end)
        {
            return TextSpan.FromBounds(start, end);
        }

        /// <inheritdoc />
        public override string ActualWord(string containingText, TextSpan wordSpan)
        {
            string word = containingText.Substring(wordSpan.Start, wordSpan.Length);

            int concatPos = word.IndexOf('\"');

            if(concatPos != -1)
            {
                int end = concatPos + 1;

                while(end < word.Length && word[end] != '\"')
                    end++;

                if(end < word.Length - 1)
                    word = word.Substring(0, concatPos) + word.Substring(end + 1);
                else
                    word = word.Substring(0, concatPos);
            }

            return word;
        }
    }
}
