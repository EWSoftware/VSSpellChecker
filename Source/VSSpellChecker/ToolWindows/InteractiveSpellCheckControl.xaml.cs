//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : InteractiveSpellCheckControl.cs
// Authors : Eric Woodruff  (Eric@EWoodruff.us), Franz Alex Gaisie-Essilfie
// Updated : 09/05/2015
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
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Outlining;

using VisualStudio.SpellChecker.Definitions;
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
                    
                    ucSpellCheck.SetAddWordContextMenuDictionaries(null);

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
                            ucSpellCheck.SetAddWordContextMenuDictionaries(
                                currentTagger.Dictionary.Dictionaries.Select(d => d.Culture));
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
        /// up.  This works around the issue so that we can reliably update state only when focused.</remarks>
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
            if(!updatingState)
            {
                updatingState = true;

                try
                {
                    if(currentTextView != null)
                    {
                        var currentIssue = currentTagger.CurrentMisspellings.FirstOrDefault();

                        ucSpellCheck.UpdateState(false, currentTagger.Dictionary.DictionaryCount > 1, currentIssue);

                        if(parentFocused && currentIssue != null)
                        {
                            var span = currentIssue.Span.GetSpan(currentIssue.Span.TextBuffer.CurrentSnapshot);

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
                    else
                        ucSpellCheck.UpdateState(true, false, null);
                }
                finally
                {
                    updatingState = false;
                }
            }
        }
        #endregion

        #region Event handlers
        //=====================================================================

        /// <summary>
        /// Update the current state when notified that the spelling tags have changed
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void tagger_TagsChanged(object sender, SnapshotSpanEventArgs e)
        {
            this.UpdateState();
        }

        /// <summary>
        /// Replace the current misspelled word with the selected word
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        /// <remarks>Since we're making the change through the text buffer, the <c>TagsChanged</c> event will
        /// be raised and will notify us of the remaining misspellings.</remarks>
        private void cmdReplace_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ITrackingSpan span;
            MisspellingTag currentIssue = ucSpellCheck.CurrentIssue as MisspellingTag;

            if(currentIssue != null && currentIssue.Word.Length != 0)
                if(currentIssue.MisspellingType != MisspellingType.DoubledWord)
                {
                    var suggestion = ucSpellCheck.SelectedSuggestion;

                    if(suggestion != null)
                    {
                        span = currentIssue.Span;
                        span.TextBuffer.Replace(span.GetSpan(span.TextBuffer.CurrentSnapshot), suggestion.Suggestion);
                    }
                }
                else
                {
                    span = currentIssue.DeleteWordSpan;
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
        private void cmdReplaceAll_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            MisspellingTag currentIssue = ucSpellCheck.CurrentIssue as MisspellingTag;

            if(currentIssue != null && currentIssue.Word.Length != 0)
            {
                var suggestion = ucSpellCheck.SelectedSuggestion;

                if(suggestion != null)
                    currentTagger.Dictionary.ReplaceAllOccurrences(currentIssue.Word, suggestion);
            }
        }

        /// <summary>
        /// Ignore just the current occurrence of the misspelled word
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        /// <remarks>Since we're making the change through the dictionary, the <c>TagsChanged</c> event will
        /// be raised and will notify us of the remaining misspellings.</remarks>
        private void cmdIgnoreOnce_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            MisspellingTag currentIssue = ucSpellCheck.CurrentIssue as MisspellingTag;

            if(currentIssue != null && currentIssue.Word.Length != 0)
                currentTagger.Dictionary.IgnoreWordOnce(currentIssue.Span);
        }

        /// <summary>
        /// Ignore all occurrences of the misspelled word
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        /// <remarks>Since we're making the change through the dictionary, the <c>TagsChanged</c> event will
        /// be raised and will notify us of the remaining misspellings.</remarks>
        private void cmdIgnoreAll_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            MisspellingTag currentIssue = ucSpellCheck.CurrentIssue as MisspellingTag;

            if(currentIssue != null && currentIssue.Word.Length != 0)
                currentTagger.Dictionary.IgnoreWord(currentIssue.Word);
        }

        /// <summary>
        /// Add the word to the dictionary
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        /// <remarks>Since we're making the change through the dictionary, the <c>TagsChanged</c> event will
        /// be raised and will notify us of the remaining misspellings.</remarks>
        private void cmdAddToDictionary_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            MisspellingTag currentIssue = ucSpellCheck.CurrentIssue as MisspellingTag;
            string word;

            if(currentIssue != null)
            {
                word = ucSpellCheck.MisspelledWord;

                if(word.Length != 0)
                {
                    // If the parameter is a CultureInfo instance, the word will be added to the dictionary for
                    // that culture.  If null, it's added to the first available dictionary.
                    currentTagger.Dictionary.AddWordToDictionary(word, e.Parameter as CultureInfo);

                    // If adding a modified word, replace the word in the file too
                    if(!word.Equals(currentIssue.Word, StringComparison.OrdinalIgnoreCase))
                        cmdReplace_Executed(sender, e);
                }
                else
                    MessageBox.Show("Cannot add an empty word to the dictionary", PackageResources.PackageTitle,
                        MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }

        /// <summary>
        /// View help for this tool window
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void cmdHelp_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("https://github.com/EWSoftware/VSSpellChecker/wiki/" +
                    "53ffc5b7-b7dc-4f03-9a51-ed4176bff504");
            }
            catch(Exception ex)
            {
                MessageBox.Show("Unable to navigate to website.  Reason: " + ex.Message,
                    PackageResources.PackageTitle, MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }
        #endregion
    }
}
