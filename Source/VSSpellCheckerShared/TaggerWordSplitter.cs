//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : TaggerWordSplitter.cs
// Authors : Eric Woodruff
// Updated : 03/14/2023
// Note    : Copyright 2021, Eric Woodruff, All rights reserved
//
// This file contains a class that handles splitting spans of text up into individual words for spell checking
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 03/14/2023  EFW  Created a word splitter for the tagger
//===============================================================================================================

using Microsoft.VisualStudio.Text;

using VisualStudio.SpellChecker.Common;
using VisualStudio.SpellChecker.ProjectSpellCheck;

namespace VisualStudio.SpellChecker
{
    /// <summary>
    /// This is used to split words for the code analyzer
    /// </summary>
    internal class TaggerWordSplitter : WordSplitter<Span>
    {
        /// <summary>
        /// The range classification
        /// </summary>
        /// <remarks>This is used during solution/project spell checking to change how certain parts of the
        /// word splitting process are handled.</remarks>
        public RangeClassification Classification { get; set; }

        /// <inheritdoc />
        public override bool CanContainEscapedCharacters
        {
            get
            {
                switch(this.Classification)
                {
                    // For these classifications, skip further checking.  These aren't likely to contain
                    // escape sequences that need skipping.  Note that we only get a classification when
                    // doing solution/project spell checking.
                    case RangeClassification.PlainText:
                    case RangeClassification.XmlFileComment:
                    case RangeClassification.XmlFileCData:
                    case RangeClassification.AttributeValue:
                    case RangeClassification.InnerText:
                    case RangeClassification.VerbatimStringLiteral:
                    case RangeClassification.RegionDirective:
                        return false;

                    default:
                        return true;
                }
            }
        }

        /// <inheritdoc />
        public override bool IsStringLiteral => this.Classification == RangeClassification.InterpolatedStringLiteral ||
            this.Classification == RangeClassification.NormalStringLiteral ||
            this.Classification == RangeClassification.VerbatimStringLiteral;

        /// <inheritdoc />
        public override Span CreateSpan(int startIndex, int endIndex)
        {
            return Span.FromBounds(startIndex, endIndex);
        }

        /// <inheritdoc />
        public override string ActualWord(string containingText, Span wordSpan)
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

