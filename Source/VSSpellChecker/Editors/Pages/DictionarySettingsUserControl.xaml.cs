//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : DictionarySettingsUserControl.xaml.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 04/21/2015
// Note    : Copyright 2014-2015, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a user control used to edit the spell checker dictionary settings
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 06/10/2014  EFW  Moved the language and user dictionary settings to a user control
//===============================================================================================================

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

using Microsoft.Win32;

using PackageResources = VisualStudio.SpellChecker.Properties.Resources;
using FolderBrowserDlg = System.Windows.Forms.FolderBrowserDialog;

using VisualStudio.SpellChecker.Configuration;

namespace VisualStudio.SpellChecker.Editors.Pages
{
    /// <summary>
    /// This user control is used to edit the spell checker dictionary settings
    /// </summary>
    public partial class DictionarySettingsUserControl : UserControl, ISpellCheckerConfiguration
    {
        #region Private data members
        //=====================================================================

        private string configFilePath;
        private bool isGlobal;
        private CultureInfo defaultLang;

        #endregion

        #region Constructor
        //=====================================================================

        public DictionarySettingsUserControl()
        {
            InitializeComponent();
        }
        #endregion

        #region ISpellCheckerConfiguration Members
        //=====================================================================

        /// <inheritdoc />
        public UserControl Control
        {
            get { return this; }
        }

        /// <inheritdoc />
        public string Title
        {
            get { return "Dictionary Settings"; }
        }

        /// <inheritdoc />
        public string HelpUrl
        {
            get { return "af34b863-6a1c-41ed-bcf2-48a714686519"; }
        }

        /// <inheritdoc />
        public void LoadConfiguration(SpellingConfigurationFile configuration)
        {
            IEnumerable<string> folders;
            lbAdditionalFolders.Items.Clear();

            var dataSource = new List<PropertyState>();

            if(configuration.ConfigurationType != ConfigurationType.Global)
                dataSource.AddRange(new[] { PropertyState.Inherited, PropertyState.Yes, PropertyState.No });
            else
                dataSource.AddRange(new[] { PropertyState.Yes, PropertyState.No });

            cboDetermineResxLang.ItemsSource = dataSource;

            cboDetermineResxLang.SelectedValue = configuration.ToPropertyState(
                PropertyNames.DetermineResourceFileLanguageFromName);

            configFilePath = Path.GetDirectoryName(configuration.Filename);
            isGlobal = configuration.ConfigurationType == ConfigurationType.Global;

            if(isGlobal)
            {
                chkInheritAdditionalFolders.IsChecked = false;
                chkInheritAdditionalFolders.Visibility = Visibility.Collapsed;
            }
            else
                chkInheritAdditionalFolders.IsChecked = configuration.ToBoolean(
                    PropertyNames.InheritAdditionalDictionaryFolders);

            if(configuration.HasProperty(PropertyNames.AdditionalDictionaryFolders))
            {
                folders = configuration.ToValues(PropertyNames.AdditionalDictionaryFolders,
                    PropertyNames.AdditionalDictionaryFoldersItem);
            }
            else
                folders = Enumerable.Empty<string>();

            foreach(string f in folders)
                lbAdditionalFolders.Items.Add(f);

            var sd = new SortDescription { Direction = ListSortDirection.Ascending };

            lbAdditionalFolders.Items.SortDescriptions.Add(sd);

            if(configuration.HasProperty(PropertyNames.DefaultLanguage) || isGlobal)
                defaultLang = configuration.ToCultureInfo(PropertyNames.DefaultLanguage);

            this.LoadAvailableLanguages();
        }

