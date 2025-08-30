//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : FileInfoUserControl.xaml.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 08/30/2025
// Note    : Copyright 2015-2025, Eric Woodruff, All rights reserved
//
// This file contains a user control used to provide some information about the settings file
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 02/08/2015  EFW  Created the code
//===============================================================================================================

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace VisualStudio.SpellChecker.Editors.Pages
{
    /// <summary>
    /// This is a simple page that provides information about a spell checker configuration file
    /// </summary>
    public partial class FileInfoUserControl : UserControl, ISpellCheckerConfiguration
    {
        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        public FileInfoUserControl()
        {
            InitializeComponent();
        }
        #endregion

        #region ISpellCheckerConfiguration Members
        //=====================================================================

        /// <inheritdoc />
        public UserControl Control => this;

        /// <inheritdoc />
        public string Title => "File Information";

        /// <inheritdoc />
        public string HelpUrl => "7b2bc3bb-5b5c-4d17-a88e-d58b476e49ab";

        /// <inheritdoc />
        public string ConfigurationFilename { get; set; }

        /// <inheritdoc />
        public bool HasChanges => false;

        /// <inheritdoc />
        public void LoadConfiguration(bool isGlobal, IDictionary<string, SpellCheckPropertyInfo> properties)
        {
            tbGlobal.Visibility = fdvAddConfigs.Visibility = isGlobal ? Visibility.Visible : Visibility.Collapsed;
            tbAllOthers.Visibility = !isGlobal ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <inheritdoc />
        public IEnumerable<(string PropertyName, string PropertyValue)> ChangedProperties(bool isGlobal,
          string sectionId)
        {
            // Nothing to do for this one
            return [];
        }

#pragma warning disable 67
        /// <inheritdoc />
        /// <remarks>This event is not used by this page</remarks>
        public event EventHandler ConfigurationChanged;
#pragma warning restore 67

        #endregion
    }
}
