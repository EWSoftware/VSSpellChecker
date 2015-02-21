//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : SpellingDictionary.cs
// Authors : Noah Richards, Roman Golovin, Michael Lehenbauer, Eric Woodruff
// Updated : 02/05/2015
// Note    : Copyright 2010-2015, Microsoft Corporation, All rights reserved
//           Portions Copyright 2013-2015, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a class that implements the spelling dictionary service
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 04/14/2013  EFW  Imported the code into the project
// 04/15/2013  EFW  Added support for NHunspell and language specific dictionaries
// 05/31/2013  EFW  Added support for Ignore Once
//===============================================================================================================

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.Text;

using VisualStudio.SpellChecker.Configuration;

namespace VisualStudio.SpellChecker
{
    /// <summary>
    /// This class implements the spelling dictionary service.  The spelling dictionary utilizes NHunspell to
    /// perform the spell checking.
    /// </summary>
    internal sealed class SpellingDictionary
    {
        #region Private data members
        //=====================================================================

        private GlobalDictionary globalDictionary;
        private IEnumerable<string> ignoredWords;

        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="globalDictionary">The global dictionary</param>
        /// <param name="ignoredWords">An optional enumerable list of ignored words</param>
        public SpellingDictionary(GlobalDictionary globalDictionary, IEnumerable<string> ignoredWords)
        {
            this.globalDictionary = globalDictionary;
            this.ignoredWords = (ignoredWords ?? Enumerable.Empty<string>());

            // Register to receive events when the global dictionary is updated
            globalDictionary.RegisterSpellingDictionaryService(this);
        }
        #endregion

        #region Spelling dictionary members
        //=====================================================================

        /// <summary>
        /// This is used to spell check a word
        /// </summary>
        /// <param name="word">The word to spell check</param>
        /// <returns>True if spelled correctly, false if not</returns>
        public bool IsSpelledCorrectly(string word)
        {
            return globalDictionary.IsSpelledCorrectly(word);
        }

        /// <summary>
        /// This is used to suggest corrections for a misspelled word
        /// </summary>
        /// <param name="word">The misspelled word for which to get suggestions</param>
        /// <returns>An enumerable list of zero or more suggested correct spellings</returns>
        public IEnumerable<string> SuggestCorrections(string word)
        {
            return globalDictionary.SuggestCorrections(word);
        }

        /// <summary>
        /// Add the given word to the dictionary so that it will no longer show up as an incorrect spelling
        /// </summary>
        /// <param name="word">The word to add to the dictionary.</param>
        /// <returns><c>true</c> if the word was successfully added to the dictionary, even if it was already in
        /// the dictionary.</returns>
        public bool AddWordToDictionary(string word)
        {
            if(String.IsNullOrWhiteSpace(word))
                return false;

            return (this.ShouldIgnoreWord(word) || globalDictionary.AddWordToDictionary(word));
        }

        /// <summary>
        /// Raised when a request is made to ignore a word once
        /// </summary>
        /// <remarks>The event arguments contains the word that should be ignored once</remarks>
        public event EventHandler<SpellingEventArgs> IgnoreOnce;

        /// <summary>
        /// Ignore the given word once, but don't add it to the dictionary
        /// </summary>
        /// <param name="span">The tracking span used to locate the word to ignore once</param>
        public void IgnoreWordOnce(ITrackingSpan span)
        {
            var handler = IgnoreOnce;

            if(handler != null)
                handler(this, new SpellingEventArgs(span));
        }

        /// <summary>
        /// Ignore all occurrences of the given word, but don't add it to the dictionary
        /// </summary>
        /// <param name="word">The word to be ignored</param>
        /// <returns><c>true</c> if the word was successfully marked as ignored</returns>
        public bool IgnoreWord(string word)
        {
            if(String.IsNullOrWhiteSpace(word))
                return false;

            return (this.ShouldIgnoreWord(word) || globalDictionary.IgnoreWord(word));
        }

        /// <summary>
        /// Check the ignored words dictionary for the given word
        /// </summary>
        /// <param name="word">The word for which to check</param>
        /// <returns>True if the word should be ignored, false if not</returns>
        public bool ShouldIgnoreWord(string word)
        {
            if(String.IsNullOrWhiteSpace(word) || ignoredWords.Contains(word))
                return true;

            return globalDictionary.ShouldIgnoreWord(word);
        }

        /// <summary>
        /// Raised when the dictionary has been changed
        /// </summary>
        /// <remarks>When a new word is added to the dictionary, the event arguments contains the word that was
        /// added.</remarks>
        public event EventHandler<SpellingEventArgs> DictionaryUpdated;

        /// <summary>
        /// This is used to raise the <see cref="DictionaryUpdated"/> event
        /// </summary>
        /// <param name="word">The word that triggered the update</param>
        private void OnDictionaryUpdated(string word)
        {
            var handler = DictionaryUpdated;

            if(handler != null)
                handler(this, new SpellingEventArgs(word));
        }

        /// <summary>
        /// Raised when all occurrences of a word should be replaced
        /// </summary>
        /// <remarks>The event arguments contains the word that should be replaced along with its replacement</remarks>
        public event EventHandler<SpellingEventArgs> ReplaceAll;

        /// <summary>
        /// This is used to replace all occurrences of the specified word
        /// </summary>
        /// <param name="word">The word to be replaced</param>
        /// <param name="replacement">The word to use as the replacement</param>
        public void ReplaceAllOccurrences(string word, string replacement)
        {
            var handler = ReplaceAll;

            if(handler != null)
                handler(this, new SpellingEventArgs(word, replacement));
        }
        #endregion

        #region Methods and event handlers
        //=====================================================================

        /// <summary>
        /// This is called by the global dictionary to raise the <see cref="DictionaryUpdated"/> event when the
        /// global dictionary changes
        /// </summary>
        /// <param name="word">The word that triggered the update</param>
        internal void GlobalDictionaryUpdated(string word)
        {
            this.OnDictionaryUpdated(word);
        }
        #endregion
    }
}
