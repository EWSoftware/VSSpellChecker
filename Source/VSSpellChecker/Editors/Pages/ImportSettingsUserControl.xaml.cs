//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : ImportSettingsUserControl.xaml.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 04/17/2023
// Note    : Copyright 2018-2023, Eric Woodruff, All rights reserved
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

// Ignore Spelling: editorconfig

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;

using Microsoft.Win32;

using PackageResources = VisualStudio.SpellChecker.Properties.Resources;

using VisualStudio.SpellChecker.Common;
using VisualStudio.SpellChecker.Common.Configuration;

namespace VisualStudio.SpellChecker.Editors.Pages
{
    /// <summary>
    /// This page provides a way to specify a file from which to import settings
    /// </summary>
    public partial class ImportSettingsUserControl : UserControl, ISpellCheckerConfiguration
    {
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
        public string ConfigurationFilename { get; set; }

        /// <inheritdoc />
        public bool HasChanges { get; private set; }

        /// <inheritdoc />
        public void LoadConfiguration(bool isGlobal, IDictionary<string, SpellCheckPropertyInfo> properties)
        {
            tbGlobal.Visibility = isGlobal ? Visibility.Visible : Visibility.Collapsed;
            tbOther.Visibility = tbParentConfigs.Visibility = !isGlobal ? Visibility.Visible : Visibility.Collapsed;

            if(properties.TryGetValue(nameof(SpellCheckerConfiguration.ImportSettingsFile), out var spi))
                txtImportSettingsFile.Text = spi.EditorConfigPropertyValue;

            this.HasChanges = false;
        }

        /// <inheritdoc />
        public IEnumerable<(string PropertyName, string PropertyValue)> ChangedProperties(bool isGlobal,
          string sectionId)
        {
            string filename = txtImportSettingsFile.Text.Trim();

            if(filename.Length != 0)
            {
                yield return (SpellCheckerConfiguration.EditorConfigSettingsFor(
                    nameof(SpellCheckerConfiguration.ImportSettingsFile)).PropertyName + sectionId, filename);
            }
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
            string configFilePath = Path.GetDirectoryName(this.ConfigurationFilename);
            bool isGlobal = configFilePath.Equals(SpellCheckerConfiguration.GlobalConfigurationFilePath,
                StringComparison.OrdinalIgnoreCase);

            OpenFileDialog dlg = new()
            {
                Title = "Select a Configuration File to Import",
                InitialDirectory = isGlobal ? Directory.GetCurrentDirectory() : configFilePath,
                Filter = ".editorconfig Files (*.editorconfig)|*.editorconfig",
            };

            if(dlg.ShowDialog() ?? false)
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

        /// <summary>
        /// Show the File Not Found indicator as needed
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void txtImportSettingsFile_LostFocus(object sender, RoutedEventArgs e)
        {
            string configFilePath = Path.GetDirectoryName(this.ConfigurationFilename);

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
            this.HasChanges = true;
            this.ConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }
        #endregion
    }
}
