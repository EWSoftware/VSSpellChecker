//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : FileInfoUserControl.xaml.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 09/02/2018
// Note    : Copyright 2015-2018, Eric Woodruff, All rights reserved
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
using System.Windows;
using System.Windows.Controls;

using VisualStudio.SpellChecker.Configuration;

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
        public void LoadConfiguration(SpellingConfigurationFile configuration)
        {
            tbGlobal.Visibility = fdvAddConfigs.Visibility =
                (configuration.ConfigurationType == ConfigurationType.Global) ? Visibility.Visible : Visibility.Collapsed;
            tbSolution.Visibility = (configuration.ConfigurationType == ConfigurationType.Solution) ?
                Visibility.Visible : Visibility.Collapsed;
            tbProject.Visibility = (configuration.ConfigurationType == ConfigurationType.Project) ?
                Visibility.Visible : Visibility.Collapsed;
            tbFolder.Visibility = (configuration.ConfigurationType == ConfigurationType.Folder) ?
                Visibility.Visible : Visibility.Collapsed;
            tbFile.Visibility = (configuration.ConfigurationType == ConfigurationType.File) ?
                Visibility.Visible : Visibility.Collapsed;
        }

        /// <inheritdoc />
        public void SaveConfiguration(SpellingConfigurationFile configuration)
        {
            // Nothing to save here, just refresh the information
            this.LoadConfiguration(configuration);
        }

#pragma warning disable 67
        /// <inheritdoc />
        /// <remarks>This event is not used by this page</remarks>
        public event EventHandler ConfigurationChanged;
#pragma warning restore 67

        #endregion
    }
}
