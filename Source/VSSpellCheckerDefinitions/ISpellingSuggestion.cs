//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : ISpellingSuggestion.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 08/25/2015
// Note    : Copyright 2015, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains the interface that represents a spelling suggestion that can be used to replace a
// misspelled word.
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

using System.Globalization;

namespace VisualStudio.SpellChecker.Definitions
{
    /// <summary>
    /// This represents a spelling suggestion that can be used to replace a misspelled word
    /// </summary>
    public interface ISpellingSuggestion
    {
        #region Properties
        //=====================================================================

        /// <summary>
        /// This read-only property returns the culture information for the suggestion
        /// </summary>
        CultureInfo Culture { get; }

        /// <summary>
        /// This read-only property returns the suggested replacement word
        /// </summary>
        string Suggestion { get; }

        #endregion
    }
}
