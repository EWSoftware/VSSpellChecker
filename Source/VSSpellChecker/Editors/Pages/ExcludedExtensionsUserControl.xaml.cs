//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : ExcludedExtensionsUserControl.xaml.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 04/21/2015
// Note    : Copyright 2015, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a user control used to edit the excluded extensions spell checker configuration settings
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
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

using VisualStudio.SpellChecker.Configuration;

namespace VisualStudio.SpellChecker.Editors.Pages
{
    /// <summary>
    /// This user control is used to edit the excluded extensions spell checker configuration settings
    /// </summary>
    public partial class ExcludedExtensionsUserControl : UserControl, ISpellCheckerConfiguration
    {
        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        public ExcludedExtensionsUserControl()
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
            get { return "Excluded Filename Extensions"; }
        }

        /// <inheritdoc />
        public string HelpUrl
        {
            get { return "70a7fa5b-1a9e-494f-9d2c-5eb3321b2596"; }
        }

        /// <inheritdoc />
        public void LoadConfiguration(SpellingConfigurationFile configuration)
        {
            IEnumerable<string> words;
            lbExcludedExtensions.Items.Clear();

            if(configuration.ConfigurationType == ConfigurationType.Global)
            {
                chkInheritExcludedExtensions.IsChecked = false;
                chkInheritExcludedExtensions.Visibility = Visibility.Collapsed;
            }
            else
                chkInheritExcludedExtensions.IsChecked = configuration.ToBoolean(PropertyNames.InheritExcludedExtensions);

            if(configuration.HasProperty(PropertyNames.ExcludedExtensions))
                words = configuration.ToValues(PropertyNames.ExcludedExtensions, PropertyNames.ExcludedExtensionsItem);
            else
                words = Enumerable.Empty<string>();

            foreach(string el in words)
                lbExcludedExtensions.Items.Add(el);

            var sd = new SortDescription { Direction = ListSortDirection.Ascending };

            lbExcludedExtensions.Items.SortDescriptions.Add(sd);
        }

        /// <inheritdoc />
        public void SaveConfiguration(SpellingConfigurationFile configuration)
        {
            HashSet<string> newList = null;

            if(lbExcludedExtensions.Items.Count != 0)
                newList = new HashSet<string>(lbExcludedExtensions.Items.OfType<string>(),
                    StringComparer.OrdinalIgnoreCase);

            if(configuration.ConfigurationType != ConfigurationType.Global)
                configuration.StoreProperty(PropertyNames.InheritExcludedExtensions, chkInheritExcludedExtensions.IsChecked);

            configuration.StoreValues(PropertyNames.ExcludedExtensions, PropertyNames.ExcludedExtensionsItem, newList);
        }

        /// <inheritdoc />
        public event EventHandler ConfigurationChanged;

        #endregion

        #region Event handlers
        //=====================================================================

        /// <summary>
        /// Add one or more new excluded extensions to the list
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnAddExcludedExt_Click(object sender, RoutedEventArgs e)
        {
            txtExcludedExtension.Text = txtExcludedExtension.Text.Trim();

            if(txtExcludedExtension.Text.Length != 0)
                foreach(string ext in txtExcludedExtension.Text.Split(new[] { ' ', '\t', ',' },
                  StringSplitOptions.RemoveEmptyEntries))
                {
                    string addExt;

                    if(ext[0] != '.')
                        addExt = "." + ext;
                    else
                        addExt = ext;

                    lbExcludedExtensions.Items.Add(addExt);
                }

            txtExcludedExtension.Text = null;
            Property_Changed(sender, e);
        }

        /// <summary>
        /// Remove the selected extension from the list of excluded extensions
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnRemoveExcludedExt_Click(object sender, RoutedEventArgs e)
        {
            int idx = lbExcludedExtensions.SelectedIndex;

            if(idx != -1)
                lbExcludedExtensions.Items.RemoveAt(idx);

            if(lbExcludedExtensions.Items.Count != 0)
            {
                if(idx < 0)
                    idx = 0;
                else
                    if(idx >= lbExcludedExtensions.Items.Count)
                        idx = lbExcludedExtensions.Items.Count - 1;

                lbExcludedExtensions.SelectedIndex = idx;
            }

            Property_Changed(sender, e);
        }

        /// <summary>
        /// Clear the list of excluded extensions
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnClearExcludedExts_Click(object sender, RoutedEventArgs e)
        {
            lbExcludedExtensions.Items.Clear();

            var sd = new SortDescription { Direction = ListSortDirection.Ascending };
            lbExcludedExtensions.Items.SortDescriptions.Add(sd);

            Property_Changed(sender, e);
        }

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
