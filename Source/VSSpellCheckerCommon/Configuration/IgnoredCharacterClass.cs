//===============================================================================================================
// System  : Spell Check My Code Package
// File    : IgnoredCharacterClass.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 01/29/2023
// Note    : Copyright 2014-2023, Eric Woodruff, All rights reserved
//
// This file contains an enumerated type that defines a character class to ignore when determining if a word
// should be spell checked.
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
//===============================================================================================================
// 06/12/2014  EFW  Created the code
//===============================================================================================================

namespace VisualStudio.SpellChecker.Common.Configuration
{
    /// <summary>
    /// This enumerated type defines a character class to ignore when determining if a word should be spell
    /// checked.
    /// </summary>
    /// <remarks>This provides a simplistic way of ignoring some words in mixed language files.  It works best
    /// for spell checking English text in files that also contain Cyrillic or Asian text.</remarks>
    public enum IgnoredCharacterClass
    {
        /// <summary>Include all words</summary>
        None,
        /// <summary>Ignore words containing non-Latin characters</summary>
        NonLatin,
        /// <summary>Ignore words containing non-ASCII characters</summary>
        NonAscii
    }
}
