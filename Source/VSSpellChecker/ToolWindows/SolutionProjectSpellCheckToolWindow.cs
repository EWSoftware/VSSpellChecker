//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : SolutionProjectSpellCheckToolWindow.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 10/13/2015
// Note    : Copyright 2015, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
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

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

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

        private SolutionProjectSpellCheckControl ucSpellCheck;
        private SolutionEvents solutionEvents;
        private object scope;

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

            // Connect to solution events to find out when solutions are opened or closed, projects are
            // added/removed, etc.
            // Register for solution events so that we can clear the global dictionary cache when necessary
            var dte = Utility.GetServiceFromPackage<DTE, DTE>(false);

            if(dte != null && dte.Events != null)
            {
                solutionEvents = dte.Events.SolutionEvents;

                if(solutionEvents != null)
                {
                    solutionEvents.Opened += solutionEvents_OpenedClosed;
                    solutionEvents.BeforeClosing += solutionEvents_BeforeClosing;
                    solutionEvents.AfterClosing += solutionEvents_OpenedClosed;
                    solutionEvents.ProjectAdded += solutionEvents_ProjectAdded;
                    solutionEvents.ProjectRemoved += solutionEvents_ProjectRemoved;
                    solutionEvents.ProjectRenamed += solutionEvents_ProjectRenamed;

                    this.solutionEvents_OpenedClosed();
                }
            }
        }

        /// <summary>
        /// Disconnect from the solution events when disposed
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if(solutionEvents != null)
            {
                solutionEvents.Opened -= solutionEvents_OpenedClosed;
                solutionEvents.AfterClosing -= solutionEvents_OpenedClosed;
                solutionEvents.ProjectRemoved -= solutionEvents_ProjectRemoved;
                solutionEvents.ProjectRenamed -= solutionEvents_ProjectRenamed;

                solutionEvents = null;
            }

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
                    ApplicationCommands.Help.Execute(null, (UserControl)base.Content);
                    return true;
                }
            }

            if(m.Msg == 0x0104 /* WM_SYSKEYDOWN */)
            {
                if(Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt))
                {
                    // Cache a copy of the scope on first use
                    if(scope == null && base.Content != null)
                    {
                        // Get the scope for handling hot keys.  The key used here doesn't matter.  We're just
                        // getting the scope to use.
                        AccessKeyPressedEventArgs e = new AccessKeyPressedEventArgs("X");

                        ((UserControl)base.Content).RaiseEvent(e);
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
        /// If spell checking, stop the process before a solution is closed
        /// </summary>
        private void solutionEvents_BeforeClosing()
        {
            ucSpellCheck.CancelSpellCheck(false);
        }

        /// <summary>
        /// Update the list of solutions/projects when a solution is opened or closed
        /// </summary>
        private void solutionEvents_OpenedClosed()
        {
            var dte2 = Utility.GetServiceFromPackage<DTE2, SDTE>(false);

            if(dte2 != null)
            {
                var solution = dte2.Solution;

                if(solution != null && !String.IsNullOrWhiteSpace(solution.FullName))
                {
                    // Only add this event after the solution is opened.  For solutions with subprojects, the
                    // ProjectAdded event fires before this one which makes it look like we're ready to start
                    // before everything is fully loaded.
                    solutionEvents.ProjectAdded -= solutionEvents_ProjectAdded;

                    List<string> names = new List<string>();

                    foreach(Project p in solution.EnumerateProjects())
                        names.Add(p.FullName);

                    ucSpellCheck.UpdateProjects(names.OrderBy(n => Path.GetFileName(n)));

                    SpellingServiceFactory.LastSolutionName = solution.FullName;
                }
                else
                {
                    // Disconnect when closed (see above)
                    solutionEvents.ProjectAdded -= solutionEvents_ProjectAdded;

                    ucSpellCheck.UpdateProjects(null);
                    SpellingServiceFactory.LastSolutionName = null;
                }
            }
            else
                ucSpellCheck.UpdateProjects(null);
        }

        /// <summary>
        /// Update the list of solutions/projects when a project is added
        /// </summary>
        /// <param name="Project">The project that was added</param>
        private void solutionEvents_ProjectAdded(Project Project)
        {
            ucSpellCheck.AddProject(Project.FullName);
        }

        /// <summary>
        /// Update the list of solutions/projects when a project is removed
        /// </summary>
        /// <param name="Project">The project that was removed</param>
        private void solutionEvents_ProjectRemoved(Project Project)
        {
            ucSpellCheck.RemoveProject(Project.FullName);
        }

        /// <summary>
        /// Update the list of solutions/projects when a project is added or removed
        /// </summary>
        /// <param name="OldName">The old project name</param>
        /// <param name="Project">The project that was renamed</param>
        private void solutionEvents_ProjectRenamed(Project Project, string OldName)
        {
            ucSpellCheck.ProjectRenamed(OldName, Project.FullName);
        }
        #endregion
    }
}
