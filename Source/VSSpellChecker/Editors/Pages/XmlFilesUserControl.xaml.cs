//===============================================================================================================
// System  : Spell Check My Code Package
// File    : XmlFilesUserControl.xaml.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 08/30/2025
// Note    : Copyright 2014-2025, Eric Woodruff, All rights reserved
//
// This file contains a user control used to edit the XML files spell checker configuration settings
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 06/09/2014  EFW  Moved the XML files settings to a user control
//===============================================================================================================

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

using VisualStudio.SpellChecker.Common.Configuration;

namespace VisualStudio.SpellChecker.Editors.Pages
{
    /// <summary>
    /// This user control is used to edit the XML files spell checker configuration settings
    /// </summary>
    public partial class XmlFilesUserControl : UserControl, ISpellCheckerConfiguration
    {
        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        public XmlFilesUserControl()
        {
            InitializeComponent();
        }
        #endregion

        #region ISpellCheckerConfiguration Members
        //=====================================================================

        /// <inheritdoc />
        public UserControl Control => this;

        /// <inheritdoc />
        public string Title => "XML Files";

        /// <inheritdoc />
        public string HelpUrl => "db9ee77f-6932-4df7-bd06-e94f20fc7450";

        /// <inheritdoc />
        public string ConfigurationFilename { get; set; }

        /// <inheritdoc />
        public bool HasChanges { get; private set; }

        /// <inheritdoc />
        public void LoadConfiguration(bool isGlobal, IDictionary<string, SpellCheckPropertyInfo> properties)
        {
            var values = new List<string>();

            lbIgnoredXmlElements.Items.Clear();
            lbSpellCheckedAttributes.Items.Clear();

            if(isGlobal)
            {
                chkInheritXmlSettings.IsChecked = false;
                chkInheritXmlSettings.Visibility = Visibility.Collapsed;
            }
            else
                chkInheritXmlSettings.IsChecked = true;

            if(properties.TryGetValue(nameof(SpellCheckerConfiguration.IgnoredXmlElements), out var spi))
            {
                values.AddRange(spi.EditorConfigPropertyValue.Split([',', ' '],
                    StringSplitOptions.RemoveEmptyEntries));

                if(values.Count != 0 && values[0].Equals(SpellCheckerConfiguration.ClearInherited,
                  StringComparison.OrdinalIgnoreCase))
                {
                    chkInheritXmlSettings.IsChecked = false;
                    values.RemoveAt(0);
                }
            }
            else
            {
                if(isGlobal)
                    values.AddRange(SpellCheckerConfiguration.DefaultIgnoredXmlElements);
            }

            foreach(string el in values)
                lbIgnoredXmlElements.Items.Add(el);

            values.Clear();

            if(properties.TryGetValue(nameof(SpellCheckerConfiguration.SpellCheckedXmlAttributes), out spi))
            {
                values.AddRange(spi.EditorConfigPropertyValue.Split([',', ' '],
                    StringSplitOptions.RemoveEmptyEntries));

                if(values.Count != 0 && values[0].Equals(SpellCheckerConfiguration.ClearInherited,
                  StringComparison.OrdinalIgnoreCase))
                {
                    chkInheritXmlSettings.IsChecked = false;
                    values.RemoveAt(0);
                }
            }
            else
            {
                if(isGlobal)
                    values.AddRange(SpellCheckerConfiguration.DefaultSpellCheckedAttributes);
            }

            foreach(string attr in values)
                lbSpellCheckedAttributes.Items.Add(attr);

            var sd = new SortDescription { Direction = ListSortDirection.Ascending };

            lbIgnoredXmlElements.Items.SortDescriptions.Add(sd);
            lbSpellCheckedAttributes.Items.SortDescriptions.Add(sd);

            this.HasChanges = false;
        }

        /// <inheritdoc />
        public IEnumerable<(string PropertyName, string PropertyValue)> ChangedProperties(bool isGlobal,
          string sectionId)
        {
            HashSet<string> compareSet;
            List<string> newValues;

            if(lbIgnoredXmlElements.Items.Count != 0 || !chkInheritXmlSettings.IsChecked.Value || isGlobal)
            {
                compareSet = [.. lbIgnoredXmlElements.Items.Cast<string>()];
                newValues = [.. compareSet];

                if(isGlobal && compareSet.SetEquals(SpellCheckerConfiguration.DefaultIgnoredXmlElements))
                    newValues.Clear();

                // The global configuration always clears the list to ensure we don't get any default elements
                // that were removed.
                if((isGlobal && newValues.Count != 0) || (!isGlobal && !chkInheritXmlSettings.IsChecked.Value))
                    newValues.Insert(0, SpellCheckerConfiguration.ClearInherited);

                if(newValues.Count != 0)
                {
                    yield return (SpellCheckerConfiguration.EditorConfigSettingsFor(
                        nameof(SpellCheckerConfiguration.IgnoredXmlElements)).PropertyName + sectionId,
                            String.Join(",", newValues));
                }
            }

            if(lbSpellCheckedAttributes.Items.Count != 0 || !chkInheritXmlSettings.IsChecked.Value || isGlobal)
            {
                compareSet = [.. lbSpellCheckedAttributes.Items.Cast<string>()];
                newValues = [.. compareSet];

                if(isGlobal && compareSet.SetEquals(SpellCheckerConfiguration.DefaultSpellCheckedAttributes))
                    newValues.Clear();

                // The global configuration always clears the list to ensure we don't get any default attributes
                // that were removed.
                if((isGlobal && newValues.Count != 0) || (!isGlobal && !chkInheritXmlSettings.IsChecked.Value))
                    newValues.Insert(0, SpellCheckerConfiguration.ClearInherited);

                if(newValues.Count != 0)
                {
                    yield return (SpellCheckerConfiguration.EditorConfigSettingsFor(
                        nameof(SpellCheckerConfiguration.SpellCheckedXmlAttributes)).PropertyName + sectionId,
                            String.Join(",", newValues));
                }
            }
        }

