//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : WpfTextBoxSpellChecker.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 01/08/2017
// Note    : Copyright 2016-2017, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a class that adds spell checking using NHunspell to any WPF text box within Visual Studio
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 04/22/2013  EFW  Created the code
//===============================================================================================================

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

using TextSpan = Microsoft.VisualStudio.Text.Span;

using VisualStudio.SpellChecker.Configuration;
using VisualStudio.SpellChecker.Definitions;
using VisualStudio.SpellChecker.ProjectSpellCheck;

namespace VisualStudio.SpellChecker.WpfTextBox
{
    /// <summary>
    /// This is used to add spell checking using NHunspell to any WPF text box within Visual Studio
    /// </summary>
    /// <remarks>Admittedly, this is a hack but it works quite well.  It uses a registered class handler to
    /// associate an instance of this class with any WPF text box when it gains focus for the first time.
    /// Unfortunately, WPF's spell checking implementation and the text box implementation that we need to get at
    /// to replace the text decorations are not exposed for public use.  As such, we must resort to this rather
    /// unorthodox approach to replacing it with our own spell checker and adorner.</remarks>
    public class WpfTextBoxSpellChecker
    {
        #region Private data members
        //=====================================================================

        private static ConcurrentDictionary<TextBox, WpfTextBoxSpellChecker> wpfSpellCheckers =
            new ConcurrentDictionary<TextBox, WpfTextBoxSpellChecker>();

        private static SpellCheckerConfiguration configuration;
        private static SpellingDictionary dictionary;
        private static bool isRegistered;

        private TextBox textBox;
        private WordSplitter wordSplitter;
        private SpellingErrorAdorner adorner;
        private Timer timer;
        private List<FileMisspelling> misspelledWords;
        private FileMisspelling selectedMisspelling;

        private static object syncRoot = new Object();

        #endregion

        #region Private constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="textBox">The text box with which this spell checker is associated</param>
        private WpfTextBoxSpellChecker(TextBox textBox)
        {
            this.textBox = textBox;

            System.Diagnostics.Debug.WriteLine("******* Connecting to " + ElementName(textBox) + " *******");

            lock(syncRoot)
            {
                wordSplitter = new WordSplitter
                {
                    Configuration = configuration,
                    Classification = RangeClassification.PlainText
                };
            }

            misspelledWords = new List<FileMisspelling>();

            var adornerLayer = AdornerLayer.GetAdornerLayer(textBox);

            if(adornerLayer != null)
            {
                timer = new Timer { Interval = 500, AutoReset = false };
                timer.Elapsed += timer_Elapsed;

                adorner = new SpellingErrorAdorner(textBox);
                adornerLayer.Add(adorner);

                textBox.TextChanged += this.textBox_TextChanged;

                if(textBox.ContextMenu == null)
                {
                    var cm = new ContextMenu();

                    cm.Items.Add(new MenuItem { Command = ApplicationCommands.Copy });
                    cm.Items.Add(new MenuItem { Command = ApplicationCommands.Cut });
                    cm.Items.Add(new MenuItem { Command = ApplicationCommands.Paste });

                    textBox.ContextMenu = cm;
                }

                textBox.ContextMenu.Opened += this.textBox_ContextMenuOpeningHandler;

                var window = Window.GetWindow(textBox);

                // For normal forms, disconnect the spell checker when the window closes
                if(window != null && !window.GetType().FullName.StartsWith("Microsoft.VisualStudio.", StringComparison.OrdinalIgnoreCase))
                    window.Closing += window_Closing;

                this.textBox_TextChanged(this, null);
            }
        }
        #endregion

        #region Methods
        //=====================================================================

        /// <summary>
        /// This is used to register the class handler that connects the spell checker to each and every WPF
        /// text box instance.
        /// </summary>
        public static void ConnectSpellChecker()
        {
            if(!isRegistered)
            {
                EventManager.RegisterClassHandler(typeof(TextBox), UIElement.GotFocusEvent,
                    new RoutedEventHandler(async (sender, args) => await Task.Run(async () =>
                    {
                        TextBox t = sender as TextBox;

                        if(t != null && !wpfSpellCheckers.ContainsKey(t))
                            if(!t.CheckAccess())
                            {
                                await t.Dispatcher.InvokeAsync(() => AddWpfSpellChecker(t));
                            }
                            else
                                AddWpfSpellChecker(t);
                    })));

                isRegistered = true;
            }
        }

