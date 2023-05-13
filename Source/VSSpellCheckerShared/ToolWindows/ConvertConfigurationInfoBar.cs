//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : ConvertConfigurationInfoBar.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 05/09/2023
// Note    : Copyright 2023, Eric Woodruff, All rights reserved
//
// This file contains a class that implements the info bar used to offer converting the spell checker
// configuration files to .editorconfig settings.
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 03/15/2023  EFW  Created the code
//===============================================================================================================

using System;
using System.Windows;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using PackageResources = VisualStudio.SpellChecker.Properties.Resources;

namespace VisualStudio.SpellChecker.ToolWindows
{
    internal class ConvertConfigurationInfoBar : IVsInfoBarUIEvents
    {
        #region Actions for the info bar options
        //=====================================================================

        /// <summary>
        /// These represent the actions that can be invoked by the info bar
        /// </summary>
        private enum ConvertAction
        {
            /// <summary>
            /// Show the More Info help topic
            /// </summary>
            MoreInfo,
            /// <summary>
            /// Convert the old configuration to .editorconfig settings
            /// </summary>
            Convert
        }
        #endregion

        #region Private data members
        //=====================================================================

        private uint infoBarCookie;

        private static ConvertConfigurationInfoBar instance;

        #endregion

        #region Properties
        //=====================================================================

        /// <summary>
        /// This read-only property returns the info bar instance
        /// </summary>
        public static ConvertConfigurationInfoBar Instance
        {
            get
            {
                if(instance == null)
                    instance = new ConvertConfigurationInfoBar();

                return instance;
            }
        }
        #endregion

        #region Methods
        //=====================================================================

        /// <summary>
        /// Show the info bar to offer upgrading the configuration files to .editorconfig settings
        /// </summary>
        public void ShowInfoBar()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var shell = Utility.GetServiceFromPackage<IVsShell, SVsShell>(true);

            if(shell != null)
            {
                shell.GetProperty((int)__VSSPROPID7.VSSPROPID_MainWindowInfoBarHost, out var obj);

                if(obj is IVsInfoBarHost host)
                {
                    InfoBarModel infoBarModel = new InfoBarModel(
                        new[] { new InfoBarTextSpan("The spell checker extension's configuration is now stored " +
                            "as .editorconfig settings.  The old .vsspell configuration files, including the " +
                            "global configuration which will remain separate, must be converted.") },
                        new[]
                        {
                            new InfoBarHyperlink("More Info", ConvertAction.MoreInfo),
                            new InfoBarHyperlink("Convert", ConvertAction.Convert)
                        },
                        KnownMonikers.StatusInformation, true);

                    var factory = Utility.GetServiceFromPackage<IVsInfoBarUIFactory, SVsInfoBarUIFactory>(true);
                    var element = factory.CreateInfoBar(infoBarModel);

                    element.Advise(this, out infoBarCookie);
                    host.AddInfoBar(element);
                }
            }
        }

        /// <inheritdoc />
        void IVsInfoBarUIEvents.OnClosed(IVsInfoBarUIElement infoBarUIElement)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            infoBarUIElement.Unadvise(infoBarCookie);
        }

        /// <inheritdoc />
        void IVsInfoBarUIEvents.OnActionItemClicked(IVsInfoBarUIElement infoBarUIElement, IVsInfoBarActionItem actionItem)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            switch((ConvertAction)actionItem.ActionContext)
            {
                case ConvertAction.MoreInfo:
                    try
                    {
                        System.Diagnostics.Process.Start(
                            "https://ewsoftware.github.io/VSSpellChecker/html/d9dc230f-ae34-464b-a3c2-4a7778907fc9.htm");
                    }
                    catch(Exception ex)
                    {
                        MessageBox.Show("Unable to navigate to website.  Reason: " + ex.Message,
                            PackageResources.PackageTitle, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    }
                    break;

                case ConvertAction.Convert:
                    IVsUIShell vsUIShell = (IVsUIShell)Package.GetGlobalService(typeof(SVsUIShell));
                    Guid guid = typeof(ConvertConfigurationToolWindow).GUID;
                    int result = vsUIShell.FindToolWindow((uint)__VSFINDTOOLWIN.FTW_fFindFirst, ref guid,
                        out var windowFrame);

                    if(result != VSConstants.S_OK)
                    {
                        result = vsUIShell.FindToolWindow((uint)__VSFINDTOOLWIN.FTW_fForceCreate, ref guid,
                            out windowFrame);
                    }

                    if(result == VSConstants.S_OK)
                        ErrorHandler.ThrowOnFailure(windowFrame.Show());

                    infoBarUIElement.Close();
                    break;

                default:
                    break;
            }
        }
        #endregion
    }
}
