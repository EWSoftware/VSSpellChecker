//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : ConvertConfigurationControl.cs
// Authors : Eric Woodruff  (Eric@EWoodruff.us), Franz Alex Gaisie-Essilfie
// Updated : 03/20/2023
// Note    : Copyright 2023, Eric Woodruff, All rights reserved
//
// This file contains the user control that handles conversion of the old configuration files to .editorconfig
// settings.
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who   Comments
// ==============================================================================================================
// 03/16/2023  EFW   Created the code
//===============================================================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;

using PackageResources = VisualStudio.SpellChecker.Properties.Resources;

using VisualStudio.SpellChecker.Common.Configuration;
using VisualStudio.SpellChecker.Common.Configuration.Legacy;
using VisualStudio.SpellChecker.Common.EditorConfig;
using VisualStudio.SpellChecker.ProjectSpellCheck;

namespace VisualStudio.SpellChecker.ToolWindows
{
    /// <summary>
    /// This user control handles conversion of the old configuration files to .editorconfig settings
    /// </summary>
    public partial class ConvertConfigurationControl : UserControl
    {
        #region Private data members
        //=====================================================================

        private string solutionFolder;
        private List<ConvertedConfiguration> configFiles;
        private Dictionary<string, string> editorConfigFiles;

        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        public ConvertConfigurationControl()
        {
            InitializeComponent();

            this.UpdateState(null);
        }
        #endregion

        #region Helper methods
        //=====================================================================

        /// <summary>
        /// Update the state of the controls based on the currently loaded solution
        /// </summary>
        /// <param name="solutionFolder">The current solution folder or null if one is not loaded</param>
#pragma warning disable VSTHRD100
        public async void UpdateState(string solutionFolder)
        {
            if(this.solutionFolder == solutionFolder)
                return;

            this.solutionFolder = solutionFolder;

            lbEditorConfigFiles.Items.Clear();
            lbSpellCheckerConfigs.Items.Clear();
            fdConfiguration.Document.Blocks.Clear();

            if(!String.IsNullOrWhiteSpace(solutionFolder))
            {
                spSpinner.Visibility = tbProgress.Visibility = Visibility.Visible;
                btnConvertSelected.IsEnabled = btnConvertAll.IsEnabled = false;

                configFiles = await Task.Run(() => this.GenerateConfigurationFileChanges()).ConfigureAwait(true);
                editorConfigFiles = new Dictionary<string, string>();

                if(this.solutionFolder == solutionFolder)
                {
                    spSpinner.Visibility = tbProgress.Visibility = Visibility.Hidden;

                    if(configFiles.Any())
                    {
                        btnConvertSelected.IsEnabled = true;

                        // If multiple solution files are found.  There may be some overlap between the
                        // configurations used.  In such cases, converting individual files and/or merging
                        // changed manually is usually the better approach to avoid issues.
                        btnConvertAll.IsEnabled = configFiles.Count > 1 && configFiles.Count(
                            c => c.LegacyConfiguration.IsSolutionConfiguration) < 2;

                        foreach(var config in configFiles)
                        {
                            if(config.LegacyConfiguration.IsGlobalConfiguration)
                                lbSpellCheckerConfigs.Items.Add("Global Configuration");
                            else
                            {
                                lbSpellCheckerConfigs.Items.Add(
                                    config.LegacyConfiguration.LegacyConfigurationFilename.ToRelativePath(solutionFolder));
                            }
                        }

                        var first = configFiles.First();

                        if(first.LegacyConfiguration.IsGlobalConfiguration)
                        {
                            editorConfigFiles.Add("Global Configuration",
                                first.LegacyConfiguration.EditorConfigFilename);
                            lbEditorConfigFiles.Items.Add("Global Configuration");
                        }

                        foreach(var editorConfig in configFiles.Where(
                            c => !c.LegacyConfiguration.IsGlobalConfiguration).Select(
                            c => c.LegacyConfiguration.EditorConfigFilename).Distinct().OrderBy(
                            c => Path.GetDirectoryName(c)).ThenBy(c => c))
                        {
                            string relativePath = editorConfig.ToRelativePath(solutionFolder);

                            editorConfigFiles.Add(relativePath, editorConfig);
                            lbEditorConfigFiles.Items.Add(relativePath);
                        }

                        lbSpellCheckerConfigs.SelectedIndex = 0;
                    }
                }
            }
        }
#pragma warning restore VSTHRD100

