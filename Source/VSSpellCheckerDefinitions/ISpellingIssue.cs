//===============================================================================================================
// System  : Visual Studio Spell Checker Definitions
// File    : ISpellingIssue.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 08/25/2015
// Note    : Copyright 2015, Microsoft Corporation, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains the interface used to define a spelling issue
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 08/25/2015  EFW  Created the code
//===============================================================================================================

using System.Collections.Generic;

namespace VisualStudio.SpellChecker.Definitions
{
    /// <summary>
    /// This represents a spelling issue such as a misspelled word, doubled word, etc.
    /// </summary>
    public interface ISpellingIssue
    {
        /// <summary>
        /// This read-only property returns the misspelling type
        /// </summary>
        MisspellingType MisspellingType { get; }

        /// <summary>
        /// This read-only property returns the misspelled or doubled word
        /// </summary>
        string Word { get; }

        /// <summary>
        /// This read-only property is used to get suggestions that can be used to replace the misspelled word
        /// </summary>
        IEnumerable<ISpellingSuggestion> Suggestions { get; }
    }
}
