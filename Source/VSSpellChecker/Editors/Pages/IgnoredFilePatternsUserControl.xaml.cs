//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : IgnoredFilePatternsUserControl.xaml.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 09/02/2018
// Note    : Copyright 2015-2018, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a user control used to edit the ignore file patterns spell checker configuration settings
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 02/08/2015  EFW  Created the code
// 03/10/2016  EFW  Changed from extensions only to full filename wildcard patterns
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
    /// This user control is used to edit the ignored file patterns spell checker configuration settings
    /// </summary>
    public partial class IgnoredFilePatternsUserControl : UserControl, ISpellCheckerConfiguration
    {
        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        public IgnoredFilePatternsUserControl()
        {
            InitializeComponent();
        }
        #endregion

        #region ISpellCheckerConfiguration Members
        //=====================================================================

        /// <inheritdoc />
        public UserControl Control => this;

        /// <inheritdoc />
        public string Title => "Ignored Files";

        /// <inheritdoc />
        public string HelpUrl => "70a7fa5b-1a9e-494f-9d2c-5eb3321b2596";

        /// <inheritdoc />
        public void LoadConfiguration(SpellingConfigurationFile configuration)
        {
            IEnumerable<string> patterns;
            lbIgnoredFilePatterns.Items.Clear();

            if(configuration.ConfigurationType == ConfigurationType.Global)
            {
                chkInheritIgnoredFilePatterns.IsChecked = false;
                chkInheritIgnoredFilePatterns.Visibility = Visibility.Collapsed;
            }
            else
                chkInheritIgnoredFilePatterns.IsChecked = configuration.ToBoolean(PropertyNames.InheritIgnoredFilePatterns);

            if(configuration.HasProperty(PropertyNames.IgnoredFilePatterns))
                patterns = configuration.ToValues(PropertyNames.IgnoredFilePatterns, PropertyNames.IgnoredFilePatternItem);
            else
                if(!chkInheritIgnoredFilePatterns.IsChecked.Value && configuration.ConfigurationType == ConfigurationType.Global)
                    patterns = SpellCheckerConfiguration.DefaultIgnoredFilePatterns;
                else
                    patterns = Enumerable.Empty<string>();

            foreach(string el in patterns)
                lbIgnoredFilePatterns.Items.Add(el);

            var sd = new SortDescription { Direction = ListSortDirection.Ascending };

            lbIgnoredFilePatterns.Items.SortDescriptions.Add(sd);
        }

        /// <inheritdoc />
        public void SaveConfiguration(SpellingConfigurationFile configuration)
        {
            HashSet<string> newList = null;

            if(lbIgnoredFilePatterns.Items.Count != 0 || !chkInheritIgnoredFilePatterns.IsChecked.Value)
            {
                newList = new HashSet<string>(lbIgnoredFilePatterns.Items.Cast<string>(),
                    StringComparer.OrdinalIgnoreCase);

                if(configuration.ConfigurationType == ConfigurationType.Global &&
                  newList.SetEquals(SpellCheckerConfiguration.DefaultIgnoredFilePatterns))
                    newList = null;
            }

            if(configuration.ConfigurationType != ConfigurationType.Global)
                configuration.StoreProperty(PropertyNames.InheritIgnoredFilePatterns, chkInheritIgnoredFilePatterns.IsChecked);

            configuration.StoreValues(PropertyNames.IgnoredFilePatterns, PropertyNames.IgnoredFilePatternItem, newList);
        }

        /// <inheritdoc />
        public bool AppliesTo(ConfigurationType configurationType)
        {
            return true;
        }

        /// <inheritdoc />
        public event EventHandler ConfigurationChanged;

        #endregion

        #region Event handlers
        //=====================================================================

        /// <summary>
        /// Add a new ignored filename pattern to the list
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnAddFilePattern_Click(object sender, RoutedEventArgs e)
        {
            txtFilePattern.Text = txtFilePattern.Text.Trim();

            if(txtFilePattern.Text.Length != 0)
                lbIgnoredFilePatterns.Items.Add(txtFilePattern.Text);

            txtFilePattern.Text = null;
            Property_Changed(sender, e);
        }

        /// <summary>
        /// Remove the selected filename pattern from the list
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnRemoveFilePattern_Click(object sender, RoutedEventArgs e)
        {
            int idx = lbIgnoredFilePatterns.SelectedIndex;

            if(idx != -1)
                lbIgnoredFilePatterns.Items.RemoveAt(idx);

            if(lbIgnoredFilePatterns.Items.Count != 0)
            {
                if(idx < 0)
                    idx = 0;
                else
                    if(idx >= lbIgnoredFilePatterns.Items.Count)
                        idx = lbIgnoredFilePatterns.Items.Count - 1;

                lbIgnoredFilePatterns.SelectedIndex = idx;
            }

            Property_Changed(sender, e);
        }

        /// <summary>
        /// Clear the list of ignored file patterns
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnClearFilePatterns_Click(object sender, RoutedEventArgs e)
        {
            lbIgnoredFilePatterns.Items.Clear();

            var sd = new SortDescription { Direction = ListSortDirection.Ascending };
            lbIgnoredFilePatterns.Items.SortDescriptions.Add(sd);

            Property_Changed(sender, e);
        }

        /// <summary>
        /// Reset the list to the list of default ignored file patterns
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnDefaultFilePatterns_Click(object sender, RoutedEventArgs e)
        {
            lbIgnoredFilePatterns.Items.Clear();

            if(!chkInheritIgnoredFilePatterns.IsChecked.Value)
                foreach(string el in SpellCheckerConfiguration.DefaultIgnoredFilePatterns)
                    lbIgnoredFilePatterns.Items.Add(el);

            var sd = new SortDescription { Direction = ListSortDirection.Ascending };
            lbIgnoredFilePatterns.Items.SortDescriptions.Add(sd);

            Property_Changed(sender, e);
        }

        /// <summary>
        /// Notify the parent of property changes that affect the file's dirty state
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void Property_Changed(object sender, System.Windows.RoutedEventArgs e)
        {
            this.ConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }
        #endregion
    }
}
