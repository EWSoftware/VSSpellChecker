//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : ImportSettingsUserControl.xaml.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 10/05/2018
// Note    : Copyright 2018, Eric Woodruff, All rights reserved
//
// This file contains a user control used to specify a file from which to import settings
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 10/04/2018  EFW  Created the code
//===============================================================================================================

// Ignore Spelling: vsspell

using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

using VisualStudio.SpellChecker.Configuration;
using PackageResources = VisualStudio.SpellChecker.Properties.Resources;
using WinForms = System.Windows.Forms;

namespace VisualStudio.SpellChecker.Editors.Pages
{
    /// <summary>
    /// This page provides a way to specify a file from which to import settings
    /// </summary>
    public partial class ImportSettingsUserControl : UserControl, ISpellCheckerConfiguration
    {
        #region Private data members
        //=====================================================================

        private bool isGlobal;
        private string configFilePath;

        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        public ImportSettingsUserControl()
        {
            InitializeComponent();

            tbFileNotFound.Visibility = Visibility.Collapsed;
        }
        #endregion

        #region ISpellCheckerConfiguration Members
        //=====================================================================

        /// <inheritdoc />
        public UserControl Control => this;

        /// <inheritdoc />
        public string Title => "Import Settings";

        /// <inheritdoc />
        public string HelpUrl => "b156d5ad-347f-4f63-89dc-4f945953ae41";

        /// <inheritdoc />
        public void LoadConfiguration(SpellingConfigurationFile configuration)
        {
            isGlobal = configuration.ConfigurationType == ConfigurationType.Global;
            configFilePath = Path.GetDirectoryName(configuration.Filename);

            tbGlobal.Visibility = isGlobal ? Visibility.Visible : Visibility.Collapsed;
            tbOther.Visibility = !isGlobal ? Visibility.Visible : Visibility.Collapsed;

            txtImportSettingsFile.Text = configuration.ToString(PropertyNames.ImportSettingsFile);
        }

        /// <inheritdoc />
        public void SaveConfiguration(SpellingConfigurationFile configuration)
        {
            configFilePath = Path.GetDirectoryName(configuration.Filename);

            string filename = txtImportSettingsFile.Text.Trim();

            if(filename.Length == 0)
                filename = null;

            configuration.StoreProperty(PropertyNames.ImportSettingsFile, filename);
        }

        /// <inheritdoc />
        public event EventHandler ConfigurationChanged;

        #endregion

        #region Event handlers
        //=====================================================================

        /// <summary>
        /// Select the settings file to import
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnSelectFile_Click(object sender, RoutedEventArgs e)
        {
            using(WinForms.OpenFileDialog dlg = new WinForms.OpenFileDialog())
            {
                dlg.Title = "Select Configuration File to Import";
                dlg.Filter = "Spell Checker Configuration Files (*.vsspell)|*.vsspell";
                dlg.DefaultExt = "vsspell";
                dlg.InitialDirectory = isGlobal ? Directory.GetCurrentDirectory() : configFilePath;

                if(dlg.ShowDialog() == WinForms.DialogResult.OK)
                {
                    txtImportSettingsFile.Text = dlg.FileName;
                    txtImportSettingsFile_LostFocus(sender, e);

                    if(!isGlobal && MessageBox.Show("Would you like to make the path relative to the current " +
                      "configuration file?", PackageResources.PackageTitle, MessageBoxButton.YesNo,
                      MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes)
                    {
                        txtImportSettingsFile.Text = txtImportSettingsFile.Text.ToRelativePath(configFilePath);
                    }
                }
            }
        }

        /// <summary>
        /// Show the File Not Found indicator as needed
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void txtImportSettingsFile_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                string filename = txtImportSettingsFile.Text.Trim();

                if(filename.Length == 0)
                    tbFileNotFound.Visibility = Visibility.Collapsed;
                else
                {
                    if(filename.IndexOf('%') != -1)
                        filename = Environment.ExpandEnvironmentVariables(filename);

                    if(!Path.IsPathRooted(filename))
                        filename = Path.GetFullPath(Path.Combine(configFilePath, filename));

                    tbFileNotFound.Visibility = File.Exists(filename) ? Visibility.Collapsed : Visibility.Visible;
                }
            }
            catch(Exception ex)
            {
                // Ignore exceptions
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        /// <summary>
        /// Notify the parent of property changes that affect the file's dirty state
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void Property_Changed(object sender, RoutedEventArgs e)
        {
            this.ConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }
        #endregion
    }
}
