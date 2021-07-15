//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : MultiLanguageSpellingSuggestion.cs
// Author  : Franz Alex Gaisie-Essilfie
// Updated : 10/05/2018
//
// This file contains a class used to represent spelling suggestions from multiple dictionaries that can be used
// to replace a misspelled word.
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who   Comments
// ==============================================================================================================
// 2015-08-22  FAGE  Created the code
//===============================================================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace VisualStudio.SpellChecker
{
    /// <summary>
    /// This represents a multi-language spelling suggestion that can be used to replace a misspelled word
    /// </summary>
    public class MultiLanguageSpellingSuggestion : SpellingSuggestion
    {
        private readonly string formattedText;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="suggestion">The suggestion to replace misspelled word with</param>
        /// <param name="cultures">The cultures from which the suggested word was chosen</param>
        public MultiLanguageSpellingSuggestion(IEnumerable<CultureInfo> cultures, string suggestion) :
          base(cultures.First(), suggestion)
        {
            formattedText = $"{this.Suggestion}\t\t({String.Join(" | ", cultures.Where(c => c != null).Select(c => c.Name))})";
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return formattedText;
        }
    }
}
