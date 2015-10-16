//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : SpellCheckControl.cs
// Authors : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 10/13/2015
// Note    : Copyright 2013-2015, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains the user control that presents the spell checking options to the user
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who   Comments
// ==============================================================================================================
// 08/25/2015  EFW   Refactored the spell checking options into a separate user control
//===============================================================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using VisualStudio.SpellChecker.Definitions;

namespace VisualStudio.SpellChecker.ToolWindows
{
    /// <summary>
    /// Interaction logic for SpellCheckControl.xaml
    /// </summary>
    public partial class SpellCheckControl : UserControl
    {
        #region Private data members
        //=====================================================================

        private bool updatingState;

        #endregion

        #region Properties
        //=====================================================================

        /// <summary>
        /// This read-only property returns the current spelling issue
        /// </summary>
        public ISpellingIssue CurrentIssue { get; private set; }

        /// <summary>
        /// This returns the current misspelled word with any edits made in the user control
        /// </summary>
        public string MisspelledWord
        {
            get { return txtMisspelledWord.Text.Trim(); }
        }

        /// <summary>
        /// This is used to get or set the text to display when there is no current issue
        /// </summary>
        public string NoCurrentIssueText { get; set; }

        /// <summary>
        /// This read-only property returns the selected suggestion
        /// </summary>
        /// <value>Returns the selected suggestion if the is one or null if there isn't one</value>
        public SpellingSuggestion SelectedSuggestion
        {
            get
            {
                if(this.CurrentIssue == null)
                    return null;

                // Return the edited word?
                if(!lbSuggestions.IsEnabled)
                    return new SpellingSuggestion(null, txtMisspelledWord.Text.Trim());

                if(lbSuggestions.SelectedIndex < 0)
                {
                    if(lbSuggestions.Items.Count != 0)
                        lbSuggestions.SelectedIndex = 0;

                    return null;
                }

                return (SpellingSuggestion)lbSuggestions.SelectedItem;
            }
        }
        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        public SpellCheckControl()
        {
            InitializeComponent();

            this.NoCurrentIssueText = "(No more issues)";
        }
        #endregion

        #region Helper methods
        //=====================================================================

        /// <summary>
        /// This is used to set the context menu items for the Add Word button's context menu when multiple
        /// dictionaries are in use.
        /// </summary>
        /// <param name="dictionaryCultures">An enumerable list of dictionary cultures or null</param>
        public void SetAddWordContextMenuDictionaries(IEnumerable<CultureInfo> dictionaryCultures)
        {
            ctxAddWord.Items.Clear();

            if(dictionaryCultures != null && dictionaryCultures.Count() > 1)
                foreach(var culture in dictionaryCultures)
                    ctxAddWord.Items.Add(new MenuItem()
                    {
                        Header = String.Format(CultureInfo.InvariantCulture, "{0} ({1})",
                            culture.EnglishName, culture.Name),
                        Tag = culture
                    });
        }

        /// <summary>
        /// Update the state of the controls based on the current issue
        /// </summary>
        /// <param name="isDisabled">True if the control host is disabled, false if not.</param>
        /// <param name="groupByWord">True to group suggestions by word (multiple languages), or false to
        /// list them as-is (single language).</param>
        /// <param name="currentIssue">The current spelling issue.</param>
        public void UpdateState(bool isDisabled, bool groupByWord, ISpellingIssue currentIssue)
        {
            try
            {
                updatingState = lbSuggestions.IsEnabled = true;

                btnReplace.IsEnabled = btnReplaceAll.IsEnabled = btnIgnoreOnce.IsEnabled = btnIgnoreAll.IsEnabled =
                    btnAddWord.IsEnabled = btnUndo.IsEnabled = txtMisspelledWord.IsEnabled = false;
                lblIssue.Content = "_Misspelled Word";
                txtMisspelledWord.Text = null;
                lbSuggestions.Items.Clear();

                this.CurrentIssue = currentIssue;

                if(isDisabled)
                {
                    lblDisabled.Visibility = Visibility.Visible;
                    return;
                }

                lblDisabled.Visibility = Visibility.Collapsed;

                if(currentIssue == null)
                {
                    txtMisspelledWord.Text = this.NoCurrentIssueText;
                    return;
                }

                if(currentIssue.MisspellingType == MisspellingType.DoubledWord)
                {
                    lblIssue.Content = "_Doubled Word";
                    btnReplace.IsEnabled = btnIgnoreOnce.IsEnabled = true;
                    lbSuggestions.Items.Add("(Delete word)");
                }
                else
                {
                    txtMisspelledWord.IsEnabled = btnIgnoreOnce.IsEnabled = btnIgnoreAll.IsEnabled = true;
                    btnAddWord.IsEnabled = (currentIssue.MisspellingType == MisspellingType.MisspelledWord);

                    switch(currentIssue.MisspellingType)
                    {
                        case MisspellingType.CompoundTerm:
                            lblIssue.Content = "Co_mpound Term";
                            break;

                        case MisspellingType.DeprecatedTerm:
                            lblIssue.Content = "_Deprecated Term";
                            break;

                        case MisspellingType.UnrecognizedWord:
                            lblIssue.Content = "Un_recognized Word";
                            break;

                        default:
                            break;
                    }

                    if(currentIssue.Suggestions.Any())
                    {
                        btnReplace.IsEnabled = btnReplaceAll.IsEnabled = true;

                        IEnumerable<ISpellingSuggestion> suggestions;

                        // Group suggestions by word if there are multiple dictionaries
                        if(groupByWord)
                        {
                            suggestions = currentIssue.Suggestions.GroupBy(w => w.Suggestion).Select(g =>
                                new MultiLanguageSpellingSuggestion(g.Select(w => w.Culture), g.Key));
                        }
                        else
                            suggestions = currentIssue.Suggestions;

                        foreach(var s in suggestions)
                            lbSuggestions.Items.Add(s);

                        lbSuggestions.SelectedIndex = 0;
                    }
                    else
                        lbSuggestions.Items.Add("(No suggestions)");
                }

                txtMisspelledWord.Text = currentIssue.Word;
            }
            finally
            {
                updatingState = false;
            }
        }
        #endregion

