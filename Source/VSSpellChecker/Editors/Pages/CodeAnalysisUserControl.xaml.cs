//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : CodeAnalysisUserControl.xaml.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 04/21/2015
// Note    : Copyright 2015, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
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

using VisualStudio.SpellChecker.Configuration;

namespace VisualStudio.SpellChecker.Editors.Pages
{
    /// <summary>
    /// This user control is used to edit the code analysis dictionary configuration settings
    /// </summary>
    public partial class CodeAnalysisUserControl : UserControl, ISpellCheckerConfiguration
    {
        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        public CodeAnalysisUserControl()
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
            get { return "Code Analysis Dictionaries"; }
        }

        /// <inheritdoc />
        public string HelpUrl
        {
            get { return "e01bd3d9-c525-4407-8c65-fcdb64539299"; }
        }

        /// <inheritdoc />
        public void LoadConfiguration(SpellingConfigurationFile configuration)
        {
            var dataSource = new List<PropertyState>();

            if(configuration.ConfigurationType != ConfigurationType.Global)
                dataSource.AddRange(new[] { PropertyState.Inherited, PropertyState.Yes, PropertyState.No });
            else
                dataSource.AddRange(new[] { PropertyState.Yes, PropertyState.No });

            cboImportCADictionaries.ItemsSource = cboUnrecognizedWords.ItemsSource =
                cboDeprecatedTerms.ItemsSource = cboCompoundTerms.ItemsSource =
                cboCasingExceptions.ItemsSource = dataSource;

            cboImportCADictionaries.SelectedValue = configuration.ToPropertyState(
                PropertyNames.CadOptionsImportCodeAnalysisDictionaries);
            cboUnrecognizedWords.SelectedValue = configuration.ToPropertyState(
                PropertyNames.CadOptionsTreatUnrecognizedWordsAsMisspelled);
            cboDeprecatedTerms.SelectedValue = configuration.ToPropertyState(
                PropertyNames.CadOptionsTreatDeprecatedTermsAsMisspelled);
            cboCompoundTerms.SelectedValue = configuration.ToPropertyState(
                PropertyNames.CadOptionsTreatCompoundTermsAsMisspelled);
            cboCasingExceptions.SelectedValue = configuration.ToPropertyState(
                PropertyNames.CadOptionsTreatCasingExceptionsAsIgnoredWords);

            if(configuration.ConfigurationType != ConfigurationType.Global)
                rbInheritRecWordHandling.Visibility = Visibility.Visible;
            else
                rbInheritRecWordHandling.Visibility = Visibility.Collapsed;

            if(!configuration.HasProperty(PropertyNames.CadOptionsRecognizedWordHandling) &&
              configuration.ConfigurationType != ConfigurationType.Global)
            {
                rbInheritRecWordHandling.IsChecked = true;
            }
            else
                switch(configuration.ToEnum<RecognizedWordHandling>(PropertyNames.CadOptionsRecognizedWordHandling))
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

        /// <inheritdoc />
        public void SaveConfiguration(SpellingConfigurationFile configuration)
        {
            configuration.StoreProperty(PropertyNames.CadOptionsImportCodeAnalysisDictionaries,
                ((PropertyState)cboImportCADictionaries.SelectedValue).ToPropertyValue());
            configuration.StoreProperty(PropertyNames.CadOptionsTreatUnrecognizedWordsAsMisspelled,
                ((PropertyState)cboUnrecognizedWords.SelectedValue).ToPropertyValue());
            configuration.StoreProperty(PropertyNames.CadOptionsTreatDeprecatedTermsAsMisspelled,
                ((PropertyState)cboDeprecatedTerms.SelectedValue).ToPropertyValue());
            configuration.StoreProperty(PropertyNames.CadOptionsTreatCompoundTermsAsMisspelled,
                ((PropertyState)cboCompoundTerms.SelectedValue).ToPropertyValue());
            configuration.StoreProperty(PropertyNames.CadOptionsTreatCasingExceptionsAsIgnoredWords,
                ((PropertyState)cboCasingExceptions.SelectedValue).ToPropertyValue());

            if(rbInheritRecWordHandling.IsChecked.Value)
                configuration.StoreProperty(PropertyNames.CadOptionsRecognizedWordHandling, null);
            else
                configuration.StoreProperty(PropertyNames.CadOptionsRecognizedWordHandling,
                    rbNone.IsChecked.Value ? RecognizedWordHandling.None :
                        rbIgnoreAll.IsChecked.Value ? RecognizedWordHandling.IgnoreAllWords :
                            rbAddToDictionary.IsChecked.Value ? RecognizedWordHandling.AddAllWords :
                                RecognizedWordHandling.AttributeDeterminesUsage);
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
