//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : IgnoredWordsUserControl.xaml.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 08/30/2025
// Note    : Copyright 2014-2025, Eric Woodruff, All rights reserved
//
// This file contains a user control used to edit the ignored words and ignored keywords spell checker
// configuration settings.
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 06/09/2014  EFW  Moved the ignored words settings to a user control
//===============================================================================================================

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;

using VisualStudio.SpellChecker.Common;
using VisualStudio.SpellChecker.Common.Configuration;

using PackageResources = VisualStudio.SpellChecker.Properties.Resources;

namespace VisualStudio.SpellChecker.Editors.Pages
{
    /// <summary>
    /// This user control is used to edit the ignored words and ignored keywords spell checker configuration
    /// settings.
    /// </summary>
    public partial class IgnoredWordsUserControl : UserControl, ISpellCheckerConfiguration
    {
        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        public IgnoredWordsUserControl()
        {
            InitializeComponent();
        }
        #endregion

        #region ISpellCheckerConfiguration Members
        //=====================================================================

        /// <inheritdoc />
        public UserControl Control => this;

        /// <inheritdoc />
        public string Title => "Ignored Words/Keywords";

        /// <inheritdoc />
        public string HelpUrl => "c592c4d8-7387-47fe-9b79-28bf0168f447";

        /// <inheritdoc />
        public string ConfigurationFilename { get; set; }

        /// <inheritdoc />
        public bool HasChanges { get; private set; }

        /// <inheritdoc />
        public void LoadConfiguration(bool isGlobal, IDictionary<string, SpellCheckPropertyInfo> properties)
        {
            var words = new List<string>();

            lbIgnoredWords.Items.Clear();
            lbIgnoredKeywords.Items.Clear();
            txtIgnoredWordsFile.Text = null;

            if(isGlobal)
            {
                chkInheritIgnoredWords.IsChecked = false;
                chkInheritIgnoredWords.Visibility = Visibility.Collapsed;
            }
            else
                chkInheritIgnoredWords.IsChecked = true;

            if(properties.TryGetValue(nameof(SpellCheckerConfiguration.IgnoredWords), out var spi))
            {
                words.AddRange(spi.EditorConfigPropertyValue.Split(['|'], StringSplitOptions.RemoveEmptyEntries));

                if(words.Count != 0 && words[0].Equals(SpellCheckerConfiguration.ClearInherited,
                  StringComparison.OrdinalIgnoreCase))
                {
                    chkInheritIgnoredWords.IsChecked = false;
                    words.RemoveAt(0);
                }

                if(words.Count != 0 && words[0].StartsWith(SpellCheckerConfiguration.FilePrefix,
                  StringComparison.OrdinalIgnoreCase))
                {
                    txtIgnoredWordsFile.Text = words[0].Substring(5);
                    words.RemoveAt(0);
                }

                foreach(string el in words)
                    lbIgnoredWords.Items.Add(el);
            }

            // For the global configuration, the default ignored words file should always be used if an override
            // is not specified.
            if(isGlobal)
            {
                if(String.IsNullOrWhiteSpace(txtIgnoredWordsFile.Text))
                    txtIgnoredWordsFile.Text = "IgnoredWords.dic";
                else
                {
                    if(Path.GetDirectoryName(txtIgnoredWordsFile.Text).Equals(
                      SpellCheckerConfiguration.GlobalConfigurationFilePath, StringComparison.OrdinalIgnoreCase))
                    {
                        txtIgnoredWordsFile.Text = Path.GetFileName(txtIgnoredWordsFile.Text);
                    }
                }
            }

            if(properties.TryGetValue(nameof(SpellCheckerConfiguration.IgnoredKeywords), out spi))
            {
                words.Clear();
                words.AddRange(spi.EditorConfigPropertyValue.Split(['|'], StringSplitOptions.RemoveEmptyEntries));

                foreach(string el in words)
                    lbIgnoredKeywords.Items.Add(el);
            }

            var sd = new SortDescription { Direction = ListSortDirection.Ascending };

            lbIgnoredWords.Items.SortDescriptions.Add(sd);
            lbIgnoredKeywords.Items.SortDescriptions.Add(sd);

            this.HasChanges = false;
        }

