//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : VSSpellCheckerPackage.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 01/20/2021
// Note    : Copyright 2013-2021, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains the class that defines the Visual Studio Spell Checker package
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 05/20/2013  EFW  Created the code
// 08/23/2015  EFW  Added support for solution/project spell checking
//===============================================================================================================

// Ignore Spelling: proj

using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Outlining;

using VisualStudio.SpellChecker.Configuration;
using VisualStudio.SpellChecker.Editors;
using VisualStudio.SpellChecker.ProjectSpellCheck;
using VisualStudio.SpellChecker.Properties;
using VisualStudio.SpellChecker.ToolWindows;

namespace VisualStudio.SpellChecker
{
    /// <summary>
    /// This is the class that implements the Visual Studio Spell Checker package
    /// </summary>
    /// <remarks>The minimum requirement for a class to be considered a valid package for Visual Studio is to
    /// implement the <c>IVsPackage</c> interface and register itself with the shell.  This package uses the
    /// helper classes defined inside the Managed Package Framework (MPF) to do it.  It derives from the
    /// <c>Package</c> class that provides the implementation of the <c>IVsPackage</c> interface and uses the
    /// registration attributes defined in the framework to  register itself and its components with the shell.</remarks>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is a package
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    // This attribute is used to register the information needed to show this package in the Help/About dialog of
    // Visual Studio.
    [InstalledProductRegistration("#110", "#111", "VSSpellChecker", IconResourceID = 400)]
    // This defines the package GUID
    [Guid(GuidList.guidVSSpellCheckerPkgString)]
    // This attribute lets the shell know that this package exposes some menus
    [ProvideMenuResource("Menus.ctmenu", 1)]
    // These attributes lets the shell know that this package exposes tool windows
    [ProvideToolWindow(typeof(ToolWindows.InteractiveSpellCheckToolWindow),
      Orientation = ToolWindowOrientation.Right, Style = VsDockStyle.Float, MultiInstances = false,
      Transient = false, PositionX = 100, PositionY = 100, Width = 300, Height = 300)]
    [ProvideToolWindow(typeof(ToolWindows.SolutionProjectSpellCheckToolWindow),
      Orientation = ToolWindowOrientation.Right, Style = VsDockStyle.Float, MultiInstances = false,
      Transient = false, PositionX = 100, PositionY = 100, Width = 300, Height = 300)]
    // This attribute lets the shell know we provide a spelling configuration file editor
    [ProvideEditorFactory(typeof(SpellingConfigurationEditorFactory), 112,
      TrustLevel = __VSEDITORTRUSTLEVEL.ETL_AlwaysTrusted)]
    [ProvideEditorExtension(typeof(SpellingConfigurationEditorFactory), ".vsspell", 50)]
    // Provide a binding path for finding custom assemblies in this package
    [ProvideBindingPath()]
    public class VSSpellCheckerPackage : AsyncPackage
    {
        #region Private data members
        //=====================================================================

        private SolutionEvents solutionEvents;

        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        /// <remarks>Inside this method you can place any initialization code that does not require any Visual
        /// Studio service because at this point the package object is created but not sited yet inside Visual
        /// Studio environment. The place to do all the other initialization is the Initialize method.</remarks>
        public VSSpellCheckerPackage()
        {
            Trace.WriteLine($"Entering constructor for {this}");
        }
        #endregion

        #region General method overrides
        //=====================================================================

