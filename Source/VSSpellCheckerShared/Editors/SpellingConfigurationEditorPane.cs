//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : SpellingConfigurationEditorPane.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 04/11/2023
// Note    : Copyright 2015-2023, Eric Woodruff, All rights reserved
//
// This file contains a class used to host the spelling configuration file editor control
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 02/06/2015  EFW  Created the code
//===============================================================================================================

using System;

using Microsoft.VisualStudio.Shell.Interop;

namespace VisualStudio.SpellChecker.Editors
{
    /// <summary>
    /// This is used to host the spelling configuration file editor control
    /// </summary>
    public class SpellingConfigurationEditorPane : SimpleEditorPane<SpellingConfigurationEditorFactory,
      SpellingConfigurationEditorControl>
    {
        #region Properties
        //=====================================================================

        /// <summary>
        /// This is used to get or set the default file glob (section) to select when the file is opened
        /// </summary>
        public static string DefaultFileGlob { get; set; }
        
        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        public SpellingConfigurationEditorPane()
        {
            this.UIControl.ConfigurationChanged += ucSpellingConfigurationEditor_ConfigurationChanged;
        }
        #endregion

        #region Abstract method implementations
        //=====================================================================

        /// <inheritdoc />
        protected override string GetFileExtension()
        {
            return ".editorconfig";
        }

        /// <inheritdoc />
        protected override Guid GetCommandSetGuid()
        {
            return Guid.Empty;
        }

        /// <inheritdoc />
        protected override void LoadFile(string fileName)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

#pragma warning disable VSTHRD010
            this.UIControl.LoadConfiguration(fileName, DefaultFileGlob);
#pragma warning restore VSTHRD010
        }

        /// <inheritdoc />
        protected override void SaveFile(string fileName)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            Utility.GetServiceFromPackage<IVsUIShell, SVsUIShell>(true).SetWaitCursor();

            if(this.IsDirty || !fileName.Equals(this.UIControl.Filename, StringComparison.OrdinalIgnoreCase))
                this.UIControl.SaveConfiguration(fileName);
        }
        #endregion

        #region General event handlers
        //=====================================================================

        /// <summary>
        /// This is used to mark the file as dirty when the content changes
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void ucSpellingConfigurationEditor_ConfigurationChanged(object sender, EventArgs e)
        {
            this.OnContentChanged();
        }
        #endregion
    }
}
