//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : InteractiveSpellCheckControl.cs
// Authors : Eric Woodruff  (Eric@EWoodruff.us), Franz Alex Gaisie-Essilfie
// Updated : 08/05/2015
// Note    : Copyright 2013-2015, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains the user control that handles spell checking a document interactively
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who   Comments
// ==============================================================================================================
// 05/28/2013  EFW   Created the code
// 02/28/2015  EFW   Added support for code analysis dictionary options
// 07/28/2015  EFW   Added support for culture information and multiple dictionaries
// 08/22/2015  FAGE  Grouping of multi-language suggestions by word
//===============================================================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Outlining;

using PackageResources = VisualStudio.SpellChecker.Properties.Resources;
using VisualStudio.SpellChecker.Tagging;

namespace VisualStudio.SpellChecker.ToolWindows
{
    /// <summary>
    /// This user control handles spell checking a document interactively
    /// </summary>
    public partial class InteractiveSpellCheckControl : UserControl
    {
        #region Private data members
        //=====================================================================

        private IWpfTextView currentTextView;
        private IOutliningManager outliningManager;
        private SpellingTagger currentTagger;
        private List<MisspellingTag> misspellings;
        private bool updatingState, parentFocused;
        #endregion

        #region Properties
        //=====================================================================

        /// <summary>
        /// This is used to get or set the current text view that is being interactively spell checked
        /// </summary>
        public IWpfTextView CurrentTextView
        {
            get { return currentTextView; }
            set
            {
                if(currentTextView != value)
                {
                    if(currentTagger != null)
                        currentTagger.TagsChanged -= tagger_TagsChanged;

                    if(value != null)
                    {
                        if(!value.Properties.TryGetProperty(typeof(SpellingTagger), out currentTagger))
                            currentTagger = null;
                    }
                    else
                        currentTagger = null;

                    ctxAddWord.Items.Clear();

                    if(currentTagger != null)
                    {
                        currentTextView = value;
                        currentTagger.TagsChanged += tagger_TagsChanged;

                        var componentModel = Utility.GetServiceFromPackage<IComponentModel, SComponentModel>(false);

                        if(componentModel != null)
                        {
                            var outliningManagerService = componentModel.GetService<IOutliningManagerService>();

                            if(outliningManagerService != null)
                                outliningManager = outliningManagerService.GetOutliningManager(currentTextView);
                        }

                        tagger_TagsChanged(this, null);

                        if(currentTagger.Dictionary.DictionaryCount != 1)
                            foreach(var d in currentTagger.Dictionary.Dictionaries)
                                ctxAddWord.Items.Add(new MenuItem()
                                {
                                    Header = String.Format(CultureInfo.InvariantCulture, "{0} ({1})",
                                        d.Culture.EnglishName, d.Culture.Name),
                                    Tag = d.Culture
                                });
                    }
                    else
                    {
                        currentTextView = null;

                        // The outlining manager is disposable but we should not dispose of it since it is
                        // still in use by the view.
                        outliningManager = null;
                    }

                    this.UpdateState();
                }
            }
        }

        /// <summary>
        /// This is used to tell the control when the parent window has the focus
        /// </summary>
        /// <remarks>Not sure if it's a WPF or a Visual Studio thing but focus detection appears to be screwed
        /// up.  This works around the issue so that we can reliable update state only when focused.</remarks>
        public bool ParentFocused
        {
            get { return parentFocused; }
            set
            {
                parentFocused = value;

                if(parentFocused)
                    this.UpdateState();
            }
        }
        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        public InteractiveSpellCheckControl()
        {
            InitializeComponent();

            this.UpdateState();
        }
        #endregion

        #region Helper methods
        //=====================================================================

