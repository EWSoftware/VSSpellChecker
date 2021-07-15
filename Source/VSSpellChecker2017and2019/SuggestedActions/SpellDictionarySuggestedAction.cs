//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : SpellDictionarySuggestedAction.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 09/02/2018
// Note    : Copyright 2016-2018, Eric Woodruff, All rights reserved
//
// This file contains a class used to provide suggested actions for ignoring words or adding new words to the
// dictionary.
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 04/14/2013  EFW  Imported the code into the project
// 05/02/2013  EFW  Added support for Replace All
// 05/31/2013  EFW  Added support for a dictionary action and an Ignore Once option
// 08/01/2015  EFW  Added support for multiple dictionary languages
//===============================================================================================================

using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;

using Microsoft.VisualStudio.Text;

namespace VisualStudio.SpellChecker.SuggestedActions
{
    /// <summary>
    /// Suggested action for ignoring words or adding new words to the dictionary
    /// </summary>
    internal class SpellDictionarySuggestedAction : SuggestedActionBase
    {
        #region Private data members
        //=====================================================================

        private readonly SpellingDictionary dictionary;
        private readonly DictionaryAction action;
        private readonly CultureInfo culture;

        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor for SpellDictionarySmartTagAction.
        /// </summary>
        /// <param name="span">The span containing the word to add or ignore.</param>
        /// <param name="dictionary">The dictionary used to ignore the word or add the word.</param>
        /// <param name="displayText">The display text for the suggested action.</param>
        /// <param name="action">The dictionary action to take.</param>
        /// <param name="culture">The culture of the dictionary on which to perform the action or null if it
        /// does not apply.</param>
        public SpellDictionarySuggestedAction(ITrackingSpan span, SpellingDictionary dictionary,
          string displayText, DictionaryAction action, CultureInfo culture) : base(displayText, span)
        {
            this.dictionary = dictionary;
            this.action = action;
            this.culture = culture;

            if(culture != null)
                this.DisplayTextSuffix = $"{culture.EnglishName} ({culture.Name})";

            if(String.IsNullOrEmpty(this.DisplayText))
            {
                this.DisplayText = !String.IsNullOrWhiteSpace(this.DisplayTextSuffix) ? this.DisplayTextSuffix :
                    "Add to Dictionary";
                this.DisplayTextSuffix = null;
            }
        }
        #endregion

        #region Abstract method implementations
        //=====================================================================

        /// <inheritdoc />
        public override void Invoke(CancellationToken cancellationToken)
        {
            bool succeeded;

            switch(action)
            {
                case DictionaryAction.IgnoreOnce:
                    dictionary.IgnoreWordOnce(this.Span);
                    succeeded = true;
                    break;

                case DictionaryAction.IgnoreAll:
                    succeeded = dictionary.IgnoreWord(this.Span.GetText(this.Span.TextBuffer.CurrentSnapshot));
                    break;

                default:
                    succeeded = dictionary.AddWordToDictionary(this.Span.GetText(
                        this.Span.TextBuffer.CurrentSnapshot), culture);
                    break;
            }

            Debug.Assert(succeeded, "Call to modify dictionary was unsuccessful");
        }
        #endregion
    }
}
