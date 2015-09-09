//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : SpellCheckTarget.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 08/26/2015
// Note    : Copyright 2015, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains an enumerated type that defines the spell check targets for solution/project spell
// checking.
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
//===============================================================================================================
// 08/25/2015  EFW  Created the code
//===============================================================================================================

namespace VisualStudio.SpellChecker.ToolWindows
{
    /// <summary>
    /// This defines the spell check targets for solution/project spell checking
    /// </summary>
    public enum SpellCheckTarget
    {
        /// <summary>
        /// No spell check target
        /// </summary>
        None,
        /// <summary>
        /// The entire solution
        /// </summary>
        EntireSolution,
        /// <summary>
        /// The current project
        /// </summary>
        CurrentProject,
        /// <summary>
        /// The selected items in the Solution Explorer window
        /// </summary>
        /// <remarks>The selected items may be projects, folders, or files with dependencies</remarks>
        SelectedItems
    }
}
