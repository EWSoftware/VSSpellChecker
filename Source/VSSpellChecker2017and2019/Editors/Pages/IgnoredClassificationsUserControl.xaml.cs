//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : IgnoredClassificationsUserControl.xaml.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 09/02/2018
// Note    : Copyright 2018, Eric Woodruff, All rights reserved
//
// This file contains a user control used to edit the ignored classifications spell checker configuration settings
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 08/16/2018  EFW  Created the code
//===============================================================================================================

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;

using VisualStudio.SpellChecker.Configuration;
using VisualStudio.SpellChecker.ProjectSpellCheck;
using VisualStudio.SpellChecker.Tagging;

namespace VisualStudio.SpellChecker.Editors.Pages
{
    /// <summary>
    /// This user control is used to edit the ignored classifications spell checker configuration settings
    /// </summary>
    public partial class IgnoredClassificationsUserControl : UserControl, ISpellCheckerConfiguration
    {
        #region List box item class
        //=====================================================================

        /// <summary>
        /// This is used to contain items for the checked list box
        /// </summary>
        private class ClassificationItem : INotifyPropertyChanged
        {
            #region Private data members
            //=====================================================================

            private bool isSelected;

            #endregion

            #region Properties
            //=====================================================================

            /// <summary>
            /// The content type
            /// </summary>
            public string ContentType { get; set; }

            /// <summary>
            /// The classification
            /// </summary>
            public string Classification { get; set; }

            /// <summary>
            /// The selection state
            /// </summary>
            public bool IsSelected
            {
                get => isSelected;
                set
                {
                    if(value != isSelected)
                    {
                        isSelected = value;
                        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
                    }
                }
            }
            #endregion

            #region INotifyPropertyChanged implementation
            //=====================================================================

            public event PropertyChangedEventHandler PropertyChanged;

            #endregion
        }
        #endregion

        #region Private data members
        //=====================================================================

        private BindingList<ClassificationItem> visualStudioItems, solutionSpellCheckItems;

        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        public IgnoredClassificationsUserControl()
        {
            InitializeComponent();

            // Turn caching on to collect classification info.  We don't turn it off since other configuration
            // editors may be open.  It has little if any impact if left on for the remainder of the session.
            ClassificationCache.CachingEnabled = true;
        }
        #endregion

        #region ISpellCheckerConfiguration Members
        //=====================================================================

        /// <inheritdoc />
        public UserControl Control => this;

        /// <inheritdoc />
        public string Title => "Ignored Classifications";

        /// <inheritdoc />
        public string HelpUrl => "6a987caf-5ad9-4dab-a17c-c887881fec7a";

        /// <inheritdoc />
        public void LoadConfiguration(SpellingConfigurationFile configuration)
        {
            if(configuration.ConfigurationType == ConfigurationType.Global)
            {
                chkInheritIgnoredClassifications.IsChecked = false;
                chkInheritIgnoredClassifications.Visibility = Visibility.Collapsed;
            }
            else
                chkInheritIgnoredClassifications.IsChecked = configuration.ToBoolean(PropertyNames.InheritIgnoredClassifications);

            visualStudioItems = new BindingList<ClassificationItem>();
            solutionSpellCheckItems = new BindingList<ClassificationItem>();

            if(!configuration.HasProperty(PropertyNames.IgnoredClassifications))
            {
                if(configuration.ConfigurationType == ConfigurationType.Global)
                    foreach(var type in SpellCheckerConfiguration.DefaultIgnoredClassifications)
                        foreach(string c in type.Value)
                            visualStudioItems.Add(new ClassificationItem
                            {
                                ContentType = type.Key,
                                Classification = c,
                                IsSelected = true,
                            });
            }
            else
                foreach(var type in configuration.Element(PropertyNames.IgnoredClassifications).Elements(
                  PropertyNames.ContentType))
                {
                    string typeName = type.Attribute(PropertyNames.ContentTypeName).Value;

                    // Solution/project spell check classifications go in a separate collection
                    if(typeName.StartsWith(PropertyNames.FileType, StringComparison.Ordinal) ||
                      typeName.StartsWith(PropertyNames.Extension, StringComparison.Ordinal))
                    {
                        foreach(var c in type.Elements(PropertyNames.Classification))
                            solutionSpellCheckItems.Add(new ClassificationItem
                            {
                                ContentType = typeName,
                                Classification = c.Value,
                                IsSelected = true
                            });
                    }
                    else
                        foreach(var c in type.Elements(PropertyNames.Classification))
                            visualStudioItems.Add(new ClassificationItem
                            {
                                ContentType = typeName,
                                Classification = c.Value,
                                IsSelected = true,
                            });
                }

            cboFileType.ItemsSource = ClassifierFactory.ClassifierIds;
            cboFileType.SelectedIndex = 0;

            lbFileTypeExt.ItemsSource = solutionSpellCheckItems.Select(s => s.ContentType).Distinct().OrderBy(c => c).ToList();

            if(solutionSpellCheckItems.Count != 0)
                lbFileTypeExt.SelectedIndex = 0;

            this.btnRefresh_Click(this, null);
        }