        /// <summary>
        /// Update the state of the controls based on the current issue
        /// </summary>
        private void UpdateState()
        {
            if(updatingState)
                return;

            try
            {
                updatingState = lbSuggestions.IsEnabled = true;

                btnReplace.IsEnabled = btnReplaceAll.IsEnabled = btnIgnoreOnce.IsEnabled = btnIgnoreAll.IsEnabled =
                    btnAddWord.IsEnabled = btnUndo.IsEnabled = txtMisspelledWord.IsEnabled = false;
                lblIssue.Content = "_Misspelled Word";
                txtMisspelledWord.Text = null;
                lbSuggestions.Items.Clear();

                if(currentTextView == null)
                {
                    lblDisabled.Visibility = Visibility.Visible;
                    return;
                }

                lblDisabled.Visibility = Visibility.Collapsed;

                if(currentTextView == null)
                    return;

                if(misspellings.Count == 0)
                {
                    txtMisspelledWord.Text = "(No more issues)";
                    return;
                }

                var issue = misspellings[0];

                if(issue.MisspellingType == MisspellingType.DoubledWord)
                {
                    lblIssue.Content = "_Doubled Word";
                    btnReplace.IsEnabled = btnIgnoreOnce.IsEnabled = true;
                    lbSuggestions.Items.Add("(Delete word)");
                }
                else
                {
                    txtMisspelledWord.IsEnabled = btnIgnoreOnce.IsEnabled = btnIgnoreAll.IsEnabled = true;

                    switch(issue.MisspellingType)
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

                    if(issue.Suggestions.Any())
                    {
                        btnReplace.IsEnabled = btnReplaceAll.IsEnabled = true;
                        btnAddWord.IsEnabled = (issue.MisspellingType == MisspellingType.MisspelledWord);

                        IEnumerable<SpellingSuggestion> suggestions;

                        // group suggestions by suggestion (word) if there are multiple dictionaries
                        if(currentTagger.Dictionary.DictionaryCount > 1)
                        {
                            suggestions = from word in issue.Suggestions
                                          where !word.IsGroupHeader
                                          group word by word.Suggestion into grp
                                          select new MultiLanguageSpellingSuggestion(grp.Select(w => w.Culture), grp.Key);
                        }
                        else
                        {
                            suggestions = issue.Suggestions;
                        }

                        foreach(var s in suggestions)
                            lbSuggestions.Items.Add(s);

                        lbSuggestions.SelectedIndex = 0;
                    }
                    else
                        lbSuggestions.Items.Add("(No suggestions)");
                }

                txtMisspelledWord.Text = issue.Word;

                if(parentFocused)
                {
                    var span = issue.Span.GetSpan(issue.Span.TextBuffer.CurrentSnapshot);

                    // If in a collapsed region, expand the region
                    if(outliningManager != null)
                        foreach(var region in outliningManager.GetCollapsedRegions(span, false))
                            if(region.IsCollapsed)
                                outliningManager.Expand(region);

                    currentTextView.Caret.MoveTo(span.Start);
                    currentTextView.ViewScroller.EnsureSpanVisible(span, EnsureSpanVisibleOptions.AlwaysCenter);
                    currentTextView.Selection.Select(span, false);
                }
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
        /// Update the list of spelling errors when notified that the spelling tags have changed
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void tagger_TagsChanged(object sender, SnapshotSpanEventArgs e)
        {
            misspellings = currentTagger.CurrentMisspellings.ToList();
            this.UpdateState();
        }

        /// <summary>
        /// When an item is double clicked, handle it as a request to replace the misspelling with the selected
        /// word.  If Ctrl is held down, it is treated as a request to replace all occurrences.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void lbSuggestions_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var elem = lbSuggestions.InputHitTest(e.GetPosition(lbSuggestions)) as UIElement;

            // Only do it if an item is double clicked
            while(elem != null && elem != lbSuggestions)
            {
                if(elem is ListBoxItem)
                {
                    if(Keyboard.Modifiers == ModifierKeys.Control)
                        btnReplaceAll_Click(sender, e);
                    else
                        btnReplace_Click(sender, e);

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
            if(!updatingState && misspellings.Count != 0)
            {
                bool hasChanged = !txtMisspelledWord.Text.Trim().Equals(misspellings[0].Word,
                    StringComparison.OrdinalIgnoreCase);

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
            if(misspellings.Count != 0)
                txtMisspelledWord.Text = misspellings[0].Word;
        }

        /// <summary>
        /// Replace the current misspelled word with the selected word
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        /// <remarks>Since we're making the change through the text buffer, the <c>TagsChanged</c> event will
        /// be raised and will notify us of the remaining misspellings.</remarks>
        private void btnReplace_Click(object sender, RoutedEventArgs e)
        {
            ITrackingSpan span;
            string word;

            if(misspellings.Count != 0 && misspellings[0].Word.Length != 0)
                if(misspellings[0].MisspellingType != MisspellingType.DoubledWord)
                {
                    if(!lbSuggestions.IsEnabled)
                        word = txtMisspelledWord.Text.Trim();
                    else
                    {
                        if(lbSuggestions.SelectedIndex < 0)
                        {
                            if(lbSuggestions.Items.Count != 0 && lbSuggestions.SelectedIndex < 0)
                                lbSuggestions.SelectedIndex = 0;

                            return;
                        }

                        word = ((SpellingSuggestion)lbSuggestions.SelectedItem).Suggestion;
                    }

                    span = misspellings[0].Span;
                    span.TextBuffer.Replace(span.GetSpan(span.TextBuffer.CurrentSnapshot), word);
                }
                else
                {
                    span = misspellings[0].DeleteWordSpan;
                    span.TextBuffer.Replace(span.GetSpan(span.TextBuffer.CurrentSnapshot), String.Empty);
                }
        }

        /// <summary>
        /// Replace all occurrences of the misspelled word with the selected word
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        /// <remarks>Since we're making the change through the dictionary, the <c>TagsChanged</c> event will
        /// be raised and will notify us of the remaining misspellings.</remarks>
        private void btnReplaceAll_Click(object sender, RoutedEventArgs e)
        {
            SpellingSuggestion suggestion;

            if(misspellings.Count != 0 && misspellings[0].Word.Length != 0)
            {
                if(!lbSuggestions.IsEnabled)
                    suggestion = new SpellingSuggestion(null, txtMisspelledWord.Text.Trim());
                else
                {
                    if(lbSuggestions.SelectedIndex < 0)
                    {
                        if(lbSuggestions.Items.Count != 0 && lbSuggestions.SelectedIndex < 0)
                            lbSuggestions.SelectedIndex = 0;

                        return;
                    }

                    suggestion = (SpellingSuggestion)lbSuggestions.SelectedItem;
                }

                currentTagger.Dictionary.ReplaceAllOccurrences(misspellings[0].Word, suggestion);
            }
        }

        /// <summary>
        /// Ignore just the current occurrence of the misspelled word
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        /// <remarks>Since we're making the change through the dictionary, the <c>TagsChanged</c> event will
        /// be raised and will notify us of the remaining misspellings.</remarks>
        private void btnIgnoreOnce_Click(object sender, RoutedEventArgs e)
        {
            if(misspellings.Count != 0 && misspellings[0].Word.Length != 0)
                currentTagger.Dictionary.IgnoreWordOnce(misspellings[0].Span);
        }

        /// <summary>
        /// Ignore all occurrences of the misspelled word
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        /// <remarks>Since we're making the change through the dictionary, the <c>TagsChanged</c> event will
        /// be raised and will notify us of the remaining misspellings.</remarks>
        private void btnIgnoreAll_Click(object sender, RoutedEventArgs e)
        {
            if(misspellings.Count != 0 && misspellings[0].Word.Length != 0)
                currentTagger.Dictionary.IgnoreWord(misspellings[0].Word);
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
            string word;

            if(misspellings.Count != 0)
                if(ctxAddWord.Items.Count == 0)
                {
                    if(!lbSuggestions.IsEnabled)
                        word = txtMisspelledWord.Text.Trim();
                    else
                        word = misspellings[0].Word;

                    if(word.Length != 0)
                    {
                        currentTagger.Dictionary.AddWordToDictionary(word, null);

                        // If adding a modified word, replace the word in the file too
                        if(!lbSuggestions.IsEnabled)
                            btnReplace_Click(sender, e);
                    }
                    else
                        MessageBox.Show("Cannot add an empty word to the dictionary", PackageResources.PackageTitle,
                            MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
                else
                {
                    btnAddWord_ContextMenuOpening(sender, null);
                    ctxAddWord.IsOpen = true;
                }
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
        /// Handle Add Word context menu item clicks
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void ctxAddWordMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var item = e.Source as MenuItem;
            string word;

            if(item != null && misspellings.Count != 0)
            {
                if(!lbSuggestions.IsEnabled)
                    word = txtMisspelledWord.Text.Trim();
                else
                    word = misspellings[0].Word;

                if(word.Length != 0)
                {
                    currentTagger.Dictionary.AddWordToDictionary(word, (CultureInfo)item.Tag);

                    // If adding a modified word, replace the word in the file too
                    if(!lbSuggestions.IsEnabled)
                        btnReplace_Click(sender, e);
                }
                else
                    MessageBox.Show("Cannot add an empty word to the dictionary", PackageResources.PackageTitle,
                        MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }
        #endregion
    }
}
