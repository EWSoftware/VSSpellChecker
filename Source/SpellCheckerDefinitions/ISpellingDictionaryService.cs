//===============================================================================================================
// System  : Visual Studio Spell Checker Definitions
// File    : ISpellingDictionaryService.cs
// Authors : Noah Richards, Roman Golovin, Michael Lehenbauer
// Updated : 04/14/2013
// Note    : Copyright 2010-2013, Microsoft Corporation, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains the interface used to retrieve the spelling dictionary for a given text buffer
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 04/14/2013  EFW  Imported the code into the project
//===============================================================================================================

using Microsoft.VisualStudio.Text;

namespace VisualStudio.SpellChecker.Definitions
{
    /// <summary>
    /// This is used to retrieve the spelling dictionary for a given text buffer.  The spelling dictionary
    /// service also aggregates <see cref="IBufferSpecificDictionary"/> objects.
    /// </summary>
    /// <exampe>
    /// This is a MEF component, and should be imported with the following code:
    /// <code>
    /// [Import]
    /// ISpellingDictionaryService spellingDictionaryService = null;
    /// </code>
    /// </example>
    public interface ISpellingDictionaryService
    {
        /// <summary>
        /// Get the dictionary for the specified buffer
        /// </summary>
        /// <param name="buffer">The buffer for which to get a dictionary</param>
        /// <returns>The spelling dictionary for the buffer or null if one is not provided</returns>
        ISpellingDictionary GetDictionary(ITextBuffer buffer);
    }
}
