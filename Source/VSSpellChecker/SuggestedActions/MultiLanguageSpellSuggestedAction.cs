//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : MultiLanguageSpellSuggestedAction.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 08/02/2018
// Note    : Copyright 2016-2018, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a class used to provide a suggested action for inserting multi-language spelling
// suggestions.
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who   Comments
// ==============================================================================================================
// 12/06/2016  EFW  Created the code
//===============================================================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Microsoft.VisualStudio.Text;

using VisualStudio.SpellChecker.Definitions;

namespace VisualStudio.SpellChecker.SuggestedActions
{
    /// <summary>
    /// Suggested action for inserting multi-language spelling suggestions
    /// </summary>
    internal class MultiLanguageSpellSuggestedAction : SpellSuggestedAction
    {
        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="trackingSpan">The tracking span</param>
        /// <param name="replaceWith">The suggestion to replace the misspelled word</param>
        /// <param name="escapeApostrophes">True to escape apostrophes in the suggestion, false if not</param>
        /// <param name="cultures">The cultures from which the suggested word was chosen</param>
        /// <param name="dictionary">The dictionary used to perform the Replace All action</param>
        public MultiLanguageSpellSuggestedAction(ITrackingSpan trackingSpan, ISpellingSuggestion replaceWith,
          bool escapeApostrophes, IEnumerable<CultureInfo> cultures, SpellingDictionary dictionary) :
          base(trackingSpan, replaceWith, escapeApostrophes, dictionary)
        {
            this.DisplayTextSuffix = String.Join(" | ", cultures.Select(c => c.Name));
        }
        #endregion
    }
}
