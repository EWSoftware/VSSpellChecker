﻿//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : MultiLanguageSpellSmartTagAction.cs
// Author  : Franz Alex Gaisie-Essilfie
// Updated : 10/08/2018
// Compiler: Microsoft Visual C#
//
// This file contains a class used to provide a smart tag action for inserting multi-language spelling suggestions
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who   Comments
// ==============================================================================================================
// 2015-08-18  FAGE  Created the code
//===============================================================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Microsoft.VisualStudio.Text;

using VisualStudio.SpellChecker.Definitions;

namespace VisualStudio.SpellChecker.SmartTags
{
    /// <summary>
    /// Smart tag action for inserting multi-language spelling suggestions
    /// </summary>
    internal class MultiLanguageSpellSmartTagAction : SpellSmartTagAction
    {
        private readonly string displayText;

        /// <summary>
        /// Constructor for multi-language spelling suggestions smart tag actions
        /// </summary>
        /// <param name="trackingSpan">The tracking span.</param>
        /// <param name="replaceWith">The suggestion to replace misspelled word with</param>
        /// <param name="cultures">The cultures from which the suggested word was chosen.</param>
        /// <param name="dictionary">The dictionary used to perform the Replace All action</param>
        public MultiLanguageSpellSmartTagAction(ITrackingSpan trackingSpan, ISpellingSuggestion replaceWith,
            IEnumerable<CultureInfo> cultures, SpellingDictionary dictionary) : base(trackingSpan, replaceWith, dictionary)
        {
            if(cultures != null && cultures.Any(c => c != null))
            {
                displayText = String.Format(CultureInfo.InvariantCulture, "{0}\t\t({1})", base.DisplayText,
                    String.Join(" | ", cultures.Where(c => c != null).Select(c => c.Name)));
            }
            else
                displayText = base.DisplayText;
        }

        /// <summary>
        /// Display text
        /// </summary>
        public override string DisplayText
        {
            get { return displayText; }
        }
    }
}
