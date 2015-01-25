//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : SpellCheckerConfigDlg.xaml.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 01/25/2015
// Note    : Copyright 2013-2015, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a window used to edit the spell checker configuration settings
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 04/14/2013  EFW  Created the code
// 06/09/2014  EFW  Reworked to use a tree view and user controls for the various configuration categories
//===============================================================================================================

using System;
using System.Diagnostics;
using System.Globalization;
using System.Web;
using System.Windows;
using System.Windows.Controls;

using PackageResources = VisualStudio.SpellChecker.Properties.Resources;

namespace VisualStudio.SpellChecker.UI
{
    /// <summary>
    /// This window is used to modify the Visual Studio spell checker configuration settings
    /// </summary>
    /// <remarks>Settings are stored in an XML file in the user's local application data folder and will be used
    /// by all versions of Visual Studio in which the package is installed.</remarks>
    public partial class SpellCheckerConfigDlg : Window
    {
        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        public SpellCheckerConfigDlg()
        {
            ISpellCheckerConfiguration page;
            TreeViewItem node;

            InitializeComponent();

            // The property pages will be listed in this order
            Type[] propertyPages = new[] {
                typeof(GeneralSettingsUserControl),
                typeof(UserDictionaryUserControl),
                typeof(IgnoredWordsUserControl),
                typeof(XmlFilesUserControl),
                typeof(CSharpOptionsUserControl)
            };

            try
            {
                tvPages.BeginInit();

                // Create the property pages
                foreach(Type pageType in propertyPages)
                {
                    page = (ISpellCheckerConfiguration)Activator.CreateInstance(pageType);
                    page.Control.Visibility = Visibility.Collapsed;

                    node = new TreeViewItem();
                    node.Header = page.Title;
                    node.Name = pageType.Name;
                    node.Tag = page;

                    tvPages.Items.Add(node);
                    pnlPages.Children.Add(page.Control);
                }

                foreach(TreeViewItem item in tvPages.Items)
                    ((ISpellCheckerConfiguration)item.Tag).LoadConfiguration();
            }
            finally
            {
                tvPages.EndInit();

                if(tvPages.Items.Count != 0)
                    ((TreeViewItem)tvPages.Items[0]).IsSelected = true;
            }
        }
        #endregion

        #region Event handlers
        //=====================================================================

        /// <summary>
        /// View the project website
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void lnkProjectSite_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(lnkProjectSite.NavigateUri.AbsoluteUri);
            }
            catch(Exception ex)
            {
                MessageBox.Show("Unable to navigate to website.  Reason: " + ex.Message,
                    PackageResources.PackageTitle, MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }

        /// <summary>
        /// View help for the selected property category
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            TreeViewItem item = (TreeViewItem)tvPages.SelectedItem;

            if(item != null)
            {
                ISpellCheckerConfiguration page = (ISpellCheckerConfiguration)item.Tag;

                try
                {
                    string targetUrl = lnkProjectSite.NavigateUri.AbsoluteUri + "/wiki/" +
                        HttpUtility.UrlEncode(page.HelpUrl);

                    Process.Start(targetUrl);
                }
                catch(Exception ex)
                {
                    MessageBox.Show("Unable to navigate to website.  Reason: " + ex.Message,
                        PackageResources.PackageTitle, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
            }
        }

        /// <summary>
        /// Close this form
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Save changes to the configuration
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            foreach(TreeViewItem item in tvPages.Items)
            {
                ISpellCheckerConfiguration page = (ISpellCheckerConfiguration)item.Tag;

                if(!page.SaveConfiguration())
                {
                    item.IsSelected = true;
                    return;
                }
            }

            if(!SpellCheckerConfiguration.SaveConfiguration())
                MessageBox.Show("Unable to save spell checking configuration", PackageResources.PackageTitle,
                    MessageBoxButton.OK, MessageBoxImage.Exclamation);

            this.Close();
        }

        /// <summary>
        /// Reset the configuration to its default settings excluding the user dictionary
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            if(MessageBox.Show("Are you sure you want to reset the configuration to its default settings " +
              "(excluding the user dictionary)?", PackageResources.PackageTitle, MessageBoxButton.YesNo,
              MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes)
            {
                SpellCheckerConfiguration.ResetConfiguration(true);

                foreach(TreeViewItem item in tvPages.Items)
                    ((ISpellCheckerConfiguration)item.Tag).LoadConfiguration();
            }
        }

        /// <summary>
        /// Prevent the user from changing the selected tree view item if the page is not valid
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void tvPages_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            TreeViewItem item = (TreeViewItem)tvPages.SelectedItem;

            if(item != null)
            {
                ISpellCheckerConfiguration page = (ISpellCheckerConfiguration)item.Tag;

                e.Handled = !page.IsValid;
            }
        }

        /// <summary>
        /// Change the displayed property page based on the selected tree view item
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void tvPages_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            foreach(TreeViewItem item in tvPages.Items)
            {
                ISpellCheckerConfiguration page = (ISpellCheckerConfiguration)item.Tag;

                if(item.IsSelected)
                    page.Control.Visibility = Visibility.Visible;
                else
                    page.Control.Visibility = Visibility.Collapsed;
            }
        }
        #endregion
    }
}
