//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : VSSpellCheckerPackage.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 01/27/2015
// Note    : Copyright 2013-2015, Eric Woodruff, All rights reserved
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
//===============================================================================================================

using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using VisualStudio.SpellChecker.UI;

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
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the information needed to show this package in the Help/About dialog of
    // Visual Studio.
    [InstalledProductRegistration("#110", "#111", "VSSpellChecker", IconResourceID = 400)]
    // This defines the package GUID
    [Guid(GuidList.guidVSSpellCheckerPkgString)]
    // This attribute lets the shell know that this package exposes some menus
    [ProvideMenuResource("Menus.ctmenu", 1)]
    // This attribute lets the shell know that this package exposes a tool window
    [ProvideToolWindow(typeof(ToolWindows.InteractiveSpellCheckToolWindow),
      Orientation = ToolWindowOrientation.Right, Style = VsDockStyle.Float, MultiInstances = false,
      Transient = false, PositionX = 100, PositionY = 100, Width = 300, Height = 300)]
    // Provide a binding path for finding custom assemblies in this package
    [ProvideBindingPath()]
    public class VSSpellCheckerPackage : Package
    {
        #region Properties
        //=====================================================================

        /// <summary>
        /// This read-only property returns the package instance
        /// </summary>
        internal static VSSpellCheckerPackage Instance { get; private set; }

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
            Trace.WriteLine(String.Format(CultureInfo.CurrentCulture, "Entering constructor for {0}",
                this.ToString()));
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
            VSSpellCheckerPackage.Instance = null;

            base.Dispose(disposing);
        }

        /// <summary>
        /// This is overridden to initialize the package
        /// </summary>
        /// <remarks>This method is called right after the package is sited, so this is the place where you can
        /// put all the initialization code that relies on services provided by Visual Studio.</remarks>
        protected override void Initialize()
        {
            Trace.WriteLine(String.Format(CultureInfo.CurrentCulture, "Entering Initialize() of {0}",
                this.ToString()));
            base.Initialize();

            VSSpellCheckerPackage.Instance = this;

            // Add our command handlers for menu items (commands must exist in the .vsct file)
            OleMenuCommandService mcs = this.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;

            if(mcs != null)
            {
                // Create the command for button SpellCheckerConfiguration
                CommandID commandId = new CommandID(GuidList.guidVSSpellCheckerCmdSet,
                    (int)PkgCmdIDList.SpellCheckerConfiguration);
                OleMenuCommand menuItem = new OleMenuCommand(SpellCheckerConfigurationExecuteHandler, commandId);
                mcs.AddCommand(menuItem);

                // Create the command for button SpellCheckInteractive
                commandId = new CommandID(GuidList.guidVSSpellCheckerCmdSet, (int)PkgCmdIDList.SpellCheckInteractive);
                menuItem = new OleMenuCommand(SpellCheckInteractiveExecuteHandler, commandId);
                mcs.AddCommand(menuItem);
            }
        }
        #endregion

        #region Command event handlers
        //=====================================================================

        /// <summary>
        /// This is used to show the configuration dialog
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void SpellCheckerConfigurationExecuteHandler(object sender, EventArgs e)
        {
            var dlg = new SpellCheckerConfigDlg();
            dlg.ShowDialog();
        }

        /// <summary>
        /// Show the interactive spell checker tool window
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void SpellCheckInteractiveExecuteHandler(object sender, EventArgs e)
        {
            var window = this.FindToolWindow(typeof(ToolWindows.InteractiveSpellCheckToolWindow), 0, true);

            if(window == null || window.Frame == null)
                throw new NotSupportedException("Unable to create Interactive Spell Checker tool window");

            var windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }
        #endregion
    }
}
