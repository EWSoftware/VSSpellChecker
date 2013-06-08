//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : SpellCheckerConfigDlg.cs
// Author  : Eric Woodruff
// Updated : 05/23/2013
// Note    : Copyright 2013, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a window used to edit the spell checker configuration settings
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code.  It can also be found at the project website: http://VSSpellChecker.CodePlex.com.
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
// Version     Date     Who  Comments
//===============================================================================================================
// 1.0.0.0  04/14/2013  EFW  Created the code
//===============================================================================================================

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;

using PackageResources = VisualStudio.SpellChecker.Properties.Resources;

namespace VisualStudio.SpellChecker.UI
{
    /// <summary>
    /// This window is used to modify the Visual Studio spell checker configuration settings
    /// </summary>
    /// <remarks>Settings are stored in an XML file in the user's local application data folder and will be used
    /// by all versions of Visual Studio in which the package is installed.</remarks>
    public partial class SpellCheckerConfigDlg : Window
    {
        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        public SpellCheckerConfigDlg()
        {
            InitializeComponent();

            foreach(var lang in SpellCheckerConfiguration.AvailableDictionaryLanguages.OrderBy(c => c.Name))
                cboDefaultLanguage.Items.Add(lang);

            if(cboDefaultLanguage.Items.Contains(SpellCheckerConfiguration.DefaultLanguage))
                cboDefaultLanguage.SelectedItem = SpellCheckerConfiguration.DefaultLanguage;

            chkSpellCheckAsYouType.IsChecked = SpellCheckerConfiguration.SpellCheckAsYouType;
            chkIgnoreWordsWithDigits.IsChecked = SpellCheckerConfiguration.IgnoreWordsWithDigits;
            chkIgnoreAllUppercase.IsChecked = SpellCheckerConfiguration.IgnoreWordsInAllUppercase;
            chkIgnoreFilenamesAndEMail.IsChecked = SpellCheckerConfiguration.IgnoreFilenamesAndEMailAddresses;
            chkIgnoreXmlInText.IsChecked = SpellCheckerConfiguration.IgnoreXmlElementsInText;
            chkTreatUnderscoresAsSeparators.IsChecked = SpellCheckerConfiguration.TreatUnderscoreAsSeparator;

            foreach(string el in SpellCheckerConfiguration.IgnoredXmlElements)
                lbIgnoredXmlElements.Items.Add(el);

            foreach(string el in SpellCheckerConfiguration.SpellCheckedXmlAttributes)
                lbSpellCheckedAttributes.Items.Add(el);

            var sd = new SortDescription { Direction = ListSortDirection.Ascending };

            lbIgnoredXmlElements.Items.SortDescriptions.Add(sd);
            lbSpellCheckedAttributes.Items.SortDescriptions.Add(sd);
            lbUserDictionary.Items.SortDescriptions.Add(sd);
        }
        #endregion

        #region General event handlers
        //=====================================================================

        /// <summary>
        /// Close this form
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Save changes to the configuration
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            SpellCheckerConfiguration.DefaultLanguage = (CultureInfo)cboDefaultLanguage.SelectedItem;

            SpellCheckerConfiguration.SpellCheckAsYouType = chkSpellCheckAsYouType.IsChecked.Value;
            SpellCheckerConfiguration.IgnoreWordsWithDigits = chkIgnoreWordsWithDigits.IsChecked.Value;
            SpellCheckerConfiguration.IgnoreWordsInAllUppercase = chkIgnoreAllUppercase.IsChecked.Value;
            SpellCheckerConfiguration.IgnoreFilenamesAndEMailAddresses = chkIgnoreFilenamesAndEMail.IsChecked.Value;
            SpellCheckerConfiguration.IgnoreXmlElementsInText = chkIgnoreXmlInText.IsChecked.Value;
            SpellCheckerConfiguration.TreatUnderscoreAsSeparator = chkTreatUnderscoresAsSeparators.IsChecked.Value;

            SpellCheckerConfiguration.SetIgnoredXmlElements(lbIgnoredXmlElements.Items.OfType<string>());
            SpellCheckerConfiguration.SetSpellCheckedXmlAttributes(lbSpellCheckedAttributes.Items.OfType<string>());

            if(!SpellCheckerConfiguration.SaveConfiguration())
                MessageBox.Show("Unable to save spell checking configuration", PackageResources.PackageTitle,
                    MessageBoxButton.OK, MessageBoxImage.Exclamation);

            this.Close();
        }
        #endregion

        #region General tab event handlers
        //=====================================================================

        /// <summary>
        /// Load the user dictionary file when the selected language changes
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void cboDefaultLanguage_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            string filename = Path.Combine(SpellCheckerConfiguration.ConfigurationFilePath,
                ((CultureInfo)cboDefaultLanguage.SelectedItem).Name + "_Ignored.dic");

