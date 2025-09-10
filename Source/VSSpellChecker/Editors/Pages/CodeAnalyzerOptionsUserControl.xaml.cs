//===============================================================================================================
// System  : Spell Check My Code Package
// File    : CodeAnalyzerOptionsUserControl.xaml.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 08/30/2025
// Note    : Copyright 2014-2025, Eric Woodruff, All rights reserved
//
// This file contains a user control used to edit the code analyzer spell checker configuration settings
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

using VisualStudio.SpellChecker.Common.Configuration;

namespace VisualStudio.SpellChecker.Editors.Pages
{
    /// <summary>
    /// This user control is used to edit the code analyzer spell checker configuration settings
    /// </summary>
    public partial class CodeAnalyzerOptionsUserControl : UserControl, ISpellCheckerConfiguration
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
        public CodeAnalyzerOptionsUserControl()
        {
            InitializeComponent();

            configPropertyControls =
            [
                (cboIgnoreIdentifierIfPrivate, nameof(CodeAnalyzerOptions.IgnoreIdentifierIfPrivate)),
                (cboIgnoreIdentifierIfInternal, nameof(CodeAnalyzerOptions.IgnoreIdentifierIfInternal)),
                (cboIgnoreIdentifierIfAllUppercase, nameof(CodeAnalyzerOptions.IgnoreIdentifierIfAllUppercase)),
                (cboIgnoreIdentifierWithinMemberBodies, nameof(CodeAnalyzerOptions.IgnoreIdentifiersWithinMemberBodies)),
                (cboIgnoreTypeParameters, nameof(CodeAnalyzerOptions.IgnoreTypeParameters)),
                (cboIgnoreIfCompilerGenerated, nameof(CodeAnalyzerOptions.IgnoreIfCompilerGenerated)),
                (cboIgnoreXmlDocComments, nameof(CodeAnalyzerOptions.IgnoreXmlDocComments)),
                (cboIgnoreDelimitedComments, nameof(CodeAnalyzerOptions.IgnoreDelimitedComments)),
                (cboIgnoreStandardSingleLineComments, nameof(CodeAnalyzerOptions.IgnoreStandardSingleLineComments)),
                (cboIgnoreQuadrupleSlashComments, nameof(CodeAnalyzerOptions.IgnoreQuadrupleSlashComments)),
                (cboIgnoreNormalStrings, nameof(CodeAnalyzerOptions.IgnoreNormalStrings)),
                (cboIgnoreVerbatimStrings, nameof(CodeAnalyzerOptions.IgnoreVerbatimStrings)),
                (cboIgnoreInterpolatedStrings, nameof(CodeAnalyzerOptions.IgnoreInterpolatedStrings)),
                (cboIgnoreRawStrings, nameof(CodeAnalyzerOptions.IgnoreRawStrings)),
                (cboApplyToAllCStyleLanguages, nameof(CodeAnalyzerOptions.ApplyToAllCStyleLanguages)),
            ];
        }
        #endregion

        #region ISpellCheckerConfiguration Members
        //=====================================================================

        /// <inheritdoc />
        public UserControl Control => this;

        /// <inheritdoc />
        public string Title => "Code Analyzer Options";

        /// <inheritdoc />
        public string HelpUrl => "09cc5bfa-9eba-47e5-ba5f-a36e04f09b0d";

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
                dataSource.AddRange([PropertyState.Inherited, PropertyState.Yes, PropertyState.No]);
            else
                dataSource.AddRange([PropertyState.Yes, PropertyState.No]);

            foreach(var configProp in configPropertyControls)
            {
                configProp.cbo.ItemsSource = dataSource;
                configProp.cbo.SelectedValue = properties.ToPropertyState(configProp.PropertyName, isGlobal);
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
            this.HasChanges = true;
            this.ConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }
        #endregion
    }
}
