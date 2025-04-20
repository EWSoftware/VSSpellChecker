//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : ExclusionExpressionsUserControl.xaml.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 04/16/2023
// Note    : Copyright 2015-2023, Eric Woodruff, All rights reserved
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

using VisualStudio.SpellChecker.Common;
using VisualStudio.SpellChecker.Common.Configuration;

namespace VisualStudio.SpellChecker.Editors.Pages
{
    /// <summary>
    /// This user control is used to edit the exclusion expression spell checker configuration settings
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
        public UserControl Control => this;

        /// <inheritdoc />
        public string Title => "Exclusion Expressions";

        /// <inheritdoc />
        public string HelpUrl => "6216eedb-6434-4cad-be06-576814e0b735";

        /// <inheritdoc />
        public string ConfigurationFilename { get; set; }

        /// <inheritdoc />
        public bool HasChanges { get; private set; }

        /// <inheritdoc />
        public void LoadConfiguration(bool isGlobal, IDictionary<string, SpellCheckPropertyInfo> properties)
        {
            string displayText;

            lbExclusionExpressions.Items.Clear();

            if(isGlobal)
            {
                chkInheritExclusionExpressions.IsChecked = false;
                chkInheritExclusionExpressions.Visibility = Visibility.Collapsed;
            }
            else
                chkInheritExclusionExpressions.IsChecked = true;

            if(properties.TryGetValue(nameof(SpellCheckerConfiguration.ExclusionExpressions), out var spi))
            {
                expressions = spi.EditorConfigPropertyValue.ToRegexes().ToList();

                if(expressions.Count != 0 && expressions[0].ToString().Equals(SpellCheckerConfiguration.ClearInherited,
                  StringComparison.OrdinalIgnoreCase))
                {
                    chkInheritExclusionExpressions.IsChecked = false;
                    expressions.RemoveAt(0);
                }
            }
            else
                expressions = new List<Regex>();

            foreach(var exp in expressions.OrderBy(e => e.ToString()))
            {
                displayText = exp.ToString();

                if(exp.Options != RegexOptions.None)
                    displayText += "  (" + exp.Options.ToString() + ")";

                lbExclusionExpressions.Items.Add(displayText);
            }

            btnEditExpression.IsEnabled = btnRemoveExpression.IsEnabled = (expressions.Count != 0);

            this.HasChanges = false;
        }

        /// <inheritdoc />
        public IEnumerable<(string PropertyName, string PropertyValue)> ChangedProperties(bool isGlobal,
          string sectionId)
        {
            var values = new List<string>(expressions.Select(r => $"{r}(?#/Options/{r.Options})"));

            if(!isGlobal && !chkInheritExclusionExpressions.IsChecked.Value)
                values.Insert(0, SpellCheckerConfiguration.ClearInherited + "(?#/Options/)");

            if(values.Count != 0)
            {
                yield return (SpellCheckerConfiguration.EditorConfigSettingsFor(
                    nameof(SpellCheckerConfiguration.ExclusionExpressions)).PropertyName + sectionId,
                        String.Concat(values));
            }
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
                var form = new ExclusionExpressionAddEditForm { Expression = expressions[idx] };

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
            {
                if(lbExclusionExpressions.Items.Count != 0)
                    lbExclusionExpressions.SelectedIndex = 0;
            }
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
                {
                    if(idx >= lbExclusionExpressions.Items.Count)
                        idx = lbExclusionExpressions.Items.Count - 1;
                }

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
            this.HasChanges = true;
            this.ConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }
        #endregion
    }
}
