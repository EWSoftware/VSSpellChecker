//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : XmlFilesUserControl.xaml.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 04/21/2015
// Note    : Copyright 2014-2015, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
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

using VisualStudio.SpellChecker.Configuration;

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
        public UserControl Control
        {
            get { return this; }
        }

        /// <inheritdoc />
        public string Title
        {
            get { return "XML Files"; }
        }

        /// <inheritdoc />
        public string HelpUrl
        {
            get { return "db9ee77f-6932-4df7-bd06-e94f20fc7450"; }
        }

        /// <inheritdoc />
        public void LoadConfiguration(SpellingConfigurationFile configuration)
        {
            IEnumerable<string> words;

            lbIgnoredXmlElements.Items.Clear();
            lbSpellCheckedAttributes.Items.Clear();

            if(configuration.ConfigurationType == ConfigurationType.Global)
            {
                chkInheritXmlSettings.IsChecked = false;
                chkInheritXmlSettings.Visibility = Visibility.Collapsed;
            }
            else
                chkInheritXmlSettings.IsChecked = configuration.ToBoolean(PropertyNames.InheritXmlSettings);

            if(configuration.HasProperty(PropertyNames.IgnoredXmlElements))
            {
                words = configuration.ToValues(PropertyNames.IgnoredXmlElements,
                    PropertyNames.IgnoredXmlElementsItem);
            }
            else
                if(!chkInheritXmlSettings.IsChecked.Value && configuration.ConfigurationType == ConfigurationType.Global)
                    words = SpellCheckerConfiguration.DefaultIgnoredXmlElements;
                else
                    words = Enumerable.Empty<string>();

            foreach(string el in words)
                lbIgnoredXmlElements.Items.Add(el);

            if(configuration.HasProperty(PropertyNames.SpellCheckedXmlAttributes))
            {
                words = configuration.ToValues(PropertyNames.SpellCheckedXmlAttributes,
                    PropertyNames.SpellCheckedXmlAttributesItem);
            }
            else
                if(!chkInheritXmlSettings.IsChecked.Value && configuration.ConfigurationType == ConfigurationType.Global)
                    words = SpellCheckerConfiguration.DefaultSpellCheckedAttributes;
                else
                    words = Enumerable.Empty<string>();

            foreach(string el in words)
                lbSpellCheckedAttributes.Items.Add(el);

            var sd = new SortDescription { Direction = ListSortDirection.Ascending };

            lbIgnoredXmlElements.Items.SortDescriptions.Add(sd);
            lbSpellCheckedAttributes.Items.SortDescriptions.Add(sd);
        }

        /// <inheritdoc />
        public void SaveConfiguration(SpellingConfigurationFile configuration)
        {
            HashSet<string> newElementList = null, newAttributeList = null;

            if(lbIgnoredXmlElements.Items.Count != 0 || !chkInheritXmlSettings.IsChecked.Value)
            {
                newElementList = new HashSet<string>(lbIgnoredXmlElements.Items.OfType<string>());

                if(configuration.ConfigurationType == ConfigurationType.Global &&
                  newElementList.SetEquals(SpellCheckerConfiguration.DefaultIgnoredXmlElements))
                    newElementList = null;
            }

            if(lbSpellCheckedAttributes.Items.Count != 0 || !chkInheritXmlSettings.IsChecked.Value)
            {
                newAttributeList = new HashSet<string>(lbSpellCheckedAttributes.Items.OfType<string>());

                if(configuration.ConfigurationType == ConfigurationType.Global &&
                  newAttributeList.SetEquals(SpellCheckerConfiguration.DefaultSpellCheckedAttributes))
                    newAttributeList = null;
            }

            if(configuration.ConfigurationType != ConfigurationType.Global)
                configuration.StoreProperty(PropertyNames.InheritXmlSettings, chkInheritXmlSettings.IsChecked);

            configuration.StoreValues(PropertyNames.IgnoredXmlElements, PropertyNames.IgnoredXmlElementsItem,
                newElementList);
            configuration.StoreValues(PropertyNames.SpellCheckedXmlAttributes,
                PropertyNames.SpellCheckedXmlAttributesItem, newAttributeList);
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
                foreach(string word in txtIgnoredElement.Text.Split(new[] { ' ', '\t', ',', '.' },
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
                    if(idx >= lbIgnoredXmlElements.Items.Count)
                        idx = lbIgnoredXmlElements.Items.Count - 1;

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
                foreach(string el in SpellCheckerConfiguration.DefaultIgnoredXmlElements)
                    lbIgnoredXmlElements.Items.Add(el);

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
                foreach(string word in txtAttributeName.Text.Split(new[] { ' ', '\t', ',', '.' },
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
                    if(idx >= lbSpellCheckedAttributes.Items.Count)
                        idx = lbSpellCheckedAttributes.Items.Count - 1;

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
                foreach(string el in SpellCheckerConfiguration.DefaultSpellCheckedAttributes)
                    lbSpellCheckedAttributes.Items.Add(el);

            var sd = new SortDescription { Direction = ListSortDirection.Ascending };
            lbSpellCheckedAttributes.Items.SortDescriptions.Add(sd);

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