        /// <summary>
        /// This is called to clear the textbox cache
        /// </summary>
        /// <remarks>This is done whenever a change in solution is detected.  Dictionaries are cleared when that
        /// occurs so this will allow the text boxes to be reconnected with a valid dictionary afterwards.  The
        /// text box instances tend to accumulate so it's also a good time to clear them all out.</remarks>
        public static void ClearCache()
        {
            lock(syncRoot)
            {
                configuration = null;
                dictionary = null;
            }

            foreach(var wsc in wpfSpellCheckers.Values.ToArray())
                wsc.Disconnect();
        }

        /// <summary>
        /// Disconnect the event handlers from the text box to dispose of this instance
        /// </summary>
        /// <remarks>WPF text boxes don't implement <c>IDisposable</c> so we'll manage it ourselves</remarks>
        public async void Disconnect()
        {
            WpfTextBoxSpellChecker value;

            if(!textBox.CheckAccess())
            {
                await textBox.Dispatcher.InvokeAsync(() => this.Disconnect());
                return;
            }

            var window = Window.GetWindow(textBox);

            if(window != null && !window.GetType().FullName.StartsWith("Microsoft.VisualStudio.", StringComparison.OrdinalIgnoreCase))
                window.Closing -= window_Closing;

            textBox.TextChanged -= this.textBox_TextChanged;
            
            if(textBox.ContextMenu != null)
            {
                textBox.ContextMenu.Opened -= this.textBox_ContextMenuOpeningHandler;

                foreach(var m in textBox.ContextMenu.Items.OfType<FrameworkElement>().Where(m => m.Tag == this).ToArray())
                    textBox.ContextMenu.Items.Remove(m);
            }

            if(adorner != null)
                adorner.Disconnect();

            if(timer != null)
            {
                timer.Stop();
                timer.Dispose();
                timer = null;
            }

            wpfSpellCheckers.TryRemove(textBox, out value);

            System.Diagnostics.Debug.WriteLine("******* Disconnected from " + ElementName(textBox) + " *******");
        }

        /// <summary>
        /// This creates an instance of the WPF spell checker for the given text box
        /// </summary>
        /// <param name="textBox">The text box to initialize</param>
        private static void AddWpfSpellChecker(TextBox textBox)
        {
            // Don't do anything if the default spell checker is enabled or it's read-only
            if(!textBox.SpellCheck.IsEnabled && !textBox.IsReadOnly)
            {
                lock(syncRoot)
                {
                    // Create the shared configuration and dictionary on first use
                    if(configuration == null)
                    {
                        configuration = new SpellCheckerConfiguration();
                        configuration.Load(SpellingConfigurationFile.GlobalConfigurationFilename);

                        var globalDictionaries = configuration.DictionaryLanguages.Select(l =>
                            GlobalDictionary.CreateGlobalDictionary(l, null,
                            configuration.AdditionalDictionaryFolders, configuration.RecognizedWords)).Where(
                                d => d != null).Distinct().ToList();

                        dictionary = new SpellingDictionary(globalDictionaries, configuration.IgnoredWords);
                    }

                    // Ignore it if disabled or it's an excluded text box
                    string name = ElementName(textBox);

                    if(!configuration.EnableWpfTextBoxSpellChecking || configuration.VisualStudioExclusions.Any(
                      v => v.IsMatch(name)))
                        return;
                }

                var wsc = new WpfTextBoxSpellChecker(textBox);

                wpfSpellCheckers.AddOrUpdate(textBox, wsc, (k, v) => wsc);
            }
        }

