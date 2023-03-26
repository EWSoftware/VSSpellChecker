﻿//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : SpellConfigurationEditorControl.xaml.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 03/26/2023
// Note    : Copyright 2015-2023, Eric Woodruff, All rights reserved
//
// This file contains a user control used to edit spell checker configuration settings files
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 04/14/2013  EFW  Created the code
// 06/09/2014  EFW  Reworked to use a tree view and user controls for the various configuration categories
// 02/01/2015  EFW  Refactored the configuration settings to allow for solution and project specific settings
// 02/07/2015  EFW  Moved the code into a user control hosted within a Visual Studio editor pane
//===============================================================================================================

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Microsoft.VisualStudio.Shell;

using VisualStudio.SpellChecker.Configuration;
using VisualStudio.SpellChecker.Editors.Pages;
using PackageResources = VisualStudio.SpellChecker.Properties.Resources;

namespace VisualStudio.SpellChecker.Editors
{
    /// <summary>
    /// This user control is used to edit spell checker configuration settings in .editorconfig files
    /// </summary>
    /// <remarks>Since all settings files are .editorconfig files, this can be used to edit the global
    /// configuration as well as any project-specific settings files.</remarks>
    public partial class SpellingConfigurationEditorControl : UserControl
    {
        #region Private data members
        //=====================================================================

        private SpellingConfigurationFile configFile;
        private bool isGlobalConfig;

        #endregion

        #region Events
        //=====================================================================

        /// <summary>
        /// This is raised to let the parent know that the configuration changed
        /// </summary>
        public event EventHandler ConfigurationChanged;

        /// <summary>
        /// This is called to raise the <see cref="ConfigurationChanged"/> event
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void OnConfigurationChanged(object sender, EventArgs e)
        {
            ConfigurationChanged?.Invoke(sender, e);
        }
        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        public SpellingConfigurationEditorControl()
        {
            ISpellCheckerConfiguration page;
            TreeViewItem node;

            InitializeComponent();

            // The property pages will be listed in this order
            Type[] propertyPages = new[] {
                typeof(FileInfoUserControl),
                typeof(ImportSettingsUserControl),
                typeof(DictionarySettingsUserControl),
                typeof(GeneralSettingsUserControl),
                typeof(CSharpOptionsUserControl),
                typeof(IgnoredClassificationsUserControl),
                typeof(IgnoredWordsUserControl),
                typeof(ExclusionExpressionsUserControl),
                typeof(IgnoredFilePatternsUserControl),
                typeof(XmlFilesUserControl),
                typeof(CodeAnalysisUserControl),
                // Global only, should always be last
                typeof(VisualStudioUserControl)
            };

            try
            {
                tvPages.BeginInit();

                // Create the property pages
                foreach(Type pageType in propertyPages)
                {
                    page = (ISpellCheckerConfiguration)Activator.CreateInstance(pageType);
                    page.Control.Visibility = Visibility.Collapsed;
                    page.ConfigurationChanged += this.OnConfigurationChanged;

                    node = new TreeViewItem
                    {
                        Header = page.Title,
                        Name = pageType.Name,
                        Tag = page
                    };

                    tvPages.Items.Add(node);
                    pnlPages.Children.Add(page.Control);
                }
            }
            finally
            {
                tvPages.EndInit();

                if(tvPages.Items.Count != 0)
                    ((TreeViewItem)tvPages.Items[0]).IsSelected = true;
            }
        }
        #endregion

        #region Helper methods
        //=====================================================================

