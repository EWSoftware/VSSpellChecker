//===============================================================================================================
// System  : Spell Check My Code Package
// File    : SolutionProjectSpellCheckToolWindow.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 03/16/2023
// Note    : Copyright 2015-2021, Eric Woodruff, All rights reserved
//
// This file contains the class used to implement the solution/project spell check tool window
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 08/23/2015  EFW  Created the code
//===============================================================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Windows.Input;

using EnvDTE;
using EnvDTE80;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using ShellSolutionEvents = Microsoft.VisualStudio.Shell.Events.SolutionEvents;

namespace VisualStudio.SpellChecker.ToolWindows
{
    /// <summary>
    /// This class implements the tool window used to spell check solutions and projects
    /// </summary>
    /// <remarks>In Visual Studio, tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.  This class derives from the <c>ToolWindowPane</c> class
    /// provided from the MPF in order to use its implementation of the <c>IVsUIElementPane</c> interface.</remarks>
    [Guid("64DEBE95-07EA-48AC-8744-AF87605D624A")]
    public sealed class SolutionProjectSpellCheckToolWindow : ToolWindowPane
    {
        #region Private data members
        //=====================================================================

        private readonly SolutionProjectSpellCheckControl ucSpellCheck;
        private object scope;
        private bool solutionClosing, solutionOpen;

        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        public SolutionProjectSpellCheckToolWindow() : base(null)
        {
            ucSpellCheck = new SolutionProjectSpellCheckControl();

            this.Caption = "Spell Check Solution/Project";
            this.Content = ucSpellCheck;
        }
        #endregion

        #region Method overrides
        //=====================================================================

        /// <summary>
        /// Connect to solution events when initialized to track changes in the active solution
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            ThreadHelper.ThrowIfNotOnUIThread();

            // Connect to solution events to find out when solutions are opened or closed, projects are
            // added/removed, etc. so that we can clear the global dictionary cache when necessary
            ShellSolutionEvents.OnAfterOpenSolution += this.ShellSolutionEvents_OnAfterOpenCloseSolution;
            ShellSolutionEvents.OnBeforeCloseSolution += this.ShellSolutionEvents_OnBeforeCloseSolution;
            ShellSolutionEvents.OnAfterCloseSolution += this.ShellSolutionEvents_OnAfterCloseSolution;
            ShellSolutionEvents.OnAfterOpenProject += this.ShellSolutionEvents_OnAfterOpenProject;
            ShellSolutionEvents.OnBeforeCloseProject += this.ShellSolutionEvents_OnBeforeCloseProject;
            ShellSolutionEvents.OnAfterChangeProjectParent += this.ShellSolutionEvents_OnAfterChangeProject;
            ShellSolutionEvents.OnAfterRenameProject += this.ShellSolutionEvents_OnAfterChangeProject;
            

            this.ShellSolutionEvents_OnAfterOpenCloseSolution(this, null);
        }

        /// <summary>
        /// Disconnect from the solution events when disposed
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            ShellSolutionEvents.OnAfterOpenSolution -= this.ShellSolutionEvents_OnAfterOpenCloseSolution;
            ShellSolutionEvents.OnBeforeCloseSolution -= this.ShellSolutionEvents_OnBeforeCloseSolution;
            ShellSolutionEvents.OnAfterCloseSolution -= this.ShellSolutionEvents_OnAfterCloseSolution;
            ShellSolutionEvents.OnAfterOpenProject -= this.ShellSolutionEvents_OnAfterOpenProject;
            ShellSolutionEvents.OnBeforeCloseProject -= this.ShellSolutionEvents_OnBeforeCloseProject;
            ShellSolutionEvents.OnAfterChangeProjectParent -= this.ShellSolutionEvents_OnAfterChangeProject;
            ShellSolutionEvents.OnAfterRenameProject -= this.ShellSolutionEvents_OnAfterChangeProject;

