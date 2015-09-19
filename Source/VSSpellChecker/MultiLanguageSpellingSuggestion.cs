//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : MultiLanguageSpellingSuggestion.cs
// Author  : Franz Alex Gaisie-Essilfie
// Updated : 09/18/2015
// Compiler: Microsoft Visual C#
//
// This file contains a class used to represent spelling suggestions from multiple dictionaries
// that can be used to replace a misspelled word.
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
    /// This represents a multi-language spelling suggestion that can be used to replace a misspelled word.
    /// </summary>
    public class MultiLanguageSpellingSuggestion : SpellingSuggestion
    {
        private IEnumerable<CultureInfo> cultures;
        private string formattedText;

        /// <summary>Multi-language suggestion constructor.</summary>
        /// <param name="suggestion">The suggestion to replace misspelled word with</param>
        /// <param name="cultures">The cultures from which the suggested word was chosen.</param>
        public MultiLanguageSpellingSuggestion(IEnumerable<CultureInfo> cultures, string suggestion) :
          base(cultures.First(), suggestion)
        {
            this.cultures = cultures;
            this.formattedText = null;
        }

        /// <summary>Gets the culture information for the suggestion.</summary>
        public IEnumerable<CultureInfo> Cultures
        {
            get
            {
                return cultures;
            }
        }

        /// <summary>Returns a <see cref="System.String" /> that represents this instance.</summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            return formattedText ?? (formattedText = FormatSuggestion(base.Suggestion, cultures));
        }

        /// <summary>Formats the suggestion to display the language to which it applies.</summary>
        /// <param name="suggestion">The suggested word.</param>
        /// <param name="cultures">The cultures to which the suggestion applies.</param>
        public static string FormatSuggestion(string suggestion, IEnumerable<CultureInfo> cultures)
        {
            return String.Format(CultureInfo.InvariantCulture, "{0}\t\t({1})", suggestion,
                String.Join(" | ", cultures.Select(c => c.Name)));
        }
    }
}
