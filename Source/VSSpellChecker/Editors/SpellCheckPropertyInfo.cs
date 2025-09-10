//===============================================================================================================
// System  : Spell Check My Code Package
// File    : SpellCheckPropertyInfo.xaml.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 04/14/2023
// Note    : Copyright 2023, Eric Woodruff, All rights reserved
//
// This file contains a user control used to edit spell checker configuration settings files
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 04/14/2023  EFW  Created the code
//===============================================================================================================

using VisualStudio.SpellChecker.Common.Configuration;

namespace VisualStudio.SpellChecker.Editors
{
    /// <summary>
    /// This is used to contain information about a spell checker property for editing
    /// </summary>
    public class SpellCheckPropertyInfo
    {
        #region Properties
        //=====================================================================

        /// <summary>
        /// This read-only property is used to get the .editorconfig property name
        /// </summary>
        public string EditorConfigPropertyName { get; }

        /// <summary>
        /// This property is used to get or set the .editorconfig property value
        /// </summary>
        public string EditorConfigPropertyValue { get; set; }

        /// <summary>
        /// This read-only property is used to get the configuration property name
        /// </summary>
        public string ConfigurationPropertyName { get; }

        /// <summary>
        /// This read-only property is used to get the default property value
        /// </summary>
        public object DefaultValue { get; }

        /// <summary>
        /// This read-only property is used to get whether or not this property supports multiple instances
        /// </summary>
        public bool CanHaveMultipleInstances { get; }

        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="propertyName">The .editorconfig property name</param>
        /// <param name="propertyValue">The .editorconfig property value</param>
        public SpellCheckPropertyInfo(string propertyName, string propertyValue)
        {
            this.EditorConfigPropertyName = propertyName;
            this.EditorConfigPropertyValue = propertyValue;

            this.ConfigurationPropertyName = SpellCheckerConfiguration.PropertyNameForEditorConfigSetting(propertyName);
            this.DefaultValue = SpellCheckerConfiguration.DefaultValueFor(this.ConfigurationPropertyName);
            this.CanHaveMultipleInstances = SpellCheckerConfiguration.EditorConfigSettingsFor(
                this.ConfigurationPropertyName).CanHaveMultipleInstances;
        }
        #endregion
    }
}