        /// <inheritdoc />
        public void SaveConfiguration(SpellingConfigurationFile configuration)
        {
            if(configuration.ConfigurationType != ConfigurationType.Global)
                configuration.StoreProperty(PropertyNames.InheritIgnoredClassifications, chkInheritIgnoredClassifications.IsChecked);

            var ignoredClassifications = new XElement(PropertyNames.IgnoredClassifications);

            foreach(var g in visualStudioItems.Where(v => v.IsSelected).Concat(solutionSpellCheckItems.Where(
              s => s.IsSelected)).GroupBy(c => c.ContentType))
            {
                ignoredClassifications.Add(
                    new XElement(PropertyNames.ContentType,
                        new XAttribute(PropertyNames.ContentTypeName, g.Key),
                        g.Select(c => new XElement(PropertyNames.Classification, c.Classification))));
            }

            configuration.StoreElement(ignoredClassifications);
        }

        /// <inheritdoc />
        public bool AppliesTo(ConfigurationType configurationType)
        {
            return true;
        }

        /// <inheritdoc />
        public event EventHandler ConfigurationChanged;

        #endregion

        #region General event handlers
        //=====================================================================

        /// <summary>
        /// Notify the parent of property changes that affect the file's dirty state
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void Property_Changed(object sender, RoutedEventArgs e)
        {
            this.ConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }
        #endregion

        #region Visual Studio classification tab event handlers
        //=====================================================================

        /// <summary>
        /// Display the classifications for the selected content type
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void cboContentType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            lbClassifications.ItemsSource = visualStudioItems.Where(
                i => i.ContentType == (string)cboContentType.SelectedItem).OrderBy(i => i.Classification);
        }

        /// <summary>
        /// Refresh the available content types
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            BindingList<ClassificationItem> items = new BindingList<ClassificationItem>();

            // Add classifications from the cache
            foreach(var contentType in ClassificationCache.ContentTypes)
                foreach(var classification in ClassificationCache.CacheFor(contentType).Classifications)
                    items.Add(new ClassificationItem
                    {
                        ContentType = contentType,
                        Classification = classification,
                        IsSelected = visualStudioItems.Any(i => i.ContentType == contentType &&
                            i.Classification == classification && i.IsSelected)
                    });

            // Add classifications from the configuration that aren't in the cache yet
            foreach(var item in visualStudioItems)
                if(!items.Any(i => i.ContentType == item.ContentType && i.Classification == item.Classification))
                    items.Add(item);

            if(visualStudioItems != null)
                visualStudioItems.ListChanged -= this.ClassificationItems_ListChanged;

            visualStudioItems = items;
            visualStudioItems.ListChanged += this.ClassificationItems_ListChanged;

            cboContentType.ItemsSource = visualStudioItems.Select(i => i.ContentType).Distinct().OrderBy(i => i);

            if(visualStudioItems.Count != 0)
                cboContentType.SelectedIndex = 0;
        }

        /// <summary>
        /// Notify the parent of changes to the selected classifications
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void ClassificationItems_ListChanged(object sender, ListChangedEventArgs e)
        {
            this.Property_Changed(sender, null);
        }
        #endregion

        #region Project/solution classification tab event handlers
        //=====================================================================

        /// <summary>
        /// Update the available file extensions based on the file type
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void cboFileType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            cboExtension.ItemsSource = (new[] { "All" }).Concat(
                ClassifierFactory.ExtensionsFor((string)cboFileType.SelectedItem));
            cboExtension.SelectedIndex = 0;
        }

        /// <summary>
        /// Update the exclusions based on the file type or extension
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void lbFileTypeExt_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(lbFileTypeExt.SelectedIndex != -1)
            {
                string contentType = (string)lbFileTypeExt.SelectedItem;
                var classifications = solutionSpellCheckItems.Where(s => s.ContentType == contentType).ToList();

                // Don't notify about change events when adding missing items as they may not end up being used
                solutionSpellCheckItems.ListChanged -= ClassificationItems_ListChanged;

                foreach(var r in Enum.GetValues(typeof(RangeClassification)))
                    if((RangeClassification)r != RangeClassification.Undefined &&
                      !classifications.Any(c => c.Classification == r.ToString()))
                    {
                        var c = new ClassificationItem
                        {
                            ContentType = contentType,
                            Classification = r.ToString()
                        };

                        solutionSpellCheckItems.Add(c);
                        classifications.Add(c);
                    }

                solutionSpellCheckItems.ListChanged += ClassificationItems_ListChanged;

                lbExtClassifications.ItemsSource = classifications.OrderBy(c => c.Classification);
            }
        }

        /// <summary>
        /// Add a new file type/extension exclusion
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnAddExclusion_Click(object sender, RoutedEventArgs e)
        {
            var items = (IList<string>)lbFileTypeExt.ItemsSource;
            string newItem;

            // Add by file type or extension?
            if(cboExtension.SelectedIndex == 0)
                newItem = PropertyNames.FileType + (string)cboFileType.SelectedItem;
            else
                newItem = PropertyNames.Extension + (string)cboExtension.SelectedItem;

            if(!items.Contains(newItem))
            {
                items.Add(newItem);

                lbFileTypeExt.ItemsSource = null;
                lbFileTypeExt.ItemsSource = items;
                lbFileTypeExt.SelectedIndex = items.Count - 1;
            }
            else
                lbFileTypeExt.SelectedIndex = items.IndexOf(newItem);
        }
        #endregion
    }
}
