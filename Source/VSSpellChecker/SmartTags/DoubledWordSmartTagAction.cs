//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : DoubledWordSmartTagAction.cs
// Authors : Eric Woodruff
// Updated : 06/06/2014
// Note    : Copyright 2014, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a class used to provide a smart tag action for deleting doubled words
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 06/06/2014  EFW  Created the code
//===============================================================================================================

using System;
using System.Collections.ObjectModel;
using System.Windows.Input;

using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace VisualStudio.SpellChecker.SmartTags
{
    /// <summary>
    /// Smart tag action for deleting doubled words
    /// </summary>
    internal class DoubledWordSmartTagAction : ISmartTagAction
    {
        #region Private data members
        //=====================================================================

        private ITrackingSpan span;
        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor for doubled word deletion smart tag action
        /// </summary>
        /// <param name="span">The word span to delete</param>
        public DoubledWordSmartTagAction(ITrackingSpan span)
        {
            this.span = span;
        }
        #endregion

        #region ISmartTagAction members
        //=====================================================================

        /// <summary>
        /// Display text
        /// </summary>
        public string DisplayText
        {
            get { return "Delete word"; }
        }

        /// <summary>
        /// Icon to place next to the display text
        /// </summary>
        public System.Windows.Media.ImageSource Icon
        {
            get { return null; }
        }

        /// <summary>
        /// This method is executed when action is selected in the context menu
        /// </summary>
        public void Invoke()
        {
            span.TextBuffer.Replace(span.GetSpan(span.TextBuffer.CurrentSnapshot), String.Empty);
        }

        /// <summary>
        /// Always enabled unless a span is not specified
        /// </summary>
        public bool IsEnabled
        {
            get { return (span != null); }
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
