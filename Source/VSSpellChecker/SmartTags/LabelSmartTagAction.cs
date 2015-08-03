//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : LabelSmartTagAction.cs
// Authors : Eric Woodruff
// Updated : 07/28/2014
// Note    : Copyright 2014, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a class used to provide a label smart tag
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 07/28/2015  EFW  Created the code
//===============================================================================================================

using System.Collections.ObjectModel;

using Microsoft.VisualStudio.Language.Intellisense;

namespace VisualStudio.SpellChecker.SmartTags
{
    /// <summary>
    /// This smart tag acts as a label (disabled, no invocable action)
    /// </summary>
    internal class LabelSmartTagAction : ISmartTagAction
    {
        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="label">The display text</param>
        public LabelSmartTagAction(string label)
        {
            this.DisplayText = label;
        }
        #endregion

        #region ISmartTagAction members
        //=====================================================================

        /// <summary>
        /// Display text
        /// </summary>
        public string DisplayText { get; private set; }

        /// <summary>
        /// Icon to place next to the display text
        /// </summary>
        public System.Windows.Media.ImageSource Icon
        {
            get { return null; }
        }

        /// <summary>
        /// This smart tag does nothing
        /// </summary>
        public void Invoke()
        {
        }

        /// <summary>
        /// Always disabled
        /// </summary>
        public bool IsEnabled
        {
            get { return false; }
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
