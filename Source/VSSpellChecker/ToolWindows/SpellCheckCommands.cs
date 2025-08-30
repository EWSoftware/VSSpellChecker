//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : SpellCheckCommands.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 08/30/2025
// Note    : Copyright 2015-2025, Eric Woodruff, All rights reserved
//
// This file contains a class for the spell checker's routed UI commands
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 08/23/2015  EFW  Created the code
//===============================================================================================================

using System.Windows.Input;

namespace VisualStudio.SpellChecker.ToolWindows
{
    /// <summary>
    /// This class contains the spell checker's routed UI commands
    /// </summary>
    public static class SpellCheckCommands
    {
        #region Replace command
        //=====================================================================

        /// <summary>
        /// Replace a word
        /// </summary>
        /// <remarks>This command has no default key binding</remarks>
        public static RoutedUICommand Replace { get; } = new RoutedUICommand("Replace", "Replace",
            typeof(SpellCheckCommands));

        #endregion

        #region Replace All command
        //=====================================================================

        /// <summary>
        /// Replace all occurrences of a word
        /// </summary>
        /// <remarks>This command has no default key binding</remarks>
        public static RoutedUICommand ReplaceAll { get; } = new RoutedUICommand("Replace All", "ReplaceAll",
            typeof(SpellCheckCommands));

        #endregion

        #region Ignore Once command
        //=====================================================================

        /// <summary>
        /// Ignore the occurrence of a word once
        /// </summary>
        /// <remarks>This command has no default key binding</remarks>
        public static RoutedUICommand IgnoreOnce { get; } = new RoutedUICommand("Ignore Once", "IgnoreOnce",
            typeof(SpellCheckCommands));

        #endregion

        #region Ignore All command
        //=====================================================================

        /// <summary>
        /// Ignore all occurrences of a word
        /// </summary>
        /// <remarks>This command has no default key binding</remarks>
        public static RoutedUICommand IgnoreAll { get; } = new RoutedUICommand("Ignore All", "IgnoreAll",
            typeof(SpellCheckCommands));

        #endregion

        #region Ignore File command
        //=====================================================================

        /// <summary>
        /// Ignore all issues within a file
        /// </summary>
        /// <remarks>This command has no default key binding</remarks>
        public static RoutedUICommand IgnoreFile { get; } = new RoutedUICommand("Ignore Issues in This File",
            "IgnoreFile", typeof(SpellCheckCommands));

        #endregion

        #region Ignore Project command
        //=====================================================================

        /// <summary>
        /// Ignore all issues within a project
        /// </summary>
        /// <remarks>This command has no default key binding</remarks>
        public static RoutedUICommand IgnoreProject { get; } = new RoutedUICommand("Ignore Issues in This Project",
            "IgnoreProject", typeof(SpellCheckCommands));

        #endregion

        #region Go To Issue command
        //=====================================================================

        /// <summary>
        /// Open the related file and go to the spelling issue
        /// </summary>
        /// <remarks>This command has no default key binding</remarks>
        public static RoutedUICommand GoToIssue { get; } = new RoutedUICommand("Go To Issue", "GoToIssue",
            typeof(SpellCheckCommands));

        #endregion

        #region Add to Dictionary command
        //=====================================================================

        /// <summary>
        /// Add a word to the dictionary
        /// </summary>
        /// <remarks>This command has no default key binding.  If a parameter is specified, it should be a
        /// <see cref="CultureInfo"/> instance used to specify to which dictionary the word is added.  If null
        /// or not a culture instance, the word is added to the first available dictionary.</remarks>
        public static RoutedUICommand AddToDictionary { get; } = new RoutedUICommand("Add to Dictionary",
            "AddToDictionary", typeof(SpellCheckCommands));

        #endregion

        #region Export All Issues command
        //=====================================================================

        /// <summary>
        /// Export all issues to a CSV file
        /// </summary>
        /// <remarks>This command has no default key binding</remarks>
        public static RoutedUICommand ExportAllIssues { get; } = new RoutedUICommand("Export All Issues",
            "ExportAllIssues", typeof(SpellCheckCommands));

        #endregion

        #region Export Project Issues command
        //=====================================================================

        /// <summary>
        /// Export project issues to a CSV file
        /// </summary>
        /// <remarks>This command has no default key binding</remarks>
        public static RoutedUICommand ExportProjectIssues { get; } = new RoutedUICommand("Export Project Issues",
            "ExportProjectIssues", typeof(SpellCheckCommands));

        #endregion

        #region Export File Issues command
        //=====================================================================

        /// <summary>
        /// Export file issues to a CSV file
        /// </summary>
        /// <remarks>This command has no default key binding</remarks>
        public static RoutedUICommand ExportFileIssues { get; } = new RoutedUICommand("Export File Issues",
            "ExportFileIssues", typeof(SpellCheckCommands));

        #endregion

        #region Copy as Ignore Spelling Directive command
        //=====================================================================

        /// <summary>
        /// Copy as Ignore Spelling directive
        /// </summary>
        /// <remarks>This command has no default key binding</remarks>
        public static RoutedUICommand CopyAsDirective { get; } = new RoutedUICommand("Copy as Ignore Spelling Directive",
            "CopyAsDirective", typeof(SpellCheckCommands));

        #endregion
    }
}
