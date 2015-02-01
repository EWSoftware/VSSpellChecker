//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : SpellingDictionaryService.cs
// Authors : Noah Richards, Roman Golovin, Michael Lehenbauer, Eric Woodruff
// Updated : 01/31/2015
// Note    : Copyright 2010-2015, Microsoft Corporation, All rights reserved
//           Portions Copyright 2013-2014, Eric Woodruff, All rights reserved
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

using VisualStudio.SpellChecker.Definitions;

namespace VisualStudio.SpellChecker
{
    /// <summary>
    /// This class implements the spelling dictionary service.  The spelling dictionary utilizes NHunspell to
    /// perform the spell checking.
    /// </summary>
    internal sealed class SpellingDictionaryService : ISpellingDictionary
    {
        #region Private data members
        //=====================================================================

        private IList<ISpellingDictionary> bufferSpecificDictionaries;
        private GlobalDictionary globalDictionary;
        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="bufferSpecificDictionaries">A list of buffer-specific dictionaries</param>
        /// <param name="globalDictionary">The global dictionary</param>
        public SpellingDictionaryService(IList<ISpellingDictionary> bufferSpecificDictionaries,
          GlobalDictionary globalDictionary)
        {
            this.globalDictionary = globalDictionary;
            this.bufferSpecificDictionaries = bufferSpecificDictionaries;

            // TODO: This never gets disconnected and would probably keep this instance alive, right?  Probably
            // should switch to something like RegisterSpellingDictionaryService used by global dictionary.
            // Perhaps make that method part of the ISpellingDictionary interface?  Need to test it once a
            // buffer-specific class is actually implemented.
            foreach(var dictionary in bufferSpecificDictionaries)
                dictionary.DictionaryUpdated += this.BufferSpecificDictionaryUpdated;

            // Register to receive events when the global dictionary is updated
            globalDictionary.RegisterSpellingDictionaryService(this);
        }
        #endregion

        #region ISpellingDictionary Members
        //=====================================================================

        /// <inheritdoc />
        public bool IsSpelledCorrectly(string word)
        {
            foreach(var dictionary in bufferSpecificDictionaries)
            {
                if(dictionary.IsSpelledCorrectly(word))
                    return true;
            }

            return globalDictionary.IsSpelledCorrectly(word);
        }

        /// <inheritdoc />
        public IEnumerable<string> SuggestCorrections(string word)
        {
            foreach(var dictionary in bufferSpecificDictionaries)
            {
                var suggestions = dictionary.SuggestCorrections(word);

                if(suggestions.Count() != 0)
                    return suggestions;
            }

            return globalDictionary.SuggestCorrections(word);
        }

        /// <inheritdoc />
        public bool AddWordToDictionary(string word)
        {
            if(String.IsNullOrWhiteSpace(word))
                return false;

            foreach(var dictionary in bufferSpecificDictionaries)
                if(dictionary.AddWordToDictionary(word))
                    return true;

            return globalDictionary.AddWordToDictionary(word);
        }

        /// <inheritdoc />
        public event EventHandler<SpellingEventArgs> IgnoreOnce;

        /// <inheritdoc />
        public void IgnoreWordOnce(ITrackingSpan span)
        {
            var handler = IgnoreOnce;

            if(handler != null)
                handler(this, new SpellingEventArgs(span));
        }

        /// <inheritdoc />
        public bool IgnoreWord(string word)
        {
            if(String.IsNullOrWhiteSpace(word) || this.ShouldIgnoreWord(word))
                return false;

            foreach(var dictionary in bufferSpecificDictionaries)
                if(dictionary.IgnoreWord(word))
                    return true;

            return globalDictionary.IgnoreWord(word);
        }

        /// <inheritdoc />
        public bool ShouldIgnoreWord(string word)
        {
            foreach(var dictionary in bufferSpecificDictionaries)
                if(dictionary.ShouldIgnoreWord(word))
                    return true;

            return globalDictionary.ShouldIgnoreWord(word);
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public event EventHandler<SpellingEventArgs> ReplaceAll;

        /// <inheritdoc />
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
        /// This is used to raise the <see cref="DictionaryUpdated"/> event when a buffer-specific dictionary
        /// changes.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void BufferSpecificDictionaryUpdated(object sender, SpellingEventArgs e)
        {
            this.OnDictionaryUpdated(e.Word);
        }

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
