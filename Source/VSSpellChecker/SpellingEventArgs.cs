//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : SpellingEventArgs.cs
// Authors : Noah Richards, Roman Golovin, Michael Lehenbauer
// Updated : 05/31/2013
// Note    : Copyright 2010-2013, Microsoft Corporation, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a class used to contain arguments for spelling events
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 04/14/2013  EFW  Imported the code into the project
// 05/02/2013  EFW  Add support for defining a replacement word
// 05/31/2013  EFW  Added support for defining the word's position in the editor's buffer
//===============================================================================================================

using System;
using Microsoft.VisualStudio.Text;

namespace VisualStudio.SpellChecker
{
    /// <summary>
    /// This class contain arguments for spelling events
    /// </summary>
    public class SpellingEventArgs : EventArgs
    {
        #region Properties
        //=====================================================================

        /// <summary>
        /// This read-only property returns the word related to the event
        /// </summary>
        /// <remarks>If <c>null</c>, it means that the entire dictionary has changed and words that may have
        /// been ignored before may now no longer be in the dictionary.</remarks>
        public string Word { get; private set; }

        /// <summary>
        /// This read-only property returns the word that should replace <see cref="Word"/> if applicable to
        /// the event.
        /// </summary>
        /// <value>This will be null if not applicable</value>
        public string ReplacementWord { get; private set; }

        /// <summary>
        /// This read-only property returns the tracking span related to the event
        /// </summary>
        /// <value>This will be null if not applicable</value>
        public ITrackingSpan Span { get; private set; }
        #endregion

        #region Constructors
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="word">The word related to the event or null</param>
        public SpellingEventArgs(string word)
        {
            this.Word = word;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="word">The word related to the event</param>
        /// <param name="replacement">The replacement word.  If null, an empty string is used.</param>
        public SpellingEventArgs(string word, string replacement)
        {
            if(String.IsNullOrWhiteSpace(word))
                throw new ArgumentException("The word cannot be null or empty", "word");

            this.Word = word;
            this.ReplacementWord = (replacement ?? String.Empty);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="span">The tracking span related to the event</param>
        public SpellingEventArgs(ITrackingSpan span)
        {
            this.Word = span.GetText(span.TextBuffer.CurrentSnapshot);
            this.Span = span;
        }
        #endregion
    }
}
