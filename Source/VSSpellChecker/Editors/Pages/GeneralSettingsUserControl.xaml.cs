//===============================================================================================================
// System  : Spell Check My Code Package
// File    : GeneralSettingsUserControl.xaml.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 08/30/2025
// Note    : Copyright 2014-2025, Eric Woodruff, All rights reserved
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

using VisualStudio.SpellChecker.Common.Configuration;

namespace VisualStudio.SpellChecker.Editors.Pages
{
    /// <summary>
    /// This user control is used to edit the general spell checker configuration settings
    /// </summary>
    public partial class GeneralSettingsUserControl : UserControl, ISpellCheckerConfiguration
    {
        #region Private data members
        //=====================================================================

        private readonly (ComboBox cbo, string PropertyName)[] configPropertyControls;

        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        public GeneralSettingsUserControl()
        {
            InitializeComponent();

            configPropertyControls =
            [
                (cboSpellCheckAsYouType, nameof(SpellCheckerConfiguration.SpellCheckAsYouType)),
                (cboIncludeInProjectSpellCheck, nameof(SpellCheckerConfiguration.IncludeInProjectSpellCheck)),
                (cboEnableCodeAnalyzers, nameof(SpellCheckerConfiguration.EnableCodeAnalyzers)),
                (cboDetectDoubledWords, nameof(SpellCheckerConfiguration.DetectDoubledWords)),
                (cboIgnoreWordsWithDigits, nameof(SpellCheckerConfiguration.IgnoreWordsWithDigits)),
                (cboIgnoreAllUppercase, nameof(SpellCheckerConfiguration.IgnoreWordsInAllUppercase)),
                (cboIgnoreMixedCase, nameof(SpellCheckerConfiguration.IgnoreWordsInMixedCase)),
                (cboIgnoreFormatSpecifiers, nameof(SpellCheckerConfiguration.IgnoreFormatSpecifiers)),
                (cboIgnoreFilenamesAndEMail, nameof(SpellCheckerConfiguration.IgnoreFilenamesAndEMailAddresses)),
                (cboIgnoreXmlInText, nameof(SpellCheckerConfiguration.IgnoreXmlElementsInText)),
                (cboTreatUnderscoresAsSeparators, nameof(SpellCheckerConfiguration.TreatUnderscoreAsSeparator)),
                (cboIgnoreMnemonics, nameof(SpellCheckerConfiguration.IgnoreMnemonics))
            ];
        }
        #endregion

        #region ISpellCheckerConfiguration Members
        //=====================================================================

        /// <inheritdoc />
        public UserControl Control => this;

        /// <inheritdoc />
        public string Title => "General Settings";

        /// <inheritdoc />
        public string ConfigurationFilename { get; set; }

        /// <inheritdoc />
        public bool HasChanges { get; private set; }

        /// <inheritdoc />
        public string HelpUrl => "b4a8726f-5bee-48a4-81a9-00b1be332607";

        /// <inheritdoc />
        public void LoadConfiguration(bool isGlobal, IDictionary<string, SpellCheckPropertyInfo> properties)
        {
            var dataSource = new List<PropertyState>();

            if(properties == null)
                throw new ArgumentNullException(nameof(properties));

            if(!isGlobal)
                dataSource.AddRange([PropertyState.Inherited, PropertyState.Yes, PropertyState.No]);
            else
                dataSource.AddRange([PropertyState.Yes, PropertyState.No]);

            foreach(var configProp in configPropertyControls)
            {
                configProp.cbo.ItemsSource = dataSource;
                configProp.cbo.SelectedValue = properties.ToPropertyState(configProp.PropertyName, isGlobal);
            }

            rbInheritIgnoredCharClass.Visibility = isGlobal ? Visibility.Collapsed : Visibility.Visible;

            if(!properties.TryGetValue(nameof(SpellCheckerConfiguration.IgnoredCharacterClass), out var pi) &&
              !isGlobal)
            {
                rbInheritIgnoredCharClass.IsChecked = true;
            }
            else
            {
                if(pi == null || !Enum.TryParse(pi.EditorConfigPropertyValue, out IgnoredCharacterClass charClass))
                {
                    charClass = (IgnoredCharacterClass)SpellCheckerConfiguration.DefaultValueFor(
                        nameof(SpellCheckerConfiguration.IgnoredCharacterClass));
                }

                switch(charClass)
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

            this.HasChanges = false;
        }

        /// <inheritdoc />
        public IEnumerable<(string PropertyName, string PropertyValue)> ChangedProperties(bool isGlobal,
          string sectionId)
        {
            foreach(var configProp in configPropertyControls)
            {
                var propertyValue = ((PropertyState)configProp.cbo.SelectedValue).ToPropertyValue(
                    configProp.PropertyName, isGlobal);

                if(propertyValue.PropertyName != null)
                    yield return propertyValue;
            }

            if((!isGlobal && !rbInheritIgnoredCharClass.IsChecked.Value) ||
              (isGlobal && !rbIncludeAll.IsChecked.Value))
            {
                yield return (SpellCheckerConfiguration.EditorConfigSettingsFor(
                    nameof(SpellCheckerConfiguration.IgnoredCharacterClass)).PropertyName,
                    (rbIncludeAll.IsChecked.Value ? IgnoredCharacterClass.None : rbIgnoreNonLatin.IsChecked.Value ?
                        IgnoredCharacterClass.NonLatin : IgnoredCharacterClass.NonAscii).ToString());
            }
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
        private void Property_Changed(object sender, RoutedEventArgs e)
        {
            this.HasChanges = true;
            this.ConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }
        #endregion
    }
}