        /// <summary>
        /// This generates a list of the spell checker configuration files and their converted settings
        /// </summary>
        /// <returns>A list of converted configuration files</returns>
        private List<ConvertedConfiguration> GenerateConfigurationFileChanges()
        {
            var convertedConfigurations = new List<ConvertedConfiguration>();

            try
            {
                if(File.Exists(SpellCheckerLegacyConfiguration.GlobalConfigurationFilename) &&
                  !File.Exists(SpellCheckerConfiguration.GlobalConfigurationFilename))
                {
                    convertedConfigurations.Add(new ConvertedConfiguration(SpellCheckerLegacyConfiguration.GlobalConfigurationFilename));
                }

                if(!String.IsNullOrWhiteSpace(solutionFolder))
                {
                    convertedConfigurations.AddRange(Directory.EnumerateFiles(solutionFolder, "*.vsspell",
                        SearchOption.AllDirectories).Select(c => new ConvertedConfiguration(c)));
                }

                // Make sure imported configuration files are included in case they are outside the solution folder
                foreach(var importedConfig in convertedConfigurations.Where(
                    c => !String.IsNullOrWhiteSpace(c.LegacyConfiguration.ImportSettingsFile)).Select(
                    c => Path.Combine(Path.GetDirectoryName(c.LegacyConfiguration.LegacyConfigurationFilename),
                         Path.ChangeExtension(c.LegacyConfiguration.ImportSettingsFile, ".vsspell"))).ToArray())
                {
                    if(!convertedConfigurations.Any(c => c.LegacyConfiguration.LegacyConfigurationFilename.Equals(
                      importedConfig, StringComparison.OrdinalIgnoreCase)))
                    {
                        convertedConfigurations.Add(new ConvertedConfiguration(importedConfig));
                    }
                }
            }
            catch(Exception ex)
            {
                // Ignore exceptions.  We'll try again later.
                System.Diagnostics.Debug.WriteLine(ex);
            }

            // Sort the old configurations by folder and by type to ensure they are applied in the proper order.
            // This prevents a file configuration getting above a folder configuration if it's name sorts
            // alphabetically before the containing folder name.
            return convertedConfigurations.OrderBy(c => c.LegacyConfiguration.IsGlobalConfiguration ? String.Empty :
                Path.GetDirectoryName(c.LegacyConfiguration.LegacyConfigurationFilename)).ThenBy(c =>
                {
                    return c.LegacyConfiguration.IsGlobalConfiguration ? 0 :
                        c.LegacyConfiguration.IsSolutionConfiguration ? 1 :
                        c.LegacyConfiguration.IsProjectConfiguration ? 2 :
                        c.LegacyConfiguration.IsFolderConfiguration ? 3 : 4;
                }).ThenBy(c => c.LegacyConfiguration.LegacyConfigurationFilename).ToList();
        }

