//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : VSSpellCheckerPackage.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 05/25/2013
// Note    : Copyright 2013, Eric Woodruff, All rights reserved
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
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the information needed to show the this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#111", "VSSpellChecker", IconResourceID = 400)]
    // Define the package GUID
    [Guid(GuidList.guidVSSpellCheckerPkgString)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    // Provide a binding path for finding custom assemblies in this package
    [ProvideBindingPath()]
    public class VSSpellCheckerPackage : VSSpellCheckerPackageBase
    {
        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        public VSSpellCheckerPackage()
        {
            Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}",
                this.ToString()));
        }
        #endregion

        #region Properties
        //=====================================================================

        /// <summary>
        /// This read-only property returns the package instance
        /// </summary>
        internal static VSSpellCheckerPackage Instance { get; private set; }

        #endregion

        #region General method overrides
        //=====================================================================

        /// <inheritdoc />
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
            Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}",
                this.ToString()));
            base.Initialize();

            VSSpellCheckerPackage.Instance = this;
        }
        #endregion

        #region Command method overrides
        //=====================================================================

        /// <summary>
        /// This is used to show the configuration dialog
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        protected override void SpellCheckerConfigurationExecuteHandler(object sender, EventArgs e)
        {
            var dlg = new SpellCheckerConfigDlg();
            dlg.ShowDialog();
        }

        /// <summary>
        /// Show the interactive spell checker tool window
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        protected override void SpellCheckInteractiveExecuteHandler(object sender, EventArgs e)
        {
            var window = this.FindToolWindow(typeof(ToolWindows.InteractiveSpellCheckToolWindow), 0, true);

            if(window == null || window.Frame == null)
                throw new NotSupportedException("Unable to create Entity References tool window");

            var windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }
        #endregion
    }
}