            base.Dispose(disposing);
        }

        /// <summary>
        /// This is overridden to pass hot keys on to the contained user control.
        /// </summary>
        /// <param name="m">The message to pre-process</param>
        /// <returns>True if the message was handled, false if not</returns>
        /// <remarks>When a WPF user control is hosted in a docked tool window, the hot keys no longer work.
        /// This works around the problem by manually seeing if the control makes use of the hot key, and if
        /// it does, processing it here.</remarks>
        protected override bool PreProcessMessage(ref System.Windows.Forms.Message m)
        {
            if(m.Msg == 0x0100 /* WM_KEYDOWN */)
            {
                System.Windows.Forms.Keys keyCode = (System.Windows.Forms.Keys)m.WParam &
                    System.Windows.Forms.Keys.KeyCode;

                if(keyCode == System.Windows.Forms.Keys.F1)
                {
                    ApplicationCommands.Help.Execute(null, (UserControl)this.Content);
                    return true;
                }
            }

            if(m.Msg == 0x0104 /* WM_SYSKEYDOWN */)
            {
                if(Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt))
                {
                    // Cache a copy of the scope on first use
                    if(scope == null && this.Content != null)
                    {
                        // Get the scope for handling hot keys.  The key used here doesn't matter.  We're just
                        // getting the scope to use.
                        AccessKeyPressedEventArgs e = new("X");

                        ((UserControl)this.Content).RaiseEvent(e);
                        scope = e.Scope;
                    }

                    string key = ((char)m.WParam).ToString();

                    // See if the hot key is registered for the control.  If so, handle it.  Ignore anything
                    // that isn't 'A' to 'Z'
                    if(scope != null && key[0] >= 'A' && key[0] <= 'Z' && AccessKeyManager.IsKeyRegistered(scope, key))
                    {
                        AccessKeyManager.ProcessKey(scope, key, false);
                        return true;
                    }
                }
            }

            return base.PreProcessMessage(ref m);
        }
        #endregion

        #region Event handlers
        //=====================================================================

        /// <summary>
        /// Update the list of solutions/projects when a solution is opened or closed
        /// </summary>
        private void ShellSolutionEvents_OnAfterOpenCloseSolution(object sender, Microsoft.VisualStudio.Shell.Events.OpenSolutionEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var dte2 = Utility.GetServiceFromPackage<DTE2, SDTE>(false);

            if(dte2 != null)
            {
                var solution = dte2.Solution;

                if(solution != null && !String.IsNullOrWhiteSpace(solution.FullName))
                {
                    List<string> names = [];

                    foreach(Project p in solution.EnumerateProjects())
                        names.Add(p.FullName);

                    ucSpellCheck.UpdateProjects(names.OrderBy(n => Path.GetFileName(n)));

                    SpellingServiceProxy.LastSolutionName = solution.FullName;
                    SpellingServiceProxy.CheckForOldConfigurationFiles = true;
                    solutionOpen = true;
                }
                else
                {
                    ucSpellCheck.UpdateProjects(null);

                    SpellingServiceProxy.LastSolutionName = null;
                    SpellingServiceProxy.CheckForOldConfigurationFiles = true;
                }
            }
            else
                ucSpellCheck.UpdateProjects(null);
        }

        /// <summary>
        /// If spell checking, stop the process before a solution is closed
        /// </summary>
        private void ShellSolutionEvents_OnBeforeCloseSolution(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            ucSpellCheck.CancelSpellCheck(false);
            solutionClosing = true;
        }

        /// <summary>
        /// Clear the project list when the solution is closed
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void ShellSolutionEvents_OnAfterCloseSolution(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            this.ShellSolutionEvents_OnAfterOpenCloseSolution(sender, null);
            solutionOpen = solutionClosing = false;
        }

        /// <summary>
        /// Update the project list when one is opened
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void ShellSolutionEvents_OnAfterOpenProject(object sender, Microsoft.VisualStudio.Shell.Events.OpenProjectEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if(solutionOpen && e.Hierarchy.GetCanonicalName((uint)VSConstants.VSITEMID.Root, out string projectName) == VSConstants.S_OK)
                ucSpellCheck.AddProject(projectName);
        }

        /// <summary>
        /// Update the project list when one is closed
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void ShellSolutionEvents_OnBeforeCloseProject(object sender, Microsoft.VisualStudio.Shell.Events.CloseProjectEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            
            if(!solutionClosing && e.Hierarchy.GetCanonicalName((uint)VSConstants.VSITEMID.Root, out string projectName) == VSConstants.S_OK)
                ucSpellCheck.RemoveProject(projectName);
        }

        /// <summary>
        /// Update the project list when one is moved or renamed
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void ShellSolutionEvents_OnAfterChangeProject(object sender, Microsoft.VisualStudio.Shell.Events.HierarchyEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            this.ShellSolutionEvents_OnAfterOpenCloseSolution(sender, null);
        }
        #endregion
    }
}
