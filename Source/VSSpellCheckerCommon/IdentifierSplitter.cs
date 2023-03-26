//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : IdentifierSplitter.cs
// Author  : Eric Woodruff
// Updated : 03/22/2023
// Note    : Copyright 2023, Eric Woodruff, All rights reserved
//
// This file contains a class that handles splitting identifiers up into individual words for spell checking
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 03/12/2023  EFW  Created the code
//===============================================================================================================

using System;
using System.Collections.Generic;
using System.Linq;

using VisualStudio.SpellChecker.Common.Configuration;

namespace VisualStudio.SpellChecker.Common
{
    /// <summary>
    /// This class is used to split identifiers up into individual words for spell checking
    /// </summary>
    /// <typeparam name="T">The span type returned by the identifier splitter</typeparam>
    public abstract class IdentifierSplitter<T>
    {
        #region Properties
        //=====================================================================

        /// <summary>
        /// The spell checker configuration to use for splitting text into words
        /// </summary>
        public SpellCheckerConfiguration Configuration { get; set; }

        #endregion

        #region Helper methods
        //=====================================================================

        /// <summary>
        /// This is used to create a span containing a word to be spell checked
        /// </summary>
        /// <param name="startIndex">The starting index of the span</param>
        /// <param name="endIndex">The ending index of the span</param>
        /// <returns>The span type for the derived word splitter</returns>
        public abstract T CreateSpan(int startIndex, int endIndex);

        /// <summary>
        /// Get all words in the specified identifier
        /// </summary>
        /// <param name="identifier">The identifier to split into words</param>
        /// <returns>An enumerable list of word spans</returns>
        public IEnumerable<T> GetWordsInIdentifier(string identifier)
        {
            if(String.IsNullOrWhiteSpace(identifier))
                yield break;

            for(int i = 0, end = 0; i < identifier.Length; i++)
            {
                // Skip word separator
                if(!Char.IsLetter(identifier, i))
                    continue;

                // Find the end of the word
                end = i;

                while(++end < identifier.Length && Char.IsLetter(identifier, end))
                    ;

                // Ignore empty strings and anything less than 2 characters
                if(end - i > 1)
                {
                    // Check for mixed/camel case words.  Split those up into individual spans.
                    string word = identifier.Substring(i, end - i);
                    bool isAllUppercase = word.All(c => Char.IsUpper(c));

                    if(isAllUppercase || !word.Skip(1).Any(c => Char.IsUpper(c)))
                    {
                        if(!isAllUppercase || !this.Configuration.CodeAnalyzerOptions.IgnoreIdentifierIfAllUppercase)
                            yield return this.CreateSpan(i, end);
                    }
                    else
                    {
                        // An exception is if it appears in the code analysis dictionary options.  These may
                        // be camel cased but the user wants them replaced with something else.
                        if((this.Configuration.CadOptions.TreatDeprecatedTermsAsMisspelled &&
                          this.Configuration.DeprecatedTerms.ContainsKey(word)) ||
                          (this.Configuration.CadOptions.TreatCompoundTermsAsMisspelled &&
                          this.Configuration.CompoundTerms.ContainsKey(word)))
                        {
                            yield return this.CreateSpan(i, end);
                        }
                        else
                        {
                            int split = i;

                            while(split < end)
                            {
                                // Skip consecutive uppercase letters (i.e NHunSpell).  This may not always
                                // be accurate but it's the best we can do.
                                while(split + 1 < end && Char.IsUpper(identifier[split + 1]))
                                    split++;

                                i = split;
                                split++;

                                while(split < end && !Char.IsUpper(identifier[split]))
                                    split++;

                                // A common occurrence is a final uppercase letter followed by 's' such as
                                // IDs or GUIDs.  Ignore those.
                                if(split - i == 2 && Char.IsUpper(identifier[i]) && identifier[i + 1] == 's')
                                    i = split;

                                if(split - i > 1)
                                    yield return this.CreateSpan(i, split);
                            }
                        }
                    }
                }

                i = --end;
            }
        }
        #endregion
    }
}