        /// <summary>
        /// Clean up any resources being used
        /// </summary>
        /// <param name="disposing">True if managed resources should be disposed; otherwise, false</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        /// <summary>
        /// This is overridden to initialize the package
        /// </summary>
        /// <remarks>This method is called right after the package is sited, so this is the place where you can
        /// put all the initialization code that relies on services provided by Visual Studio.</remarks>
        protected override async System.Threading.Tasks.Task InitializeAsync(
          System.Threading.CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            Trace.WriteLine($"Entering Initialize() of {this}");

            await base.InitializeAsync(cancellationToken, progress);

            // When initialized asynchronously, we *may* be on a background thread at this point.  Do any
            // initialization that requires the UI thread after switching to the UI thread.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // Register the spelling configuration file editor factory
            this.RegisterEditorFactory(new SpellingConfigurationEditorFactory());

            // Add our command handlers for menu items (commands must exist in the .vsct file)
            if((await this.GetServiceAsync(typeof(IMenuCommandService)) is OleMenuCommandService mcs))
            {
                CommandID commandId = new CommandID(GuidList.guidVSSpellCheckerCmdSet,
                    (int)PkgCmdIDList.SpellCheckerConfiguration);
                OleMenuCommand menuItem = new OleMenuCommand(SpellCheckerConfigurationExecuteHandler, commandId);
                mcs.AddCommand(menuItem);

                commandId = new CommandID(GuidList.guidVSSpellCheckerCmdSet, (int)PkgCmdIDList.SpellCheckInteractive);
                menuItem = new OleMenuCommand(SpellCheckInteractiveExecuteHandler, null,
                    SpellCheckActionsQueryStatusHandler, commandId);
                mcs.AddCommand(menuItem);

                commandId = new CommandID(GuidList.guidVSSpellCheckerCmdSet, (int)PkgCmdIDList.SpellCheckNextIssue);
                menuItem = new OleMenuCommand(SpellCheckNextPriorIssueExecuteHandler, null,
                    SpellCheckActionsQueryStatusHandler, commandId);
                mcs.AddCommand(menuItem);

                commandId = new CommandID(GuidList.guidVSSpellCheckerCmdSet, (int)PkgCmdIDList.SpellCheckPriorIssue);
                menuItem = new OleMenuCommand(SpellCheckNextPriorIssueExecuteHandler, null,
                    SpellCheckActionsQueryStatusHandler, commandId);
                mcs.AddCommand(menuItem);

                commandId = new CommandID(GuidList.guidVSSpellCheckerCmdSet, (int)PkgCmdIDList.EnableInCurrentSession);
                menuItem = new classOleMenuCommand(EnableInCurrentSessionExecuteHandler, null,
                    EnableInCurrentSessionQueryStatusHandler, commandId);
                mcs.AddCommand(menuItem);

                commandId = new CommandID(GuidList.guidVSSpellCheckerCmdSet,
                    (int)PkgCmdIDList.AddSpellCheckerConfigForItem);
                menuItem = new OleMenuCommand(AddSpellCheckerConfigExecuteHandler, null,
                    AddSpellCheckerConfigQueryStatusHandler, commandId);
                mcs.AddCommand(menuItem);

                commandId = new CommandID(GuidList.guidVSSpellCheckerCmdSet,
                    (int)PkgCmdIDList.AddSpellCheckerConfigForSelItem);
                menuItem = new OleMenuCommand(AddSpellCheckerConfigExecuteHandler, null,
                    AddSpellCheckerConfigQueryStatusHandler, commandId);
                mcs.AddCommand(menuItem);

                commandId = new CommandID(GuidList.guidVSSpellCheckerCmdSet,
                    (int)PkgCmdIDList.AddSpellCheckerConfigCtx);
                menuItem = new OleMenuCommand(AddSpellCheckerConfigExecuteHandler, null,
                    AddSpellCheckerConfigQueryStatusHandler, commandId);
                mcs.AddCommand(menuItem);

                commandId = new CommandID(GuidList.guidVSSpellCheckerCmdSet, (int)PkgCmdIDList.SpellCheckEntireSolution);
                menuItem = new OleMenuCommand(SpellCheckEntireSolutionExecuteHandler, commandId);
                mcs.AddCommand(menuItem);

                commandId = new CommandID(GuidList.guidVSSpellCheckerCmdSet, (int)PkgCmdIDList.SpellCheckCurrentProject);
                menuItem = new OleMenuCommand(SpellCheckCurrentProjectExecuteHandler, commandId);
                mcs.AddCommand(menuItem);

                commandId = new CommandID(GuidList.guidVSSpellCheckerCmdSet, (int)PkgCmdIDList.SpellCheckSelectedItems);
                menuItem = new OleMenuCommand(SpellCheckSelectedItemsExecuteHandler, commandId);
                mcs.AddCommand(menuItem);

                commandId = new CommandID(GuidList.guidVSSpellCheckerCmdSet, (int)PkgCmdIDList.SpellCheckOpenDocuments);
                menuItem = new OleMenuCommand(SpellCheckOpenDocumentsExecuteHandler, commandId);
                mcs.AddCommand(menuItem);

                commandId = new CommandID(GuidList.guidVSSpellCheckerCmdSet, (int)PkgCmdIDList.ViewSpellCheckToolWindow);
                menuItem = new OleMenuCommand(ViewSpellCheckToolWindowExecuteHandler, commandId);
                mcs.AddCommand(menuItem);
            }

            // Register for solution events so that we can clear the global dictionary cache when necessary
            if(await this.GetServiceAsync(typeof(DTE)) is DTE dte && dte.Events != null)
            {
                solutionEvents = dte.Events.SolutionEvents;

                if(solutionEvents != null)
                    solutionEvents.AfterClosing += solutionEvents_AfterClosing;
            }
        }
        #endregion

