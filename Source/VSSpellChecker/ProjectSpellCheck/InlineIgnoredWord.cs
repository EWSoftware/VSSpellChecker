//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : InlineIgnoredWord.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 08/23/2018
// Note    : Copyright 2018, Eric Woodruff, All rights reserved
//
// This file contains a class used to define an ignored word indicated in a file using ignore spelling directive
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 08/24/2017  EFW  Created the code
//===============================================================================================================

using System;
using System.Text.RegularExpressions;

using Microsoft.VisualStudio.Text;

namespace VisualStudio.SpellChecker.ProjectSpellCheck
{
    /// <summary>
    /// This is used to define an ignored word indicated in a file using the inline ignore spelling directive
    /// (Ignore spelling: word1, word2, .... [/matchCase]).
    /// </summary>
    /// <remarks>The directive can contain any number of words, appear any number of times anywhere in the file,
    /// and can include the optional "/matchCase" option to make the words in the given directive case-sensitive.
    /// Typically, the directives will be placed in comments.  All file types are supported.</remarks>
    internal class InlineIgnoredWord
    {
        internal static readonly Regex reIgnoreSpelling = new(
            @"Ignore spelling:\s*?(?<IgnoredWords>[^\r\n/]+)(?<CaseSensitive>/matchCase)?", RegexOptions.IgnoreCase);

        /// <summary>
        /// The word to ignore
        /// </summary>
        public string Word { get; set; }

        /// <summary>
        /// True if the word comparison should be case-sensitive, false if not
        /// </summary>
        public bool CaseSensitive { get; set; }

        /// <summary>
        /// The span containing the ignored word
        /// </summary>
        public ITrackingSpan Span { get; set; }

        /// <summary>
        /// For the text editor spelling tagger, this indicates whether or not the word is new since the line
        /// containing it was last parsed.
        /// </summary>
        /// <value>True if it new, false if not</value>
        /// <remarks>If new, a rescan of the whole file is initiated to remove any current misspellings that
        /// match an ignored word.</remarks>
        public bool IsNew { get; set; }

        /// <summary>
        /// This is used to see if the given word is a match to this one
        /// </summary>
        /// <param name="word">The word to compare</param>
        /// <returns>True if it matches, false if not</returns>
        public bool IsMatch(string word)
        {
            return this.Word.Equals(word, this.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
        }
    }
}
