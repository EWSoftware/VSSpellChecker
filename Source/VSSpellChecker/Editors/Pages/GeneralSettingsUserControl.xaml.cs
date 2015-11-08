//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : GeneralSettingsUserControl.xaml.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 10/28/2015
// Note    : Copyright 2014-2015, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a user control used to edit the general spell checker configuration settings
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 06/09/2014  EFW  Moved the general settings to a user control
//===============================================================================================================

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

using VisualStudio.SpellChecker.Configuration;

namespace VisualStudio.SpellChecker.Editors.Pages
{
    /// <summary>
    /// This user control is used to edit the general spell checker configuration settings
    /// </summary>
    public partial class GeneralSettingsUserControl : UserControl, ISpellCheckerConfiguration
    {
        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        public GeneralSettingsUserControl()
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
            get { return "General Settings"; }
        }

        /// <inheritdoc />
        public string HelpUrl
        {
            get { return "b4a8726f-5bee-48a4-81a9-00b1be332607"; }
        }

        /// <inheritdoc />
        public void LoadConfiguration(SpellingConfigurationFile configuration)
        {
            var dataSource = new List<PropertyState>();

            if(configuration.ConfigurationType != ConfigurationType.Global)
                dataSource.AddRange(new[] { PropertyState.Inherited, PropertyState.Yes, PropertyState.No });
            else
                dataSource.AddRange(new[] { PropertyState.Yes, PropertyState.No });

            cboSpellCheckAsYouType.ItemsSource = cboIncludeInProjectSpellCheck.ItemsSource =
                cboDetectDoubledWords.ItemsSource = cboIgnoreWordsWithDigits.ItemsSource =
                cboIgnoreAllUppercase.ItemsSource = cboIgnoreFormatSpecifiers.ItemsSource =
                cboIgnoreFilenamesAndEMail.ItemsSource = cboIgnoreXmlInText.ItemsSource =
                cboTreatUnderscoresAsSeparators.ItemsSource = cboIgnoreMnemonics.ItemsSource = dataSource;

            cboSpellCheckAsYouType.SelectedValue = configuration.ToPropertyState(
                PropertyNames.SpellCheckAsYouType);
            cboIncludeInProjectSpellCheck.SelectedValue = configuration.ToPropertyState(
                PropertyNames.IncludeInProjectSpellCheck);
            cboDetectDoubledWords.SelectedValue = configuration.ToPropertyState(
                PropertyNames.DetectDoubledWords);
            cboIgnoreWordsWithDigits.SelectedValue = configuration.ToPropertyState(
                PropertyNames.IgnoreWordsWithDigits);
            cboIgnoreAllUppercase.SelectedValue = configuration.ToPropertyState(
                PropertyNames.IgnoreWordsInAllUppercase);
            cboIgnoreFormatSpecifiers.SelectedValue = configuration.ToPropertyState(
                PropertyNames.IgnoreFormatSpecifiers);
            cboIgnoreFilenamesAndEMail.SelectedValue = configuration.ToPropertyState(
                PropertyNames.IgnoreFilenamesAndEMailAddresses);
            cboIgnoreXmlInText.SelectedValue = configuration.ToPropertyState(
                PropertyNames.IgnoreXmlElementsInText);
            cboTreatUnderscoresAsSeparators.SelectedValue = configuration.ToPropertyState(
                PropertyNames.TreatUnderscoreAsSeparator);
            cboIgnoreMnemonics.SelectedValue = configuration.ToPropertyState(
                PropertyNames.IgnoreMnemonics);

            if(configuration.ConfigurationType != ConfigurationType.Global)
                spIncludeInProjectSpellCheck.Visibility = rbInheritIgnoredCharClass.Visibility = Visibility.Visible;
            else
                spIncludeInProjectSpellCheck.Visibility = rbInheritIgnoredCharClass.Visibility = Visibility.Collapsed;

            if(!configuration.HasProperty(PropertyNames.IgnoreCharacterClass) &&
              configuration.ConfigurationType != ConfigurationType.Global)
            {
                rbInheritIgnoredCharClass.IsChecked = true;
            }
            else
                switch(configuration.ToEnum<IgnoredCharacterClass>(PropertyNames.IgnoreCharacterClass))
                {
                    case IgnoredCharacterClass.NonAscii:
                        rbIgnoreNonAscii.IsChecked = true;
                        break;

                    case IgnoredCharacterClass.NonLatin:
                        rbIgnoreNonLatin.IsChecked = true;
                        break;

                    default:
                        rbIncludeAll.IsChecked = true;
                        break;
                }
        }

        /// <inheritdoc />
        public void SaveConfiguration(SpellingConfigurationFile configuration)
        {
            configuration.StoreProperty(PropertyNames.SpellCheckAsYouType,
                ((PropertyState)cboSpellCheckAsYouType.SelectedValue).ToPropertyValue());
            configuration.StoreProperty(PropertyNames.IncludeInProjectSpellCheck,
                ((PropertyState)cboIncludeInProjectSpellCheck.SelectedValue).ToPropertyValue());
            configuration.StoreProperty(PropertyNames.DetectDoubledWords,
                ((PropertyState)cboDetectDoubledWords.SelectedValue).ToPropertyValue());
            configuration.StoreProperty(PropertyNames.IgnoreWordsWithDigits,
                ((PropertyState)cboIgnoreWordsWithDigits.SelectedValue).ToPropertyValue());
            configuration.StoreProperty(PropertyNames.IgnoreWordsInAllUppercase,
                ((PropertyState)cboIgnoreAllUppercase.SelectedValue).ToPropertyValue());
            configuration.StoreProperty(PropertyNames.IgnoreFormatSpecifiers,
                ((PropertyState)cboIgnoreFormatSpecifiers.SelectedValue).ToPropertyValue());
            configuration.StoreProperty(PropertyNames.IgnoreFilenamesAndEMailAddresses,
                ((PropertyState)cboIgnoreFilenamesAndEMail.SelectedValue).ToPropertyValue());
            configuration.StoreProperty(PropertyNames.IgnoreXmlElementsInText,
                ((PropertyState)cboIgnoreXmlInText.SelectedValue).ToPropertyValue());
            configuration.StoreProperty(PropertyNames.TreatUnderscoreAsSeparator,
                ((PropertyState)cboTreatUnderscoresAsSeparators.SelectedValue).ToPropertyValue());
            configuration.StoreProperty(PropertyNames.IgnoreMnemonics,
                ((PropertyState)cboIgnoreMnemonics.SelectedValue).ToPropertyValue());

            if(rbInheritIgnoredCharClass.IsChecked.Value)
                configuration.StoreProperty(PropertyNames.IgnoreCharacterClass, null);
            else
                configuration.StoreProperty(PropertyNames.IgnoreCharacterClass,
                    rbIncludeAll.IsChecked.Value ? IgnoredCharacterClass.None : rbIgnoreNonLatin.IsChecked.Value ?
                        IgnoredCharacterClass.NonLatin : IgnoredCharacterClass.NonAscii);
        }

        /// <inheritdoc />
        public event EventHandler ConfigurationChanged;

        #endregion

        #region Event handlers
        //=====================================================================

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
