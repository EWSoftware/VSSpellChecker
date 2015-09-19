//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : FileMisspelling.cs
// Authors : Eric Woodruff
// Updated : 09/10/2015
// Note    : Copyright 2015, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a class that represents a misspelling withing a project file
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 09/02/2015  EFW  Created the code
//===============================================================================================================

using System;
using System.Collections.Generic;

using Microsoft.VisualStudio.Text;

using VisualStudio.SpellChecker.Definitions;

namespace VisualStudio.SpellChecker.ProjectSpellCheck
{
    /// <summary>
    /// This class represents a misspelling within a project file
    /// </summary>
    internal sealed class FileMisspelling : ISpellingIssue
    {
        #region Properties
        //=====================================================================

        /// <summary>
        /// This read-only property returns the misspelling type
        /// </summary>
        public MisspellingType MisspellingType { get; private set; }

        /// <summary>
        /// This is used to get or set the span containing the misspelled word
        /// </summary>
        public Span Span { get; set; }

        /// <summary>
        /// This is used to get or set the span for deleting a doubled word which includes leading whitespace
        /// </summary>
        public Span DeleteWordSpan { get; set; }

        /// <summary>
        /// This is used to get or set the suggestions that can be used to replace the misspelled word
        /// </summary>
        public IEnumerable<ISpellingSuggestion> Suggestions { get; set; }

        /// <summary>
        /// This read-only property returns the misspelled or doubled word
        /// </summary>
        public string Word { get; private set; }

        /// <summary>
        /// This is used to get or set the spelling dictionary for the issue
        /// </summary>
        /// <remarks>Suggestions on normal misspelled words are deferred until the issue is selected for
        /// review.  This saves time during the spell checking process and memory overall.</remarks>
        public SpellingDictionary Dictionary { get; set; }

        /// <summary>
        /// This is used to get or set whether or not suggestions have been determined
        /// </summary>
        /// <remarks>Suggestions on normal misspelled words are deferred until the issue is selected for
        /// review.  This saves time during the spell checking process and memory overall.</remarks>
        public bool SuggestionsDetermined { get; set; }

        /// <summary>
        /// This is used to get or set the name of the project containing the file
        /// </summary>
        public string ProjectName { get; set; }

        /// <summary>
        /// This is used to get or set the name of the file containing the issue (no path)
        /// </summary>
        public string Filename { get; set; }

        /// <summary>
        /// This is used to get or set the canonical name of the file (full path)
        /// </summary>
        public string CanonicalName { get; set; }

        /// <summary>
        /// This is used to get or set the line number for the issue
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// This is used to get or set the text of the line containing the issue
        /// </summary>
        public string LineText { get; set; }

        /// <summary>
        /// This read-only property gets a description of the issue
        /// </summary>
        public string IssueDescription
        {
            get
            {
                switch(this.MisspellingType)
                {
                    case Definitions.MisspellingType.CompoundTerm:
                        return "Compound Term";

                    case Definitions.MisspellingType.DeprecatedTerm:
                        return "Deprecated Term";

                    case Definitions.MisspellingType.DoubledWord:
                        return "Doubled Word";

                    case Definitions.MisspellingType.UnrecognizedWord:
                        return "Unrecognized Word";

                    default:
                        return "Misspelled Word";
                }
            }
        }
        #endregion

        #region Constructors
        //=====================================================================

        /// <summary>
        /// This constructor is used for misspelled words with a specific type
        /// </summary>
        /// <param name="misspellingType">The misspelling type</param>
        /// <param name="span">The span containing the misspelled word</param>
        /// <param name="word">The misspelled word</param>
        /// <param name="suggestions">The suggestions that can be used to replace the misspelled word</param>
        /// <overloads>There are three overloads for the constructor</overloads>
        public FileMisspelling(MisspellingType misspellingType, Span span, string word,
          IEnumerable<SpellingSuggestion> suggestions)
        {
            if(misspellingType == MisspellingType.DoubledWord)
                throw new ArgumentException("Misspelling type cannot be doubled word");

            this.MisspellingType = misspellingType;
            this.Span = this.DeleteWordSpan = span;
            this.Word = word;
            this.Suggestions = suggestions ?? new SpellingSuggestion[0];
            this.SuggestionsDetermined = (suggestions != null);
        }

        /// <summary>
        /// This constructor is used for general misspelled words
        /// </summary>
        /// <param name="span">The span containing the misspelled word</param>
        /// <param name="word">The misspelled word</param>
        /// <remarks>For this constructor, no suggestions are given.  They are set once the spell checking has
        /// been completed for the entire range so that common misspellings can share a common set of
        /// suggestions.</remarks>
        public FileMisspelling(Span span, string word) : this(MisspellingType.MisspelledWord, span, word, null)
        {
        }

        /// <summary>
        /// This constructor is used for doubled words
        /// </summary>
        /// <param name="span">The span containing the doubled word</param>
        /// <param name="deleteWordSpan">The span to use when deleting the doubled word</param>
        /// <param name="word">The doubled word</param>
        public FileMisspelling(Span span, Span deleteWordSpan, string word)
        {
            this.MisspellingType = MisspellingType.DoubledWord;
            this.Span = span;
            this.DeleteWordSpan = deleteWordSpan;
            this.Word = word;
            this.Suggestions = new SpellingSuggestion[0];
            this.SuggestionsDetermined = true;
        }
        #endregion
    }
}