        /// <inheritdoc />
        public event EventHandler ConfigurationChanged;

        #endregion

        #region Event handlers
        //=====================================================================

        /// <summary>
        /// Add a new ignored XML element name to the list
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnAddElement_Click(object sender, RoutedEventArgs e)
        {
            int idx;

            txtIgnoredElement.Text = txtIgnoredElement.Text.Trim();

            if(txtIgnoredElement.Text.Length != 0)
            {
                foreach(string word in txtIgnoredElement.Text.Split([' ', '\t', ',', '.', '|'],
                  StringSplitOptions.RemoveEmptyEntries))
                {
                    idx = lbIgnoredXmlElements.Items.IndexOf(word);

                    if(idx == -1)
                        idx = lbIgnoredXmlElements.Items.Add(word);

                    if(idx != -1)
                    {
                        lbIgnoredXmlElements.SelectedIndex = idx;
                        lbIgnoredXmlElements.ScrollIntoView(lbIgnoredXmlElements.Items[idx]);
                    }

                    txtIgnoredElement.Text = null;
                }
            }

            Property_Changed(sender, e);
        }

        /// <summary>
        /// Remove the selected element from the list of ignored elements
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnRemoveElement_Click(object sender, RoutedEventArgs e)
        {
            int idx = lbIgnoredXmlElements.SelectedIndex;

            if(idx != -1)
                lbIgnoredXmlElements.Items.RemoveAt(idx);

            if(lbIgnoredXmlElements.Items.Count != 0)
            {
                if(idx < 0)
                    idx = 0;
                else
                {
                    if(idx >= lbIgnoredXmlElements.Items.Count)
                        idx = lbIgnoredXmlElements.Items.Count - 1;
                }

                lbIgnoredXmlElements.SelectedIndex = idx;
            }

            Property_Changed(sender, e);
        }

        /// <summary>
        /// Reset the ignored XML elements to the default list
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnDefaultElements_Click(object sender, RoutedEventArgs e)
        {
            lbIgnoredXmlElements.Items.Clear();

            if(!chkInheritXmlSettings.IsChecked.Value)
            {
                foreach(string el in SpellCheckerConfiguration.DefaultIgnoredXmlElements)
                    lbIgnoredXmlElements.Items.Add(el);
            }

            var sd = new SortDescription { Direction = ListSortDirection.Ascending };
            lbIgnoredXmlElements.Items.SortDescriptions.Add(sd);

            Property_Changed(sender, e);
        }

        /// <summary>
        /// Add a new spell checked attribute name to the list
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnAddAttribute_Click(object sender, RoutedEventArgs e)
        {
            int idx;

            txtAttributeName.Text = txtAttributeName.Text.Trim();

            if(txtAttributeName.Text.Length != 0)
            {
                foreach(string word in txtAttributeName.Text.Split([' ', '\t', ',', '.', '|'],
                  StringSplitOptions.RemoveEmptyEntries))
                {
                    idx = lbSpellCheckedAttributes.Items.IndexOf(word);

                    if(idx == -1)
                        idx = lbSpellCheckedAttributes.Items.Add(word);

                    if(idx != -1)
                    {
                        lbSpellCheckedAttributes.SelectedIndex = idx;
                        lbSpellCheckedAttributes.ScrollIntoView(lbSpellCheckedAttributes.Items[idx]);
                    }

                    txtAttributeName.Text = null;
                }
            }

            Property_Changed(sender, e);
        }

        /// <summary>
        /// Remove the selected attribute from the list of spell checked attributes
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnRemoveAttribute_Click(object sender, RoutedEventArgs e)
        {
            int idx = lbSpellCheckedAttributes.SelectedIndex;

            if(idx != -1)
                lbSpellCheckedAttributes.Items.RemoveAt(idx);

            if(lbSpellCheckedAttributes.Items.Count != 0)
            {
                if(idx < 0)
                    idx = 0;
                else
                {
                    if(idx >= lbSpellCheckedAttributes.Items.Count)
                        idx = lbSpellCheckedAttributes.Items.Count - 1;
                }

                lbSpellCheckedAttributes.SelectedIndex = idx;
            }

            Property_Changed(sender, e);
        }

        /// <summary>
        /// Reset the spell checked attributes to the default list
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnDefaultAttributes_Click(object sender, RoutedEventArgs e)
        {
            lbSpellCheckedAttributes.Items.Clear();

            if(!chkInheritXmlSettings.IsChecked.Value)
            {
                foreach(string el in SpellCheckerConfiguration.DefaultSpellCheckedAttributes)
                    lbSpellCheckedAttributes.Items.Add(el);
            }

            var sd = new SortDescription { Direction = ListSortDirection.Ascending };
            lbSpellCheckedAttributes.Items.SortDescriptions.Add(sd);

            Property_Changed(sender, e);
        }

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
