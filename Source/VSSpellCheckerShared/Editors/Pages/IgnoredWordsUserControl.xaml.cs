//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : IgnoredWordsUserControl.xaml.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 01/13/2021
// Note    : Copyright 2014-2021, Eric Woodruff, All rights reserved
//
// This file contains a user control used to edit the ignored words spell checker configuration settings
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

using VisualStudio.SpellChecker.Configuration;

using PackageResources = VisualStudio.SpellChecker.Properties.Resources;

namespace VisualStudio.SpellChecker.Editors.Pages
{
    /// <summary>
    /// This user control is used to edit the ignored words spell checker configuration settings
    /// </summary>
    public partial class IgnoredWordsUserControl : UserControl, ISpellCheckerConfiguration
    {
        #region Private data members
        //=====================================================================

        private string configFilePath;

        #endregion

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
        public string Title => "Ignored Words";

        /// <inheritdoc />
        public string HelpUrl => "c592c4d8-7387-47fe-9b79-28bf0168f447";

        /// <inheritdoc />
        public void LoadConfiguration(SpellingConfigurationFile configuration)
        {
            IEnumerable<string> words;
            lbIgnoredWords.Items.Clear();

            configFilePath = Path.GetDirectoryName(configuration.Filename);

            if(configuration.ConfigurationType == ConfigurationType.Global)
            {
                chkInheritIgnoredWords.IsChecked = false;
                chkInheritIgnoredWords.Visibility = Visibility.Collapsed;
            }
            else
                chkInheritIgnoredWords.IsChecked = configuration.ToBoolean(PropertyNames.InheritIgnoredWords);

            txtIgnoredWordsFile.Text = configuration.ToString(PropertyNames.IgnoredWordsFile);

            if(String.IsNullOrWhiteSpace(txtIgnoredWordsFile.Text) && configuration.ConfigurationType == ConfigurationType.Global)
                txtIgnoredWordsFile.Text = "IgnoredWords.dic";

            if(configuration.HasProperty(PropertyNames.IgnoredWords))
                words = configuration.ToValues(PropertyNames.IgnoredWords, PropertyNames.IgnoredWordsItem);
            else
                if(!chkInheritIgnoredWords.IsChecked.Value && configuration.ConfigurationType == ConfigurationType.Global)
                    words = SpellCheckerConfiguration.DefaultIgnoredWords;
                else
                    words = Enumerable.Empty<string>();

            foreach(string el in words)
                lbIgnoredWords.Items.Add(el);

            var sd = new SortDescription { Direction = ListSortDirection.Ascending };

            lbIgnoredWords.Items.SortDescriptions.Add(sd);
        }

        /// <inheritdoc />
        public void SaveConfiguration(SpellingConfigurationFile configuration)
        {
            HashSet<string> newList = null;

            configFilePath = Path.GetDirectoryName(configuration.Filename);

            if(lbIgnoredWords.Items.Count != 0 || !chkInheritIgnoredWords.IsChecked.Value)
            {
                newList = new HashSet<string>(lbIgnoredWords.Items.Cast<string>(), StringComparer.OrdinalIgnoreCase);

                if(configuration.ConfigurationType == ConfigurationType.Global &&
                  newList.SetEquals(SpellCheckerConfiguration.DefaultIgnoredWords))
                    newList = null;
            }

            if(configuration.ConfigurationType != ConfigurationType.Global)
                configuration.StoreProperty(PropertyNames.InheritIgnoredWords, chkInheritIgnoredWords.IsChecked);

            configuration.StoreValues(PropertyNames.IgnoredWords, PropertyNames.IgnoredWordsItem, newList);
            configuration.StoreProperty(PropertyNames.IgnoredWordsFile, txtIgnoredWordsFile.Text.Trim());
        }

        /// <inheritdoc />
        public bool AppliesTo(ConfigurationType configurationType)
        {
            return true;
        }

        /// <inheritdoc />
        public event EventHandler ConfigurationChanged;

        #endregion

        #region Event handlers
        //=====================================================================

        /// <summary>
        /// Add one or more new ignored word to the list
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnAddIgnoredWord_Click(object sender, RoutedEventArgs e)
        {
            char[] escapedLetters = new[] { 'a', 'b', 'f', 'n', 'r', 't', 'v', 'x', 'u', 'U' };
            int idx;

            txtIgnoredWord.Text = txtIgnoredWord.Text.Trim();

            if(txtIgnoredWord.Text.Length != 0)
                foreach(string word in txtIgnoredWord.Text.Split(new[] { ' ', '\t', ',', '.' },
                  StringSplitOptions.RemoveEmptyEntries))
                {
                    string addWord = word;

                    if(addWord.Length < 3 && addWord[0] == '\\')
                        addWord = String.Empty;
                    else
                        if(addWord.Length > 1 && addWord[0] == '\\' && !escapedLetters.Contains(addWord[1]))
                            addWord = addWord.Substring(1);

                    if(addWord.Length > 1)
                    {
                        idx = lbIgnoredWords.Items.IndexOf(addWord);

                        if(idx == -1)
                            idx = lbIgnoredWords.Items.Add(addWord);

                        if(idx != -1)
                        {
                            lbIgnoredWords.SelectedIndex = idx;
                            lbIgnoredWords.ScrollIntoView(lbIgnoredWords.Items[idx]);
                        }
                    }
                }

            txtIgnoredWord.Text = null;
            Property_Changed(sender, e);
        }

