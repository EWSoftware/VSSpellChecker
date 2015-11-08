//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : CSharpOptionsUserControl.xaml.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 10/29/2015
// Note    : Copyright 2014-2015, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a user control used to edit the C# spell checker configuration settings
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 06/12/2014  EFW  Created the code
//===============================================================================================================

using System;
using System.Collections.Generic;
using System.Windows.Controls;

using VisualStudio.SpellChecker.Configuration;

namespace VisualStudio.SpellChecker.Editors.Pages
{
    /// <summary>
    /// This user control is used to edit the C# spell checker configuration settings
    /// </summary>
    public partial class CSharpOptionsUserControl : UserControl, ISpellCheckerConfiguration
    {
        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        public CSharpOptionsUserControl()
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
            get { return "C# Options"; }
        }

        /// <inheritdoc />
        public string HelpUrl
        {
            get { return "09cc5bfa-9eba-47e5-ba5f-a36e04f09b0d"; }
        }

        /// <inheritdoc />
        public void LoadConfiguration(SpellingConfigurationFile configuration)
        {
            var dataSource = new List<PropertyState>();
            
            if(configuration.ConfigurationType != ConfigurationType.Global)
                dataSource.AddRange(new[] { PropertyState.Inherited, PropertyState.Yes, PropertyState.No });
            else
                dataSource.AddRange(new[] { PropertyState.Yes, PropertyState.No });

            cboIgnoreXmlDocComments.ItemsSource = cboIgnoreDelimitedComments.ItemsSource =
                cboIgnoreStandardSingleLineComments.ItemsSource = cboIgnoreQuadrupleSlashComments.ItemsSource =
                cboIgnoreNormalStrings.ItemsSource = cboIgnoreVerbatimStrings.ItemsSource =
                cboIgnoreInterpolatedStrings.ItemsSource = cboApplyToAllCStyleLanguages.ItemsSource = dataSource;

            cboIgnoreXmlDocComments.SelectedValue = configuration.ToPropertyState(
                PropertyNames.CSharpOptionsIgnoreXmlDocComments);
            cboIgnoreDelimitedComments.SelectedValue = configuration.ToPropertyState(
                PropertyNames.CSharpOptionsIgnoreDelimitedComments);
            cboIgnoreStandardSingleLineComments.SelectedValue = configuration.ToPropertyState(
                PropertyNames.CSharpOptionsIgnoreStandardSingleLineComments);
            cboIgnoreQuadrupleSlashComments.SelectedValue = configuration.ToPropertyState(
                PropertyNames.CSharpOptionsIgnoreQuadrupleSlashComments);
            cboIgnoreNormalStrings.SelectedValue = configuration.ToPropertyState(
                PropertyNames.CSharpOptionsIgnoreNormalStrings);
            cboIgnoreVerbatimStrings.SelectedValue = configuration.ToPropertyState(
                PropertyNames.CSharpOptionsIgnoreVerbatimStrings);
            cboIgnoreInterpolatedStrings.SelectedValue = configuration.ToPropertyState(
                PropertyNames.CSharpOptionsIgnoreInterpolatedStrings);
            cboApplyToAllCStyleLanguages.SelectedValue = configuration.ToPropertyState(
                PropertyNames.CSharpOptionsApplyToAllCStyleLanguages);
        }

        /// <inheritdoc />
        public void SaveConfiguration(SpellingConfigurationFile configuration)
        {
            configuration.StoreProperty(PropertyNames.CSharpOptionsIgnoreXmlDocComments,
                ((PropertyState)cboIgnoreXmlDocComments.SelectedValue).ToPropertyValue());
            configuration.StoreProperty(PropertyNames.CSharpOptionsIgnoreDelimitedComments,
                ((PropertyState)cboIgnoreDelimitedComments.SelectedValue).ToPropertyValue());
            configuration.StoreProperty(PropertyNames.CSharpOptionsIgnoreStandardSingleLineComments,
                ((PropertyState)cboIgnoreStandardSingleLineComments.SelectedValue).ToPropertyValue());
            configuration.StoreProperty(PropertyNames.CSharpOptionsIgnoreQuadrupleSlashComments,
                ((PropertyState)cboIgnoreQuadrupleSlashComments.SelectedValue).ToPropertyValue());
            configuration.StoreProperty(PropertyNames.CSharpOptionsIgnoreNormalStrings,
                ((PropertyState)cboIgnoreNormalStrings.SelectedValue).ToPropertyValue());
            configuration.StoreProperty(PropertyNames.CSharpOptionsIgnoreVerbatimStrings,
                ((PropertyState)cboIgnoreVerbatimStrings.SelectedValue).ToPropertyValue());
            configuration.StoreProperty(PropertyNames.CSharpOptionsIgnoreInterpolatedStrings,
                ((PropertyState)cboIgnoreInterpolatedStrings.SelectedValue).ToPropertyValue());
            configuration.StoreProperty(PropertyNames.CSharpOptionsApplyToAllCStyleLanguages,
                ((PropertyState)cboApplyToAllCStyleLanguages.SelectedValue).ToPropertyValue());
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