        /// <inheritdoc />
        public IEnumerable<(string PropertyName, string PropertyValue)> ChangedProperties(bool isGlobal,
          string sectionId)
        {
            var newWordList = new List<string>();
            string configFilePath = Path.GetDirectoryName(this.ConfigurationFilename),
                ignoredWordsFile = txtIgnoredWordsFile.Text.Trim();

            // For the global configuration, don't store the ignored words filename if it has no path and
            // is the default name.  This prevents it getting added to every section.
            if(isGlobal && ignoredWordsFile == "IgnoredWords.dic")
                ignoredWordsFile = null;

            if(lbIgnoredWords.Items.Count != 0 || (!isGlobal && !chkInheritIgnoredWords.IsChecked.Value) ||
              !String.IsNullOrWhiteSpace(ignoredWordsFile))
            {
                newWordList.AddRange(lbIgnoredWords.Items.Cast<string>());

                // The ignored words file always appears first with a prefix to identify it as such
                if(!String.IsNullOrWhiteSpace(ignoredWordsFile))
                {
                    // For the global configuration, always fully qualify the path
                    if(isGlobal && !Path.IsPathRooted(ignoredWordsFile))
                        ignoredWordsFile = Path.GetFullPath(Path.Combine(configFilePath, ignoredWordsFile));

                    newWordList.Insert(0, SpellCheckerConfiguration.FilePrefix + ignoredWordsFile);

                    if(!Path.IsPathRooted(ignoredWordsFile))
                        ignoredWordsFile = Path.GetFullPath(Path.Combine(configFilePath, ignoredWordsFile));

                    try
                    {
                        // Create the file if it doesn't exist and add it to the project if possible if it looks
                        // like it is within the project folder structure and isn't in the project already.
                        if(!File.Exists(ignoredWordsFile) ||
                          Path.GetDirectoryName(ignoredWordsFile).StartsWith(configFilePath, StringComparison.OrdinalIgnoreCase) ||
                          configFilePath.StartsWith(Path.GetDirectoryName(ignoredWordsFile), StringComparison.OrdinalIgnoreCase))
                        {
#pragma warning disable VSTHRD010
                            _ = ignoredWordsFile.CanWriteToUserWordsFile(this.ConfigurationFilename, false);
#pragma warning restore VSTHRD010
                        }
                    }
                    catch(Exception ex)
                    {
                        // Ignore exceptions.  The user will have to create it and add it to the project.
                        System.Diagnostics.Debug.WriteLine(ex);
                    }
                }

                if(!isGlobal && !chkInheritIgnoredWords.IsChecked.Value)
                    newWordList.Insert(0, SpellCheckerConfiguration.ClearInherited);

                if(newWordList.Count != 0)
                {
                    yield return (SpellCheckerConfiguration.EditorConfigSettingsFor(
                        nameof(SpellCheckerConfiguration.IgnoredWords)).PropertyName + sectionId,
                            String.Join("|", newWordList));
                }
            }

            if(lbIgnoredKeywords.Items.Count != 0)
            {
                newWordList.Clear();
                newWordList.AddRange(lbIgnoredKeywords.Items.Cast<string>());

                if(newWordList.Count != 0)
                {
                    yield return (SpellCheckerConfiguration.EditorConfigSettingsFor(
                        nameof(SpellCheckerConfiguration.IgnoredKeywords)).PropertyName + sectionId,
                            String.Join("|", newWordList));
                }
            }
        }

        /// <inheritdoc />
        public event EventHandler ConfigurationChanged;

        #endregion

        #region Event handlers
        //=====================================================================

        /// <summary>
        /// Add one or more new ignored words to the ignored words or keywords list
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnAddIgnoredWord_Click(object sender, RoutedEventArgs e)
        {
            int idx;

            var ignoredWordTextBox = sender == btnAddIgnoredWord ? txtIgnoredWord : txtIgnoredKeyword;
            var ignoredWordsListBox = ignoredWordTextBox == txtIgnoredWord ? lbIgnoredWords : lbIgnoredKeywords;

            ignoredWordTextBox.Text = ignoredWordTextBox.Text.Trim();

            if(ignoredWordTextBox.Text.Length != 0)
            {
                foreach(string word in ignoredWordTextBox.Text.Split([' ', '\t', ',', '.', '|'],
                  StringSplitOptions.RemoveEmptyEntries))
                {
                    string addWord = word;

                    if(addWord.Length < 3 && addWord[0] == '\\')
                        addWord = String.Empty;
                    else
                    {
                        if(addWord.Length > 1 && addWord[0] == '\\' && !CommonUtilities.EscapedLetters.Contains(addWord[1]))
                            addWord = addWord.Substring(1);
                    }

                    if(addWord.Length > 1)
                    {
                        idx = ignoredWordsListBox.Items.IndexOf(addWord);

                        if(idx == -1)
                            idx = ignoredWordsListBox.Items.Add(addWord);

                        if(idx != -1)
                        {
                            ignoredWordsListBox.SelectedIndex = idx;
                            ignoredWordsListBox.ScrollIntoView(ignoredWordsListBox.Items[idx]);
                        }
                    }
                }
            }

            ignoredWordTextBox.Text = null;
            Property_Changed(sender, e);
        }

