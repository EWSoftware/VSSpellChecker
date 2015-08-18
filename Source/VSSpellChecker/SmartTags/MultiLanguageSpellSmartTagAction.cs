using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace VisualStudio.SpellChecker.SmartTags
{
    /// <summary>Smart tag action for inserting multi-language spelling suggestions.</summary>
    internal class MultiLanguageSpellSmartTagAction : SpellSmartTagAction
    {
        private CultureInfo[] _cultures;
        private string _displayText;

        /// <summary>Constructor for multi-language spelling suggestions smart tag actions.</summary>
        /// <param name="trackingSpan">The tracking span.</param>
        /// <param name="replaceWith">The suggestion to replace misspelled word with</param>
        /// <param name="cultures">The cultures from which the suggested word was chosen.</param>
        /// <param name="dictionary">The dictionary used to perform the Replace All action</param>
        public MultiLanguageSpellSmartTagAction(ITrackingSpan trackingSpan,
                                                SpellingSuggestion replaceWith,
                                                IEnumerable<CultureInfo> cultures,
                                                SpellingDictionary dictionary)
            : base(trackingSpan, replaceWith, dictionary)
        {
            _cultures = cultures.ToArray();
            _displayText = null;
        }

        /// <summary>Display text</summary>
        public override string DisplayText
        {
            get
            {
                return (_displayText ?? FormatDisplayText());
            }
        }

        /// <summary>Formats the display text.</summary>
        private string FormatDisplayText()
        {
            _displayText = string.Format("{0}\t\t({1})",
                                         base.DisplayText,
                                         string.Join(" | ", _cultures.Select(c => c.Name).ToArray()));
            return _displayText;
        }
    }
}