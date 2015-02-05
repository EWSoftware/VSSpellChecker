//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : InteractiveSpellCheckControl.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 02/04/2015
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
//    Date     Who  Comments
// ==============================================================================================================
// 05/28/2013  EFW  Created the code
//===============================================================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

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

                    if(currentTagger != null)
                    {
                        currentTextView = value;
                        currentTagger.TagsChanged += tagger_TagsChanged;

                        tagger_TagsChanged(this, null);
                    }
                    else
                        currentTextView = null;

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
                updatingState = true;

                btnReplace.IsEnabled = btnReplaceAll.IsEnabled = btnIgnoreOnce.IsEnabled = btnIgnoreAll.IsEnabled =
                    btnAddWord.IsEnabled = false;
                lblIssue.Text = "Misspelled Word:";
                lblMisspelledWord.Text = null;
                lblMisspelledWord.ToolTip = null;
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
                    lblMisspelledWord.Text = "(No more issues)";
                    return;
                }

                var issue = misspellings[0];

                if(!issue.IsMisspelling)
                {
                    lblIssue.Text = "Doubled Word";
                    btnReplace.IsEnabled = btnIgnoreOnce.IsEnabled = true;
                    lbSuggestions.Items.Add("(Delete word)");
                }
                else
                {
                    btnIgnoreOnce.IsEnabled = btnIgnoreAll.IsEnabled = true;

                    if(issue.Suggestions.Count() != 0)
                    {
                        btnReplace.IsEnabled = btnReplaceAll.IsEnabled = btnAddWord.IsEnabled = true;

                        foreach(string s in issue.Suggestions)
                            lbSuggestions.Items.Add(s);
                    }
                    else
                        lbSuggestions.Items.Add("(No suggestions)");
                }

                lblMisspelledWord.Text = issue.Word;
                lblMisspelledWord.ToolTip = issue.Word;
                lbSuggestions.SelectedIndex = 0;

                if(parentFocused)
                {
                    var span = issue.Span.GetSpan(issue.Span.TextBuffer.CurrentSnapshot);

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
        /// Replace the current misspelled word with the selected word
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        /// <remarks>Since we're making the change through the text buffer, the <c>TagsChanged</c> event will
        /// be raised and will notify us of the remaining misspellings.</remarks>
        private void btnReplace_Click(object sender, RoutedEventArgs e)
        {
            ITrackingSpan span;

            if(lbSuggestions.SelectedIndex < 0 || misspellings.Count == 0 || misspellings[0].Word.Length == 0)
            {
                if(lbSuggestions.Items.Count != 0 && lbSuggestions.SelectedIndex < 0)
                    lbSuggestions.SelectedIndex = 0;

                return;
            }

            if(misspellings[0].IsMisspelling)
            {
                span = misspellings[0].Span;
                span.TextBuffer.Replace(span.GetSpan(span.TextBuffer.CurrentSnapshot), (string)lbSuggestions.SelectedItem);
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
            if(lbSuggestions.SelectedIndex < 0 || misspellings.Count == 0 || misspellings[0].Word.Length == 0)
            {
                if(lbSuggestions.Items.Count != 0 && lbSuggestions.SelectedIndex < 0)
                    lbSuggestions.SelectedIndex = 0;

                return;
            }

            currentTagger.Dictionary.ReplaceAllOccurrences(misspellings[0].Word, (string)lbSuggestions.SelectedItem);
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
        /// Add the word to the global dictionary
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        /// <remarks>Since we're making the change through the dictionary, the <c>TagsChanged</c> event will
        /// be raised and will notify us of the remaining misspellings.</remarks>
        private void btnAddWord_Click(object sender, RoutedEventArgs e)
        {
            if(misspellings.Count != 0 && misspellings[0].Word.Length != 0)
                currentTagger.Dictionary.AddWordToDictionary(misspellings[0].Word);
        }
        #endregion
    }
}
