//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : SpellConfigurationEditorControl.xaml.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 06/15/2023
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
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Microsoft.VisualStudio.Shell;

using VisualStudio.SpellChecker.Common;
using VisualStudio.SpellChecker.Common.Configuration;
using VisualStudio.SpellChecker.Common.EditorConfig;
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

        private EditorConfigFile configFile;
        private BindingList<SectionInfo> sections;
        private bool isGlobalConfig, suppressSectionChange, suppressChangeNotification;
        private int lastSelectedSection;

        #endregion

        #region Properties
        //=====================================================================

        /// <summary>
        /// This read-only property returns the filename
        /// </summary>
        public string Filename => configFile?.Filename ?? String.Empty;

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
            if(!suppressChangeNotification)
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

            lastSelectedSection = -1;

            // The property pages will be listed in this order
            Type[] propertyPages = new[] {
                typeof(FileInfoUserControl),
                typeof(GeneralSettingsUserControl),
                typeof(CodeAnalyzerOptionsUserControl),
                typeof(DictionarySettingsUserControl),
                typeof(IgnoredWordsUserControl),
                typeof(ExclusionExpressionsUserControl),
                typeof(XmlFilesUserControl),
                typeof(CodeAnalysisDictionaryUserControl),
                typeof(IgnoredClassificationsUserControl),
                typeof(ImportSettingsUserControl),
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
        /// <param name="defaultFileGlob">The default file glob to select or add if not present</param>
        public void LoadConfiguration(string configurationFile, string defaultFileGlob)
        {
            isGlobalConfig = configurationFile.Equals(SpellCheckerConfiguration.GlobalConfigurationFilename,
                StringComparison.OrdinalIgnoreCase);

            configFile = EditorConfigFile.FromFile(configurationFile);

            this.SetTitle();

            if(configFile.IsGlobal)
            {
                // There should only be one non-file section in a .globalconfig file
                sections = new BindingList<SectionInfo>(configFile.Sections.Select(s => new SectionInfo(s)).ToList());
                btnAddSection.IsEnabled = false;
            }
            else
            {
                sections = new BindingList<SectionInfo>(configFile.Sections.Where(s => s.IsFileSection).Select(
                    s => new SectionInfo(s)).ToList());
                btnAddSection.IsEnabled = true;
            }

            var vsItem = tvPages.Items.Cast<TreeViewItem>().FirstOrDefault(p => p.Tag is VisualStudioUserControl);

            if(vsItem != null)
                vsItem.Visibility = isGlobalConfig ? Visibility.Visible : Visibility.Collapsed;

            lbSections.ItemsSource = sections;

            if(defaultFileGlob != null && !configFile.IsGlobal)
            {
                var match = sections.FirstOrDefault(s => s.Section.SectionHeader.FileGlob.Equals(defaultFileGlob));

                if(match != null)
                    lbSections.SelectedIndex = sections.IndexOf(match);
                else
                {
                    var newSection = new EditorConfigSection(new[]
                    {
                        new SectionLine($"[{defaultFileGlob}]"),
                    });

                    configFile.Sections.Add(newSection);
                    sections.Add(new SectionInfo(newSection));

                    this.OnConfigurationChanged(this, EventArgs.Empty);

                    lbSections.SelectedIndex = lbSections.Items.Count - 1;
                }
            }
            else
            {
                if(lbSections.Items.Count != 0)
                    lbSections.SelectedIndex = 0;
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

            // Force an update of the current section's properties
            this.lbSections_SelectionChanged(this, new RoutedEventArgs());

            try
            {
                configFile.Save();

                if(isGlobalConfig)
                {
                    if(Utility.ToPropertyState(null, nameof(SpellCheckerConfiguration.EnableWpfTextBoxSpellChecking),
                      true) == PropertyState.Yes)
                    {
                        VSSpellCheckEverywherePackage.Instance?.ConnectSpellChecker();
                    }

                    WpfTextBox.WpfTextBoxSpellChecker.ClearCache();
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("Unable to save spell checker configuration settings: " + ex.Message,
                    PackageResources.PackageTitle, MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
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

                lblTitle.Text = $"Spell Checker Settings from {configFilename}";
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
        /// Add a new file section
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnAddSection_Click(object sender, RoutedEventArgs e)
        {
            var form = new EditorConfigSectionAddEditForm(true);

            if(form.ShowDialog() ?? false)
            {
                var newSection = new EditorConfigSection(new[]
                {
                    new SectionLine($"[{form.FileGlob}]"),
                });

                if(!String.IsNullOrWhiteSpace(form.Comments))
                    newSection.SectionLines.Add(new SectionLine($"# VSSPELL: {form.Comments}"));

                configFile.Sections.Add(newSection);
                sections.Add(new SectionInfo(newSection));

                this.OnConfigurationChanged(sender, e);
            }
        }

        /// <summary>
        /// Delete the selected file section
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnDeleteSection_Click(object sender, RoutedEventArgs e)
        {
            if(lbSections.SelectedIndex != -1 && MessageBox.Show("Are you sure you want to delete the selected " +
              "section from the file?", PackageResources.PackageTitle, MessageBoxButton.YesNo,
              MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes)
            {
                int idx = lbSections.SelectedIndex;

                lastSelectedSection = -1;

                sections.RemoveAt(idx);

                // Adjust the index when there is a root section
                if(configFile.Sections[0].IsRoot)
                    configFile.Sections.RemoveAt(idx + 1);
                else
                    configFile.Sections.RemoveAt(idx);

                if(lbSections.Items.Count <= idx)
                    idx--;

                if(lbSections.Items.Count > idx)
                    lbSections.SelectedIndex = idx;

                this.OnConfigurationChanged(sender, e);
            }
        }

        /// <summary>
        /// Edit the file glob and comments
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnEditGlob_Click(object sender, RoutedEventArgs e)
        {
            if(lbSections.SelectedIndex != -1)
            {
                var section = sections[lbSections.SelectedIndex];
                var configSection = section.Section;

                var form = new EditorConfigSectionAddEditForm(section.Section.IsFileSection)
                {
                    FileGlob = configSection.SectionHeader?.FileGlob ?? String.Empty,
                    Comments = section.Comments
                };

                if(form.ShowDialog() ?? false)
                {
                    if(configSection.IsFileSection)
                        configSection.SectionHeader.FileGlob = form.FileGlob;

                    var currentComments = section.Section.SpellCheckerComments.ToList();

                    if(currentComments.Count > 1)
                    {
                        foreach(var c in currentComments.Skip(1))
                            configSection.SectionLines.Remove(c);
                    }

                    if(!String.IsNullOrWhiteSpace(form.Comments))
                    {
                        if(currentComments.Count == 0)
                            configSection.SectionLines.Add(new SectionLine($"# VSSPELL: {form.Comments}"));
                        else
                            currentComments[0].LineText = $"# VSSPELL: {form.Comments}";
                    }
                    else
                        configSection.SectionLines.Remove(currentComments[0]);

                    section.RefreshSectionDescription();

                    this.OnConfigurationChanged(sender, e);
                }
            }
        }

        /// <summary>
        /// Move the currently selected section up/down
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnMoveUpDown_Click(object sender, RoutedEventArgs e)
        {
            if(lbSections.SelectedIndex != -1)
            {
                int currentIndex = lbSections.SelectedIndex,
                    newIndex = (sender == btnMoveUp) ? currentIndex - 1 : currentIndex + 1;
                var sectionToMove = sections[currentIndex];

                suppressSectionChange = true;
                lbSections.SelectedIndex = -1;

                sections.RemoveAt(currentIndex);
                sections.Insert(newIndex, sectionToMove);

                // Adjust the index when there is a root section
                if(configFile.Sections[0].IsRoot)
                {
                    configFile.Sections.RemoveAt(currentIndex + 1);
                    configFile.Sections.Insert(newIndex + 1, sectionToMove.Section);
                }
                else
                {
                    configFile.Sections.RemoveAt(currentIndex);
                    configFile.Sections.Insert(newIndex, sectionToMove.Section);
                }

                suppressSectionChange = false;

                lbSections.SelectedIndex = newIndex;

                this.OnConfigurationChanged(sender, e);
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
            var result = MessageBox.Show("Do you want to reset the entire configuration for the selected " +
              "section or just the selected page?  Click YES to reset the whole configuration, NO to reset " +
              "just this page, or CANCEL to do neither.", PackageResources.PackageTitle,
              MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Cancel);

            if(result == MessageBoxResult.Cancel)
                return;

            var noProperties = new Dictionary<string, SpellCheckPropertyInfo>();

            if(result == MessageBoxResult.Yes)
            {
                if(MessageBox.Show("Are you sure you want to reset the configuration for the selected section " +
                  "to its default settings (excluding the user dictionary)?", PackageResources.PackageTitle,
                  MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes)
                {
                    foreach(TreeViewItem item in tvPages.Items)
                    {
                        ISpellCheckerConfiguration page = (ISpellCheckerConfiguration)item.Tag;

                        if(item.Visibility == Visibility.Visible)
                            page.LoadConfiguration(isGlobalConfig, noProperties);
                    }
                }
            }
            else
            {
                TreeViewItem item = (TreeViewItem)tvPages.SelectedItem;

                if(item != null)
                    ((ISpellCheckerConfiguration)item.Tag).LoadConfiguration(isGlobalConfig, noProperties);
            }

            // Force an update of the current section's properties
            this.lbSections_SelectionChanged(btnReset, new RoutedEventArgs());

            this.OnConfigurationChanged(sender, e);
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

        /// <summary>
        /// Store changes to the prior selected section, load settings for the new selected section, and update
        /// the state of the section buttons.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void lbSections_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if(suppressSectionChange)
                return;

            if(lastSelectedSection != -1)
            {
                // Get changes to the previous selected section and save them
                var lastSection = sections[lastSelectedSection].Section;
                var lastSectionProperties = new List<(string PropertyName, string PropertyValue)>();
                bool hasChanges = false;
                string sectionIdProperty = SpellCheckerConfiguration.EditorConfigSettingsFor(
                    nameof(SpellCheckerConfiguration.SectionId)).PropertyName;

                // Get the current section ID or assign a new one if it doesn't already exist
                var sectionId = lastSection.SpellCheckerProperties.FirstOrDefault(
                    p => p.PropertyName.Equals(sectionIdProperty,
                        StringComparison.OrdinalIgnoreCase))?.PropertyValue;
                
                if(String.IsNullOrWhiteSpace(sectionId))
                    sectionId = Guid.NewGuid().ToString("N");

                lastSectionProperties.Add((sectionIdProperty, sectionId));
                sectionId = "_" + sectionId;

                foreach(TreeViewItem item in tvPages.Items)
                {
                    ISpellCheckerConfiguration page = (ISpellCheckerConfiguration)item.Tag;

                    // We will get the changed properties from all pages but only save them if one or more
                    // actually has changes.
                    if(item.Visibility == Visibility.Visible && lastSelectedSection != -1)
                    {
                        if(page.HasChanges)
                            hasChanges = true;

                        lastSectionProperties.AddRange(page.ChangedProperties(isGlobalConfig, sectionId));
                    }
                }

                if(hasChanges || sender == btnReset)
                {
                    // Remove the section ID if it isn't used
                    if(!lastSectionProperties.Skip(1).Any(p => p.PropertyName.EndsWith(sectionId,
                      StringComparison.OrdinalIgnoreCase)))
                    {
                        lastSectionProperties.RemoveAt(0);
                    }

                    // Clear the current spell checker setting and add the new ones if any
                    foreach(var oldProperty in lastSection.SpellCheckerProperties.ToArray())
                        lastSection.SectionLines.Remove(oldProperty);

                    // Remove blank lines at the end of the section
                    while(lastSection.SectionLines.Count > 0 &&
                      lastSection.SectionLines[lastSection.SectionLines.Count - 1].LineType == LineType.Blank)
                    {
                        lastSection.SectionLines.RemoveAt(lastSection.SectionLines.Count - 1);
                    }

                    // If all properties are at their default value, there won't be any properties to store
                    foreach(var changedProp in lastSectionProperties)
                    {
                        lastSection.SectionLines.Add(new SectionLine(
                            $"{changedProp.PropertyName} = {changedProp.PropertyValue.EscapeEditorConfigValue()}"));
                    }

                    lastSection.SectionLines.Add(new SectionLine());
                }
            }

            lastSelectedSection = lbSections.SelectedIndex;

            if(lastSelectedSection != -1)
            {
                // Load properties for the new section
                var newSection = sections[lastSelectedSection];
                var newSectionProperties = new Dictionary<string, SpellCheckPropertyInfo>();

                // There could be duplicate properties.  If so, the last one found is used
                foreach(var scp in newSection.Section.SpellCheckerProperties)
                {
                    var scpi = new SpellCheckPropertyInfo(scp.PropertyName, scp.PropertyValue);

                    newSectionProperties[scpi.ConfigurationPropertyName] = scpi;
                }

                suppressChangeNotification = true;

                foreach(TreeViewItem item in tvPages.Items)
                {
                    ISpellCheckerConfiguration page = (ISpellCheckerConfiguration)item.Tag;

                    if(item.Visibility == Visibility.Visible)
                    {
                        page.ConfigurationFilename = configFile.Filename;
                        page.LoadConfiguration(isGlobalConfig, newSectionProperties);
                    }
                }

                suppressChangeNotification = false;

                btnDeleteSection.IsEnabled = !newSection.ContainsOtherSettings;
                btnMoveUp.IsEnabled = lbSections.SelectedIndex != 0;
                btnMoveDown.IsEnabled = lbSections.SelectedIndex != lbSections.Items.Count - 1;
                grdSettings.IsEnabled = btnEditGlob.IsEnabled = btnReset.IsEnabled = true;
            }
            else
            {
                grdSettings.IsEnabled = btnDeleteSection.IsEnabled = btnMoveUp.IsEnabled =
                    btnMoveDown.IsEnabled = btnEditGlob.IsEnabled = btnReset.IsEnabled = false;
            }
        }
        #endregion
    }
}