        #region Command event handlers
        //=====================================================================

        /// <summary>
        /// This is used to clear the global dictionary cache whenever a solution is closed
        /// </summary>
        /// <remarks>The spelling service factory also contains code to clear the dictionary cache when it
        /// detects a change in solution.  This package will not load unless a configuration is edited.  This is
        /// needed to consistently clear the cache if editing configurations in different solutions without
        /// opening any spell checked files.</remarks>
        private static void solutionEvents_AfterClosing()
        {
            WpfTextBox.WpfTextBoxSpellChecker.ClearCache();
            GlobalDictionary.ClearDictionaryCache();
        }

        /// <summary>
        /// This is used to edit the global configuration file
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void SpellCheckerConfigurationExecuteHandler(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string configFile = SpellingConfigurationFile.GlobalConfigurationFilename;

            // Convert the legacy configuration?
            if(Path.GetFileName(configFile).Equals("SpellChecker.config"))
            {
                string newConfigFile = Path.Combine(SpellingConfigurationFile.GlobalConfigurationFilePath,
                    "VSSpellChecker.vsspell");

                File.Copy(configFile, newConfigFile, true);
                File.Delete(configFile);

                configFile = newConfigFile;
            }

            // If it doesn't exist, create an empty file so that the editor can find it
            if(!File.Exists(configFile))
            {
                var file = new SpellingConfigurationFile(configFile, null);
                file.Save();
            }

            if(this.GetService(typeof(SDTE)) is DTE dte)
            {
                var doc = dte.ItemOperations.OpenFile(configFile, EnvDTE.Constants.vsViewKindPrimary);

                if(doc != null)
                    doc.Activate();
            }
        }

