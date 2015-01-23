//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : XmlFilesUserControl.xaml.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 06/14/2014
// Note    : Copyright 2014, Eric Woodruff, All rights reserved
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

using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace VisualStudio.SpellChecker.UI
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
            get { return this.Title; }
        }

        /// <inheritdoc />
        public bool IsValid
        {
            get { return true; }
        }

        /// <inheritdoc />
        public void LoadConfiguration()
        {
            lbIgnoredXmlElements.Items.Clear();
            lbSpellCheckedAttributes.Items.Clear();

            foreach(string el in SpellCheckerConfiguration.IgnoredXmlElements)
                lbIgnoredXmlElements.Items.Add(el);

            foreach(string el in SpellCheckerConfiguration.SpellCheckedXmlAttributes)
                lbSpellCheckedAttributes.Items.Add(el);

            var sd = new SortDescription { Direction = ListSortDirection.Ascending };

            lbIgnoredXmlElements.Items.SortDescriptions.Add(sd);
            lbSpellCheckedAttributes.Items.SortDescriptions.Add(sd);
        }

        /// <inheritdoc />
        public bool SaveConfiguration()
        {
            SpellCheckerConfiguration.SetIgnoredXmlElements(lbIgnoredXmlElements.Items.OfType<string>());
            SpellCheckerConfiguration.SetSpellCheckedXmlAttributes(lbSpellCheckedAttributes.Items.OfType<string>());

            return true;
        }
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
                idx = lbIgnoredXmlElements.Items.IndexOf(txtIgnoredElement.Text);

                if(idx == -1)
                    idx = lbIgnoredXmlElements.Items.Add(txtIgnoredElement.Text);

                if(idx != -1)
                {
                    lbIgnoredXmlElements.SelectedIndex = idx;
                    lbIgnoredXmlElements.ScrollIntoView(lbIgnoredXmlElements.Items[idx]);
                }

                txtIgnoredElement.Text = null;
            }
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
        }

        /// <summary>
        /// Reset the ignored XML elements to the default list
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnDefaultElements_Click(object sender, RoutedEventArgs e)
        {
            lbIgnoredXmlElements.Items.Clear();

            foreach(string el in SpellCheckerConfiguration.DefaultIgnoredXmlElements)
                lbIgnoredXmlElements.Items.Add(el);

            var sd = new SortDescription { Direction = ListSortDirection.Ascending };
            lbIgnoredXmlElements.Items.SortDescriptions.Add(sd);
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
                idx = lbSpellCheckedAttributes.Items.IndexOf(txtAttributeName.Text);

                if(idx == -1)
                    idx = lbSpellCheckedAttributes.Items.Add(txtAttributeName.Text);

                if(idx != -1)
                {
                    lbSpellCheckedAttributes.SelectedIndex = idx;
                    lbSpellCheckedAttributes.ScrollIntoView(lbSpellCheckedAttributes.Items[idx]);
                }

                txtAttributeName.Text = null;
            }
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
        }

        /// <summary>
        /// Reset the spell checked attributes to the default list
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnDefaultAttributes_Click(object sender, RoutedEventArgs e)
        {
            lbSpellCheckedAttributes.Items.Clear();

            foreach(string el in SpellCheckerConfiguration.DefaultSpellCheckedAttributes)
                lbSpellCheckedAttributes.Items.Add(el);

            var sd = new SortDescription { Direction = ListSortDirection.Ascending };
            lbSpellCheckedAttributes.Items.SortDescriptions.Add(sd);
        }
        #endregion
    }
}