        #region Event handlers
        //=====================================================================

        /// <summary>
        /// When an item is double clicked, handle it as a request to replace the misspelling with the selected
        /// word.  If Ctrl is held down, it is treated as a request to replace all occurrences.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void lbSuggestions_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            AutomationPeer peer;

            var elem = lbSuggestions.InputHitTest(e.GetPosition(lbSuggestions)) as UIElement;
            
            // Only do it if an item is double clicked
            while(elem != null && elem != lbSuggestions)
            {
                if(elem is ListBoxItem)
                {
                    if(Keyboard.Modifiers == ModifierKeys.Control)
                        peer = UIElementAutomationPeer.CreatePeerForElement(btnReplaceAll);
                    else
                        peer = UIElementAutomationPeer.CreatePeerForElement(btnReplace);

                    var provider = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;

                    if(provider != null)
                        provider.Invoke();

                    break;
                }

                elem = VisualTreeHelper.GetParent(elem) as UIElement;
            }
        }

        /// <summary>
        /// Update the control states when the misspelled word changes
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void txtMisspelledWord_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(!updatingState && this.CurrentIssue != null)
            {
                bool hasChanged = !txtMisspelledWord.Text.Trim().Equals(this.CurrentIssue.Word,
                    StringComparison.Ordinal);

                btnUndo.IsEnabled = hasChanged;
                lbSuggestions.IsEnabled = !hasChanged;
            }
        }

        /// <summary>
        /// Undo changes to the misspelled word
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnUndo_Click(object sender, RoutedEventArgs e)
        {
            if(this.CurrentIssue != null)
                txtMisspelledWord.Text = this.CurrentIssue.Word;
        }

        /// <summary>
        /// Set the properties on the context menu when it opens
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void btnAddWord_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if(ctxAddWord.Items.Count == 0)
                e.Handled = true;
            else
            {
                ctxAddWord.PlacementTarget = btnAddWord;
                ctxAddWord.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                ContextMenuService.SetPlacement(btnAddWord, System.Windows.Controls.Primitives.PlacementMode.Bottom);
            }
        }

        /// <summary>
        /// Add the word to the dictionary if there is only one dictionary or show the context menu if there are
        /// multiple dictionaries.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        /// <remarks>Since we're making the change through the dictionary, the <c>TagsChanged</c> event will
        /// be raised and will notify us of the remaining misspellings.</remarks>
        private void btnAddWord_Click(object sender, RoutedEventArgs e)
        {
            if(this.CurrentIssue != null)
                if(ctxAddWord.Items.Count == 0)
                    SpellCheckCommands.AddToDictionary.Execute(null, sender as IInputElement);
                else
                {
                    btnAddWord_ContextMenuOpening(sender, null);
                    ctxAddWord.IsOpen = true;
                }
        }

        /// <summary>
        /// Handle Add Word context menu item clicks
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void ctxAddWordMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var item = e.Source as MenuItem;

            if(item != null && this.CurrentIssue != null)
                SpellCheckCommands.AddToDictionary.Execute((CultureInfo)item.Tag, sender as IInputElement);
        }
        #endregion
    }
}
