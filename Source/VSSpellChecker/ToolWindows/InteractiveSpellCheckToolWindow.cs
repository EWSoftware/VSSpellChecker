//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : InteractiveSpellCheckToolWindow.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 09/04/2015
// Note    : Copyright 2013-2015, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains the class used to implement the interactive spell check tool window
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 05/25/2013  EFW  Created the code
//===============================================================================================================

using System;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Windows.Input;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;

namespace VisualStudio.SpellChecker.ToolWindows
{
    /// <summary>
    /// This class implements the tool window used to spell check the current document window.
    /// </summary>
    /// <remarks>In Visual Studio, tool windows are composed of a frame (implemented by the shell) and a pane, 
    /// usually implemented by the package implementer.  This class derives from the <c>ToolWindowPane</c> class
    /// provided from the MPF in order to use its implementation of the <c>IVsUIElementPane</c> interface.</remarks>
    [Guid("fd92f3d8-cebf-47b9-bb98-674a1618f364")]
    public sealed class InteractiveSpellCheckToolWindow : ToolWindowPane, IVsSelectionEvents, IVsRunningDocTableEvents
    {
        #region Private data members
        //=====================================================================

        private InteractiveSpellCheckControl ucSpellCheck;
        private uint selectionMonitorCookie, docTableCookie;
        private object scope;

        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        public InteractiveSpellCheckToolWindow() : base(null)
        {
            ucSpellCheck = new InteractiveSpellCheckControl();

            this.Caption = "Spell Check Active Document";
            this.Content = ucSpellCheck;
        }
        #endregion

        #region Method overrides
        //=====================================================================

        /// <summary>
        /// Start monitoring for selection change events when initialized
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            var ms = Utility.GetServiceFromPackage<IVsMonitorSelection, SVsShellMonitorSelection>(true);

            if(ms != null && ms.AdviseSelectionEvents(this, out selectionMonitorCookie) == VSConstants.S_OK)
            {
                // Get the current window frame and connect to it if it's a document editor
                object value;

                if(ms.GetCurrentElementValue((uint)Constants.SEID_WindowFrame, out value) == VSConstants.S_OK)
                    ((IVsSelectionEvents)this).OnElementValueChanged((uint)Constants.SEID_WindowFrame,
                        null, value);
            }

            var rdt = Utility.GetServiceFromPackage<IVsRunningDocumentTable, SVsRunningDocumentTable>(true);

            if(rdt != null)
                rdt.AdviseRunningDocTableEvents(this, out docTableCookie);
        }

        /// <summary>
        /// Stop monitoring for selection change events when disposed
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            var ms = Utility.GetServiceFromPackage<IVsMonitorSelection, SVsShellMonitorSelection>(true);

            if(ms != null)
                ms.UnadviseSelectionEvents(selectionMonitorCookie);

            var rdt = Utility.GetServiceFromPackage<IVsRunningDocumentTable, SVsRunningDocumentTable>(true);

            if(rdt != null)
                rdt.UnadviseRunningDocTableEvents(docTableCookie);

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

            if(m.Msg == 0x0104 /*WM_SYSKEYDOWN*/)
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

                    // See if the hot key is registered for the control.  If so, handle it.  Ignore Alt+F4 though
                    // as it should close Visual Studio (it comes through as a lowercase "s").
                    if(scope != null && key != "s" && AccessKeyManager.IsKeyRegistered(scope, key))
                    {
                        AccessKeyManager.ProcessKey(scope, key, false);
                        return true;
                    }
                }
            }

            return base.PreProcessMessage(ref m);
        }
        #endregion

        #region IVsSelectionEvents Members
        //=====================================================================

        /// <inheritdoc />
        /// <remarks>Not used by this tool window</remarks>
        int IVsSelectionEvents.OnCmdUIContextChanged(uint dwCmdUICookie, int fActive)
        {
            return VSConstants.S_OK;
        }

        /// <inheritdoc />
        /// <remarks>When an editor window gains the focus, set it as the active spell checking target</remarks>
        int IVsSelectionEvents.OnElementValueChanged(uint elementid, object varValueOld, object varValueNew)
        {
            IWpfTextView wpfTextView = null;
            object value;

            if((Constants)elementid == Constants.SEID_WindowFrame)
            {
                var frame = varValueNew as IVsWindowFrame;

                ucSpellCheck.ParentFocused = false;

                if(frame != null)
                    if(this.Frame == frame)
                        ucSpellCheck.ParentFocused = true;
                    else
                        if(frame.GetProperty((int)__VSFPROPID.VSFPROPID_FrameMode, out value) == VSConstants.S_OK)
                            if((VSFRAMEMODE)value == VSFRAMEMODE.VSFM_MdiChild ||
                              (VSFRAMEMODE)value == VSFRAMEMODE.VSFM_Float)
                            {
                                var textView = VsShellUtilities.GetTextView(frame);

                                if(textView != null)
                                {
                                    var componentModel = Utility.GetServiceFromPackage<IComponentModel, SComponentModel>(true);

                                    if(componentModel != null)
                                    {
                                        var editorAdapterFactoryService = componentModel.GetService<IVsEditorAdaptersFactoryService>();

                                        if(editorAdapterFactoryService != null)
                                            try
                                            {
                                                wpfTextView = editorAdapterFactoryService.GetWpfTextView(textView);
                                            }
                                            catch(ArgumentException)
                                            {
                                                // Not an IWpfGetView so ignore it
                                            }
                                    }

                                    ucSpellCheck.CurrentTextView = wpfTextView;
                                }
                            }
            }

            return VSConstants.S_OK;
        }

        /// <inheritdoc />
        /// <remarks>Not used by this tool window</remarks>
        int IVsSelectionEvents.OnSelectionChanged(IVsHierarchy pHierOld, uint itemidOld,
          IVsMultiItemSelect pMISOld, ISelectionContainer pSCOld, IVsHierarchy pHierNew, uint itemidNew,
          IVsMultiItemSelect pMISNew, ISelectionContainer pSCNew)
        {
            return VSConstants.S_OK;
        }
        #endregion

        #region IVsRunningDocTableEvents Members
        //=====================================================================

        /// <inheritdoc />
        /// <remarks>Not used by this tool window</remarks>
        public int OnAfterAttributeChange(uint docCookie, uint grfAttribs)
        {
            return VSConstants.S_OK;
        }

        /// <inheritdoc />
        /// <remarks>When an editor window closes, clear the current spell checking target</remarks>
        public int OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame)
        {
            if(VsShellUtilities.GetTextView(pFrame) != null)
                ucSpellCheck.CurrentTextView = null;

            return VSConstants.S_OK;
        }

        /// <inheritdoc />
        /// <remarks>Not used by this tool window</remarks>
        public int OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining,
          uint dwEditLocksRemaining)
        {
            return VSConstants.S_OK;
        }

        /// <inheritdoc />
        /// <remarks>Not used by this tool window</remarks>
        public int OnAfterSave(uint docCookie)
        {
            return VSConstants.S_OK;
        }

        /// <inheritdoc />
        /// <remarks>Not used by this tool window</remarks>
        public int OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame)
        {
            return VSConstants.S_OK;
        }

        /// <inheritdoc />
        /// <remarks>Not used by this tool window</remarks>
        public int OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining,
          uint dwEditLocksRemaining)
        {
            return VSConstants.S_OK;
        }
        #endregion
    }
}
