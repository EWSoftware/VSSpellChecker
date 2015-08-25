//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : MisspellingType.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 08/25/2015
// Note    : Copyright 2015, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains an enumerated type that defines the different misspelling types
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
//===============================================================================================================
// 02/28/2015  EFW  Created the code
// 08/25/2015  EFW  Moved the file to the definitions project
//===============================================================================================================

namespace VisualStudio.SpellChecker.Definitions
{
    /// <summary>
    /// This enumerated type defines the misspelling types
    /// </summary>
    public enum MisspellingType
    {
        /// <summary>
        /// A misspelled word
        /// </summary>
        MisspelledWord,
        /// <summary>
        /// A doubled word
        /// </summary>
        DoubledWord,
        /// <summary>
        /// A deprecated term from a code analysis dictionary
        /// </summary>
        DeprecatedTerm,
        /// <summary>
        /// A compound term from a code analysis dictionary
        /// </summary>
        CompoundTerm,
        /// <summary>
        /// An unrecognized word from a code analysis dictionary
        /// </summary>
        UnrecognizedWord
    }
}