        /// <summary>
        /// This is used to load the configuration file to edit
        /// </summary>
        /// <param name="configurationFile">The configuration filename</param>
        public void LoadConfiguration(string configurationFile)
        {
            configFile = new SpellingConfigurationFile(configurationFile, null);

            isGlobalConfig = configurationFile.Equals(Common.Configuration.SpellCheckerConfiguration.GlobalConfigurationFilename,
                StringComparison.OrdinalIgnoreCase);

            this.SetTitle();

            foreach(TreeViewItem item in tvPages.Items)
            {
                ISpellCheckerConfiguration page = ((ISpellCheckerConfiguration)item.Tag);

                if(!(page is VisualStudioUserControl) || !isGlobalConfig)
                {
                    item.Visibility = Visibility.Visible;
                    page.LoadConfiguration(configFile);
                }
                else
                    item.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Save changes to the configuration
        /// </summary>
        /// <param name="configurationFile">The configuration filename</param>
        public void SaveConfiguration(string configurationFile)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            configFile.Filename = configurationFile;

            this.SetTitle();

            foreach(TreeViewItem item in tvPages.Items)
            {
                ISpellCheckerConfiguration page = (ISpellCheckerConfiguration)item.Tag;

                if(item.Visibility == Visibility.Visible)
                    page.SaveConfiguration(configFile);
            }

            if(configFile.Save())
            {
                if(isGlobalConfig)
                {
                    if(configFile.ToBoolean(PropertyNames.EnableWpfTextBoxSpellChecking))
                        VSSpellCheckEverywherePackage.Instance?.ConnectSpellChecker();

                    WpfTextBox.WpfTextBoxSpellChecker.ClearCache();
                }
            }
            else
                MessageBox.Show("Unable to save spell checking configuration", PackageResources.PackageTitle,
                    MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }

        /// <summary>
        /// Set the title based on the configuration file type
        /// </summary>
        private void SetTitle()
        {
            string configFilename = configFile.Filename;

            if(isGlobalConfig)
                lblTitle.Text = "Global Spell Checker Configuration Settings";
            else
            {
                if(!String.IsNullOrWhiteSpace(SpellingServiceProxy.LastSolutionName))
                    configFilename = configFilename.ToRelativePath(Path.GetDirectoryName(SpellingServiceProxy.LastSolutionName));
                else
                {
                    if(configFilename.Length > 50)
                        configFilename = "..." + configFilename.Substring(50);
                }

                lblTitle.Text = String.Format(CultureInfo.CurrentCulture, "Spell Checker Settings from {0}", configFilename);
            }
        }
        #endregion

        #region Event handlers
        //=====================================================================

        /// <summary>
        /// View the project website
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void lnkProjectSite_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(lnkProjectSite.NavigateUri.AbsoluteUri);
            }
            catch(Exception ex)
            {
                MessageBox.Show("Unable to navigate to website.  Reason: " + ex.Message,
                    PackageResources.PackageTitle, MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }

        /// <summary>
        /// View help for the selected property category
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void cmdHelp_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            TreeViewItem item = (TreeViewItem)tvPages.SelectedItem;

            if(item != null)
            {
                ISpellCheckerConfiguration page = (ISpellCheckerConfiguration)item.Tag;

                try
                {
                    string targetUrl = "https://ewsoftware.github.io/VSSpellChecker/html/" + page.HelpUrl + ".htm";

                    Process.Start(targetUrl);
                }
                catch(Exception ex)
                {
                    MessageBox.Show("Unable to navigate to website.  Reason: " + ex.Message,
                        PackageResources.PackageTitle, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
            }
        }

        /// <summary>
        /// Reset the configuration for the current page or the whole file to the default settings excluding the
        /// user dictionary.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Do you want to reset the entire configuration or just this page?  " +
              "Click YES to reset the whole configuration, NO to reset just this page, or CANCEL to do neither.",
              PackageResources.PackageTitle, MessageBoxButton.YesNoCancel, MessageBoxImage.Question,
              MessageBoxResult.Cancel);

            if(result == MessageBoxResult.Cancel)
                return;

            // Pass a dummy filename to create a new configuration and then set the filename so that the pages
            // know the type of configuration file in use.
            var newConfigFile = new SpellingConfigurationFile("__ResetTemp__", new SpellCheckerConfiguration())
            {
                Filename = configFile.Filename
            };

            if(result == MessageBoxResult.Yes)
            {
                if(MessageBox.Show("Are you sure you want to reset the configuration to its default settings " +
                  "(excluding the user dictionary)?", PackageResources.PackageTitle, MessageBoxButton.YesNo,
                  MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes)
                {
                    foreach(TreeViewItem item in tvPages.Items)
                    {
                        ISpellCheckerConfiguration page = (ISpellCheckerConfiguration)item.Tag;

                        if(item.Visibility == Visibility.Visible)
                            page.LoadConfiguration(newConfigFile);
                    }

                    this.OnConfigurationChanged(sender, e);
                }
            }
            else
            {
                TreeViewItem item = (TreeViewItem)tvPages.SelectedItem;

                if(item != null)
                {
                    ((ISpellCheckerConfiguration)item.Tag).LoadConfiguration(newConfigFile);
                    this.OnConfigurationChanged(sender, e);
                }
            }
        }

        /// <summary>
        /// Change the displayed property page based on the selected tree view item
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void tvPages_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            foreach(TreeViewItem item in tvPages.Items)
            {
                ISpellCheckerConfiguration page = (ISpellCheckerConfiguration)item.Tag;

                if(item.IsSelected)
                    page.Control.Visibility = Visibility.Visible;
                else
                    page.Control.Visibility = Visibility.Collapsed;
            }
        }
        #endregion
    }
}