        /// <summary>
        /// Show the interactive spell checker tool window
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void SpellCheckInteractiveExecuteHandler(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var window = this.FindToolWindow(typeof(ToolWindows.InteractiveSpellCheckToolWindow), 0, true);

            if(window == null || window.Frame == null)
                throw new NotSupportedException("Unable to create Interactive Spell Checker tool window");

            var windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        /// <summary>
        /// Move to the next/prior spelling issue within the current document
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void SpellCheckNextPriorIssueExecuteHandler(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if(!(sender is OleMenuCommand command))
                return;

            if(!(this.GetService(typeof(SVsShellMonitorSelection)) is IVsMonitorSelection ms))
                return;

            int result = ms.GetCurrentElementValue((uint)VSConstants.VSSELELEMID.SEID_DocumentFrame, out object value);

            if(result == VSConstants.S_OK && value is IVsWindowFrame frame &&
              frame.GetProperty((int)__VSFPROPID.VSFPROPID_FrameMode, out value) == VSConstants.S_OK &&
              ((VSFRAMEMODE)value == VSFRAMEMODE.VSFM_MdiChild || (VSFRAMEMODE)value == VSFRAMEMODE.VSFM_Float))
            {
                var textView = VsShellUtilities.GetTextView(frame);

                if(textView != null)
                {
                    if(this.GetService(typeof(SComponentModel)) is IComponentModel componentModel)
                    {
                        var editorAdapterFactoryService = componentModel.GetService<IVsEditorAdaptersFactoryService>();

                        if(editorAdapterFactoryService != null)
                            try
                            {
                                var wpfTextView = editorAdapterFactoryService.GetWpfTextView(textView);

                                if(wpfTextView.Properties.TryGetProperty(typeof(SpellingTagger), out SpellingTagger tagger) &&
                                  tagger.CurrentMisspellings.Any())
                                {
                                    var caretPoint = wpfTextView.Caret.Position.BufferPosition;
                                    var allIssues = tagger.CurrentMisspellings.OrderBy(
                                        i => i.Span.GetSpan(i.Span.TextBuffer.CurrentSnapshot).Start.Position).ToList();
                                    var issue = allIssues.FirstOrDefault(i => i.Span.GetSpan(
                                        i.Span.TextBuffer.CurrentSnapshot).Contains(caretPoint));
                                    int offset = (command.CommandID.ID == PkgCmdIDList.SpellCheckNextIssue) ? 1 : -1;

                                    if(issue == null)
                                    {
                                        issue = allIssues.FirstOrDefault(i => i.Span.GetSpan(
                                            i.Span.TextBuffer.CurrentSnapshot).Start.Position > caretPoint.Position);

                                        if(issue == null)
                                        {
                                            issue = tagger.CurrentMisspellings.Last();
                                            offset = (command.CommandID.ID == PkgCmdIDList.SpellCheckNextIssue) ? 1 : 0;
                                        }
                                        else
                                            offset = (command.CommandID.ID == PkgCmdIDList.SpellCheckNextIssue) ? 0 : -1;
                                    }

                                    if(issue != null)
                                    {
                                        int idx = allIssues.IndexOf(issue) + offset;

                                        if(idx < 0)
                                            idx = allIssues.Count - 1;
                                        else
                                            if(idx >= allIssues.Count)
                                                idx = 0;

                                        issue = allIssues[idx];

                                        var span = issue.Span.GetSpan(issue.Span.TextBuffer.CurrentSnapshot);

                                        // If in a collapsed region, expand the region
                                        var outliningManagerService = componentModel.GetService<IOutliningManagerService>();

                                        if(outliningManagerService != null)
                                        {
                                            var outliningManager = outliningManagerService.GetOutliningManager(wpfTextView);

                                            if(outliningManager != null)
                                                foreach(var region in outliningManager.GetCollapsedRegions(span, false))
                                                    if(region.IsCollapsed)
                                                        outliningManager.Expand(region);
                                        }

                                        wpfTextView.Caret.MoveTo(span.Start);
                                        wpfTextView.ViewScroller.EnsureSpanVisible(span, EnsureSpanVisibleOptions.AlwaysCenter);
                                        wpfTextView.Selection.Select(span, false);
                                    }
                                }
                            }
                            catch(ArgumentException)
                            {
                                // Not an IWpfTextView so ignore it
                            }
                    }
                }
            }
        }

        private void SpellCheckActionsQueryStatusHandler(object sender, EventArgs e)
        {
            if(sender is OleMenuCommand command)
                command.Enabled = !SpellingTagger.DisabledInSession;
        }

        /// <summary>
        /// Enable or disable interactive spell checking in the current session
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void EnableInCurrentSessionExecuteHandler(object sender, EventArgs e)
        {
            SpellingTagger.DisabledInSession = !SpellingTagger.DisabledInSession;
        }

        /// <summary>
        /// Check the status of the interactive spell checking state for the session
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void EnableInCurrentSessionQueryStatusHandler(object sender, EventArgs e)
        {
            if(sender is OleMenuCommand command)
            {
                command.Text = SpellingTagger.DisabledInSession ? "Enable in Current Session" : "Disable in Current Session";
                command.Checked = SpellingTagger.DisabledInSession;
            }
        }

        /// <summary>
        /// Set the state of the Add Spell Checker Configuration file command
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void AddSpellCheckerConfigQueryStatusHandler(object sender, EventArgs e)
        {
            if(sender is OleMenuCommand command)
                try
                {
#pragma warning disable VSTHRD010
                    command.Visible = command.Enabled = DetermineContainingProjectAndSettingsFile(out Project p,
                      out string filename);
#pragma warning restore VSTHRD010
                }
                catch(Exception ex)
                {
                    // Ignore errors, just hide the command
                    System.Diagnostics.Debug.WriteLine(ex);
                    command.Visible = command.Enabled = false;
                }
        }

        /// <summary>
        /// Add a spell checker configuration file based on the currently selected solution/project item
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void AddSpellCheckerConfigExecuteHandler(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                if(DetermineContainingProjectAndSettingsFile(out Project containingProject, out string settingsFilename))
                {
                    if(this.GetService(typeof(SDTE)) is DTE2 dte2)
                    {
                        // Don't add it again if it's already there, just open it
                        var existingItem = dte2.Solution.FindProjectItemForFile(settingsFilename);

                        if(existingItem == null)
                        {
                            var newConfigFile = new SpellingConfigurationFile(settingsFilename, null);
                            newConfigFile.Save();

                            if(containingProject != null)
                            {
                                // If file settings, add them as a dependency if possible
                                if(!settingsFilename.StartsWith(containingProject.FullName, StringComparison.OrdinalIgnoreCase))
                                {
                                    existingItem = dte2.Solution.FindProjectItemForFile(
                                        Path.GetFileNameWithoutExtension(settingsFilename));

                                    if(existingItem != null && existingItem.ProjectItems != null)
                                        existingItem = existingItem.ProjectItems.AddFromFile(settingsFilename);
                                    else
                                        existingItem = containingProject.ProjectItems.AddFromFile(settingsFilename);
                                }
                                else
                                    existingItem = containingProject.ProjectItems.AddFromFile(settingsFilename);
                            }
                            else
                            {
#pragma warning disable VSTHRD010
                                // Add solution settings file.  Don't enumerate projects to find an existing copy
                                // of the folder.  It's not a project so it won't be found that way.  Just search
                                // the project collection.  It'll be at the root level if it exists.
                                var siProject = dte2.Solution.Projects.Cast<Project>().FirstOrDefault(
                                    p => p.Name == "Solution Items");
#pragma warning restore VSTHRD010

                                if(siProject == null)
                                    siProject = ((Solution2)dte2.Solution).AddSolutionFolder("Solution Items");

                                existingItem = siProject.ProjectItems.AddFromFile(settingsFilename);
                            }
                        }

                        if(existingItem != null)
                        {
                            try
                            {
                                var window = existingItem.Open();

                                if(window != null)
                                    window.Activate();
                            }
                            catch(Exception ex)
                            {
                                // Sometimes this isn't implemented for some reason.  The file does get added and
                                // can be opened manually.
                                if(ex.HResult != VSConstants.E_NOTIMPL)
                                    throw;
                            }
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                // Ignore on error but report it
                System.Diagnostics.Debug.WriteLine(ex);

                VsShellUtilities.ShowMessageBox(this, $"Unable to add spell checker configuration file: {ex.Message}",
                     Resources.PackageTitle, OLEMSGICON.OLEMSGICON_CRITICAL, OLEMSGBUTTON.OLEMSGBUTTON_OK,
                     OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }

        /// <summary>
        /// Show the solution/project spell checker tool window and spell check the entire solution
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void SpellCheckEntireSolutionExecuteHandler(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var window = this.FindToolWindow(typeof(ToolWindows.SolutionProjectSpellCheckToolWindow), 0, true);

            if(window == null || window.Frame == null)
                throw new NotSupportedException("Unable to create solution/project spell checker tool window");

            var windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());

            if(window.Content is SolutionProjectSpellCheckControl control)
                control.SpellCheck(SpellCheckTarget.EntireSolution);
        }

        /// <summary>
        /// Show the solution/project spell checker tool window and spell check the current project
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void SpellCheckCurrentProjectExecuteHandler(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var window = this.FindToolWindow(typeof(ToolWindows.SolutionProjectSpellCheckToolWindow), 0, true);

            if(window == null || window.Frame == null)
                throw new NotSupportedException("Unable to create solution/project spell checker tool window");

            var windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());

            if(window.Content is SolutionProjectSpellCheckControl control)
                control.SpellCheck(SpellCheckTarget.CurrentProject);
        }

        /// <summary>
        /// Show the solution/project spell checker tool window and spell check the selected items from the
        /// Solution Explorer window.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void SpellCheckSelectedItemsExecuteHandler(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var window = this.FindToolWindow(typeof(ToolWindows.SolutionProjectSpellCheckToolWindow), 0, true);

            if(window == null || window.Frame == null)
                throw new NotSupportedException("Unable to create solution/project spell checker tool window");

            var windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());

            if(window.Content is SolutionProjectSpellCheckControl control)
                control.SpellCheck(SpellCheckTarget.SelectedItems);
        }

        /// <summary>
        /// Show the solution/project spell checker tool window and spell check all open documents
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void SpellCheckOpenDocumentsExecuteHandler(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var window = this.FindToolWindow(typeof(ToolWindows.SolutionProjectSpellCheckToolWindow), 0, true);

            if(window == null || window.Frame == null)
                throw new NotSupportedException("Unable to create solution/project spell checker tool window");

            var windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());

            if(window.Content is SolutionProjectSpellCheckControl control)
                control.SpellCheck(SpellCheckTarget.AllOpenDocuments);
        }

