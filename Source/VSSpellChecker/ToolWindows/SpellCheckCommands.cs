//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : SpellCheckCommands.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 09/04/2015
// Note    : Copyright 2015, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
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

        private static RoutedUICommand replace;

        /// <summary>
        /// Replace a word
        /// </summary>
        /// <remarks>This command has no default key binding</remarks>
        public static RoutedUICommand Replace
        {
            get
            {
                if(replace == null)
                    replace = new RoutedUICommand("Replace", "Replace", typeof(SpellCheckCommands));

                return replace;
            }
        }
        #endregion

        #region Replace All command
        //=====================================================================

        private static RoutedUICommand replaceAll;

        /// <summary>
        /// Replace all occurrences of a word
        /// </summary>
        /// <remarks>This command has no default key binding</remarks>
        public static RoutedUICommand ReplaceAll
        {
            get
            {
                if(replaceAll == null)
                    replaceAll = new RoutedUICommand("Replace All", "ReplaceAll", typeof(SpellCheckCommands));

                return replaceAll;
            }
        }
        #endregion

        #region Ignore Once command
        //=====================================================================

        private static RoutedUICommand ignoreOnce;

        /// <summary>
        /// Ignore the occurrence of a word once
        /// </summary>
        /// <remarks>This command has no default key binding</remarks>
        public static RoutedUICommand IgnoreOnce
        {
            get
            {
                if(ignoreOnce == null)
                    ignoreOnce = new RoutedUICommand("Ignore Once", "IgnoreOnce", typeof(SpellCheckCommands));

                return ignoreOnce;
            }
        }
        #endregion

        #region Ignore All command
        //=====================================================================

        private static RoutedUICommand ignoreAll;

        /// <summary>
        /// Ignore all occurrences of a word
        /// </summary>
        /// <remarks>This command has no default key binding</remarks>
        public static RoutedUICommand IgnoreAll
        {
            get
            {
                if(ignoreAll == null)
                    ignoreAll = new RoutedUICommand("Ignore All", "IgnoreAll", typeof(SpellCheckCommands));

                return ignoreAll;
            }
        }
        #endregion

        #region Ignore File command
        //=====================================================================

        private static RoutedUICommand ignoreFile;

        /// <summary>
        /// Ignore all issues within a file
        /// </summary>
        /// <remarks>This command has no default key binding</remarks>
        public static RoutedUICommand IgnoreFile
        {
            get
            {
                if(ignoreFile == null)
                    ignoreFile = new RoutedUICommand("Ignore Issues in This File", "IgnoreFile",
                        typeof(SpellCheckCommands));

                return ignoreFile;
            }
        }
        #endregion

        #region Ignore Project command
        //=====================================================================

        private static RoutedUICommand ignoreProject;

        /// <summary>
        /// Ignore all issues within a project
        /// </summary>
        /// <remarks>This command has no default key binding</remarks>
        public static RoutedUICommand IgnoreProject
        {
            get
            {
                if(ignoreProject == null)
                    ignoreProject = new RoutedUICommand("Ignore Issues in This Project", "IgnoreProject",
                        typeof(SpellCheckCommands));

                return ignoreProject;
            }
        }
        #endregion

        #region Go To Issue command
        //=====================================================================

        private static RoutedUICommand goToIssue;

        /// <summary>
        /// Open the related file and go to the spelling issue
        /// </summary>
        /// <remarks>This command has no default key binding</remarks>
        public static RoutedUICommand GoToIssue
        {
            get
            {
                if(goToIssue == null)
                    goToIssue = new RoutedUICommand("Go To Issue", "GoToIssue", typeof(SpellCheckCommands));

                return goToIssue;
            }
        }
        #endregion

        #region Add to Dictionary command
        //=====================================================================

        private static RoutedUICommand addToDictionary;

        /// <summary>
        /// Add a word to the dictionary
        /// </summary>
        /// <remarks>This command has no default key binding.  If a parameter is specified, it should be a
        /// <see cref="CultureInfo"/> instance used to specify to which dictionary the word is added.  If null
        /// or not a culture instance, the word is added to the first available dictionary.</remarks>
        public static RoutedUICommand AddToDictionary
        {
            get
            {
                if(addToDictionary == null)
                    addToDictionary = new RoutedUICommand("Add to Dictionary", "AddToDictionary",
                        typeof(SpellCheckCommands));

                return addToDictionary;
            }
        }
        #endregion
    }
}