        /// <summary>
        /// Remove the selected word from the list of ignored words
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnRemoveIgnoredWord_Click(object sender, RoutedEventArgs e)
        {
            int idx = lbIgnoredWords.SelectedIndex;

            if(idx != -1)
                lbIgnoredWords.Items.RemoveAt(idx);

            if(lbIgnoredWords.Items.Count != 0)
            {
                if(idx < 0)
                    idx = 0;
                else
                    if(idx >= lbIgnoredWords.Items.Count)
                        idx = lbIgnoredWords.Items.Count - 1;

                lbIgnoredWords.SelectedIndex = idx;
            }

            Property_Changed(sender, e);
        }

        /// <summary>
        /// Reset the ignored words to the default list or blank if inherited
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnDefaultIgnoredWords_Click(object sender, RoutedEventArgs e)
        {
            lbIgnoredWords.Items.Clear();

            if(!chkInheritIgnoredWords.IsChecked.Value)
                foreach(string el in SpellCheckerConfiguration.DefaultIgnoredWords)
                    lbIgnoredWords.Items.Add(el);

            var sd = new SortDescription { Direction = ListSortDirection.Ascending };
            lbIgnoredWords.Items.SortDescriptions.Add(sd);

            Property_Changed(sender, e);
        }

        /// <summary>
        /// Import ignored words from a user dictionary file
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnImport_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Filter = "User Dictionary Files (*.dic,*.xml)|*.dic;*.xml|" +
                    "StyleCop Settings Files (*.stylecop)|*.stylecop|Text documents (*.txt)|*.txt|" +
                    "All Files (*.*)|*.*",
                CheckFileExists = true
            };

            if((dlg.ShowDialog() ?? false))
            {
                try
                {
                    var uniqueWords = new HashSet<string>(Utility.LoadUserDictionary(dlg.FileName, false, false),
                        StringComparer.OrdinalIgnoreCase);

                    if(uniqueWords.Count == 0)
                    {
                        MessageBox.Show("Unable to load any words from the selected file", PackageResources.PackageTitle,
                            MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        return;
                    }

                    if(lbIgnoredWords.Items.Count != 0 && MessageBox.Show("Do you want to replace the " +
                      "existing list of words?  Click Yes to replace them or No to merge the new words into " +
                      "the existing list.", PackageResources.PackageTitle, MessageBoxButton.YesNo,
                      MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.No)
                    {
                        uniqueWords.UnionWith(lbIgnoredWords.Items.Cast<string>());
                    }

                    lbIgnoredWords.Items.Clear();

                    foreach(string w in uniqueWords)
                        lbIgnoredWords.Items.Add(w);

                    var sd = new SortDescription { Direction = ListSortDirection.Ascending };

                    lbIgnoredWords.Items.SortDescriptions.Add(sd);

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
        /// Export ignored words to a file
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                FileName = "IgnoredWords.dic",
                DefaultExt = ".dic",
                OverwritePrompt = false,
                Filter = "User Dictionary Files (*.dic,*.xml)|*.dic;*.xml|Text documents (*.txt)|*.txt|" +
                    "All Files (*.*)|*.*"
            };

            if((dlg.ShowDialog() ?? false))
            {
                try
                {
                    var uniqueWords = new HashSet<string>(lbIgnoredWords.Items.Cast<string>(),
                        StringComparer.OrdinalIgnoreCase);
                    bool replaceWords = true;

                    if(File.Exists(dlg.FileName))
                    {
#pragma warning disable VSTHRD010
                        if(!dlg.FileName.CanWriteToUserWordsFile(null))
                        {
                            MessageBox.Show("File is read-only or could not be checked out",
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
                            uniqueWords.UnionWith(Utility.LoadUserDictionary(dlg.FileName, false, true));
                            replaceWords = false;
                        }
                    }

                    Utility.SaveCustomDictionary(dlg.FileName, replaceWords, false, uniqueWords);
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
            OpenFileDialog dlg = new OpenFileDialog
            {
                InitialDirectory = Directory.Exists(configFilePath) ? configFilePath :
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Filter = "Ignored Words Files (*.dic,*.txt)|*.dic;*.txt|All Files (*.*)|*.*",
                CheckFileExists = false
            };

            if(dlg.ShowDialog() ?? false)
            {
                string filename = dlg.FileName;

                if(filename.StartsWith(configFilePath, StringComparison.OrdinalIgnoreCase))
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
                filename = Path.GetFullPath(Path.Combine(configFilePath, filename));

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
                    if(ppWindowFrame != null)
                        ppWindowFrame.Show();
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
            this.ConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }
        #endregion
    }
}
