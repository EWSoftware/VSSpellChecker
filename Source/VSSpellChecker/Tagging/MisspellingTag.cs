//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : MisspellingTag.cs
// Authors : Noah Richards, Roman Golovin, Michael Lehenbauer, Eric Woodruff
// Updated : 06/06/2014
// Note    : Copyright 2010-2014, Microsoft Corporation, All rights reserved
//           Portions Copyright 2013-2014, Eric Woodruff, All rights reserved
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
//===============================================================================================================

using System.Collections.Generic;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace VisualStudio.SpellChecker.Tagging
{
    /// <summary>
    /// This class represents a misspelling tag
    /// </summary>
    internal sealed class MisspellingTag : ITag
    {
        #region Properties
        //=====================================================================

        /// <summary>
        /// This read-only property returns true if this represents a misspelled word of false if it represents
        /// a doubled word.
        /// </summary>
        public bool IsMisspelling { get; private set; }

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
        /// This read-only property returns the suggestions that can be used to replace the misspelled word
        /// </summary>
        public IEnumerable<string> Suggestions { get; private set; }

        /// <summary>
        /// This read-only property returns the misspelled word
        /// </summary>
        public string Word
        {
            get { return this.Span.GetText(this.Span.TextBuffer.CurrentSnapshot); }
        }
        #endregion

        #region Constructors
        //=====================================================================

        /// <summary>
        /// This constructor is used for misspelled words
        /// </summary>
        /// <param name="span">The span containing the misspelled word</param>
        /// <param name="suggestions">The suggestions that can be used to replace the misspelled word</param>
        /// <overloads>There are two overloads for the constructor</overloads>
        public MisspellingTag(SnapshotSpan span, IEnumerable<string> suggestions)
        {
            this.IsMisspelling = true;
            this.Span = this.DeleteWordSpan = span.Snapshot.CreateTrackingSpan(span, SpanTrackingMode.EdgeExclusive);
            this.Suggestions = suggestions;
        }

        /// <summary>
        /// This constructor is used for doubled words
        /// </summary>
        /// <param name="span">The span containing the doubled word</param>
        /// <param name="deleteWordSpan">The span to use when deleting the doubled word</param>
        public MisspellingTag(SnapshotSpan span, SnapshotSpan deleteWordSpan)
        {
            this.Span = span.Snapshot.CreateTrackingSpan(span, SpanTrackingMode.EdgeExclusive);
            this.DeleteWordSpan = deleteWordSpan.Snapshot.CreateTrackingSpan(deleteWordSpan, SpanTrackingMode.EdgeExclusive);
            this.Suggestions = new string[0];
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