            lbUserDictionary.Items.Clear();
            lblUserDictLang.Content = String.Format(CultureInfo.CurrentCulture,
                "User dictionary for default language ({0})", cboDefaultLanguage.SelectedItem.ToString());

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
        }

        /// <summary>
        /// View the project website
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void lnkCodePlex_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(lnkCodePlex.NavigateUri.AbsoluteUri);
            }
            catch(Exception ex)
            {
                MessageBox.Show("Unable to navigate to website.  Reason: " + ex.Message,
                    PackageResources.PackageTitle, MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }
        #endregion

        #region XML files tab event handlers
        //=====================================================================

        /// <summary>
        /// Add a new ignored XML element name to the list
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnAddIgnored_Click(object sender, RoutedEventArgs e)
        {
            int idx;

            txtIgnoredElement.Text = txtIgnoredElement.Text.Trim();

            if(txtIgnoredElement.Text.Length != 0)
            {
                idx = lbIgnoredXmlElements.Items.IndexOf(txtIgnoredElement.Text);

                if(idx == -1)
                    idx = lbIgnoredXmlElements.Items.Add(txtIgnoredElement.Text);

                if(idx != -1)
                {
                    lbIgnoredXmlElements.SelectedIndex = idx;
                    lbIgnoredXmlElements.ScrollIntoView(lbIgnoredXmlElements.Items[idx]);
                }

                txtIgnoredElement.Text = null;
            }
        }

        /// <summary>
        /// Remove the selected element from the list of ignored elements
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnRemoveIgnored_Click(object sender, RoutedEventArgs e)
        {
            int idx = lbIgnoredXmlElements.SelectedIndex;

            if(idx != -1)
                lbIgnoredXmlElements.Items.RemoveAt(idx);

            if(lbIgnoredXmlElements.Items.Count != 0)
            {
                if(idx < 0)
                    idx = 0;
                else
                    if(idx >= lbIgnoredXmlElements.Items.Count)
                        idx = lbIgnoredXmlElements.Items.Count - 1;

                lbIgnoredXmlElements.SelectedIndex = idx;
            }
        }

        /// <summary>
        /// Reset the ignored XML elements to the default list
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnDefaultIgnored_Click(object sender, RoutedEventArgs e)
        {
            lbIgnoredXmlElements.Items.Clear();

            foreach(string el in SpellCheckerConfiguration.DefaultIgnoredXmlElements)
                lbIgnoredXmlElements.Items.Add(el);
        }

        /// <summary>
        /// Add a new spell checked attribute name to the list
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnAddAttribute_Click(object sender, RoutedEventArgs e)
        {
            int idx;

            txtAttributeName.Text = txtAttributeName.Text.Trim();

            if(txtAttributeName.Text.Length != 0)
            {
                idx = lbSpellCheckedAttributes.Items.IndexOf(txtAttributeName.Text);

                if(idx == -1)
                    idx = lbSpellCheckedAttributes.Items.Add(txtAttributeName.Text);

                if(idx != -1)
                {
                    lbSpellCheckedAttributes.SelectedIndex = idx;
                    lbSpellCheckedAttributes.ScrollIntoView(lbSpellCheckedAttributes.Items[idx]);
                }

                txtAttributeName.Text = null;
            }
        }

        /// <summary>
        /// Remove the selected attribute from the list of spell checked attributes
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnRemoveAttribute_Click(object sender, RoutedEventArgs e)
        {
            int idx = lbSpellCheckedAttributes.SelectedIndex;

            if(idx != -1)
                lbSpellCheckedAttributes.Items.RemoveAt(idx);

            if(lbSpellCheckedAttributes.Items.Count != 0)
            {
                if(idx < 0)
                    idx = 0;
                else
                    if(idx >= lbSpellCheckedAttributes.Items.Count)
                        idx = lbSpellCheckedAttributes.Items.Count - 1;

                lbSpellCheckedAttributes.SelectedIndex = idx;
            }
        }

        /// <summary>
        /// Reset the spell checked attributes to the default list
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnDefaultAttributes_Click(object sender, RoutedEventArgs e)
        {
            lbSpellCheckedAttributes.Items.Clear();

            foreach(string el in SpellCheckerConfiguration.DefaultSpellCheckedAttributes)
                lbSpellCheckedAttributes.Items.Add(el);
        }
        #endregion

        #region User dictionary tab event handlers
        //=====================================================================

        /// <summary>
        /// Remove the selected word from the user dictionary
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnRemoveWord_Click(object sender, RoutedEventArgs e)
        {
            int idx = lbUserDictionary.SelectedIndex;

            if(idx != -1)
                lbUserDictionary.Items.RemoveAt(idx);

            if(lbUserDictionary.Items.Count != 0)
            {
                if(idx < 0)
                    idx = 0;
                else
                    if(idx >= lbUserDictionary.Items.Count)
                        idx = lbUserDictionary.Items.Count - 1;

                lbUserDictionary.SelectedIndex = idx;
            }

            CultureInfo culture = (CultureInfo)cboDefaultLanguage.SelectedItem;
            string filename = Path.Combine(SpellCheckerConfiguration.ConfigurationFilePath,
                culture.Name + "_Ignored.dic");

            try
            {
                File.WriteAllLines(filename, lbUserDictionary.Items.OfType<string>());
                GlobalDictionary.LoadIgnoredWordsFile(culture);
            }
            catch(Exception ex)
            {
                MessageBox.Show("Unable to save user dictionary.  Reason: " + ex.Message,
                    PackageResources.PackageTitle, MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }
        #endregion
    }
}