        /// <inheritdoc />
        public void SaveConfiguration(SpellingConfigurationFile configuration)
        {
            HashSet<string> newList = null;

            configuration.StoreProperty(PropertyNames.DetermineResourceFileLanguageFromName,
                ((PropertyState)cboDetermineResxLang.SelectedValue).ToPropertyValue());

            configFilePath = Path.GetDirectoryName(configuration.Filename);
            isGlobal = configuration.ConfigurationType == ConfigurationType.Global;

            if(lbAdditionalFolders.Items.Count != 0)
                newList = new HashSet<string>(lbAdditionalFolders.Items.OfType<string>(),
                    StringComparer.OrdinalIgnoreCase);

            if(!isGlobal)
                configuration.StoreProperty(PropertyNames.InheritAdditionalDictionaryFolders,
                    chkInheritAdditionalFolders.IsChecked);

            configuration.StoreValues(PropertyNames.AdditionalDictionaryFolders,
                PropertyNames.AdditionalDictionaryFoldersItem, newList);

            if(cboDefaultLanguage.SelectedIndex == 0 && !isGlobal)
                configuration.StoreProperty(PropertyNames.DefaultLanguage, null);
            else
                configuration.StoreProperty(PropertyNames.DefaultLanguage,
                    ((SpellCheckerDictionary)cboDefaultLanguage.SelectedItem).Culture.Name);
        }

        /// <inheritdoc />
        public event EventHandler ConfigurationChanged;

        #endregion

        #region Helper methods
        //=====================================================================

        /// <summary>
        /// This loads the list of available dictionary languages
        /// </summary>
        /// <remarks>This will find all dictionaries in the global configuration folder as well as any additional
        /// folders specified in this configuration file.</remarks>
        private void LoadAvailableLanguages()
        {
            List<SpellCheckerDictionary> availableDictionaries = new List<SpellCheckerDictionary>();

            if(cboDefaultLanguage.Items.Count != 0)
                defaultLang = ((SpellCheckerDictionary)cboDefaultLanguage.SelectedItem).Culture;

            if(!isGlobal)
                availableDictionaries.Add(new SpellCheckerDictionary(CultureInfo.InvariantCulture, null, null, null));

            List<string> additionalFolders = new List<string>();

            // Fully qualify relative paths with the configuration file path
            foreach(string folder in lbAdditionalFolders.Items.OfType<string>())
                if(Path.IsPathRooted(folder))
                    additionalFolders.Add(folder);
                else
                    additionalFolders.Add(Path.GetFullPath(Path.Combine(configFilePath, folder)));

            foreach(var lang in SpellCheckerDictionary.AvailableDictionaries(additionalFolders).Values.OrderBy(
              d => d.ToString()))
            {
                availableDictionaries.Add(lang);
            }

            cboDefaultLanguage.ItemsSource = availableDictionaries;

            if(defaultLang != null)
            {
                var match = cboDefaultLanguage.Items.OfType<SpellCheckerDictionary>().FirstOrDefault(
                    d => d.Culture.Name == defaultLang.Name);

                if(match != null)
                    cboDefaultLanguage.SelectedItem = match;
                else
                    cboDefaultLanguage.SelectedIndex = 0;
            }
            else
                cboDefaultLanguage.SelectedIndex = 0;
        }
        #endregion

        #region Event handlers
        //=====================================================================

        /// <summary>
        /// Select a folder to add
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnSelectFolder_Click(object sender, RoutedEventArgs e)
        {
            using(FolderBrowserDlg dlg = new FolderBrowserDlg())
            {
                dlg.Description = "Select an additional dictionary folder";
                dlg.SelectedPath = !isGlobal && Directory.Exists(configFilePath) ? configFilePath :
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                if(dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    txtAdditionalFolder.Text = dlg.SelectedPath;
                    btnAddFolder_Click(sender, e);
                }
            }
        }

