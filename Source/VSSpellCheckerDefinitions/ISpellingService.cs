//===============================================================================================================
// System  : Visual Studio Spell Checker Definitions
// File    : ISpellingService.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 04/07/2015
// Note    : Copyright 2015, Microsoft Corporation, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains the interface used to expose the spell checker to third-party tagger providers
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 02/19/2015  EFW  Created the code
//===============================================================================================================

using Microsoft.VisualStudio.Text;

namespace VisualStudio.SpellChecker.Definitions
{
    /// <summary>
    /// This is used to retrieve the spelling service
    /// </summary>
    /// <preliminary>This interface is under development and may be expanded in the future to provide more access
    /// to necessary spell checker features.</preliminary>
    /// <example>
    /// This is a MEF component, and should be imported with the following code:
    /// <code language="cs">
    /// [Import]
    /// private ISpellingService spellingService = null;
    /// </code>
    /// </example>
    public interface ISpellingService
    {
        /// <summary>
        /// This is used to see if spell checking is enabled for the given text buffer
        /// </summary>
        /// <param name="buffer">The text buffer to check</param>
        /// <returns>True if enabled, false if not</returns>
        bool IsEnabled(ITextBuffer buffer);
    }
}
