﻿//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : ExclusionExpressionAddEditForm.xaml.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 05/06/2016
// Note    : Copyright 2015-2016, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a window used to edit exclusion expressions for the configuration file
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
using System.Text.RegularExpressions;
using System.Windows;

using PackageResources = VisualStudio.SpellChecker.Properties.Resources;

namespace VisualStudio.SpellChecker.Editors.Pages
{
    /// <summary>
    /// This form is used to add or edit an exclusion regular expression for the configuration file
    /// </summary>
    public partial class ExclusionExpressionAddEditForm : Window
    {
        #region Private data members
        //=====================================================================

        private Regex expression;

        private static Regex reComment = new Regex(@"^.*?(?<Comment>\(\?\#[^\)]*?\))$");

        #endregion

        #region Properties
        //=====================================================================

        public Regex Expression
        {
            get { return expression; }
            set
            {
                expression = value;

                if(value != null)
                {
                    this.Title = "Edit an Exclusion Expression";

                    string expr = expression.ToString();
                    Match m = reComment.Match(expr);

                    if(m.Success)
                    {
                        txtExpression.Text = expr.Substring(0, m.Groups["Comment"].Index);
                        txtComment.Text = expr.Substring(m.Groups["Comment"].Index + 3,
                            expr.Length - m.Groups["Comment"].Index - 4).Trim();
                    }
                    else
                        txtExpression.Text = expr;

                    chkIgnoreCase.IsChecked = ((expression.Options & RegexOptions.IgnoreCase) != 0);
                    chkMultiLine.IsChecked = ((expression.Options & RegexOptions.Multiline) != 0);
                    chkSingleLine.IsChecked = ((expression.Options & RegexOptions.Singleline) != 0);
                }
                else
                {
                    this.Title = "Add an Exclusion Expression";

                    txtExpression.Text = null;
                    chkIgnoreCase.IsChecked = chkMultiLine.IsChecked = chkSingleLine.IsChecked = false;
                }
            }
        }
        #endregion

        #region Constructor
        //=====================================================================
        
        /// <summary>
        /// Constructor
        /// </summary>
        public ExclusionExpressionAddEditForm()
        {
            InitializeComponent();
        }
        #endregion

        #region Event handlers
        //=====================================================================

        /// <summary>
        /// Save the regular expression if it is valid
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            RegexOptions options = RegexOptions.None;
            string expr = null;

            if(txtExpression.Text.Trim().Length == 0)
            {
                MessageBox.Show("A regular expression is required", PackageResources.PackageTitle,
                    MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            try
            {
                if(chkIgnoreCase.IsChecked.Value)
                    options |= RegexOptions.IgnoreCase;

                if(chkMultiLine.IsChecked.Value)
                    options |= RegexOptions.Multiline;

                if(chkSingleLine.IsChecked.Value)
                    options |= RegexOptions.Singleline;

                expr = txtExpression.Text;

                if(txtComment.Text.Trim().Length != 0)
                    expr += String.Format("(?# {0})", txtComment.Text.Trim());

                expression = new Regex(expr, options);

                this.DialogResult = true;
            }
            catch(ArgumentException ex)
            {
                MessageBox.Show(String.Format("The regular expression is not valid.\r\n\r\nExpression: {0}\r\n" +
                    "Error: {1}", expr, ex.Message), PackageResources.PackageTitle, MessageBoxButton.OK,
                    MessageBoxImage.Exclamation);
            }
        }
        #endregion
    }
}
