//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : MenuCommand.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 09/01/2018
// Note    : Copyright 2016-2018, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a class that is used to define a simple menu command
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 04/22/2013  EFW  Created the code
//===============================================================================================================

using System;
using System.Windows.Input;

namespace VisualStudio.SpellChecker.WpfTextBox
{
    /// <summary>
    /// This is used to define a simple menu command
    /// </summary>
    public class MenuCommand : ICommand
    {
        #region Private data members
        //=====================================================================

        private readonly Action<object> action;

        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="action">The action to execute</param>
        public MenuCommand(Action<object> action)
        {
            this.action = action;
        }
        #endregion

        #region ICommand implementation
        //=====================================================================

#pragma warning disable 0067

        /// <inheritdoc />
        /// <remarks>This is not used by this implementation</remarks>
        public event EventHandler CanExecuteChanged;

#pragma warning restore 0067

        /// <inheritdoc />
        public bool CanExecute(object parameter)
        {
            return (action != null);
        }

        /// <inheritdoc />
        public void Execute(object parameter)
        {
            action?.Invoke(parameter);
        }
        #endregion
    }
}
