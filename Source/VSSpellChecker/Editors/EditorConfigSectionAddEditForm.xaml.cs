//===============================================================================================================
// System  : Spell Check My Code Package
// File    : EditorConfigSectionAddEditForm.xaml.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 04/13/2023
// Note    : Copyright 2023, Eric Woodruff, All rights reserved
//
// This file contains a window used to add or edit an .editorconfig section's file glob and spell checker
// configuration comments.
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 04/13/2023  EFW  Created the code
//===============================================================================================================

using System;
using System.Windows;

using GlobExpressions;

using PackageResources = VisualStudio.SpellChecker.Properties.Resources;

namespace VisualStudio.SpellChecker.Editors.Pages
{
    /// <summary>
    /// This form is used to add or edit an .editorconfig section's file glob and spell checker configuration
    /// comments.
    /// </summary>
    public partial class EditorConfigSectionAddEditForm : Window
    {
        #region Properties
        //=====================================================================

        /// <summary>
        /// This is used to get or set the file glob for the section
        /// </summary>
        public string FileGlob
        {
            get => txtFileGlob.Text;
            set => txtFileGlob.Text = value;
        }

        /// <summary>
        /// This is used to get or set the spell checker settings comments for the section
        /// </summary>
        public string Comments
        {
            get => txtComment.Text;
            set => txtComment.Text = value;
        }
        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="fileGlobIsEditable">True if the file glob is editable, false if this is a non-file
        /// section and it is not (.globalconfig)</param>
        public EditorConfigSectionAddEditForm(bool fileGlobIsEditable)
        {
            InitializeComponent();

            txtFileGlob.IsEnabled = fileGlobIsEditable;
        }
        #endregion

        #region Event handlers
        //=====================================================================

        /// <summary>
        /// Save the regular expression if it is valid
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            txtFileGlob.Text = txtFileGlob.Text.Trim();

            if(txtFileGlob.IsEnabled && txtFileGlob.Text.Length == 0)
            {
                MessageBox.Show("A file glob is required", PackageResources.PackageTitle,
                    MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            try
            {
                var g = new Glob(txtFileGlob.Text);

                _ = g.IsMatch("Test.cs");

                this.DialogResult = true;
            }
            catch(Exception ex)
            {
                MessageBox.Show($"The file glob is not valid.\r\n\r\nFile Glob: {txtFileGlob.Text}\r\nError: {ex.Message}",
                    PackageResources.PackageTitle, MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }
        #endregion
    }
}