        /// <summary>
        /// This is used to get the element's fully qualified name
        /// </summary>
        /// <param name="element">The element from which to start</param>
        /// <remarks>This gets the fully qualified name by walking up the visual tree and getting each element's
        /// name if it has one.  This may not always result in a unique ID but when combined with the current
        /// editor/tool window's name, it's usually sufficient.</remarks>
        private static string ElementName(FrameworkElement element)
        {
            List<string> nameParts = new List<string>();
            string name;
            bool isWindow = false;

            while(element != null)
            {
                name = element.Uid;

                if(String.IsNullOrWhiteSpace(name))
                    name = element.Name;

                if(!String.IsNullOrWhiteSpace(name))
                    nameParts.Add(name);

                // Stop at a user control or window.  If not, we can go up into the Visual Studio control hierarchy.
                if(element is UserControl || element is Window)
                {
                    if(element is UserControl)
                    {
                        // User controls may be hosted in a parent control with a name so get it if possible
                        element = element.Parent as FrameworkElement;

                        if(element != null && String.IsNullOrEmpty(element.Uid) && String.IsNullOrEmpty(element.Name))
                            element = null;
                    }
                    else
                    {
                        // Windows aren't necessarily associated with the active editor or tool window.  As such,
                        // prefix them with their type name instead.
                        nameParts.Add(element.GetType().FullName);
                        isWindow = true;

                        element = null;
                    }
                }
                else
                    element = System.Windows.Media.VisualTreeHelper.GetParent(element) as FrameworkElement;
            }

            if(!isWindow && !String.IsNullOrWhiteSpace(VSSpellCheckEverywherePackage.Instance.CurrentWindowId))
                nameParts.Add(VSSpellCheckEverywherePackage.Instance.CurrentWindowId);

            System.Diagnostics.Debug.WriteLine("******* " + String.Join(".", nameParts.Reverse<string>()));

            return String.Join(".", nameParts.Reverse<string>());
        }

