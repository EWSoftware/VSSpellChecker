//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : SolutionProjectSpellCheckControl.cs
// Authors : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 10/28/2015
// Note    : Copyright 2015, Eric Woodruff, All rights reserved
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
// 08/23/2015  EFW   Created the code
//===============================================================================================================

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using EnvDTE;
using EnvDTE80;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;

using VisualStudio.SpellChecker.Definitions;
using PackageResources = VisualStudio.SpellChecker.Properties.Resources;
using VisualStudio.SpellChecker.ProjectSpellCheck;

namespace VisualStudio.SpellChecker.ToolWindows
{
    /// <summary>
    /// This user control handles spell checking a solution, project, or selected items
    /// </summary>
    public partial class SolutionProjectSpellCheckControl : UserControl
    {
        #region Private data members
        //=====================================================================

        private List<string> projectNames;
        private CancellationTokenSource cancellationTokenSource;
        private WordSplitter wordSplitter;

        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        public SolutionProjectSpellCheckControl()
        {
            projectNames = new List<string>();

            InitializeComponent();

            ucSpellCheck.UpdateState(false, false, null);

            this.UpdateProjects(null);
        }
        #endregion

        #region Helper methods
        //=====================================================================

        /// <summary>
        /// Select and spell check the given target if not already performing a spell check
        /// </summary>
        /// <param name="target">The target to select and spell check</param>
        public void SpellCheck(SpellCheckTarget target)
        {
            bool startSpellCheck = true;

            if(cancellationTokenSource != null)
            {
                if(!this.CancelSpellCheck(true))
                    return;

                // If we canceled one, don't start another with the new settings
                startSpellCheck = false;
            }

            if(cboSpellCheckTarget.Items.Count != 0 && cboSpellCheckTarget.IsEnabled)
            {
                switch(target)
                {
                    case SpellCheckTarget.EntireSolution:
                        cboSpellCheckTarget.SelectedIndex = 0;
                        break;

                    case SpellCheckTarget.SelectedItems:
                        cboSpellCheckTarget.SelectedIndex = cboSpellCheckTarget.Items.Count - 1;
                        break;

                    case SpellCheckTarget.CurrentProject:
                        var dte2 = Utility.GetServiceFromPackage<DTE2, SDTE>(false);

                        if(dte2 != null && dte2.Solution != null && !String.IsNullOrWhiteSpace(dte2.Solution.FullName))
                        {
                            string currentProject = null;

                            if(dte2.ActiveSolutionProjects != null)
                            {
                                Project project = null;
                                Array activeProjects = (Array)dte2.ActiveSolutionProjects;

                                if(activeProjects != null && activeProjects.Length != 0)
                                {
                                    project = (Project)activeProjects.GetValue(0);

                                    if(project != null && project.Kind != EnvDTE.Constants.vsProjectKindUnmodeled)
                                        currentProject = project.FullName;
                                }

                                if(String.IsNullOrWhiteSpace(currentProject))
                                    if(dte2.Solution.SolutionBuild != null)
                                    {
                                        var startupProjects = (Array)dte2.Solution.SolutionBuild.StartupProjects;

                                        if(startupProjects != null && startupProjects.Length != 0)
                                        {
                                            currentProject = (string)startupProjects.GetValue(0);

                                            var item = dte2.Solution.EnumerateProjects().FirstOrDefault(
                                                p => p.UniqueName == currentProject);

                                            if(item != null)
                                                currentProject = item.FullName;
                                            else
                                                currentProject = null;
                                        }
                                    }
                            }

                            if(currentProject == null)
                            {
                                MessageBox.Show("Unable to determine the current project from the Solution " +
                                    "Explorer selection.  Please select an item within the desired project first.",
                                    PackageResources.PackageTitle, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                                return;
                            }

                            int idx = projectNames.IndexOf(currentProject);

                            if(idx == -1)
                            {
                                MessageBox.Show("Unable to find the current project in the list of available " +
                                    "projects.  Please select the desired project manually.",
                                    PackageResources.PackageTitle, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                                return;
                            }

                            cboSpellCheckTarget.SelectedIndex = idx + 1;
                        }
                        break;

                    default:
                        break;
                }

                if(startSpellCheck)
                    btnStartCancel_Click(this, null);
            }
        }

        /// <summary>
        /// Update the list of projects that can be spell checked
        /// </summary>
        /// <param name="spellCheckProjectNames">An enumerable list of project names that should be spell checked</param>
        public void UpdateProjects(IEnumerable<string> spellCheckProjectNames)
        {
            // If spell checking is in progress, cancel it
            this.CancelSpellCheck(false);

            projectNames.Clear();

            cboSpellCheckTarget.Items.Clear();
            dgIssues.ItemsSource = null;

            if(spellCheckProjectNames != null && spellCheckProjectNames.Count() != 0)
            {
                projectNames.AddRange(spellCheckProjectNames);

                cboSpellCheckTarget.Items.Add("Entire solution");

                // Website projects use a folder name for the project so remove the trailing backslash
                foreach(string p in spellCheckProjectNames)
                    if(p.Length > 1 && p[p.Length - 1] == '\\')
                        cboSpellCheckTarget.Items.Add(Path.GetFileName(p.Substring(0, p.Length - 1)));
                    else
                        cboSpellCheckTarget.Items.Add(Path.GetFileName(p));

                cboSpellCheckTarget.Items.Add("Selected Solution Explorer items");
                cboSpellCheckTarget.SelectedIndex = 0;

                cboSpellCheckTarget.IsEnabled = txtMaxIssues.IsEnabled = btnStartCancel.IsEnabled = true;
                lblProgress.Text = "Select an option above to spell check and click Start";
            }
            else
            {
                cboSpellCheckTarget.IsEnabled = txtMaxIssues.IsEnabled = btnStartCancel.IsEnabled = false;
                lblProgress.Text = "Load a solution with projects to spell check";
            }

            lblProgress.Style = (Style)this.FindResource("NotificationText");
        }

        /// <summary>
        /// Add a new project to the list that can be spell checked
        /// </summary>
        /// <param name="projectName">The project name to add</param>
        public void AddProject(string projectName)
        {
            // If spell checking is in progress, cancel it
            this.CancelSpellCheck(false);

            if(cboSpellCheckTarget.Items.Count == 0)
                this.UpdateProjects(new[] { projectName });
            else
            {
                projectNames.Add(projectName);
                cboSpellCheckTarget.Items.Insert(cboSpellCheckTarget.Items.Count - 1,
                    Path.GetFileName(projectName));
            }
        }

        /// <summary>
        /// Remove a project from the list that can be spell checked
        /// </summary>
        /// <param name="projectName">The project name to remove</param>
        public void RemoveProject(string projectName)
        {
            // If spell checking is in progress, cancel it
            this.CancelSpellCheck(false);

            if(cboSpellCheckTarget.Items.Count == 3 && projectNames.Contains(projectName))
                this.UpdateProjects(new string[] { });
            else
            {
                int idx = projectNames.IndexOf(projectName);

                if(idx != -1)
                {
                    projectNames.RemoveAt(idx);
                    cboSpellCheckTarget.Items.RemoveAt(idx + 1);

                    if(cboSpellCheckTarget.SelectedIndex == -1)
                        cboSpellCheckTarget.SelectedIndex = (idx - 1) < 0 ? 0 : idx - 1;
                }
            }
        }

        /// <summary>
        /// Rename a project in the list that can be spell checked
        /// </summary>
        /// <param name="oldName">The old project name</param>
        /// <param name="newName">The new project name</param>
        public void ProjectRenamed(string oldName, string newName)
        {
            // If spell checking is in progress, cancel it
            this.CancelSpellCheck(false);

            int idx = projectNames.IndexOf(oldName);

            if(idx != -1)
            {
                projectNames[idx] = newName;
                cboSpellCheckTarget.Items[idx] = Path.GetFileName(newName);
            }
        }

        /// <summary>
        /// If a spell check is already in progress, offer to cancel it
        /// </summary>
        /// <returns>True if cancelled or one wasn't in progress, false if one was and the user chose not to
        /// cancel it.</returns>
        public bool CancelSpellCheck(bool withPrompt)
        {
            if(cancellationTokenSource != null)
            {
                if(!withPrompt)
                {
                    cancellationTokenSource.Cancel();
                    return true;
                }

                if(MessageBox.Show("A spell check is in progress.  Do you want to cancel it?",
                  PackageResources.PackageTitle, MessageBoxButton.YesNo, MessageBoxImage.Question,
                  MessageBoxResult.No) == MessageBoxResult.No)
                    return false;

                if(cancellationTokenSource != null)
                    cancellationTokenSource.Cancel();
            }

            return true;
        }

        /// <summary>
        /// This is used to open the given file in a text editor if possible
        /// </summary>
        /// <param name="filename">The filename for which to open a text editor</param>
        /// <returns>The window frame reference if successful, null if not</returns>
        private static IVsWindowFrame OpenTextEditorForFile(string filename)
        {
            Microsoft.VisualStudio.OLE.Interop.IServiceProvider ppSP;
            IVsUIHierarchy ppHier;
            IVsWindowFrame ppWindowFrame = null;
            uint pitemid;

            var openDoc = Utility.GetServiceFromPackage<IVsUIShellOpenDocument, SVsUIShellOpenDocument>(false);

            if(openDoc != null && openDoc.OpenDocumentViaProject(filename, VSConstants.LOGVIEWID_TextView,
              out ppSP, out ppHier, out pitemid, out ppWindowFrame) == VSConstants.S_OK)
            {
                // On occasion, the call above is successful but we get a null frame for some reason
                if(ppWindowFrame != null)
                    if(ppWindowFrame.Show() != VSConstants.S_OK)
                        ppWindowFrame = null;
            }

            return ppWindowFrame;
        }

        /// <summary>
        /// This is used to get an <see cref="IVsTextView"/> reference for the given document
        /// </summary>
        /// <param name="filename">The filename for which to get a text view reference</param>
        /// <param name="position">The initial position at which to place the cursor or -1 to leave it at the
        /// top of the file.</param>
        /// <param name="selectionLength">The length of text to select a <paramref name="position"/> or -1 to
        /// not select any text at that location.</param>
        /// <returns>Returns the text view if the document could be opened in a text editor instance or was
        /// already open in one.  Returns null if the reference could not be obtained.</returns>
        private static IVsTextView GetTextViewForDocument(string filename, int position, int selectionLength)
        {
            IVsTextView textView = null;
            var frame = OpenTextEditorForFile(filename);

            if(frame != null)
            {
                textView = VsShellUtilities.GetTextView(frame);

                if(textView != null && position != -1)
                {
                    int line, column, topLine;

                    if(textView.GetLineAndColumn(position, out line, out column) == VSConstants.S_OK &&
                      textView.SetCaretPos(line, column) == VSConstants.S_OK)
                    {
                        if(selectionLength != -1)
                            textView.SetSelection(line, column, line, column + selectionLength);

                        // Ensure some surrounding lines are visible so that it's not right at the top
                        // or bottom of the view.
                        topLine = line - 5;

                        if(topLine < 0)
                            topLine = 0;

                        textView.EnsureSpanVisible(new TextSpan
                        {
                            iStartLine = topLine,
                            iStartIndex = column,
                            iEndLine = line + 5,
                            iEndIndex = column
                        });
                    }
                    else
                        textView = null;
                }
            }

            return textView;
        }

        /// <summary>
        /// This is used to adjust the locations of other issues that are after the given issue
        /// </summary>
        /// <param name="issue">The issue used to adjust the related issues</param>
        /// <param name="replacement">The replacement text to use in determining the adjustment</param>
        private void AdjustAffectedIssues(FileMisspelling issue, string replacement)
        {
            int adjustment;

            if(issue.MisspellingType != MisspellingType.DoubledWord)
                adjustment = replacement.Length - issue.Span.Length;
            else
                adjustment = replacement.Length - issue.DeleteWordSpan.Length;

            var relatedIssues = ((IList<FileMisspelling>)dgIssues.ItemsSource).Where(i =>
                i.CanonicalName.Equals(issue.CanonicalName, StringComparison.OrdinalIgnoreCase) &&
                i.Span.Start > issue.Span.Start);

            foreach(var ri in relatedIssues)
            {
                ri.Span = new Span(ri.Span.Start + adjustment, ri.Span.Length);

                if(ri.MisspellingType == MisspellingType.DoubledWord)
                    ri.DeleteWordSpan = new Span(ri.DeleteWordSpan.Start + adjustment, ri.DeleteWordSpan.Length);
            }
        }

        /// <summary>
        /// Get a list of filenames for all files currently open in the IDE
        /// </summary>
        /// <returns>An enumerable list of the filenames of currently open documents</returns>
        /// <remarks>This has to be done on the UI thread.  By getting the names upfront, we avoid having to
        /// switch to the UI thread repeatedly while doing the spell checking to see if the document is open.</remarks>
        private IEnumerable<string> GetOpenDocumentNames()
        {
            IEnumRunningDocuments documents;
            IVsHierarchy docHierarchy;
            IntPtr docData = IntPtr.Zero;
            uint[] docCookie = new uint[1];
            uint flags, editLocks, readLocks, docId, fetched;
            string moniker;

            var rdt = Utility.GetServiceFromPackage<IVsRunningDocumentTable, SVsRunningDocumentTable>(false);

            if(rdt != null)
            {
                rdt.GetRunningDocumentsEnum(out documents);

                while(documents.Next(1, docCookie, out fetched) == VSConstants.S_OK && fetched == 1)
                {
                    rdt.GetDocumentInfo(docCookie[0], out flags, out readLocks, out editLocks, out moniker,
                        out docHierarchy, out docId, out docData);

                    yield return moniker;
                }
            }
        }

        /// <summary>
        /// This is used to get the content of a document that is currently open in the IDE
        /// </summary>
        /// <param name="filename">The filename of the document for which to get the content</param>
        /// <returns>The document content if the file is still open or null if it could not be found</returns>
        /// <remarks>This runs on the UI thread as it has to access the running document table</remarks>
        private static string GetDocumentText(string filename)
        {
            string documentText;

            // Switch to the UI thread to get the document text
            documentText = ThreadHelper.Generic.Invoke<string>(() =>
            {
                IVsHierarchy hierarchy;
                uint itemid, lockCookie = 0;
                int endLine, endIndex;
                string text = null;
                IntPtr docDataUnk = IntPtr.Zero;

                var rdt = Utility.GetServiceFromPackage<IVsRunningDocumentTable, SVsRunningDocumentTable>(false);

                if(rdt != null)
                {
                    try
                    {
                        if(rdt.FindAndLockDocument((uint)_VSRDTFLAGS.RDT_ReadLock, filename, out hierarchy,
                          out itemid, out docDataUnk, out lockCookie) == VSConstants.S_OK)
                        {
                            var textLines = Marshal.GetUniqueObjectForIUnknown(docDataUnk) as IVsTextLines;

                            if(textLines == null || textLines.GetLastLineIndex(out endLine,
                              out endIndex) != VSConstants.S_OK || textLines.GetLineText(0, 0, endLine, endIndex,
                              out text) != VSConstants.S_OK)
                            {
                                text = null;
                            }
                        }
                    }
                    finally
                    {
                        if(docDataUnk != IntPtr.Zero)
                            Marshal.Release(docDataUnk);

                        if(lockCookie != 0)
                            rdt.UnlockDocument((uint)_VSRDTFLAGS.RDT_ReadLock, lockCookie);
                    }
                }

                return text;
            });

            return documentText;
        }
        #endregion

        #region Spell checking methods
        //=====================================================================

        /// <summary>
        /// This is used to handle spell checking the given set of files in the background
        /// </summary>
        /// <param name="maxIssues">The maximum number of issues to report.</param>
        /// <param name="spellCheckFiles">The files to spell check.</param>
        /// <param name="codeAnalysisFiles">The code analysis dictionaries from each project that may be used in
        /// the configurations used for spell checking.</param>
        /// <param name="openDocuments">A list of documents open in the IDE.  For these files, we'll get the
        /// content from the editor if possible rather than the file on disk.</param>
        private IEnumerable<FileMisspelling> SpellCheckFiles(int maxIssues,
          IEnumerable<SpellCheckFileInfo> spellCheckFiles, Dictionary<string, List<string>> codeAnalysisFiles,
          HashSet<string> openDocuments)
        {
            BindingList<FileMisspelling> issues = new BindingList<FileMisspelling>();
            TextClassifier classifier;
            List<string> cadFiles;
            string documentText;

            try
            {
                if(wordSplitter == null)
                    wordSplitter = new WordSplitter();

                foreach(var file in spellCheckFiles)
                {
                    cancellationTokenSource.Token.ThrowIfCancellationRequested();

                    // Get the code analysis files for the related project.  These may be used to generate the
                    // spell checking configuration.
                    if(!codeAnalysisFiles.TryGetValue(file.ProjectFile, out cadFiles))
                        cadFiles = null;

                    wordSplitter.Configuration = file.GenerateConfiguration(cadFiles);

                    if(wordSplitter.Configuration != null)
                    {
                        // Create a dictionary for each configuration dictionary language ignoring any that are
                        // invalid and duplicates caused by missing languages which return the en-US dictionary.
                        var globalDictionaries = wordSplitter.Configuration.DictionaryLanguages.Select(l =>
                            GlobalDictionary.CreateGlobalDictionary(l, null,
                            wordSplitter.Configuration.AdditionalDictionaryFolders,
                            wordSplitter.Configuration.RecognizedWords)).Where(d => d != null).Distinct().ToList();

                        if(globalDictionaries.Any())
                        {
                            var dictionary = new SpellingDictionary(globalDictionaries,
                                wordSplitter.Configuration.IgnoredWords);

                            classifier = ClassifierFactory.GetClassifier(file.CanonicalName, wordSplitter.Configuration);

                            // If null, the file type is ignored
                            if(classifier != null)
                            {
                                wordSplitter.Mnemonic = ClassifierFactory.GetMnemonic(file.Filename);

                                // If open in an editor, use the current text from it if possible
                                if(openDocuments.Contains(file.CanonicalName))
                                {
                                    documentText = GetDocumentText(file.CanonicalName);

                                    if(documentText != null)
                                        classifier.SetText(documentText);
                                }

                                // Switch to the UI thread to update the progress and then switch back to this one
                                ThreadHelper.Generic.Invoke(() =>
                                {
                                    lblProgress.Text = "Spell checking " + file.Description;
                                });

                                foreach(var issue in this.GetMisspellingsInSpans(dictionary, classifier.Parse()))
                                {
                                    issue.Dictionary = dictionary;
                                    issue.ProjectName = Path.GetFileName(file.ProjectFile);
                                    issue.Filename = file.Filename;
                                    issue.CanonicalName = file.CanonicalName;
                                    issue.LineNumber = classifier.GetLineNumber(issue.Span.Start);
                                    issue.LineText = classifier[issue.LineNumber].Trim();

                                    issues.Add(issue);

                                    if(issues.Count >= maxIssues)
                                        break;
                                }
                            }
                        }
                    }
                }
            }
            catch(OperationCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("Spell checking process canceled");
            }
            finally
            {
                wordSplitter.Configuration = null;
            }

            return issues;
        }

        /// <summary>
        /// Get misspelled words in the given set of spans
        /// </summary>
        /// <param name="dictionary">The dictionary to use for checking words</param>
        /// <param name="spans">An enumerable list of spans to check</param>
        /// <returns>An enumerable list of misspelling issues</returns>
        private IEnumerable<FileMisspelling> GetMisspellingsInSpans(SpellingDictionary dictionary,
          IEnumerable<SpellCheckSpan> spans)
        {
            List<Match> rangeExclusions = null;
            IList<string> spellingAlternates;
            Span errorSpan, deleteWordSpan, lastWord;
            string textToSplit, actualWord, textToCheck, preferredTerm;
            int mnemonicPos;

            // **************************************************************************************************
            // NOTE: If anything changes here, update the related tagger spell checking code in
            // SpellingTagger.cs\GetMisspellingsInSpans().
            // **************************************************************************************************
            foreach(var span in spans)
            {
                cancellationTokenSource.Token.ThrowIfCancellationRequested();

                textToSplit = span.Text;

                // Always ignore URLs
                rangeExclusions = WordSplitter.Url.Matches(textToSplit).OfType<Match>().ToList();

                // Note the location of all XML elements if needed
                if(wordSplitter.Configuration.IgnoreXmlElementsInText)
                    rangeExclusions.AddRange(WordSplitter.XmlElement.Matches(textToSplit).OfType<Match>());

                // Add exclusions from the configuration if any
                foreach(var exclude in wordSplitter.Configuration.ExclusionExpressions)
                    try
                    {
                        rangeExclusions.AddRange(exclude.Matches(textToSplit).OfType<Match>());
                    }
                    catch(RegexMatchTimeoutException ex)
                    {
                        // Ignore expression timeouts
                        System.Diagnostics.Debug.WriteLine(ex);
                    }

                lastWord = new Span();

                wordSplitter.Classification = span.Classification;

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
                        errorSpan = new Span(span.Span.Start + word.Start, word.Length);

                        // Check for a doubled word
                        if(wordSplitter.Configuration.DetectDoubledWords && lastWord.Length != 0 &&
                          textToSplit.Substring(lastWord.Start, lastWord.Length).Equals(actualWord,
                          StringComparison.OrdinalIgnoreCase) && String.IsNullOrWhiteSpace(textToSplit.Substring(
                          lastWord.Start + lastWord.Length, word.Start - lastWord.Start - lastWord.Length)))
                        {
                            // Delete the whitespace ahead of it too
                            deleteWordSpan = new Span(span.Span.Start + lastWord.Start + lastWord.Length,
                                word.Length + word.Start - lastWord.Start - lastWord.Length);

                            yield return new FileMisspelling(errorSpan, deleteWordSpan, actualWord);

                            lastWord = word;
                            continue;
                        }

                        lastWord = word;

                        // If the word is not being ignored, perform the other checks
                        if(!dictionary.ShouldIgnoreWord(textToCheck))
                        {
                            // Handle code analysis dictionary checks first as they may be not be recognized as
                            // correctly spelled words but have alternate handling.
                            if(wordSplitter.Configuration.CadOptions.TreatDeprecatedTermsAsMisspelled &&
                              wordSplitter.Configuration.DeprecatedTerms.TryGetValue(textToCheck, out preferredTerm))
                            {
                                yield return new FileMisspelling(MisspellingType.DeprecatedTerm, errorSpan,
                                    actualWord, new[] { new SpellingSuggestion(null, preferredTerm) });
                                continue;
                            }

                            if(wordSplitter.Configuration.CadOptions.TreatCompoundTermsAsMisspelled &&
                              wordSplitter.Configuration.CompoundTerms.TryGetValue(textToCheck, out preferredTerm))
                            {
                                yield return new FileMisspelling(MisspellingType.CompoundTerm, errorSpan,
                                    actualWord, new[] { new SpellingSuggestion(null, preferredTerm) });
                                continue;
                            }

                            if(wordSplitter.Configuration.CadOptions.TreatUnrecognizedWordsAsMisspelled &&
                              wordSplitter.Configuration.UnrecognizedWords.TryGetValue(textToCheck, out spellingAlternates))
                            {
                                yield return new FileMisspelling(MisspellingType.UnrecognizedWord, errorSpan,
                                    actualWord, spellingAlternates.Select(a => new SpellingSuggestion(null, a)));
                                continue;
                            }

                            if(!dictionary.IsSpelledCorrectly(textToCheck))
                            {
                                // Sometimes it flags a word as misspelled if it ends with "'s".  Try checking the
                                // word without the "'s".  If ignored or correct without it, don't flag it.  This
                                // appears to be caused by the definitions in the dictionary rather than Hunspell.
                                if(textToCheck.EndsWith("'s", StringComparison.OrdinalIgnoreCase))
                                {
                                    textToCheck = textToCheck.Substring(0, textToCheck.Length - 2);

                                    if(dictionary.ShouldIgnoreWord(textToCheck) ||
                                      dictionary.IsSpelledCorrectly(textToCheck))
                                        continue;

                                    textToCheck += "'s";
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

                                yield return new FileMisspelling(errorSpan, actualWord);
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region General event handlers
        //=====================================================================

        /// <summary>
        /// View the project website
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void lnkFeedback_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(lnkFeedback.NavigateUri.AbsoluteUri);
            }
            catch(Exception ex)
            {
                MessageBox.Show("Unable to navigate to website.  Reason: " + ex.Message,
                    PackageResources.PackageTitle, MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }

        /// <summary>
        /// Start the spell checking process if not underway or cancel it if it is underway
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private async void btnStartCancel_Click(object sender, RoutedEventArgs e)
        {
            if(cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
                btnStartCancel.IsEnabled = false;
                return;
            }

            Dictionary<string, List<string>> codeAnalysisFiles = new Dictionary<string, List<string>>(
                StringComparer.OrdinalIgnoreCase);
            List<SpellCheckFileInfo> spellCheckFiles;
            int maxIssues;

            if(!Int32.TryParse(txtMaxIssues.Text, out maxIssues))
            {
                txtMaxIssues.Text = "5000";
                maxIssues = 5000;
            }

            lblProgress.Style = (Style)this.FindResource("PlainText");
            lblProgress.Text = "Beginning spell check of " + cboSpellCheckTarget.Text;
            cboSpellCheckTarget.IsEnabled = txtMaxIssues.IsEnabled = false;
            btnStartCancel.Content = "Cancel";
            spSpinner.Visibility = Visibility.Visible;
            dgIssues.ItemsSource = null;

            Utility.GetServiceFromPackage<IVsUIShell, SVsUIShell>(true).SetWaitCursor();

            // Get the files to spell check.  This must be done on the UI thread as it interacts with the
            // project system.  It should be fast even for large projects.
            if(cboSpellCheckTarget.SelectedIndex == 0)
                spellCheckFiles = SpellCheckFileInfo.AllProjectFiles(null).ToList();
            else
                if(cboSpellCheckTarget.SelectedIndex == cboSpellCheckTarget.Items.Count - 1)
                    spellCheckFiles = SpellCheckFileInfo.SelectedProjectFiles().ToList();
                else
                    spellCheckFiles = SpellCheckFileInfo.AllProjectFiles(projectNames[
                        cboSpellCheckTarget.SelectedIndex - 1]).ToList();

            // Likewise, get the list of open documents upfront.  That way, we only need to switch to the UI
            // thread to get the content of documents that we know are open.
            var openDocuments = new HashSet<string>(this.GetOpenDocumentNames(), StringComparer.OrdinalIgnoreCase);

            // I'm not sure if there's a better way to do this but it does seem to work.  We need to find one or
            // more arbitrary files with an item type of "CodeAnalysisDictionary".  We do so by getting the
            // MSBuild project from the global project collection and using its GetItems() method to find them.
            foreach(var p in Microsoft.Build.Evaluation.ProjectCollection.GlobalProjectCollection.LoadedProjects)
            {
                List<string> files = new List<string>();

                // Typically there is only one but multiple files are supported
                foreach(var cad in p.GetItems("CodeAnalysisDictionary"))
                {
                    string filename = Path.Combine(Path.GetDirectoryName(p.FullPath), cad.EvaluatedInclude);

                    if(File.Exists(filename))
                        files.Add(filename);
                }

                if(files.Count != 0)
                {
                    codeAnalysisFiles.Add(p.FullPath, files);

                    var excludeFiles = new HashSet<string>(codeAnalysisFiles.Values.SelectMany(c => c).Distinct(),
                        StringComparer.OrdinalIgnoreCase);

                    spellCheckFiles = spellCheckFiles.Where(s => !excludeFiles.Contains(s.CanonicalName)).ToList();
                }
            }

            try
            {
                cancellationTokenSource = new CancellationTokenSource();

                var issues = await System.Threading.Tasks.Task.Run<IEnumerable<FileMisspelling>>(
                    () => this.SpellCheckFiles(maxIssues, spellCheckFiles, codeAnalysisFiles, openDocuments),
                            cancellationTokenSource.Token);

                dgIssues.ItemsSource = issues;

                if(!cancellationTokenSource.IsCancellationRequested)
                {
                    if(issues.Count() < maxIssues)
                        lblProgress.Text = "Spell check completed";
                    else
                        lblProgress.Text = "Spell check stopped.  Maximum number of issues reached.";
                }
                else
                    lblProgress.Text = "Spell check canceled";

                if(dgIssues.Items.Count != 0)
                {
                    dgIssues.SelectedIndex = 0;
                    dgIssues.Focus();
                }
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());

                lblProgress.Text = "Spell check failed";

                MessageBox.Show("Unable to complete the spell checking operation.  Error: " +
                    ex.Message, PackageResources.PackageTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if(cancellationTokenSource != null)
                {
                    cancellationTokenSource.Dispose();
                    cancellationTokenSource = null;
                }

                // If a solution is closed, and there are not targets, don't enable the controls
                if(cboSpellCheckTarget.Items.Count != 0)
                {
                    cboSpellCheckTarget.IsEnabled = txtMaxIssues.IsEnabled = true;
                    btnStartCancel.IsEnabled = true;
                }

                btnStartCancel.Content = "S_tart";
                spSpinner.Visibility = Visibility.Hidden;
            }
        }

        /// <summary>
        /// Determine whether or not the data grid context menu should open
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void dgIssues_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            e.Handled = (dgIssues.Items.Count == 0);

            if(!e.Handled)
            {
                var issue = ucSpellCheck.CurrentIssue;

                miIgnoreAll.IsEnabled = (issue != null && issue.MisspellingType != MisspellingType.DoubledWord);
                dgIssues.Focus();
            }
        }

        /// <summary>
        /// Copy just the selected issue's word to the clipboard, not the whole row
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void dgIssues_CopyingRowClipboardContent(object sender, DataGridRowClipboardEventArgs e)
        {
            var currentCell = e.ClipboardRowContent[4];

            e.ClipboardRowContent.Clear();
            e.ClipboardRowContent.Add(currentCell);
        }

        /// <summary>
        /// Update the spell check control when the selection changes in the grid
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void dgIssues_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int idx = dgIssues.SelectedIndex;

            if(idx != -1 && idx < dgIssues.Items.Count)
            {
                lblIssueCount.Text = String.Format(CultureInfo.CurrentUICulture, "{0} of {1}",
                    dgIssues.SelectedIndex + 1, dgIssues.Items.Count);

                var issues = (IList<FileMisspelling>)dgIssues.ItemsSource;
                var issue = issues[idx];

                // Get suggestions if not already set
                if(!issue.SuggestionsDetermined)
                {
                    issue.SuggestionsDetermined = true;
                    issue.Suggestions = issue.Dictionary.SuggestCorrections(issue.Word);
                }

                if(issue.Dictionary.DictionaryCount == 1)
                    ucSpellCheck.SetAddWordContextMenuDictionaries(null);
                else
                    ucSpellCheck.SetAddWordContextMenuDictionaries(
                        issue.Dictionary.Dictionaries.Select(d => d.Culture));

                ucSpellCheck.UpdateState(false, (issue.Dictionary.DictionaryCount != 1), issue);
            }
            else
            {
                if(dgIssues.Items.Count == 0)
                {
                    ucSpellCheck.NoCurrentIssueText = "(No more issues)";
                    lblIssueCount.Text = "--";
                }
                else
                {
                    ucSpellCheck.NoCurrentIssueText = "(Select an issue to correct)";
                    lblIssueCount.Text = String.Format(CultureInfo.CurrentUICulture, "-- of {0}",
                        dgIssues.Items.Count);
                }

                ucSpellCheck.UpdateState(false, false, null);
            }
        }

        /// <summary>
        /// Treat double clicks on grid rows as requests to open the file and go to the issue
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void dgIssues_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DataGridRow row = ItemsControl.ContainerFromElement((DataGrid)sender,
              e.OriginalSource as DependencyObject) as DataGridRow;

            if(row != null)
                cmdGoToIssue_Executed(sender, null);
        }

        /// <summary>
        /// Sort the issues based on the column clicked
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        /// <remarks>There's probably an easier way to do this but this works.  Line number and line text are
        /// ignored as there's not much point in sorting on those values.</remarks>
        private void dgIssues_Sorting(object sender, DataGridSortingEventArgs e)
        {
            var issues = (IList<FileMisspelling>)dgIssues.ItemsSource;

            e.Handled = true;

            if(issues != null && issues.Count != 0)
            {
                IOrderedEnumerable<FileMisspelling> sort = null;
                var direction = (e.Column.SortDirection != ListSortDirection.Ascending) ?
                    ListSortDirection.Ascending : ListSortDirection.Descending;

                switch(dgIssues.Columns.IndexOf(e.Column))
                {
                    case 0:
                        if(direction == ListSortDirection.Ascending)
                            sort = issues.OrderBy(i => i.ProjectName);
                        else
                            sort = issues.OrderByDescending(i => i.ProjectName);
                        break;

                    case 1:
                        if(direction == ListSortDirection.Ascending)
                            sort = issues.OrderBy(i => i.Filename);
                        else
                            sort = issues.OrderByDescending(i => i.Filename);
                        break;

                    case 3:
                        if(direction == ListSortDirection.Ascending)
                            sort = issues.OrderBy(i => i.IssueDescription);
                        else
                            sort = issues.OrderByDescending(i => i.IssueDescription);
                        break;

                    case 4:
                        if(direction == ListSortDirection.Ascending)
                            sort = issues.OrderBy(i => i.Word);
                        else
                            sort = issues.OrderByDescending(i => i.Word);
                        break;

                    default:
                        break;
                }

                if(sort != null)
                {
                    int idx = dgIssues.SelectedIndex;

                    dgIssues.ItemsSource = new BindingList<FileMisspelling>(sort.ToList());
                    dgIssues.SelectedIndex = idx;

                    e.Column.SortDirection = direction;

                    dgIssues.Focus();
                }
            }
        }
        #endregion

        #region Command event handlers
        //=====================================================================

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
                    "fa790577-88c0-4141-b8f4-d8b70f625cfd");
            }
            catch(Exception ex)
            {
                MessageBox.Show("Unable to navigate to website.  Reason: " + ex.Message,
                    PackageResources.PackageTitle, MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }

        /// <summary>
        /// Replace the current misspelled word with the selected word
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void cmdReplace_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            string viewWord;
            int line, column, idx = dgIssues.SelectedIndex;

            if(idx != -1)
            {
                var issues = (IList<FileMisspelling>)dgIssues.ItemsSource;
                var currentIssue = issues[dgIssues.SelectedIndex];
                var textView = GetTextViewForDocument(currentIssue.CanonicalName, currentIssue.Span.Start, -1);

                if(textView != null)
                {
                    if(textView.GetLineAndColumn(currentIssue.Span.Start, out line, out column) ==
                      VSConstants.S_OK && textView.SetCaretPos(line, column) == VSConstants.S_OK)
                    {
                        textView.SetSelection(line, column, line, column + currentIssue.Span.Length);

                        if(textView.GetTextStream(line, column, line, column + currentIssue.Span.Length,
                          out viewWord) == VSConstants.S_OK && viewWord.Equals(currentIssue.Word,
                          StringComparison.OrdinalIgnoreCase))
                        {
                            if(currentIssue.MisspellingType != MisspellingType.DoubledWord)
                            {
                                var suggestion = ucSpellCheck.SelectedSuggestion;

                                if(suggestion != null && textView.ReplaceTextOnLine(line, column,
                                  currentIssue.Span.Length, suggestion.Suggestion,
                                  suggestion.Suggestion.Length) == VSConstants.S_OK)
                                {
                                    textView.SetSelection(line, column, line, column +
                                        suggestion.Suggestion.Length);
                                    issues.RemoveAt(dgIssues.SelectedIndex);

                                    this.AdjustAffectedIssues(currentIssue, suggestion.Suggestion);
                                }
                            }
                            else
                                if(textView.GetLineAndColumn(currentIssue.DeleteWordSpan.Start, out line,
                                  out column) == VSConstants.S_OK && textView.SetCaretPos(line, column) ==
                                  VSConstants.S_OK)
                                {
                                    if(textView.ReplaceTextOnLine(line, column, currentIssue.DeleteWordSpan.Length,
                                      String.Empty, 0) == VSConstants.S_OK)
                                    {
                                        issues.RemoveAt(dgIssues.SelectedIndex);

                                        this.AdjustAffectedIssues(currentIssue, String.Empty);
                                    }
                                }

                            if(idx >= dgIssues.Items.Count)
                                idx = dgIssues.Items.Count - 1;

                            if(idx != -1)
                                dgIssues.SelectedIndex = idx;
                            else
                                dgIssues_SelectionChanged(sender, null);

                            dgIssues.Focus();
                        }
                        else
                            MessageBox.Show("The text at the issue's location has changed and cannot be replaced",
                                PackageResources.PackageTitle, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    }
                }
                else
                    MessageBox.Show("Unable to open a text editor window for " + currentIssue.Filename,
                        PackageResources.PackageTitle, MessageBoxButton.OK, MessageBoxImage.Exclamation);
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
            int line, column, idx = dgIssues.SelectedIndex;
            var suggestion = ucSpellCheck.SelectedSuggestion;
            string viewWord, replacementWord;
            bool cancel = false;

            if(idx != -1 && suggestion != null)
            {
                var issues = (IList<FileMisspelling>)dgIssues.ItemsSource;
                var currentIssue = issues[dgIssues.SelectedIndex];

                var replacements = issues.Where(i => i.MisspellingType == currentIssue.MisspellingType &&
                    i.Word.Equals(currentIssue.Word, StringComparison.OrdinalIgnoreCase)).GroupBy(
                    i => i.CanonicalName).ToList();

                if(MessageBox.Show(String.Format(CultureInfo.CurrentUICulture, "You about to replace {0} " +
                  "occurrence(s) of the word '{1}' with '{2}' in a total of {3} file(s).  Do you want to continue?",
                  replacements.Sum(r => r.Count()), currentIssue.Word, suggestion.Suggestion, replacements.Count()),
                  PackageResources.PackageTitle, MessageBoxButton.YesNo, MessageBoxImage.Question,
                  MessageBoxResult.No) == MessageBoxResult.No)
                {
                    return;
                }

                Utility.GetServiceFromPackage<IVsUIShell, SVsUIShell>(true).SetWaitCursor();

                var replacementsPeformed = new List<FileMisspelling>();

                foreach(var file in replacements)
                {
                    var textView = GetTextViewForDocument(file.Key, -1, -1);

                    if(textView == null)
                    {
                        if(MessageBox.Show("Unable to open a text editor for '" + file.Key + "'.  Do you want " +
                          "to continue with the other replacements?", PackageResources.PackageTitle,
                          MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.No)
                        {
                            break;
                        }

                        continue;
                    }

                    foreach(var misspelling in file)
                    {
                        if(textView.GetLineAndColumn(misspelling.Span.Start, out line, out column) ==
                          VSConstants.S_OK && textView.SetCaretPos(line, column) == VSConstants.S_OK)
                        {
                            textView.SetSelection(line, column, line, column + misspelling.Span.Length);

                            if(textView.GetTextStream(line, column, line, column + misspelling.Span.Length,
                              out viewWord) == VSConstants.S_OK && viewWord.Equals(misspelling.Word,
                              StringComparison.OrdinalIgnoreCase))
                            {
                                replacementWord = suggestion.Suggestion;

                                var language = suggestion.Culture ?? CultureInfo.CurrentUICulture;

                                // Match the case of the first letter if necessary
                                if(replacementWord.Length > 1 &&
                                  (Char.IsUpper(replacementWord[0]) != Char.IsUpper(replacementWord[1]) ||
                                  (Char.IsLower(replacementWord[0]) && Char.IsLower(replacementWord[1]))))
                                    if(Char.IsUpper(viewWord[0]) && !Char.IsUpper(replacementWord[0]))
                                    {
                                        replacementWord = replacementWord.Substring(0, 1).ToUpper(language) +
                                            replacementWord.Substring(1);
                                    }
                                    else
                                        if(Char.IsLower(viewWord[0]) && !Char.IsLower(replacementWord[0]))
                                            replacementWord = replacementWord.Substring(0, 1).ToLower(language) +
                                                replacementWord.Substring(1);

                                if(textView.ReplaceTextOnLine(line, column, viewWord.Length, replacementWord,
                                  replacementWord.Length) == VSConstants.S_OK)
                                {
                                    textView.SetSelection(line, column, line, column + replacementWord.Length);
                                    replacementsPeformed.Add(misspelling);

                                    this.AdjustAffectedIssues(misspelling, replacementWord);
                                }
                            }
                            else
                            {
                                if(MessageBox.Show("The text at the issue's location in '" + misspelling.Filename +
                                  "' has changed and cannot be replaced.  Do you want to continue with the " +
                                  "other replacements?", PackageResources.PackageTitle, MessageBoxButton.YesNo,
                                  MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.No)
                                {
                                    cancel = true;
                                    break;
                                }
                            }
                        }
                    }

                    if(cancel)
                        break;
                }

                if(replacementsPeformed.Count != 0)
                    foreach(var issue in replacementsPeformed)
                        issues.Remove(issue);

                if(idx >= dgIssues.Items.Count)
                    idx = dgIssues.Items.Count - 1;

                if(idx != -1)
                    dgIssues.SelectedIndex = idx;
                else
                    dgIssues_SelectionChanged(sender, null);

                dgIssues.Focus();
            }
        }

        /// <summary>
        /// Ignore just the current occurrence of the misspelled word
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void cmdIgnoreOnce_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            int idx = dgIssues.SelectedIndex;

            if(idx != -1)
            {
                var issues = (IList<FileMisspelling>)dgIssues.ItemsSource;
                issues.RemoveAt(dgIssues.SelectedIndex);

                if(idx >= dgIssues.Items.Count)
                    idx = dgIssues.Items.Count - 1;

                if(idx != -1)
                    dgIssues.SelectedIndex = idx;
                else
                    dgIssues_SelectionChanged(sender, null);

                dgIssues.Focus();
            }
        }

        /// <summary>
        /// Ignore all occurrences of the misspelled word
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        /// <remarks>As with interactive spell checking, words ignored with this option are ignored for the
        /// remainder of the session.</remarks>
        private void cmdIgnoreAll_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            int idx = dgIssues.SelectedIndex;

            if(idx != -1)
            {
                var issues = (IList<FileMisspelling>)dgIssues.ItemsSource;
                var currentIssue = issues[dgIssues.SelectedIndex];

                currentIssue.Dictionary.IgnoreWord(currentIssue.Word);

                foreach(var issue in issues.Where(i => i.MisspellingType == currentIssue.MisspellingType &&
                  i.Word.Equals(currentIssue.Word, StringComparison.OrdinalIgnoreCase)).ToList())
                    issues.Remove(issue);

                if(idx >= dgIssues.Items.Count)
                    idx = dgIssues.Items.Count - 1;

                if(idx != -1)
                    dgIssues.SelectedIndex = idx;
                else
                    dgIssues_SelectionChanged(sender, null);

                dgIssues.Focus();
            }
        }

        /// <summary>
        /// Ignore all issues in the current issue's file
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void cmdIgnoreFile_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            int idx = dgIssues.SelectedIndex;

            if(idx != -1)
            {
                var issues = (IList<FileMisspelling>)dgIssues.ItemsSource;
                string file = issues[dgIssues.SelectedIndex].CanonicalName;

                foreach(var issue in issues.Where(i => i.CanonicalName.Equals(file,
                  StringComparison.OrdinalIgnoreCase)).ToList())
                    issues.Remove(issue);

                if(idx >= dgIssues.Items.Count)
                    idx = dgIssues.Items.Count - 1;

                if(idx != -1)
                    dgIssues.SelectedIndex = idx;
                else
                    dgIssues_SelectionChanged(sender, null);

                dgIssues.Focus();
            }
        }

        /// <summary>
        /// Ignore all issues in the current issue's project
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void cmdIgnoreProject_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            int idx = dgIssues.SelectedIndex;

            if(idx != -1)
            {
                var issues = (IList<FileMisspelling>)dgIssues.ItemsSource;
                string project = issues[dgIssues.SelectedIndex].ProjectName;

                foreach(var issue in issues.Where(i => i.ProjectName.Equals(project,
                  StringComparison.OrdinalIgnoreCase)).ToList())
                    issues.Remove(issue);

                if(idx >= dgIssues.Items.Count)
                    idx = dgIssues.Items.Count - 1;

                if(idx != -1)
                    dgIssues.SelectedIndex = idx;
                else
                    dgIssues_SelectionChanged(sender, null);

                dgIssues.Focus();
            }
        }

        /// <summary>
        /// Open the file for the current issue and place the cursor on it
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void cmdGoToIssue_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var currentIssue = ucSpellCheck.CurrentIssue as FileMisspelling;

            if(currentIssue != null)
            {
                if(GetTextViewForDocument(currentIssue.CanonicalName, currentIssue.Span.Start,
                  currentIssue.Span.Length) == null)
                {
                    MessageBox.Show("Unable to open a text editor window for " + currentIssue.Filename,
                        PackageResources.PackageTitle, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
            }
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
            int idx = dgIssues.SelectedIndex;
            string word;

            if(idx != -1)
            {
                var issues = (IList<FileMisspelling>)dgIssues.ItemsSource;
                var currentIssue = issues[idx];
                word = ucSpellCheck.MisspelledWord;

                if(word.Length != 0)
                {
                    // If the parameter is a CultureInfo instance, the word will be added to the dictionary for
                    // that culture.  If null, it's added to the first available dictionary.
                    currentIssue.Dictionary.AddWordToDictionary(word, e.Parameter as CultureInfo);

                    // If adding a modified word, replace the word in the file too and remove only this issue
                    if(!word.Equals(currentIssue.Word, StringComparison.OrdinalIgnoreCase))
                        cmdReplace_Executed(sender, e);
                    else
                    {
                        // Remove all issues related to the unmodified added word
                        foreach(var issue in issues.Where(i => i.Word.Equals(currentIssue.Word,
                          StringComparison.OrdinalIgnoreCase)).ToList())
                            issues.Remove(issue);
                    }

                    if(idx >= dgIssues.Items.Count)
                        idx = dgIssues.Items.Count - 1;

                    if(idx != -1)
                        dgIssues.SelectedIndex = idx;
                    else
                        dgIssues_SelectionChanged(sender, null);

                    dgIssues.Focus();
                }
                else
                    MessageBox.Show("Cannot add an empty word to the dictionary", PackageResources.PackageTitle,
                        MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }
        #endregion
    }
}
