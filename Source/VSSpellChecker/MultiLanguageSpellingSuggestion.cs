using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public MultiLanguageSpellingSuggestion(IEnumerable<CultureInfo> cultures, string suggestion)
            : base(cultures.First(), suggestion)
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

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return formattedText ??
                   (formattedText = FormatSuggestion(base.Suggestion, cultures));
        }

        /// <summary>Formats the suggestion to display the language to which it applies.</summary>
        /// <param name="suggestion">The suggested word.</param>
        /// <param name="cultures">The cultures to which the suggestion applies.</param>
        public static string FormatSuggestion(string suggestion, IEnumerable<CultureInfo> cultures)
        {
            return string.Format("{0}\t\t({1})",
                                 suggestion,
                                 string.Join(" | ", cultures.Select(c => c.Name).ToArray()));
        }
    }
}