        /// <summary>
        /// Show the solution/project spell checker tool window but don't execute any action
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void ViewSpellCheckToolWindowExecuteHandler(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var window = this.FindToolWindow(typeof(ToolWindows.SolutionProjectSpellCheckToolWindow), 0, true);

            if(window == null || window.Frame == null)
                throw new NotSupportedException("Unable to create solution/project spell checker tool window");

            var windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }
        #endregion

        #region Helper methods
        //=====================================================================

        /// <summary>
        /// This is used to determine the containing project and settings filename when adding a new spell
        /// checker configuration file.
        /// </summary>
        /// <param name="containingProject">On return, this contains the containing project or null if adding
        /// a solution configuration file.</param>
        /// <param name="settingsFilename">On return, this contains the name of the settings file to be added</param>
        /// <returns>True if a settings file can be added for the item selected in the Solution Explorer window
        /// or false if not.</returns>
        private static bool DetermineContainingProjectAndSettingsFile(out Project containingProject,
          out string settingsFilename)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string folderName = null;

            containingProject = null;
            settingsFilename = null;

            // Only add if a single file is selected
            if(!(Package.GetGlobalService(typeof(SDTE)) is DTE2 dte2) || dte2.SelectedItems.Count != 1)
                return false;

            SelectedItem item = dte2.SelectedItems.Item(1);

