//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : SpellDictionarySmartTagAction.cs
// Authors : Noah Richards, Roman Golovin, Michael Lehenbauer, Eric Woodruff
// Updated : 06/06/2014
// Note    : Copyright 2010-2014, Microsoft Corporation, All rights reserved
//           Portions Copyright 2013-2014, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a class used to provide a smart tag actions for ignoring words or adding new words to the
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
//===============================================================================================================

using System.Collections.ObjectModel;
using System.Diagnostics;

using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace VisualStudio.SpellChecker.SmartTags
{
    /// <summary>
    /// Smart tag action for ignoring words or adding new words to the dictionary
    /// </summary>
    internal class SpellDictionarySmartTagAction : ISmartTagAction
    {
        #region Private data members
        //=====================================================================

        private ITrackingSpan span;
        private SpellingDictionary dictionary;
        private DictionaryAction action;
        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor for SpellDictionarySmartTagAction.
        /// </summary>
        /// <param name="word">The word to add or ignore.</param>
        /// <param name="dictionary">The dictionary used to ignore the word or add the word.</param>
        /// <param name="displayText">Text to show in the context menu for this action.</param>
        /// <param name="action">The action to take.</param>
        public SpellDictionarySmartTagAction(ITrackingSpan span, SpellingDictionary dictionary,
          string displayText, DictionaryAction action)
        {
            this.span = span;
            this.dictionary = dictionary;
            this.action = action;
            this.DisplayText = displayText;
        }
        #endregion

        #region ISmartTagAction implementation
        //=====================================================================

        /// <summary>
        /// Text to display in the context menu.
        /// </summary>
        public string DisplayText { get; private set; }

        /// <summary>
        /// Icon to place next to the display text.
        /// </summary>
        public System.Windows.Media.ImageSource Icon
        {
            get { return null; }
        }

        /// <summary>
        /// This method is executed when action is selected in the context menu.
        /// </summary>
        public void Invoke()
        {
            bool succeeded;

            switch(action)
            {
                case DictionaryAction.IgnoreOnce:
                    dictionary.IgnoreWordOnce(span);
                    succeeded = true;
                    break;

                case DictionaryAction.IgnoreAll:
                    succeeded = dictionary.IgnoreWord(span.GetText(span.TextBuffer.CurrentSnapshot));
                    break;

                default:
                    succeeded = dictionary.AddWordToDictionary(span.GetText(span.TextBuffer.CurrentSnapshot));
                    break;
            }

            Debug.Assert(succeeded, "Call to modify dictionary was unsuccessful");
        }

        /// <summary>
        /// Enable/disable this action.
        /// </summary>
        public bool IsEnabled
        {
            get { return true; }
        }

        /// <summary>
        /// This smart tag has no action sets
        /// </summary>
        public ReadOnlyCollection<SmartTagActionSet> ActionSets
        {
            get { return null; }
        }
        #endregion
    }
}
