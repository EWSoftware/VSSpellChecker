//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : SpellSuggestedAction.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 12/07/2016
// Note    : Copyright 2016, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a class used to provide a suggested action for inserting spelling suggestions
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 12/06/2016  EFW  Created the code
//===============================================================================================================

using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

using Microsoft.VisualStudio.Text;

using VisualStudio.SpellChecker.Definitions;

namespace VisualStudio.SpellChecker.SuggestedActions
{
    /// <summary>
    /// Smart tag action for inserting spelling suggestions
    /// </summary>
    internal class SpellSuggestedAction : SuggestedActionBase
    {
        #region Private data members
        //=====================================================================

        private ISpellingSuggestion replaceWith;
        private SpellingDictionary dictionary;

        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="span">The word span to replace</param>
        /// <param name="replaceWith">The suggestion to replace the misspelled word</param>
        /// <param name="dictionary">The dictionary used to perform the Replace All action</param>
        public SpellSuggestedAction(ITrackingSpan span, ISpellingSuggestion replaceWith,
          SpellingDictionary dictionary) : base(replaceWith.Suggestion.Replace("_", "__"), span)
        {
            this.replaceWith = replaceWith;
            this.dictionary = dictionary;

            // The preview is used to remind users that they can hold Ctrl when selecting this suggestion to
            // replace all instances of the word.
            this.Preview = new TextBlock(new Run { Text = "Hold Ctrl to replace all" }) { Padding = new Thickness(5) };
        }
        #endregion

        #region Abstract method implementations
        //=====================================================================

        /// <inheritdoc />
        public override void Invoke(CancellationToken cancellationToken)
        {
            if(dictionary != null && Keyboard.Modifiers == ModifierKeys.Control)
                dictionary.ReplaceAllOccurrences(this.Span.GetText(this.Span.TextBuffer.CurrentSnapshot), replaceWith);
            else
                this.Span.TextBuffer.Replace(this.Span.GetSpan(this.Span.TextBuffer.CurrentSnapshot), replaceWith.Suggestion);
        }
        #endregion
    }
}
