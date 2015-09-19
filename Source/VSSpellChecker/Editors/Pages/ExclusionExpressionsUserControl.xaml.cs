//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : ExclusionExpressionsUserControl.xaml.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 09/15/2015
// Note    : Copyright 2015, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a user control used to edit the exclusion expression spell checker configuration settings
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 09/15/2015  EFW  Created the code
//===============================================================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using VisualStudio.SpellChecker.Configuration;

namespace VisualStudio.SpellChecker.Editors.Pages
{
    /// <summary>
    /// This user control is used to edit the ignored words spell checker configuration settings
    /// </summary>
    public partial class ExclusionExpressionsUserControl : UserControl, ISpellCheckerConfiguration
    {
        #region Private data members
        //=====================================================================

        private List<Regex> expressions;

        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        public ExclusionExpressionsUserControl()
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
            get { return "Exclusion Expressions"; }
        }

        /// <inheritdoc />
        public string HelpUrl
        {
            get { return "6216eedb-6434-4cad-be06-576814e0b735"; }
        }

        /// <inheritdoc />
        public void LoadConfiguration(SpellingConfigurationFile configuration)
        {
            string displayText;

            lbExclusionExpressions.Items.Clear();

            if(configuration.ConfigurationType == ConfigurationType.Global)
            {
                chkInheritExclusionExpressions.IsChecked = false;
                chkInheritExclusionExpressions.Visibility = Visibility.Collapsed;
            }
            else
                chkInheritExclusionExpressions.IsChecked = configuration.ToBoolean(PropertyNames.InheritExclusionExpressions);

            if(configuration.HasProperty(PropertyNames.ExclusionExpressions))
            {
                expressions = configuration.ToRegexes(PropertyNames.ExclusionExpressions,
                    PropertyNames.ExclusionExpressionItem).OrderBy(exp => exp.ToString()).ToList();
            }
            else
                expressions = new List<Regex>();

            foreach(var exp in expressions)
            {
                displayText = exp.ToString();

                if(exp.Options != RegexOptions.None)
                    displayText += "  (" + exp.Options.ToString() + ")";

                lbExclusionExpressions.Items.Add(displayText);
            }

            btnEditExpression.IsEnabled = btnRemoveExpression.IsEnabled = (expressions.Count != 0);
        }

        /// <inheritdoc />
        public void SaveConfiguration(SpellingConfigurationFile configuration)
        {
            if(configuration.ConfigurationType != ConfigurationType.Global)
                configuration.StoreProperty(PropertyNames.InheritExclusionExpressions, chkInheritExclusionExpressions.IsChecked);

            configuration.StoreRegexes(PropertyNames.ExclusionExpressions, PropertyNames.ExclusionExpressionItem,
                expressions.Count == 0 ? null : expressions);
        }

        /// <inheritdoc />
        public event EventHandler ConfigurationChanged;

        #endregion

        #region Event handlers
        //=====================================================================

        /// <summary>
        /// Add a new exclusion expression
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnAddExpression_Click(object sender, RoutedEventArgs e)
        {
            var form = new ExclusionExpressionAddEditForm();

            if(form.ShowDialog() ?? false)
            {
                expressions.Add(form.Expression);

                string displayText = form.Expression.ToString();

                if(form.Expression.Options != RegexOptions.None)
                    displayText += "  (" + form.Expression.Options.ToString() + ")";

                lbExclusionExpressions.SelectedIndex = lbExclusionExpressions.Items.Add(displayText);

                btnEditExpression.IsEnabled = btnRemoveExpression.IsEnabled = true;
                Property_Changed(sender, e);
            }
        }

        /// <summary>
        /// Edit the selected exclusion expression
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnEditExpression_Click(object sender, RoutedEventArgs e)
        {
            int idx = lbExclusionExpressions.SelectedIndex;

            if(idx != -1)
            {
                var form = new ExclusionExpressionAddEditForm();

                form.Expression = expressions[idx];

                if(form.ShowDialog() ?? false)
                {
                    expressions[idx] = form.Expression;

                    string displayText = form.Expression.ToString();

                    if(form.Expression.Options != RegexOptions.None)
                        displayText += "  (" + form.Expression.Options.ToString() + ")";

                    lbExclusionExpressions.Items[idx] = displayText;
                    lbExclusionExpressions.SelectedIndex = idx;

                    Property_Changed(sender, e);
                }
            }
            else
                if(lbExclusionExpressions.Items.Count != 0)
                    lbExclusionExpressions.SelectedIndex = 0;
        }

        /// <summary>
        /// Remove the selected exclusion expression from the list
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnRemoveExpression_Click(object sender, RoutedEventArgs e)
        {
            int idx = lbExclusionExpressions.SelectedIndex;

            if(idx != -1)
            {
                expressions.RemoveAt(idx);
                lbExclusionExpressions.Items.RemoveAt(idx);
                btnEditExpression.IsEnabled = btnRemoveExpression.IsEnabled = (expressions.Count != 0);

                Property_Changed(sender, e);
            }

            if(lbExclusionExpressions.Items.Count != 0)
            {
                if(idx < 0)
                    idx = 0;
                else
                    if(idx >= lbExclusionExpressions.Items.Count)
                        idx = lbExclusionExpressions.Items.Count - 1;

                lbExclusionExpressions.SelectedIndex = idx;
            }
        }

        /// <summary>
        /// Treat list box double clicks as edit requests
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void lbExclusionExpressions_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            UIElement element = (UIElement)lbExclusionExpressions.InputHitTest(
                e.GetPosition(lbExclusionExpressions));

            while(element != lbExclusionExpressions)
            {
                if(element is ListBoxItem)
                {
                    btnEditExpression_Click(sender, e);
                    break;
                }

                element = (UIElement)VisualTreeHelper.GetParent(element);
            }
        }

        /// <summary>
        /// Notify the parent of property changes that affect the file's dirty state
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void Property_Changed(object sender, RoutedEventArgs e)
        {
            var handler = ConfigurationChanged;

            if(handler != null)
                handler(this, EventArgs.Empty);
        }
        #endregion
    }
}
