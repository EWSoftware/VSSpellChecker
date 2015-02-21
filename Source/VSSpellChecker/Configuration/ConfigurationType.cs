//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : ConfigurationType.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 02/08/2015
// Note    : Copyright 2015, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains an enumerated type that defines the different configuration file types
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
//===============================================================================================================
// 02/08/2015  EFW  Created the code
//===============================================================================================================

namespace VisualStudio.SpellChecker.Configuration
{
    /// <summary>
    /// This enumerated type defines the different configuration file types
    /// </summary>
    /// <remarks>Configuration files are associated with filenames and the type is determined by examining the
    /// filename.</remarks>
    public enum ConfigurationType
    {
        /// <summary>
        /// The global configuration file
        /// </summary>
        Global,
        /// <summary>
        /// A solution configuration file
        /// </summary>
        Solution,
        /// <summary>
        /// A project configuration file
        /// </summary>
        Project,
        /// <summary>
        /// A folder configuration file
        /// </summary>
        Folder,
        /// <summary>
        /// A file configuration file
        /// </summary>
        File
    }
}
