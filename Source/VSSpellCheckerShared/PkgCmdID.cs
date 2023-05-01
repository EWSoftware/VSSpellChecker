//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : PkgCmdID.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 04/27/2023
// Note    : Copyright 2013-2023, Eric Woodruff, All rights reserved
//
// This file contains various command IDs for the package
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
//===============================================================================================================
// 05/20/2013  EFW  Created the code
// 08/23/2015  EFW  Added support for solution/project spell checking
//===============================================================================================================

// Ignore Spelling: sel ctx

namespace VisualStudio.SpellChecker
{
    /// <summary>
    /// This class defines the command IDs for the package
    /// </summary>
    internal static class PkgCmdIDList
    {
        /// <summary>
        /// Edit the spell checker configuration
        /// </summary>
        public const uint SpellCheckerConfiguration = 0x0003;
        /// <summary>
        /// Open the interactive spell checker tool window
        /// </summary>
        public const uint SpellCheckInteractive = 0x0007;
        /// <summary>
        /// Move to the next spelling issue in the current document
        /// </summary>
        public const uint SpellCheckNextIssue = 0x0015;
        /// <summary>
        /// Move to the prior spelling issue in the current document
        /// </summary>
        public const uint SpellCheckPriorIssue = 0x0016;
        /// <summary>
        /// Enable or disable interactive spell checking in the current session
        /// </summary>
        public const uint EnableInCurrentSession = 0x0018;
        /// <summary>
        /// Add a spell checker configuration file to the solution/project based for the file item selected in
        /// the Solution Explorer.
        /// </summary>
        /// <remarks>Performs the same action but has different wording on the menu item</remarks>
        public const uint AddSpellCheckerConfigForItem = 0x0009;
        /// <summary>
        /// Add a spell checker configuration file to the solution/project based on the selected item (File |
        /// New menu)
        /// </summary>
        /// <remarks>Performs the same action but has different wording on the menu item</remarks>
        public const uint AddSpellCheckerConfigForSelItem = 0x000A;
        /// <summary>
        /// Add a spell checker configuration file to the solution/project based on the selected item (various
        /// Solution Explorer Add context menus).
        /// </summary>
        /// <remarks>Performs the same action but has different wording on the menu item</remarks>
        public const uint AddSpellCheckerConfigCtx = 0x000B;
        /// <summary>
        /// Spell check the entire solution
        /// </summary>
        public const uint SpellCheckEntireSolution = 0x0010;
        /// <summary>
        /// Spell check the current project
        /// </summary>
        public const uint SpellCheckCurrentProject = 0x0011;
        /// <summary>
        /// Spell check only the selected items in the Solution Explorer
        /// </summary>
        public const uint SpellCheckSelectedItems = 0x0012;
        /// <summary>
        /// Spell check all open documents
        /// </summary>
        public const uint SpellCheckOpenDocuments = 0x0017;
        /// <summary>
        /// Open the solution/project spell checking tool window
        /// </summary>
        public const uint ViewSpellCheckToolWindow = 0x0013;
    };
}