        /// <summary>
        /// Add a new additional folder to the list
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnAddFolder_Click(object sender, RoutedEventArgs e)
        {
            txtAdditionalFolder.Text = txtAdditionalFolder.Text.Trim();

            if(txtAdditionalFolder.Text.Length != 0)
            {
                if(!isGlobal && MessageBox.Show("Would you like to make the path relative to the current " +
                  "configuration file?", PackageResources.PackageTitle, MessageBoxButton.YesNo,
                  MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes)
                {
                    txtAdditionalFolder.Text = txtAdditionalFolder.Text.ToRelativePath(configFilePath);

                    if(txtAdditionalFolder.Text.Length == 0 || txtAdditionalFolder.Text[0] != '.')
                        txtAdditionalFolder.Text = ".\\" + txtAdditionalFolder.Text;
                }

                string folder = txtAdditionalFolder.Text;

                if(!Path.IsPathRooted(folder))
                    folder = Path.Combine(configFilePath, folder);

                if(Directory.Exists(folder))
                {
                    lbAdditionalFolders.Items.Add(txtAdditionalFolder.Text);
                    txtAdditionalFolder.Text = null;
                    Property_Changed(sender, e);

                    this.LoadAvailableLanguages();
                }
                else
                    MessageBox.Show("The specified folder does not appear to exist", PackageResources.PackageTitle,
                        MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }

        /// <summary>
        /// Remove the selected additional folder from the list
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnRemoveFolder_Click(object sender, RoutedEventArgs e)
        {
            int idx = lbAdditionalFolders.SelectedIndex;

            if(idx != -1)
                lbAdditionalFolders.Items.RemoveAt(idx);

            if(lbAdditionalFolders.Items.Count != 0)
            {
                if(idx < 0)
                    idx = 0;
                else
                    if(idx >= lbAdditionalFolders.Items.Count)
                        idx = lbAdditionalFolders.Items.Count - 1;

                lbAdditionalFolders.SelectedIndex = idx;
            }

            Property_Changed(sender, e);

            this.LoadAvailableLanguages();
        }

        /// <summary>
        /// Clear the list of additional folders
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnClearFolders_Click(object sender, RoutedEventArgs e)
        {
            lbAdditionalFolders.Items.Clear();

            var sd = new SortDescription { Direction = ListSortDirection.Ascending };
            lbAdditionalFolders.Items.SortDescriptions.Add(sd);

            Property_Changed(sender, e);

            this.LoadAvailableLanguages();
        }

        /// <summary>
        /// Load the user dictionary file when the selected language changes
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void cboDefaultLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            lbUserDictionary.Items.Clear();
            grpUserDictionary.IsEnabled = false;

            if(cboDefaultLanguage.Items.Count != 0 && cboDefaultLanguage.SelectedItem != null)
            {
                if(cboDefaultLanguage.SelectedItem.ToString() != "Inherited")
                {
                    string filename = ((SpellCheckerDictionary)cboDefaultLanguage.SelectedItem).UserDictionaryFilePath;

                    grpUserDictionary.IsEnabled = true;

                    if(File.Exists(filename))
                        try
                        {
                            foreach(string word in File.ReadAllLines(filename))
                                lbUserDictionary.Items.Add(word);
                        }
                        catch(Exception ex)
                        {
                            MessageBox.Show("Unable to load user dictionary.  Reason: " + ex.Message,
                                PackageResources.PackageTitle, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        }
                        finally
                        {
                            var sd = new SortDescription { Direction = ListSortDirection.Ascending };
                            lbUserDictionary.Items.SortDescriptions.Add(sd);
                        }
                }

                Property_Changed(sender, e);
            }
        }

        /// <summary>
        /// Remove the selected word from the user dictionary
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnRemoveDictionaryWord_Click(object sender, RoutedEventArgs e)
        {
            int idx = lbUserDictionary.SelectedIndex;
            string word = null;

            if(idx != -1)
            {
                word = (string)lbUserDictionary.Items[idx];
                lbUserDictionary.Items.RemoveAt(idx);
            }

            if(lbUserDictionary.Items.Count != 0)
            {
                if(idx < 0)
                    idx = 0;
                else
                    if(idx >= lbUserDictionary.Items.Count)
                        idx = lbUserDictionary.Items.Count - 1;

                lbUserDictionary.SelectedIndex = idx;
            }

            try
            {
                var selectedDictionary = (SpellCheckerDictionary)cboDefaultLanguage.SelectedItem;

                if(selectedDictionary.UserDictionaryFilePath.CanWriteToUserWordsFile(
                  selectedDictionary.DictionaryFilePath, VSSpellCheckerPackage.Instance))
                {
                    File.WriteAllLines(selectedDictionary.UserDictionaryFilePath,
                        lbUserDictionary.Items.OfType<string>());

                    if(!String.IsNullOrWhiteSpace(word))
                        GlobalDictionary.RemoveWord(selectedDictionary.Culture, word);

                    GlobalDictionary.LoadUserDictionaryFile(selectedDictionary.Culture);
                }
                else
                    MessageBox.Show("Unable to save user dictionary.  The file could not be added to the " +
                        "project, could not be checked out, or is read-only", PackageResources.PackageTitle,
                        MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            catch(Exception ex)
            {
                MessageBox.Show("Unable to save user dictionary.  Reason: " + ex.Message,
                    PackageResources.PackageTitle, MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }

        /// <summary>
        /// Import words from a text file
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnImportDictionary_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();

            dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            dlg.Filter = "Dictionary Files (*.dic)|*.dic|Text documents (.txt)|*.txt|All Files (*.*)|*.*";
            dlg.CheckPathExists = dlg.CheckFileExists = true;

            if((dlg.ShowDialog() ?? false))
            {
                try
                {
                    // Parse words based on the common word break characters and add unique instances to the
                    // user dictionary if not already there excluding those containing digits and those less than
                    // three characters in length.
                    var uniqueWords = File.ReadAllText(dlg.FileName).Split(new[] { ',', '/', '<', '>', '?', ';',
                        ':', '\"', '[', ']', '\\', '{', '}', '|', '-', '=', '+', '~', '!', '#', '$', '%', '^',
                        '&', '*', '(', ')', ' ', '_', '.', '\'', '@', '\t', '\r', '\n' },
                        StringSplitOptions.RemoveEmptyEntries)
                            .Except(lbUserDictionary.Items.OfType<string>())
                            .Distinct()
                            .Where(w => w.Length > 2 && w.IndexOfAny(
                                new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' }) == -1).ToList();

                    try
                    {
                        var selectedDictionary = (SpellCheckerDictionary)cboDefaultLanguage.SelectedItem;

                        if(selectedDictionary.UserDictionaryFilePath.CanWriteToUserWordsFile(
                          selectedDictionary.DictionaryFilePath, VSSpellCheckerPackage.Instance))
                        {
                            File.WriteAllLines(selectedDictionary.UserDictionaryFilePath, uniqueWords);

                            GlobalDictionary.LoadUserDictionaryFile(selectedDictionary.Culture);

                            cboDefaultLanguage_SelectionChanged(sender, new SelectionChangedEventArgs(
                                e.RoutedEvent, new object[] { }, new object[] { }));
                        }
                        else
                            MessageBox.Show("Unable to save user dictionary.  The file could not be added to " +
                                "the project, could not be checked out, or is read-only",
                                PackageResources.PackageTitle, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    }
                    catch(Exception ex)
                    {
                        MessageBox.Show("Unable to save user dictionary.  Reason: " + ex.Message,
                            PackageResources.PackageTitle, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    }
                }
                catch(Exception ex)
                {
                    MessageBox.Show(String.Format(CultureInfo.CurrentCulture, "Unable to load user dictionary " +
                        "from '{0}'.  Reason: {1}", dlg.FileName, ex.Message), PackageResources.PackageTitle,
                        MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
            }
        }

        /// <summary>
        /// Export words to a text file
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnExportDictionary_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();

            dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            dlg.FileName = "UserDictionary.dic";
            dlg.DefaultExt = ".dic";
            dlg.Filter = "Dictionary Files (*.dic)|*.dic|Text documents (.txt)|*.txt|All Files (*.*)|*.*";

            if((dlg.ShowDialog() ?? false))
            {
                try
                {
                    File.WriteAllLines(dlg.FileName, lbUserDictionary.Items.OfType<string>());
                }
                catch(Exception ex)
                {
                    MessageBox.Show(String.Format(CultureInfo.CurrentCulture, "Unable to save user dictionary " +
                        "to '{0}'.  Reason: {1}", dlg.FileName, ex.Message), PackageResources.PackageTitle,
                        MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
            }
        }

        /// <summary>
        /// Notify the parent of property changes that affect the file's dirty state
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void Property_Changed(object sender, System.Windows.RoutedEventArgs e)
        {
            var handler = ConfigurationChanged;

            if(handler != null)
                handler(this, EventArgs.Empty);
        }
        #endregion
    }
}
