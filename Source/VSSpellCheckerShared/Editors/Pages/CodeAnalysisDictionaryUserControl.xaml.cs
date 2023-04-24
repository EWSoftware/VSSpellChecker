//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : CodeAnalysisDictionaryUserControl.xaml.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 04/16/2023
// Note    : Copyright 2015-2023, Eric Woodruff, All rights reserved
//
// This file contains a user control used to edit the code analysis dictionary configuration settings
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 02/28/2015  EFW  Created the code
//===============================================================================================================

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

using VisualStudio.SpellChecker.Common.Configuration;

namespace VisualStudio.SpellChecker.Editors.Pages
{
    /// <summary>
    /// This user control is used to edit the code analysis dictionary configuration settings
    /// </summary>
    public partial class CodeAnalysisDictionaryUserControl : UserControl, ISpellCheckerConfiguration
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
        public CodeAnalysisDictionaryUserControl()
        {
            InitializeComponent();

            configPropertyControls = new[]
            {
                (cboImportCADictionaries, nameof(CodeAnalysisDictionaryOptions.ImportCodeAnalysisDictionaries)),
                (cboUnrecognizedWords, nameof(CodeAnalysisDictionaryOptions.TreatUnrecognizedWordsAsMisspelled)),
                (cboDeprecatedTerms, nameof(CodeAnalysisDictionaryOptions.TreatDeprecatedTermsAsMisspelled)),
                (cboCompoundTerms, nameof(CodeAnalysisDictionaryOptions.TreatCompoundTermsAsMisspelled)),
                (cboCasingExceptions, nameof(CodeAnalysisDictionaryOptions.TreatCasingExceptionsAsIgnoredWords))
            };
        }
        #endregion

        #region ISpellCheckerConfiguration Members
        //=====================================================================

        /// <inheritdoc />
        public UserControl Control => this;

        /// <inheritdoc />
        public string Title => "Code Analysis Dictionaries";

        /// <inheritdoc />
        public string HelpUrl => "e01bd3d9-c525-4407-8c65-fcdb64539299";

        /// <inheritdoc />
        public string ConfigurationFilename { get; set; }

        /// <inheritdoc />
        public bool HasChanges { get; private set; }

        /// <inheritdoc />
        public void LoadConfiguration(bool isGlobal, IDictionary<string, SpellCheckPropertyInfo> properties)
        {
            var dataSource = new List<PropertyState>();

            if(properties == null)
                throw new ArgumentNullException(nameof(properties));

            if(!isGlobal)
                dataSource.AddRange(new[] { PropertyState.Inherited, PropertyState.Yes, PropertyState.No });
            else
                dataSource.AddRange(new[] { PropertyState.Yes, PropertyState.No });

            foreach(var configProp in configPropertyControls)
            {
                configProp.cbo.ItemsSource = dataSource;
                configProp.cbo.SelectedValue = properties.ToPropertyState(configProp.PropertyName, isGlobal);
            }

            rbInheritRecWordHandling.Visibility = isGlobal ? Visibility.Collapsed : Visibility.Visible;

            if(!properties.TryGetValue(nameof(CodeAnalysisDictionaryOptions.RecognizedWordHandling), out var pi) &&
              !isGlobal)
            {
                rbInheritRecWordHandling.IsChecked = true;
            }
            else
            {
                if(pi == null || !Enum.TryParse(pi.EditorConfigPropertyValue,
                  out RecognizedWordHandling recWordHandling))
                {
                    recWordHandling = (RecognizedWordHandling)SpellCheckerConfiguration.DefaultValueFor(
                        nameof(CodeAnalysisDictionaryOptions.RecognizedWordHandling));
                }

                switch(recWordHandling)
                {
                    case RecognizedWordHandling.IgnoreAllWords:
                        rbIgnoreAll.IsChecked = true;
                        break;

                    case RecognizedWordHandling.AddAllWords:
                        rbAddToDictionary.IsChecked = true;
                        break;

                    case RecognizedWordHandling.AttributeDeterminesUsage:
                        rbAttributeDetermines.IsChecked = true;
                        break;

                    default:
                        rbNone.IsChecked = true;
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

            if((!isGlobal && !rbInheritRecWordHandling.IsChecked.Value) ||
              (isGlobal && !rbIgnoreAll.IsChecked.Value))
            {
                yield return (SpellCheckerConfiguration.EditorConfigSettingsFor(
                    nameof(CodeAnalysisDictionaryOptions.RecognizedWordHandling)).PropertyName,
                    (rbNone.IsChecked.Value ? RecognizedWordHandling.None :
                        rbIgnoreAll.IsChecked.Value ? RecognizedWordHandling.IgnoreAllWords :
                            rbAddToDictionary.IsChecked.Value ? RecognizedWordHandling.AddAllWords :
                                RecognizedWordHandling.AttributeDeterminesUsage).ToString());
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
