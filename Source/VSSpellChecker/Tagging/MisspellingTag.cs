//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : MisspellingTag.cs
// Authors : Noah Richards, Roman Golovin, Michael Lehenbauer, Eric Woodruff
// Updated : 08/25/2015
// Note    : Copyright 2010-2015, Microsoft Corporation, All rights reserved
//           Portions Copyright 2013-2015, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a class that represents a misspelling tag
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 04/14/2013  EFW  Imported the code into the project
// 05/30/2013  EFW  Added a Word property to return the misspelled word
// 02/28/2015  EFW  Added support for code analysis dictionary options
// 07/28/2015  EFW  Added support for culture information in the spelling suggestions
//===============================================================================================================

using System;
using System.Collections.Generic;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

using VisualStudio.SpellChecker.Definitions;

namespace VisualStudio.SpellChecker.Tagging
{
    /// <summary>
    /// This class represents a misspelling tag
    /// </summary>
    internal sealed class MisspellingTag : ITag, ISpellingIssue
    {
        #region Properties
        //=====================================================================

        /// <summary>
        /// This read-only property returns the misspelling type
        /// </summary>
        public MisspellingType MisspellingType { get; private set; }

        /// <summary>
        /// This read-only property returns the span containing the misspelled word
        /// </summary>
        public ITrackingSpan Span { get; private set; }

        /// <summary>
        /// This read-only property returns the span for deleting a doubled word which includes leading
        /// whitespace.
        /// </summary>
        public ITrackingSpan DeleteWordSpan { get; private set; }

        /// <summary>
        /// This is used to get or set the suggestions that can be used to replace the misspelled word
        /// </summary>
        public IEnumerable<ISpellingSuggestion> Suggestions { get; set; }

        /// <summary>
        /// This read-only property returns the misspelled or doubled word
        /// </summary>
        public string Word
        {
            get { return this.Span.GetText(this.Span.TextBuffer.CurrentSnapshot); }
        }
        #endregion

        #region Constructors
        //=====================================================================

        /// <summary>
        /// This constructor is used for misspelled words with a specific type
        /// </summary>
        /// <param name="misspellingType">The misspelling type</param>
        /// <param name="span">The span containing the misspelled word</param>
        /// <param name="suggestions">The suggestions that can be used to replace the misspelled word</param>
        /// <overloads>There are three overloads for the constructor</overloads>
        public MisspellingTag(MisspellingType misspellingType, SnapshotSpan span, IEnumerable<SpellingSuggestion> suggestions)
        {
            if(misspellingType == MisspellingType.DoubledWord)
                throw new ArgumentException("Misspelling type cannot be doubled word");

            this.MisspellingType = misspellingType;
            this.Span = this.DeleteWordSpan = span.Snapshot.CreateTrackingSpan(span, SpanTrackingMode.EdgeExclusive);
            this.Suggestions = suggestions ?? new SpellingSuggestion[0];
        }

        /// <summary>
        /// This constructor is used for general misspelled words
        /// </summary>
        /// <param name="span">The span containing the misspelled word</param>
        /// <remarks>For this constructor, no suggestions are given.  They are set once the spell checking has
        /// been completed for the entire range so that common misspellings can share a common set of
        /// suggestions.</remarks>
        public MisspellingTag(SnapshotSpan span) : this(MisspellingType.MisspelledWord, span, null)
        {
        }

        /// <summary>
        /// This constructor is used for doubled words
        /// </summary>
        /// <param name="span">The span containing the doubled word</param>
        /// <param name="deleteWordSpan">The span to use when deleting the doubled word</param>
        public MisspellingTag(SnapshotSpan span, SnapshotSpan deleteWordSpan)
        {
            this.MisspellingType = MisspellingType.DoubledWord;
            this.Span = span.Snapshot.CreateTrackingSpan(span, SpanTrackingMode.EdgeExclusive);
            this.DeleteWordSpan = deleteWordSpan.Snapshot.CreateTrackingSpan(deleteWordSpan, SpanTrackingMode.EdgeExclusive);
            this.Suggestions = new SpellingSuggestion[0];
        }
        #endregion

        #region Helper methods
        //=====================================================================

        /// <summary>
        /// Convert the misspelling span to a tag span
        /// </summary>
        /// <param name="snapshot">The snapshot to use</param>
        /// <returns>The span wrapped in a tag span</returns>
        public ITagSpan<MisspellingTag> ToTagSpan(ITextSnapshot snapshot)
        {
            return new TagSpan<MisspellingTag>(this.Span.GetSpan(snapshot), this);
        }
        #endregion
    }
}