        /// <summary>
        /// Remove the selected word from the ignored words or keywords list
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnRemoveIgnoredWord_Click(object sender, RoutedEventArgs e)
        {
            var ignoredWordsListBox = sender == btnRemoveIgnoredWord ? lbIgnoredWords : lbIgnoredKeywords;
            int idx = ignoredWordsListBox.SelectedIndex;

            if(idx != -1)
                ignoredWordsListBox.Items.RemoveAt(idx);

            if(ignoredWordsListBox.Items.Count != 0)
            {
                if(idx < 0)
                    idx = 0;
                else
                {
                    if(idx >= ignoredWordsListBox.Items.Count)
                        idx = ignoredWordsListBox.Items.Count - 1;
                }

                ignoredWordsListBox.SelectedIndex = idx;
            }

            Property_Changed(sender, e);
        }

        /// <summary>
        /// Clear the ignored words or keywords list
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnClearIgnoredWords_Click(object sender, RoutedEventArgs e)
        {
            var ignoredWordsListBox = sender == btnClearIgnoredWords ? lbIgnoredWords : lbIgnoredKeywords;

            ignoredWordsListBox.Items.Clear();

            var sd = new SortDescription { Direction = ListSortDirection.Ascending };

            ignoredWordsListBox.Items.SortDescriptions.Add(sd);

            Property_Changed(sender, e);
        }

        /// <summary>
        /// Import words from a user dictionary file into the ignored words or keywords list
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnImportIgnoredWords_Click(object sender, RoutedEventArgs e)
        {
            var ignoredWordsListBox = sender == btnImportIgnoredWords ? lbIgnoredWords : lbIgnoredKeywords;

            OpenFileDialog dlg = new()
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Filter = "User Dictionary Files (*.dic,*.xml)|*.dic;*.xml|" +
                    "StyleCop Settings Files (*.stylecop)|*.stylecop|Text documents (*.txt)|*.txt|" +
                    "All Files (*.*)|*.*",
                CheckFileExists = true
            };

