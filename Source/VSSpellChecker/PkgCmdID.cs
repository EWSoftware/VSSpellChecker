//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : PkgCmdID.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 01/27/2015
// Note    : Copyright 2013-2015, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
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
//===============================================================================================================

namespace VisualStudio.SpellChecker
{
    /// <summary>
    /// This class defines the command IDs for the package
    /// </summary>
    static class PkgCmdIDList
    {
        /// <summary>
        /// Edit the spell checker configuration
        /// </summary>
        public const uint SpellCheckerConfiguration = 0x0003;
        /// <summary>
        /// Open the interactive spell checker tool window
        /// </summary>
        public const uint SpellCheckInteractive = 0x0007;
    };
}
