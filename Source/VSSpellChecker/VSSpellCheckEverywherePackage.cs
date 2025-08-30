//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : VSSpellCheckEverywherePackage.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 04/27/2023
// Note    : Copyright 2016-2023, Eric Woodruff, All rights reserved
//
// This file contains the class that defines the Visual Studio Spell Check Everywhere package
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 04/27/2016  EFW  Created the code
//===============================================================================================================

// Ignore Spelling: elementid itemid Hier

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using VisualStudio.SpellChecker.Common;
using VisualStudio.SpellChecker.Common.Configuration;

namespace VisualStudio.SpellChecker
{
    /// <summary>
    /// This is the class that implements the Visual Studio Spell Check Everywhere package
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
    [InstalledProductRegistration("#113", "#114", "VSSpellCheckEverywhere", IconResourceID = 400)]
    // This defines the package GUID
    [Guid(GuidList.guidVSSpellCheckEverywherePkgString)]
    // Provide a binding path for finding custom assemblies in this package
    [ProvideBindingPath()]
    // This package loads at startup as it needs to integrate with any editor/tool window regardless of whether
    // or not a solution is open.
    [ProvideAutoLoad(UIContextGuids.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class VSSpellCheckEverywherePackage : AsyncPackage, IVsSelectionEvents
    {
        #region Private data members
        //=====================================================================

        private uint selectionMonitorCookie;

        #endregion

        #region Properties
        //=====================================================================

        /// <summary>
        /// This read-only property returns the package instance
        /// </summary>
        internal static VSSpellCheckEverywherePackage Instance { get; private set; }

        /// <summary>
        /// This is used to get the current editor or tool window's ID for use in uniquely naming text boxes
        /// </summary>
        internal string CurrentWindowId { get; private set; }

        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        /// <remarks>Inside this method you can place any initialization code that does not require any Visual
        /// Studio service because at this point the package object is created but not sited yet inside Visual
        /// Studio environment. The place to do all the other initialization is the Initialize method.</remarks>
        public VSSpellCheckEverywherePackage()
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
            Instance = null;

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

            await base.InitializeAsync(cancellationToken, progress).ConfigureAwait(true);

            // When initialized asynchronously, we *may* be on a background thread at this point.  Do any
            // initialization that requires the UI thread after switching to the UI thread.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // Replace the writable user words file check with one that checks for inclusion in the project and
            // source control.
            GlobalDictionary.CanWriteToUserWordsFile = Utility.CanWriteToUserWordsFile;

            Instance = this;

            try
            {
                // No filename here.  We're only loading properties for the global configuration.
                var configuration = SpellCheckerConfiguration.CreateSpellCheckerConfigurationFor(null, null, null);

                if(configuration.EnableWpfTextBoxSpellChecking)
                    this.ConnectSpellChecker();
            }
            catch(Exception ex)
            {
                // Ignore any exceptions
                Debug.WriteLine(ex);
            }
        }

        /// <summary>
        /// This is used to enable spell checking in any WPF text box within Visual Studio
        /// </summary>
        public void ConnectSpellChecker()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if(selectionMonitorCookie == 0)
            {
                try
                {
                    if(this.GetService(typeof(SVsShellMonitorSelection)) is not IVsMonitorSelection ms ||
                      ms.AdviseSelectionEvents(this, out selectionMonitorCookie) != VSConstants.S_OK)
                    {
                        selectionMonitorCookie = 0;
                    }

                    if(selectionMonitorCookie != 0)
                        WpfTextBox.WpfTextBoxSpellChecker.ConnectSpellChecker();
                }
                catch(Exception ex)
                {
                    // Ignore any exceptions
                    Debug.WriteLine(ex);
                }
            }
        }
        #endregion

        #region IVsSelectionEvents implementation
        //=====================================================================

        /// <inheritdoc />
        /// <remarks>Not used by this package</remarks>
        int IVsSelectionEvents.OnCmdUIContextChanged(uint dwCmdUICookie, int fActive)
        {
            return VSConstants.S_OK;
        }

        /// <inheritdoc />
        /// <remarks>Track the ID of the active editor or tool window for use in uniquely naming the text boxes</remarks>
        int IVsSelectionEvents.OnElementValueChanged(uint elementid, object varValueOld, object varValueNew)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if((Constants)elementid == Constants.SEID_WindowFrame && varValueNew is IVsWindowFrame frame)
            {
                if(frame.GetGuidProperty((int)__VSFPROPID.VSFPROPID_guidEditorType, out Guid editorGuid) != VSConstants.S_OK)
                    editorGuid = Guid.Empty;

                if(editorGuid != Guid.Empty || frame.GetGuidProperty((int)__VSFPROPID.VSFPROPID_GuidPersistenceSlot,
                    out Guid toolWindowType) != VSConstants.S_OK)
                    toolWindowType = Guid.Empty;

                this.CurrentWindowId = ((editorGuid != Guid.Empty) ? editorGuid : toolWindowType).ToString();

                Debug.WriteLine("******* " + this.CurrentWindowId + " *******");
            }

            return VSConstants.S_OK;
        }

        /// <inheritdoc />
        /// <remarks>Not used by this package</remarks>
        int IVsSelectionEvents.OnSelectionChanged(IVsHierarchy pHierOld, uint itemidOld,
          IVsMultiItemSelect pMISOld, ISelectionContainer pSCOld, IVsHierarchy pHierNew, uint itemidNew,
          IVsMultiItemSelect pMISNew, ISelectionContainer pSCNew)
        {
            return VSConstants.S_OK;
        }
        #endregion
    }
}
