//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : ConvertConfigurationToolWindow.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 03/19/2023
// Note    : Copyright 2023, Eric Woodruff, All rights reserved
//
// This file contains the class used to implement the convert configuration tool window
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 03/16/2023  EFW  Created the code
//===============================================================================================================

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Windows.Input;

using EnvDTE80;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Events;
using Microsoft.VisualStudio.Shell.Interop;

using ShellSolutionEvents = Microsoft.VisualStudio.Shell.Events.SolutionEvents;

namespace VisualStudio.SpellChecker.ToolWindows
{
    /// <summary>
    /// This class implements the tool window used to implement the convert configuration tool window
    /// </summary>
    /// <remarks>In Visual Studio, tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.  This class derives from the <c>ToolWindowPane</c> class
    /// provided from the MPF in order to use its implementation of the <c>IVsUIElementPane</c> interface.</remarks>
    [Guid("BDC1DA36-B8D3-43B2-B645-373AC1931960")]
    public sealed class ConvertConfigurationToolWindow : ToolWindowPane, IVsWindowFrameNotify3
    {
        #region Private data members
        //=====================================================================

        private readonly ConvertConfigurationControl ucConvertConfig;
        private object scope;

        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        public ConvertConfigurationToolWindow() : base(null)
        {
            ucConvertConfig = new ConvertConfigurationControl();

            this.Caption = "Convert Spelling Configurations";
            this.Content = ucConvertConfig;
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
            ShellSolutionEvents.OnAfterCloseSolution += this.ShellSolutionEvents_OnAfterCloseSolution;

            this.ShellSolutionEvents_OnAfterOpenCloseSolution(this, null);
        }

        /// <summary>
        /// Disconnect from the solution events when disposed
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            ShellSolutionEvents.OnAfterOpenSolution -= this.ShellSolutionEvents_OnAfterOpenCloseSolution;
            ShellSolutionEvents.OnAfterCloseSolution -= this.ShellSolutionEvents_OnAfterCloseSolution;

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
                        AccessKeyPressedEventArgs e = new AccessKeyPressedEventArgs("X");

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
        private void ShellSolutionEvents_OnAfterOpenCloseSolution(object sender, OpenSolutionEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var dte2 = Utility.GetServiceFromPackage<DTE2, SDTE>(false);

            if(dte2 != null)
            {
                var solution = dte2.Solution;

                if(solution != null && !String.IsNullOrWhiteSpace(solution.FullName))
                    ucConvertConfig.UpdateState(Path.GetDirectoryName(solution.FullName));
                else
                    ucConvertConfig.UpdateState(null);
            }
            else
                ucConvertConfig.UpdateState(null);
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
        }
        #endregion

        #region IVsWindowFrameNotify3 implementation
        //=====================================================================

        /// <inheritdoc />
        public int OnShow(int fShow)
        {
            return VSConstants.S_OK;
        }

        /// <inheritdoc />
        public int OnMove(int x, int y, int w, int h)
        {
            return VSConstants.S_OK;
        }

        /// <inheritdoc />
        public int OnSize(int x, int y, int w, int h)
        {
            return VSConstants.S_OK;
        }

        /// <inheritdoc />
        public int OnDockableChange(int fDockable, int x, int y, int w, int h)
        {
            return VSConstants.S_OK;
        }

        /// <inheritdoc />
        public int OnClose(ref uint pgrfSaveOptions)
        {
            // Reset the configuration check state when the tool window is closed so that if it is closed without
            // saving the conversions, it will prompt again.
            SpellingServiceProxy.CheckForOldConfigurationFiles = true;

            return VSConstants.S_OK;
        }
        #endregion
    }
}
