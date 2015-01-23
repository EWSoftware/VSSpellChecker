//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : IgnoredWordsUserControl.xaml.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 06/14/2014
// Note    : Copyright 2014, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a user control used to edit the ignored words spell checker configuration settings
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 06/09/2014  EFW  Moved the ignored words settings to a user control
//===============================================================================================================

using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace VisualStudio.SpellChecker.UI
{
    /// <summary>
    /// This user control is used to edit the ignored words spell checker configuration settings
    /// </summary>
    public partial class IgnoredWordsUserControl : UserControl, ISpellCheckerConfiguration
    {
        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        public IgnoredWordsUserControl()
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
            get { return "Ignored Words"; }
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
            lbIgnoredWords.Items.Clear();

            foreach(string el in SpellCheckerConfiguration.IgnoredWords)
                lbIgnoredWords.Items.Add(el);

            var sd = new SortDescription { Direction = ListSortDirection.Ascending };

            lbIgnoredWords.Items.SortDescriptions.Add(sd);
        }

        /// <inheritdoc />
        public bool SaveConfiguration()
        {
            SpellCheckerConfiguration.SetIgnoredWords(lbIgnoredWords.Items.OfType<string>());

            return true;
        }
        #endregion

        #region Event handlers
        //=====================================================================

        /// <summary>
        /// Add a new ignored word to the list
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnAddIgnoredWord_Click(object sender, RoutedEventArgs e)
        {
            char[] escapedLetters = new[] { 'a', 'b', 'f', 'n', 'r', 't', 'v', 'x', 'u', 'U' };
            int idx;

            txtIgnoredWord.Text = txtIgnoredWord.Text.Trim();

            if(txtIgnoredWord.Text.Length < 3 && txtIgnoredWord.Text[0] == '\\')
                txtIgnoredWord.Text = String.Empty;
            else
                if(txtIgnoredWord.Text.Length > 1 && txtIgnoredWord.Text[0] == '\\' &&
                  !escapedLetters.Contains(txtIgnoredWord.Text[1]))
                    txtIgnoredWord.Text = txtIgnoredWord.Text.Substring(1);

            if(txtIgnoredWord.Text.Length != 0)
            {
                idx = lbIgnoredWords.Items.IndexOf(txtIgnoredWord.Text);

                if(idx == -1)
                    idx = lbIgnoredWords.Items.Add(txtIgnoredWord.Text);

                if(idx != -1)
                {
                    lbIgnoredWords.SelectedIndex = idx;
                    lbIgnoredWords.ScrollIntoView(lbIgnoredWords.Items[idx]);
                }
            }

            txtIgnoredWord.Text = null;
        }

        /// <summary>
        /// Remove the selected word from the list of ignored words
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnRemoveIgnoredWord_Click(object sender, RoutedEventArgs e)
        {
            int idx = lbIgnoredWords.SelectedIndex;

            if(idx != -1)
                lbIgnoredWords.Items.RemoveAt(idx);

            if(lbIgnoredWords.Items.Count != 0)
            {
                if(idx < 0)
                    idx = 0;
                else
                    if(idx >= lbIgnoredWords.Items.Count)
                        idx = lbIgnoredWords.Items.Count - 1;

                lbIgnoredWords.SelectedIndex = idx;
            }
        }

        /// <summary>
        /// Reset the ignored words to the default list
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnDefaultIgnoredWords_Click(object sender, RoutedEventArgs e)
        {
            lbIgnoredWords.Items.Clear();

            foreach(string el in SpellCheckerConfiguration.DefaultIgnoredWords)
                lbIgnoredWords.Items.Add(el);

            var sd = new SortDescription { Direction = ListSortDirection.Ascending };

            lbIgnoredWords.Items.SortDescriptions.Add(sd);
        }
        #endregion
    }
}
