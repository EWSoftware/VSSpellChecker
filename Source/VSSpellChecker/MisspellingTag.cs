//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : MisspellingTag.cs
// Author  : Noah Richards, Roman Golovin, Michael Lehenbauer
// Updated : 05/30/2013
// Note    : Copyright 2010-2013, Microsoft Corporation, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a class that represents a misspelling tag
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code.  It can also be found at the project website: http://VSSpellChecker.CodePlex.com.
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
// Version     Date     Who  Comments
//===============================================================================================================
// 1.0.0.0  04/14/2013  EFW  Imported the code into the project
//
// Change History:
// 05/30/2013 - EFW - Added a Word property to return the misspelled word
//===============================================================================================================

using System.Collections.Generic;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

using VisualStudio.SpellChecker.Definitions;

namespace VisualStudio.SpellChecker
{
    /// <summary>
    /// This class represents a misspelling tag
    /// </summary>
    internal sealed class MisspellingTag : IMisspellingTag
    {
        #region Properties
        //=====================================================================

        /// <summary>
        /// This read-only property returns the span containing the misspelled word
        /// </summary>
        public ITrackingSpan Span { get; private set; }

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

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="span">The span containing the misspelled word</param>
        /// <param name="suggestions">The suggestions that can be used to replace the misspelled word</param>
        public MisspellingTag(SnapshotSpan span, IEnumerable<string> suggestions)
        {
            this.Span = span.Snapshot.CreateTrackingSpan(span, SpanTrackingMode.EdgeExclusive);
            this.Suggestions = suggestions;
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
