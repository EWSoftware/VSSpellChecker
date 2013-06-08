using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using System.ComponentModel.Design;

namespace VisualStudio.SpellChecker.ToolWindows
{
	/// <summary>
    /// This class implements the tool window InteractiveSpellCheckToolWindowBase exposed by this package and hosts a user control.
    ///
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane, 
    /// usually implemented by the package implementer.
    ///
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its 
    /// implementation of the IVsUIElementPane interface.
    /// </summary>
    [Guid("fd92f3d8-cebf-47b9-bb98-674a1618f364")]
    public class InteractiveSpellCheckToolWindowBase : ToolWindowPane
    {
        /// <summary>
        /// Standard constructor for the tool window.
        /// </summary>
        public InteractiveSpellCheckToolWindowBase()
            : base(null)
        {
			this.Caption = "Spell Check Active Document";
        }
    }
}
