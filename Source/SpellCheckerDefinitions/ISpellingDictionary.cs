//===============================================================================================================
// System  : Visual Studio Spell Checker Definitions
// File    : ISpellingDictionaryService.cs
// Authors : Noah Richards, Roman Golovin, Michael Lehenbauer
// Updated : 05/31/2013
// Note    : Copyright 2010-2013, Microsoft Corporation, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains an interface that is used to provide spelling dictionary services
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 04/14/2013  EFW  Imported the code into the project
// 04/15/2013  EFW  Added the IsSpelledCorrectly() and SuggestCorrections() methods
// 05/02/2013  EFW  Added support for Replace All
// 05/31/2013  EFW  Added support for Ignore Once
//===============================================================================================================

using System;
using System.Collections.Generic;

using Microsoft.VisualStudio.Text;

namespace VisualStudio.SpellChecker.Definitions
{
    /// <summary>
    /// This interface is used to provide spelling dictionary services
    /// </summary>
    public interface ISpellingDictionary
    {
        /// <summary>
        /// This is used to spell check a word
        /// </summary>
        /// <param name="word">The word to spell check</param>
        /// <returns>True if spelled correctly, false if not</returns>
        bool IsSpelledCorrectly(string word);

        /// <summary>
        /// This is used to suggest corrections for a misspelled word
        /// </summary>
        /// <param name="word">The misspelled word for which to get suggestions</param>
        /// <returns>An enumerable list of zero or more suggested correct spellings</returns>
        IEnumerable<string> SuggestCorrections(string word);

        /// <summary>
        /// Add the given word to the dictionary so that it will no longer show up as an incorrect spelling.
        /// </summary>
        /// <param name="word">The word to add to the dictionary.</param>
        /// <returns><c>true</c> if the word was successfully added to the dictionary, even if it was already in
        /// the dictionary.</returns>
        bool AddWordToDictionary(string word);

        /// <summary>
        /// Ignore the given word once, but don't add it to the dictionary.
        /// </summary>
        /// <param name="span">The tracking span used to locate the word to ignore once</param>
        void IgnoreWordOnce(ITrackingSpan span);

        /// <summary>
        /// Ignore all occurrences of the given word, but don't add it to the dictionary.
        /// </summary>
        /// <param name="word">The word to be ignored.</param>
        /// <returns><c>true</c> if the word was successfully marked as ignored.</returns>
        bool IgnoreWord(string word);

        /// <summary>
        /// Check the ignored words dictionary for the given word.
        /// </summary>
        /// <param name="word">The word for which to check</param>
        /// <returns>True if the word should be ignored, false if not</returns>
        bool ShouldIgnoreWord(string word);

        /// <summary>
        /// This is used to replace all occurrences of the specified word
        /// </summary>
        /// <param name="word">The word to be replaced</param>
        /// <param name="replacement">The word to use as the replacement</param>
        void ReplaceAllOccurrences(string word, string replacement);

        /// <summary>
        /// Raised when the dictionary has been changed.
        /// </summary>
        /// <remarks>When a new word is added to the dictionary, the event arguments contains the word that was
        /// added.</remarks>
        event EventHandler<SpellingEventArgs> DictionaryUpdated;

        /// <summary>
        /// Raised when a request is made to ignore a word once
        /// </summary>
        /// <remarks>The event arguments contains the word that should be ignored once.</remarks>
        event EventHandler<SpellingEventArgs> IgnoreOnce;

        /// <summary>
        /// Raised when all occurrences of a word should be replaced
        /// </summary>
        /// <remarks>The event arguments contains the word that should be replaced along with its replacement.</remarks>
        event EventHandler<SpellingEventArgs> ReplaceAll;
    }
}
