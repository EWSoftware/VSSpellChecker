//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : ISpellCheckerConfiguration.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 04/16/2023
// Note    : Copyright 2014-2023, Eric Woodruff, All rights reserved
//
// This file contains an interface used by the configuration user controls
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 06/09/2013  EFW  Created the code
//===============================================================================================================

using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace VisualStudio.SpellChecker.Editors
{
    /// <summary>
    /// This interface is implemented by the spell checker configuration user controls
    /// </summary>
    public interface ISpellCheckerConfiguration
    {
        /// <summary>
        /// This read-only property returns a reference to the user control containing the settings
        /// </summary>
        UserControl Control { get; }

        /// <summary>
        /// This read-only property returns the category title
        /// </summary>
        string Title { get; }

        /// <summary>
        /// This read-only property returns the help URL
        /// </summary>
        string HelpUrl { get; }

        /// <summary>
        /// This is used to get or set the name of the configuration file that is being edited
        /// </summary>
        string ConfigurationFilename { get; set; }

        /// <summary>
        /// This is read-only property is used to see if the control has changes that need to be saved
        /// </summary>
        bool HasChanges { get; }

        /// <summary>
        /// Load the configuration settings for the control from the given set of spell checker properties
        /// </summary>
        /// <param name="isGlobal">True if the settings come from the global spell checker configuration file,
        /// false if not.</param>
        /// <param name="properties">The spell checker .editorconfig properties from which to load the settings</param>
        void LoadConfiguration(bool isGlobal, IDictionary<string, SpellCheckPropertyInfo> properties);

        /// <summary>
        /// Return a set of changed properties from the control to add to the configuration file
        /// </summary>
        /// <param name="isGlobal">True if the settings come from the global spell checker configuration file,
        /// false if not.</param>
        /// <param name="sectionId">The section ID used to make multiple instance properties unique</param>
        /// <returns>An enumerable list of changed properties that should be added to the .editorconfig file</returns>
        IEnumerable<(string PropertyName, string PropertyValue)> ChangedProperties(bool isGlobal, string sectionId);

        /// <summary>
        /// This event is raised to notify the parent of changes to the configuration
        /// </summary>
        event EventHandler ConfigurationChanged;
    }
}