            if(item.Project != null && item.Project.Kind != EnvDTE.Constants.vsProjectKindSolutionItems &&
              item.Project.Kind != EnvDTE.Constants.vsProjectKindUnmodeled &&
              item.Project.Kind != EnvDTE.Constants.vsProjectKindMisc)
            {
                string path = null;

                // Looks like a project.  Not all of them implement properties though.
                if(!String.IsNullOrWhiteSpace(item.Project.FullName) && item.Project.FullName.EndsWith(
                  "proj", StringComparison.OrdinalIgnoreCase))
                {
                    path = item.Project.FullName;
                }

                if(path == null && item.Project.Properties != null)
                {
                    Property fullPath;

                    try
                    {
                        fullPath = item.Project.Properties.Item("FullPath");
                    }
                    catch
                    {
                        // C++ projects use a different property name and throw an exception above
                        try
                        {
                            fullPath = item.Project.Properties.Item("ProjectFile");
                        }
                        catch
                        {
                            // If that fails, give up
                            fullPath = null;
                        }
                    }

                    if(fullPath != null && fullPath.Value != null)
                        path = (string)fullPath.Value;
                }

                if(!String.IsNullOrWhiteSpace(path))
                {
#pragma warning disable VSTHRD010
                    var project = dte2.Solution.EnumerateProjects().FirstOrDefault(p => p.Name == item.Name);
#pragma warning restore VSTHRD010

                    if(project != null)
                    {
                        containingProject = project;
                        settingsFilename = project.FullName;

                        // Website projects are named after the folder rather than a file
                        if(settingsFilename.Length > 1 && settingsFilename[settingsFilename.Length - 1] == '\\')
                        {
                            folderName = settingsFilename;
                            settingsFilename += item.Name;
                        }
                    }
                }
            }
            else
                if(item.ProjectItem == null || item.ProjectItem.ContainingProject == null)
                {
                    // Looks like a solution
                    if(Path.GetFileNameWithoutExtension(dte2.Solution.FullName) == item.Name)
                        settingsFilename = dte2.Solution.FullName;
                }
                else
                    if(item.ProjectItem.Properties != null)
                    {
                        // Looks like a folder or file item
                        Property fullPath;

                        try
                        {
                            fullPath = item.ProjectItem.Properties.Item("FullPath");
                        }
                        catch
                        {
                            fullPath = null;
                        }

                        if(fullPath != null && fullPath.Value != null)
                        {
                            string path = (string)fullPath.Value;

                            if(!String.IsNullOrWhiteSpace(path))
                            {
                                containingProject = item.ProjectItem.ContainingProject;

                                // Folder items have a trailing backslash in some project systems, others don't.
                                // We'll put the configuration file in the folder using its name as the filename.
                                if(path[path.Length - 1] == '\\' || (!File.Exists(path) && Directory.Exists(path)))
                                {
                                    if(path[path.Length - 1] != '\\')
                                        path += @"\";

                                    folderName = path;
                                    settingsFilename = path + item.Name;
                                }
                                else
                                    settingsFilename = path;
                            }
                        }
                    }
                    else
                        if(item.ProjectItem.Kind == EnvDTE.Constants.vsProjectItemKindSolutionItems)
                        {
                            // Looks like a solution item
                            settingsFilename = item.ProjectItem.get_FileNames(1);
                        }
            
            if(settingsFilename != null)
            {
                if(settingsFilename.EndsWith(".vsspell", StringComparison.OrdinalIgnoreCase) ||
                 ((folderName == null && !File.Exists(settingsFilename)) ||
                 (folderName != null && !Directory.Exists(folderName))))
                {
                    settingsFilename = null;
                }
                else
                    if(folderName == null)
                    {
                        if(SpellCheckFileInfo.IsBinaryFile(settingsFilename))
                            settingsFilename = null;
                        else
                            settingsFilename += ".vsspell";
                    }
                    else
                        settingsFilename += ".vsspell";
            }

            return (settingsFilename != null);
        }
        #endregion
    }
}