        /// <summary>
        /// This is used to check the spelling in the text box's content
        /// </summary>
        /// <param name="textToSplit">The text to split into words and check for misspellings</param>
        private void CheckSpelling(string textToSplit)
        {
            List<Match> rangeExclusions = null;
            TextSpan errorSpan, deleteWordSpan, lastWord;
            string actualWord, textToCheck;
            int mnemonicPos;

            misspelledWords.Clear();

            // Always ignore URLs
            rangeExclusions = WordSplitter.Url.Matches(textToSplit).Cast<Match>().ToList();

            // Note the location of all XML elements if needed
            if(wordSplitter.Configuration.IgnoreXmlElementsInText)
                rangeExclusions.AddRange(WordSplitter.XmlElement.Matches(textToSplit).Cast<Match>());

            // Add exclusions from the configuration if any
            foreach(var exclude in wordSplitter.Configuration.ExclusionExpressions)
                try
                {
                    rangeExclusions.AddRange(exclude.Matches(textToSplit).Cast<Match>());
                }
                catch(RegexMatchTimeoutException ex)
                {
                    // Ignore expression timeouts
                    System.Diagnostics.Debug.WriteLine(ex);
                }

            lastWord = new TextSpan();

            foreach(var word in wordSplitter.GetWordsInText(textToSplit))
            {
                actualWord = textToSplit.Substring(word.Start, word.Length);
                mnemonicPos = actualWord.IndexOf(wordSplitter.Mnemonic);

                if(mnemonicPos == -1)
                    textToCheck = actualWord;
                else
                    textToCheck = actualWord.Substring(0, mnemonicPos) + actualWord.Substring(mnemonicPos + 1);

                // Spell check the word if it looks like one and is not ignored
                if(wordSplitter.IsProbablyARealWord(textToCheck) && (rangeExclusions.Count == 0 ||
                  !rangeExclusions.Any(match => word.Start >= match.Index &&
                  word.Start <= match.Index + match.Length - 1)))
                {
                    errorSpan = new TextSpan(word.Start, word.Length);

                    // Check for a doubled word
                    if(wordSplitter.Configuration.DetectDoubledWords && lastWord.Length != 0 &&
                      textToSplit.Substring(lastWord.Start, lastWord.Length).Equals(actualWord,
                      StringComparison.OrdinalIgnoreCase) && String.IsNullOrWhiteSpace(textToSplit.Substring(
                      lastWord.Start + lastWord.Length, word.Start - lastWord.Start - lastWord.Length)))
                    {
                        // Delete the whitespace ahead of it too
                        deleteWordSpan = new TextSpan(lastWord.Start + lastWord.Length,
                            word.Length + word.Start - lastWord.Start - lastWord.Length);

                        misspelledWords.Add(new FileMisspelling(errorSpan, deleteWordSpan, actualWord));

                        lastWord = word;
                        continue;
                    }

                    lastWord = word;

                    // If the word is not being ignored, perform the other checks
                    if(!dictionary.ShouldIgnoreWord(textToCheck))
                    {
                        if(!dictionary.IsSpelledCorrectly(textToCheck))
                        {
                            // Sometimes it flags a word as misspelled if it ends with "'s".  Try checking the
                            // word without the "'s".  If ignored or correct without it, don't flag it.  This
                            // appears to be caused by the definitions in the dictionary rather than Hunspell.
                            if(textToCheck.EndsWith("'s", StringComparison.OrdinalIgnoreCase) ||
                              textToCheck.EndsWith("\u2019s", StringComparison.OrdinalIgnoreCase))
                            {
                                string aposEss = textToCheck.Substring(textToCheck.Length - 2);

                                textToCheck = textToCheck.Substring(0, textToCheck.Length - 2);

                                if(dictionary.ShouldIgnoreWord(textToCheck) ||
                                  dictionary.IsSpelledCorrectly(textToCheck))
                                    continue;

                                textToCheck += aposEss;
                            }

                            // Some dictionaries include a trailing period on certain words such as "etc." which
                            // we don't include.  If the word is followed by a period, try it with the period to
                            // see if we get a match.  If so, consider it valid.
                            if(word.Start + word.Length < textToSplit.Length && textToSplit[word.Start + word.Length] == '.')
                            {
                                if(dictionary.ShouldIgnoreWord(textToCheck + ".") ||
                                  dictionary.IsSpelledCorrectly(textToCheck + "."))
                                    continue;
                            }

                            misspelledWords.Add(new FileMisspelling(errorSpan, actualWord));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// This returns the context menu actions for the current misspelling
        /// </summary>
        /// <returns>An enumerable list of menu actions to add to the context menu</returns>
        private IEnumerable<FrameworkElement> MenuActions()
        {
            List<FrameworkElement> commands = new List<FrameworkElement>();

            if(selectedMisspelling.MisspellingType == MisspellingType.MisspelledWord)
            {
                commands.AddRange(dictionary.SuggestCorrections(selectedMisspelling.Word).Select(s => new MenuItem
                {
                    Header = s.Suggestion,
                    Command = new MenuCommand(o => this.ReplaceWord(s))
                }));

                if(commands.Count != 0)
                {
                    commands.Add(new Separator());

                    foreach(var d in dictionary.Dictionaries)
                        commands.Add(new MenuItem
                        {
                            Header = String.Format(CultureInfo.InvariantCulture, "Add to Dictionary - {0} ({1})",
                                d.Culture.EnglishName, d.Culture.Name),
                            Command = new MenuCommand(o => this.AddToDictionary(d.Culture))
                        });

                    commands.Add(new MenuItem
                    {
                        Header = "Ignore Word",
                        Command = new MenuCommand(o => this.IgnoreWord())
                    });
                }
                else
                    commands.Add(new MenuItem { Header = "No Suggestions", Command = new MenuCommand(null) });
            }
            else
                commands.Add(new MenuItem
                {
                    Header = "Delete Word",
                    Command = new MenuCommand(o => this.DeleteWord())
                });

            return commands;
        }

        /// <summary>
        /// Replace the misspelled word with the given suggestion
        /// </summary>
        /// <param name="suggestion">The suggestion with which to replace the word</param>
        private void ReplaceWord(SpellingSuggestion suggestion)
        {
            int index = selectedMisspelling.Span.Start;

            textBox.Text = textBox.Text.Remove(index, selectedMisspelling.Span.Length).Insert(index, suggestion.Suggestion);
            textBox.SelectionStart = index + suggestion.Suggestion.Length;
        }

        /// <summary>
        /// Add the word to the dictionary
        /// </summary>
        /// <param name="culture">The culture of the dictionary to which the word is added</param>
        private void AddToDictionary(CultureInfo culture)
        {
            dictionary.AddWordToDictionary(selectedMisspelling.Word, culture);
            textBox.RaiseEvent(new TextChangedEventArgs(TextBox.TextChangedEvent, UndoAction.None));
        }

        /// <summary>
        /// Ignore the word
        /// </summary>
        private void IgnoreWord()
        {
            dictionary.IgnoreWord(selectedMisspelling.Word);
            textBox.RaiseEvent(new TextChangedEventArgs(TextBox.TextChangedEvent, UndoAction.None));
        }

        /// <summary>
        /// Delete a doubled word
        /// </summary>
        private void DeleteWord()
        {
            int index = selectedMisspelling.DeleteWordSpan.Start;

            textBox.Text = textBox.Text.Remove(index, selectedMisspelling.DeleteWordSpan.Length);
            textBox.SelectionStart = index;
        }

        /// <summary>
        /// Copy the control name to the clipboard ready for pasting into the configuration editor
        /// </summary>
        private void CopyNameToClipboard()
        {
            Clipboard.SetText(ElementName(textBox));
        }
        #endregion

        #region Event handlers
        //=====================================================================

        /// <summary>
        /// Disconnect the spell checker when the parent window closes
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void window_Closing(object sender, CancelEventArgs e)
        {
            this.Disconnect();
        }

        /// <summary>
        /// This performs the spell checking process and updates the adorner when the timer elapses
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                try
                {
                    this.CheckSpelling(textBox.Text);
                    adorner.UpdateMisspellings(misspelledWords);
                }
                catch(Exception ex)
                {
                    // Ignore exceptions
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            }));
        }

        /// <summary>
        /// This is used to initiate the spell checking timer on the text box when the text changes
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void textBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(timer != null)
            {
                if(e != null)
                    adorner.UpdateOffsets(e.Changes);

                timer.Stop();
                timer.Start();
            }
        }

        /// <summary>
        /// This adds the spell checking suggestions to the text box context menu
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void textBox_ContextMenuOpeningHandler(object sender, RoutedEventArgs e)
        {
            var lastSelectedMisspelling = selectedMisspelling;
            int idx = 0, selectionStart = textBox.SelectionStart;

            selectedMisspelling = misspelledWords.FirstOrDefault(
                w => selectionStart >= w.Span.Start && selectionStart <= w.Span.Start + w.Span.Length);

            // Remove prior suggestions before adding the current ones
            if(lastSelectedMisspelling != selectedMisspelling)
                foreach(var m in textBox.ContextMenu.Items.OfType<FrameworkElement>().Where(m => m.Tag == this).ToArray())
                    textBox.ContextMenu.Items.Remove(m);

            if(selectedMisspelling != null && selectedMisspelling != lastSelectedMisspelling)
            {
                foreach(var item in this.MenuActions())
                {
                    item.Tag = this;
                    textBox.ContextMenu.Items.Insert(idx++, item);
                }

                textBox.ContextMenu.Items.Insert(idx++, new Separator { Tag = this });
            }

            if(Keyboard.IsKeyDown(Key.LeftCtrl) && (selectedMisspelling != lastSelectedMisspelling ||
              !textBox.ContextMenu.Items.OfType<FrameworkElement>().Any(m => m.Tag == this)))
            {
                textBox.ContextMenu.Items.Insert(idx++, new MenuItem
                {
                    Header = "Copy Name to Clipboard",
                    Command = new MenuCommand(o => this.CopyNameToClipboard()),
                    Tag = this
                });

                textBox.ContextMenu.Items.Insert(idx, new Separator { Tag = this });
            }
        }
        #endregion
    }
}