        /// <summary>
        /// This is used to merge old spell checker configuration file settings into the given .editorconfig file
        /// </summary>
        /// <param name="editorConfigFilename">The .editorconfig file into which the settings are merged</param>
        /// <param name="oldFiles">An enumerable list of the old configuration files and their settings</param>
        /// <returns>An instance of the merged .editorconfig file ready to be saved</returns>
        private static EditorConfigFile MergePropertiesIntoEditorConfigFile(string editorConfigFilename,
          IEnumerable<ConvertedConfiguration> oldFiles)
        {
            EditorConfigFile editorConfigFile;

            // If the global configuration doesn't exit, use the template
            if(editorConfigFilename.Equals(SpellCheckerConfiguration.GlobalConfigurationFilename,
              StringComparison.OrdinalIgnoreCase) && !File.Exists(SpellCheckerConfiguration.GlobalConfigurationFilename))
            {
                editorConfigFile = EditorConfigFile.FromText(SpellCheckerConfiguration.DefaultGlobalConfiguration);
            }
            else
                editorConfigFile = EditorConfigFile.FromFile(editorConfigFilename);

            foreach(var oldFile in oldFiles)
            {
                foreach(var section in oldFile.Sections)
                {
                    EditorConfigSection matchingSection;
                    bool sectionExisted = false;

                    if(!section.IsFileSection)
                    {
                        // Preamble
                        matchingSection = editorConfigFile.Sections.FirstOrDefault(s => !s.IsFileSection);

                        if(matchingSection == null)
                        {
                            matchingSection = new EditorConfigSection();
                            editorConfigFile.Sections.Insert(0, matchingSection);
                        }
                        else
                            sectionExisted = true;
                    }
                    else
                    {
                        // File section
                        matchingSection = editorConfigFile[section.SectionHeader.FileGlob];

                        if(matchingSection == null)
                        {
                            matchingSection = new EditorConfigSection();

                            // Wildcard settings should typically always appear first
                            if(section.SectionHeader.FileGlob == "*")
                                editorConfigFile.Sections.Insert(0, matchingSection);
                            else
                                editorConfigFile.Sections.Add(matchingSection);
                        }
                        else
                            sectionExisted = true;
                    }

                    var existingProperties = matchingSection.SpellCheckerProperties.ToDictionary(
                        p => SpellCheckerConfiguration.PropertyNameForEditorConfigSetting(p.PropertyName), p => p);

                    bool propertiesAdded = false;

                    // Remove blanks at the end of the section if they immediately follow the section header
                    while(matchingSection.SectionLines.Count > 1 &&
                      matchingSection.SectionLines[matchingSection.SectionLines.Count - 1].LineType == LineType.Blank &&
                      matchingSection.SectionLines[matchingSection.SectionLines.Count - 2].LineType == LineType.SectionHeader)
                    {
                        matchingSection.SectionLines.RemoveAt(matchingSection.SectionLines.Count - 1);
                    }

                    foreach(var line in section.SectionLines)
                    {
                        // Skip the section header if merging into an existing section
                        if(line.LineType != LineType.SectionHeader || !sectionExisted)
                        {
                            if(line.LineType != LineType.Property)
                                matchingSection.SectionLines.Add(new SectionLine(line.LineText) { Tag = true });
                            else
                            {
                                // Merge existing properties as best we can
                                string configPropName = SpellCheckerConfiguration.PropertyNameForEditorConfigSetting(
                                    line.PropertyName);

                                if(!existingProperties.TryGetValue(configPropName, out var property))
                                {
                                    matchingSection.SectionLines.Add(new SectionLine(line.LineText) { Tag = true });
                                    propertiesAdded = true;
                                }
                                else
                                {
                                    if(property.PropertyValue != line.PropertyValue)
                                    {
                                        // Overwrite single-instance properties.  For multi-instance properties,
                                        // add the duplicate and let the property editor sort it out later.  The
                                        // section ID suffix will be unique so they'll still get merged in the
                                        // usual manner.
                                        if(!SpellCheckerConfiguration.EditorConfigSettingsFor(
                                          configPropName).CanHaveMultipleInstances)
                                        {
                                            // Skip section ID properties.  The existing one will be retained.
                                            if(configPropName != nameof(SpellCheckerConfiguration.SectionId))
                                            {
                                                property.PropertyValue = line.PropertyValue;
                                                property.Tag = true;
                                            }
                                        }
                                        else
                                        {
                                            matchingSection.SectionLines.Add(new SectionLine(line.LineText) { Tag = true });
                                            propertiesAdded = true;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if(!propertiesAdded)
                    {
                        // If no properties were added because they all matched existing ones, remove any
                        // obsolete comments.
                        while(matchingSection.SectionLines.Count != 0 &&
                          (matchingSection.SectionLines[matchingSection.SectionLines.Count - 1].LineText.StartsWith(
                            "# VSSPELL:", StringComparison.OrdinalIgnoreCase) ||
                          matchingSection.SectionLines[matchingSection.SectionLines.Count - 1].LineText.StartsWith(
                            "# TODO: Review imported", StringComparison.OrdinalIgnoreCase)))
                        {
                            matchingSection.SectionLines.RemoveAt(matchingSection.SectionLines.Count - 1);
                        }
                    }
                    else
                        matchingSection.SectionLines.Add(new SectionLine());
                }
            }

            return editorConfigFile;
        }

        /// <summary>
        /// This is used to convert the given old configuration files to the equivalent .editorconfig settings
        /// </summary>
        /// <param name="oldFiles">An enumerable list of the old configuration files to convert</param>
        private void ConvertOldConfigurationFiles(IEnumerable<ConvertedConfiguration> oldFiles)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                var dte2 = Utility.GetServiceFromPackage<DTE2, SDTE>(true);
                EditorConfigFile editorConfig;

                foreach(var oldFile in oldFiles)
                {
                    // The global configuration is handled separately
                    if(oldFile.LegacyConfiguration.IsGlobalConfiguration)
                    {
                        editorConfig = MergePropertiesIntoEditorConfigFile(oldFile.LegacyConfiguration.EditorConfigFilename,
                            new[] { oldFile });

                        editorConfig.Filename = SpellCheckerConfiguration.GlobalConfigurationFilename;
                        editorConfig.Save();
                        continue;
                    }

                    string relativePath = oldFile.LegacyConfiguration.LegacyConfigurationFilename.ToRelativePath(solutionFolder);
                    var oldFileItem = dte2.Solution.FindProjectItemForFile(oldFile.LegacyConfiguration.LegacyConfigurationFilename);

                    if(oldFileItem == null && MessageBox.Show($"The old configuration file {relativePath} is not " +
                      "in the loaded solution.  Do you want to convert it anyway?  You will need to manually " +
                      "update containing solution and project if you do.", PackageResources.PackageTitle,
                      MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.No)
                    {
                        continue;
                    }

                    editorConfig = MergePropertiesIntoEditorConfigFile(oldFile.LegacyConfiguration.EditorConfigFilename,
                        new[] { oldFile });

                    var existingConfig = dte2.Solution.FindProjectItemForFile(editorConfig.Filename);

                    if(existingConfig == null)
                    {
                        if(oldFileItem != null)
                        {
                            editorConfig.Save();
                            oldFileItem.ProjectItems.AddFromFile(editorConfig.Filename);
                            oldFileItem.Delete();
                        }
                    }
                    else
                    {
                        if(dte2.SourceControl.IsItemUnderSCC(editorConfig.Filename) &&
                          !dte2.SourceControl.IsItemCheckedOut(editorConfig.Filename))
                        {
                            if(!dte2.SourceControl.CheckOutItem(editorConfig.Filename))
                                continue;
                        }

                        editorConfig.Save();
                        oldFileItem?.Delete();
                    }

                    // The file may still exist even though we deleted it from the project so remove it from the
                    // file system too.
                    if(File.Exists(oldFile.LegacyConfiguration.LegacyConfigurationFilename))
                    {
                        File.SetAttributes(oldFile.LegacyConfiguration.LegacyConfigurationFilename, FileAttributes.Normal);
                        File.Delete(oldFile.LegacyConfiguration.LegacyConfigurationFilename);
                    }
                }
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);

                MessageBox.Show("Unexpected error trying to convert old spell checker configuration files: " +
                    ex.Message, PackageResources.PackageTitle, MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }
        #endregion

        #region Event handlers
        //=====================================================================

        /// <summary>
        /// View help for this tool window
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void cmdHelp_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(
                    "https://ewsoftware.github.io/VSSpellChecker/html/d9dc230f-ae34-464b-a3c2-4a7778907fc9.htm");
            }
            catch(Exception ex)
            {
                MessageBox.Show("Unable to navigate to website.  Reason: " + ex.Message,
                    PackageResources.PackageTitle, MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }

        /// <summary>
        /// Convert the selected configuration files to .editorconfig file settings
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnConvertSelected_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            var selectedConfigs = new List<ConvertedConfiguration>();

            foreach(object item in lbSpellCheckerConfigs.SelectedItems)
                selectedConfigs.Add(configFiles[lbSpellCheckerConfigs.Items.IndexOf(item)]);

            this.ConvertOldConfigurationFiles(selectedConfigs);
            this.btnRefresh_Click(sender, e);
        }

        /// <summary>
        /// Convert all of the configuration files to .editorconfig file settings
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnConvertAll_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            this.ConvertOldConfigurationFiles(configFiles);
            this.btnRefresh_Click(sender, e);
        }

        /// <summary>
        /// Update the selected .editorconfig file when the selected legacy configuration is selected
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void lbSpellCheckerConfigs_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if(lbSpellCheckerConfigs.SelectedIndex != -1)
            {
                var config = configFiles[lbSpellCheckerConfigs.SelectedIndex];

                if(config.LegacyConfiguration.IsGlobalConfiguration)
                    lbEditorConfigFiles.SelectedIndex = 0;
                else
                {
                    int idx = lbEditorConfigFiles.Items.IndexOf(
                        config.LegacyConfiguration.EditorConfigFilename.ToRelativePath(solutionFolder));

                    lbEditorConfigFiles.SelectedIndex = idx;
                    lbEditorConfigFiles.ScrollIntoView(lbEditorConfigFiles.Items[idx]);
                }
            }
        }

        /// <summary>
        /// When an .editorconfig file is selected, show the merged changes
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void lbEditorConfigFiles_SelectionChanged(object sender, RoutedEventArgs e)
        {
            Section docSection;
            var doc = fdConfiguration.Document;

            doc.Blocks.Clear();

            try
            {
                string editorConfigFilename = editorConfigFiles[(string)lbEditorConfigFiles.SelectedItem];
                var oldFiles = configFiles.Where(c => c.LegacyConfiguration.EditorConfigFilename.Equals(
                    editorConfigFilename, StringComparison.OrdinalIgnoreCase)).ToList();

                var editorConfigFile = MergePropertiesIntoEditorConfigFile(editorConfigFilename, oldFiles);
                docSection = new Section();

                var para = new Paragraph();

                foreach(var section in editorConfigFile.Sections)
                {
                    foreach(var line in section.SectionLines)
                    {
                        if(line.Tag == null)
                            para.Inlines.Add(new Run(line.LineText));
                        else
                            para.Inlines.Add(new Bold(new Run(line.LineText)));

                        para.Inlines.Add(new LineBreak());
                    }
                }

                docSection.Blocks.Add(para);
                doc.Blocks.Add(docSection);
            }
            catch(Exception ex)
            {
                docSection = new Section();

                docSection.Blocks.Add(new Paragraph(new Run("Unable to create merged configuration file.  Reason(s):")));
                docSection.Blocks.Add(new Paragraph(new Run(ex.Message)));

                while(ex.InnerException != null)
                {
                    ex = ex.InnerException;
                    docSection.Blocks.Add(new Paragraph(new Run(ex.Message)));
                }

                doc.Blocks.Add(docSection);
            }
        }

        /// <summary>
        /// Refresh the configuration conversion information
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            string folder = solutionFolder;

            solutionFolder = null;

            this.UpdateState(folder);
        }
        #endregion
    }
}