            if(dlg.ShowDialog() ?? false)
            {
                try
                {
                    var uniqueWords = new HashSet<string>(CommonUtilities.LoadUserDictionary(dlg.FileName, false, false),
                        StringComparer.OrdinalIgnoreCase);

                    if(uniqueWords.Count == 0)
                    {
                        MessageBox.Show("Unable to load any words from the selected file", PackageResources.PackageTitle,
                            MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        return;
                    }

                    if(ignoredWordsListBox.Items.Count != 0 && MessageBox.Show("Do you want to replace the " +
                      "existing list of words?  Click Yes to replace them or No to merge the new words into " +
                      "the existing list.", PackageResources.PackageTitle, MessageBoxButton.YesNo,
                      MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.No)
                    {
                        uniqueWords.UnionWith(ignoredWordsListBox.Items.Cast<string>());
                    }

                    ignoredWordsListBox.Items.Clear();

                    foreach(string w in uniqueWords)
                        ignoredWordsListBox.Items.Add(w);

                    var sd = new SortDescription { Direction = ListSortDirection.Ascending };

                    ignoredWordsListBox.Items.SortDescriptions.Add(sd);

                    Property_Changed(sender, e);
                }
                catch(Exception ex)
                {
                    MessageBox.Show(String.Format(CultureInfo.CurrentCulture, "Unable to load ignored words " +
                        "from '{0}'.  Reason: {1}", dlg.FileName, ex.Message), PackageResources.PackageTitle,
                        MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
            }
        }

        /// <summary>
        /// Export ignored words or keywords to a file
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnExportIgnoredWords_Click(object sender, RoutedEventArgs e)
        {
            var ignoredWordsListBox = sender == btnExportIgnoredWords ? lbIgnoredWords : lbIgnoredKeywords;

            SaveFileDialog dlg = new()
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                FileName = "IgnoredWords.dic",
                DefaultExt = ".dic",
                OverwritePrompt = false,
                Filter = "User Dictionary Files (*.dic,*.xml)|*.dic;*.xml|Text documents (*.txt)|*.txt|" +
                    "All Files (*.*)|*.*"
            };

            if(dlg.ShowDialog() ?? false)
            {
                try
                {
                    var uniqueWords = new HashSet<string>(ignoredWordsListBox.Items.Cast<string>(),
                        StringComparer.OrdinalIgnoreCase);
                    bool replaceWords = true;

                    if(File.Exists(dlg.FileName))
                    {
#pragma warning disable VSTHRD010
                        if(!dlg.FileName.CanWriteToUserWordsFile(null, true))
                        {
                            MessageBox.Show("The selected file is read-only or could not be checked out",
                                PackageResources.PackageTitle, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                            return;
                        }
#pragma warning restore VSTHRD010

                        MessageBoxResult result = MessageBox.Show("Do you want to replace the words in the " +
                          "existing file?  Click Yes to replace them, No to merge the new words into the " +
                          "existing file, or Cancel to stop and do nothing.", PackageResources.PackageTitle,
                          MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.No);

                        if(result == MessageBoxResult.Cancel)
                            return;

                        if(result == MessageBoxResult.No)
                        {
                            uniqueWords.UnionWith(CommonUtilities.LoadUserDictionary(dlg.FileName, false, true));
                            replaceWords = false;
                        }
                    }

                    CommonUtilities.SaveCustomDictionary(dlg.FileName, replaceWords, false, uniqueWords);
                }
                catch(Exception ex)
                {
                    MessageBox.Show(String.Format(CultureInfo.CurrentCulture, "Unable to save ignored words " +
                        "to '{0}'.  Reason: {1}", dlg.FileName, ex.Message), PackageResources.PackageTitle,
                        MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
            }
        }

        /// <summary>
        /// Select an ignored words file
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnSelectFile_Click(object sender, RoutedEventArgs e)
        {
            string configFilePath = Path.GetDirectoryName(this.ConfigurationFilename);

            OpenFileDialog dlg = new()
            {
                InitialDirectory = Directory.Exists(configFilePath) ? configFilePath :
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Filter = "Ignored Words Files (*.dic,*.txt)|*.dic;*.txt|All Files (*.*)|*.*",
                CheckFileExists = false
            };

            if(dlg.ShowDialog() ?? false)
            {
                string filename = dlg.FileName;

                if(filename.StartsWith(configFilePath, StringComparison.OrdinalIgnoreCase) ||
                  configFilePath.StartsWith(Path.GetDirectoryName(filename), StringComparison.OrdinalIgnoreCase))
                {
                    filename = Path.Combine(Path.GetDirectoryName(filename).ToRelativePath(configFilePath),
                        Path.GetFileName(filename));
                }

                txtIgnoredWordsFile.Text = filename;
            }
        }

        /// <summary>
        /// Edit the ignored words file
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnEditFile_Click(object sender, RoutedEventArgs e)
        {
            string filename = txtIgnoredWordsFile.Text;

            if(String.IsNullOrWhiteSpace(filename))
            {
                MessageBox.Show("Specify or select an ignored words file first", PackageResources.PackageTitle,
                    MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            if(!Path.IsPathRooted(filename))
                filename = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(this.ConfigurationFilename), filename));

            try
            {
                if(!File.Exists(filename))
                {
                    if(MessageBox.Show("The ignored words file does not exist.  Do you want to create it?",
                      PackageResources.PackageTitle, MessageBoxButton.YesNo, MessageBoxImage.Question,
                      MessageBoxResult.No) == MessageBoxResult.No)
                    {
                        return;
                    }

                    File.WriteAllText(filename, String.Empty);
                }

                ThreadHelper.ThrowIfNotOnUIThread();

                var openDoc = Utility.GetServiceFromPackage<IVsUIShellOpenDocument, SVsUIShellOpenDocument>(false);

                if(openDoc != null && openDoc.OpenDocumentViaProject(filename, VSConstants.LOGVIEWID_TextView,
                  out _, out _, out _, out IVsWindowFrame ppWindowFrame) == VSConstants.S_OK)
                {
                    // On occasion, the call above is successful but we get a null frame for some reason
                    ppWindowFrame?.Show();
                }
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);

                MessageBox.Show("Unable to open the specified ignored words file for editing.  Reason: " + ex.Message,
                    PackageResources.PackageTitle, MessageBoxButton.OK, MessageBoxImage.Exclamation);
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
