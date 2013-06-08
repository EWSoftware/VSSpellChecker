using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using Microsoft.Win32;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;

#pragma warning disable 1591

using VisualStudio.SpellChecker.ToolWindows;

namespace VisualStudio.SpellChecker
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
	[ProvideToolWindow(typeof(InteractiveSpellCheckToolWindow), Orientation=ToolWindowOrientation.Right, Style=VsDockStyle.Float, MultiInstances = false, Transient = false, PositionX = 100 , PositionY = 100 , Width = 300 , Height = 300 )]
	[Guid(GuidList.guidVSSpellCheckerPkgString)]
    public abstract class VSSpellCheckerPackageBase : Package
    {
		/// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public VSSpellCheckerPackageBase()
        {
            Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
        }

        /////////////////////////////////////////////////////////////////////////////
        // Overriden Package Implementation
        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initilaization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Trace.WriteLine (string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            base.Initialize();

			// Add our command handlers for menu (commands must exist in the .vsct file)
            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if ( null != mcs )
            {
				CommandID commandId;
				OleMenuCommand menuItem;

				// Create the command for button SpellCheckerConfiguration
                commandId = new CommandID(GuidList.guidVSSpellCheckerCmdSet, (int)PkgCmdIDList.SpellCheckerConfiguration);
                menuItem = new OleMenuCommand(SpellCheckerConfigurationExecuteHandler, SpellCheckerConfigurationChangeHandler, SpellCheckerConfigurationQueryStatusHandler, commandId);
                mcs.AddCommand(menuItem);
				// Create the command for button SpellCheckInteractive
                commandId = new CommandID(GuidList.guidVSSpellCheckerCmdSet, (int)PkgCmdIDList.SpellCheckInteractive);
                menuItem = new OleMenuCommand(SpellCheckInteractiveExecuteHandler, SpellCheckInteractiveChangeHandler, SpellCheckInteractiveQueryStatusHandler, commandId);
                mcs.AddCommand(menuItem);

			}
		}
		
		#endregion

		#region Handlers for Button: SpellCheckerConfiguration

		protected virtual void SpellCheckerConfigurationExecuteHandler(object sender, EventArgs e)
		{
		}
		
		protected virtual void SpellCheckerConfigurationChangeHandler(object sender, EventArgs e)
		{
		}
		
		protected virtual void SpellCheckerConfigurationQueryStatusHandler(object sender, EventArgs e)
		{
		}

		#endregion

		#region Handlers for Button: SpellCheckInteractive

		protected virtual void SpellCheckInteractiveExecuteHandler(object sender, EventArgs e)
		{
		}
		
		protected virtual void SpellCheckInteractiveChangeHandler(object sender, EventArgs e)
		{
		}
		
		protected virtual void SpellCheckInteractiveQueryStatusHandler(object sender, EventArgs e)
		{
		}

		#endregion

        /// <summary>
        /// This function is called when the user clicks the menu item that shows the 
        /// tool window. See the Initialize method to see how the menu item is associated to 
        /// this function using the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void ShowToolWindowInteractiveSpellCheck(object sender, EventArgs e)
        {
            // Get the instance number 0 of this tool window. This window is single instance so this instance
            // is actually the only one.
            // The last flag is set to true so that if the tool window does not exists it will be created.
            ToolWindowPane window = this.FindToolWindow(typeof(InteractiveSpellCheckToolWindow), 0, true);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException(String.Format("Can not create Toolwindow: InteractiveSpellCheck"));
            }
            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        /// <summary>
        /// This function is the callback used to execute a command when the a menu item is clicked.
        /// See the Initialize method to see how the menu item is associated to this function using
        /// the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        protected void ShowMessage(string message)
        {
            // Show a Message Box to prove we were here
            IVsUIShell uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
            Guid clsid = Guid.Empty;
            int result;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(uiShell.ShowMessageBox(
                       0,
                       ref clsid,
                       "VSSpellChecker",
                       string.Format(CultureInfo.CurrentCulture, message, this.ToString()),
                       string.Empty,
                       0,
                       OLEMSGBUTTON.OLEMSGBUTTON_OK,
                       OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                       OLEMSGICON.OLEMSGICON_INFO,
                       0,        // false
                       out result));
        }
    }
}
