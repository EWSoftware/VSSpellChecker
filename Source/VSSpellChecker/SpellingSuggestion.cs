//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : SpellingSuggestion.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 10/28/2015
// Note    : Copyright 2015, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a class used to represents a spelling suggestion that can be used to replace a misspelled
// word.
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 07/28/2015  EFW  Created the code
//===============================================================================================================

using System;
using System.Globalization;

using VisualStudio.SpellChecker.Definitions;

namespace VisualStudio.SpellChecker
{
    /// <summary>
    /// This represents a spelling suggestion that can be used to replace a misspelled word
    /// </summary>
    public class SpellingSuggestion : ISpellingSuggestion
    {
        #region Properties
        //=====================================================================

        /// <summary>
        /// This read-only property returns the culture information for the suggestion
        /// </summary>
        public CultureInfo Culture { get; private set; }

        /// <summary>
        /// This read-only property returns the suggested replacement word
        /// </summary>
        public string Suggestion { get; private set; }

        #endregion

        #region Constructors
        //=====================================================================

        /// <summary>
        /// Suggestion constructor
        /// </summary>
        /// <param name="culture">The culture information for the suggestion</param>
        /// <param name="suggestion">The suggested replacement word</param>
        public SpellingSuggestion(CultureInfo culture, string suggestion)
        {
            this.Culture = culture;
            this.Suggestion = suggestion;
        }
        #endregion

        #region Equality, hash code, ToString
        //=====================================================================

        /// <summary>
        /// Returns a value indicating whether two specified instances of <c>SpellingSuggestion</c> are equal
        /// </summary>
        /// <param name="s1">The first suggestion to compare</param>
        /// <param name="s2">The second suggestion to compare</param>
        /// <returns>Returns true if the suggestions are equal, false if they are not</returns>
        public static bool Equals(SpellingSuggestion s1, SpellingSuggestion s2)
        {
            if((object)s1 == null && (object)s2 == null)
                return true;

            if((object)s1 == null)
                return false;

            return s1.Equals(s2);
        }

        /// <summary>
        /// This is overridden to allow proper comparison of <c>SpellCheckerDictionary</c> objects
        /// </summary>
        /// <param name="obj">The object to which this instance is compared</param>
        /// <returns>Returns true if the object equals this instance, false if it does not</returns>
        public override bool Equals(object obj)
        {
            SpellingSuggestion s = obj as SpellingSuggestion;

            return (s != null && ((this.Culture == null && s.Culture == null) ||
                (this.Culture != null && s.Culture != null && this.Culture.Name == s.Culture.Name)) &&
                this.Suggestion == s.Suggestion);
        }

        /// <summary>
        /// Get a hash code for the suggestion object
        /// </summary>
        /// <returns>Returns the hash code for the culture and suggestion XOR'ed together</returns>
        public override int GetHashCode()
        {
            return (this.Culture ?? CultureInfo.InvariantCulture).GetHashCode() ^ this.Suggestion.GetHashCode();
        }

        /// <summary>
        /// Convert the suggestion instance to a string
        /// </summary>
        /// <returns>Returns the <see cref="Suggestion"/> value</returns>
        public override string ToString()
        {
            return this.Suggestion;
        }
        #endregion

        #region Helper methods
        //=====================================================================

        /// <summary>
        /// This is used to add a mnemonic to the suggestion
        /// </summary>
        /// <param name="mnemonic">The mnemonic character</param>
        /// <param name="letter">The letter before which the mnemonic is added</param>
        public void AddMnemonic(char mnemonic, char letter)
        {
            string suggestion = this.Suggestion;
            int pos = suggestion.IndexOf(letter);

            if(pos == -1)
            {
                if(Char.IsUpper(letter))
                    letter = Char.ToLower(letter, this.Culture);
                else
                    if(Char.IsLower(letter))
                        letter = Char.ToUpper(letter, this.Culture);

                pos = suggestion.IndexOf(letter);

                if(pos == -1)
                    return;
            }

            this.Suggestion = suggestion.Substring(0, pos) + mnemonic.ToString() + suggestion.Substring(pos);
        }
        #endregion

    }
}
