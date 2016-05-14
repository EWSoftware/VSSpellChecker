﻿//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : VSSpellCheckEverywherePackage.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 05/11/2016
// Note    : Copyright 2016, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
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

using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using VisualStudio.SpellChecker.Configuration;

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
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the information needed to show this package in the Help/About dialog of
    // Visual Studio.
    [InstalledProductRegistration("#113", "#114", "VSSpellCheckEverywhere", IconResourceID = 400)]
    // This defines the package GUID
    [Guid(GuidList.guidVSSpellCheckEverywherePkgString)]
    // Provide a binding path for finding custom assemblies in this package
    [ProvideBindingPath()]
    // This package loads at startup as it needs to integrate with any editor/tool window regardless of whether
    // or not a solution is open.
    [ProvideAutoLoad(UIContextGuids.NoSolution)]
    public class VSSpellCheckEverywherePackage : Package, IVsSelectionEvents
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
            Instance = null;

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

            Instance = this;

            try
            {
                var configuration = new SpellCheckerConfiguration();
                configuration.Load(SpellingConfigurationFile.GlobalConfigurationFilename);

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
            if(selectionMonitorCookie == 0)
                try
                {
                    var ms = Utility.GetServiceFromPackage<IVsMonitorSelection, SVsShellMonitorSelection>(false);

                    if(ms == null || ms.AdviseSelectionEvents(this, out selectionMonitorCookie) != VSConstants.S_OK)
                        selectionMonitorCookie = 0;

                    if(selectionMonitorCookie != 0)
                        WpfTextBox.WpfTextBoxSpellChecker.ConnectSpellChecker();
                }
                catch(Exception ex)
                {
                    // Ignore any exceptions
                    Debug.WriteLine(ex);
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
            Guid editorGuid, toolWindowType;

            if((Constants)elementid == Constants.SEID_WindowFrame)
            {
                var frame = varValueNew as IVsWindowFrame;

                if(frame != null)
                {
                    if(frame.GetGuidProperty((int)__VSFPROPID.VSFPROPID_guidEditorType, out editorGuid) != VSConstants.S_OK)
                        editorGuid = Guid.Empty;

                    if(editorGuid != Guid.Empty || frame.GetGuidProperty((int)__VSFPROPID.VSFPROPID_GuidPersistenceSlot,
                      out toolWindowType) != VSConstants.S_OK)
                        toolWindowType = Guid.Empty;

                    this.CurrentWindowId = ((editorGuid != Guid.Empty) ? editorGuid : toolWindowType).ToString();

                    Debug.WriteLine("******* " + this.CurrentWindowId + " *******");
                }
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
