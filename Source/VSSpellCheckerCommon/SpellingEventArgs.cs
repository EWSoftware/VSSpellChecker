//===============================================================================================================
// System  : Spell Check My Code Package
// File    : SpellingEventArgs.cs
// Authors : Noah Richards, Roman Golovin, Michael Lehenbauer
// Updated : 03/15/2023
// Note    : Copyright 2010-2023, Microsoft Corporation, All rights reserved
//           Portions Copyright 2013-2023, Eric Woodruff, All rights reserved
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
// 07/28/2015  EFW  Added support for culture information in the spelling suggestions
//===============================================================================================================

using System;
using System.Globalization;

using VisualStudio.SpellChecker.Definitions;

namespace VisualStudio.SpellChecker.Common
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
        public string Word { get; }

        /// <summary>
        /// This read-only property returns the culture related to the replacement word
        /// </summary>
        /// <value>This will be null if not applicable</value>
        public CultureInfo Culture { get; }

        /// <summary>
        /// This read-only property returns the word that should replace <see cref="Word"/> if applicable to
        /// the event.
        /// </summary>
        /// <value>This will be null if not applicable</value>
        public string ReplacementWord { get; }

        /// <summary>
        /// This read-only property returns the position related to the event
        /// </summary>
        /// <value>This will be -1 if not applicable</value>
        public int Position { get; } = -1;

        /// <summary>
        /// This read-only property returns the start index related to the event
        /// </summary>
        /// <value>This will be -1 if not applicable</value>
        public int StartIndex { get; } = -1;

        /// <summary>
        /// This read-only property returns the end index related to the event
        /// </summary>
        /// <value>This will be -1 if not applicable</value>
        public int EndIndex { get; } = -1;

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
        /// <param name="replacement">The replacement to use.  If null, an empty string is used.</param>
        public SpellingEventArgs(string word, ISpellingSuggestion replacement)
        {
            if(String.IsNullOrWhiteSpace(word))
                throw new ArgumentException("The word cannot be null or empty", nameof(word));

            this.Word = word;

            if(replacement == null)
                this.ReplacementWord = String.Empty;
            else
            {
                this.Culture = replacement.Culture;
                this.ReplacementWord = String.IsNullOrWhiteSpace(replacement.Suggestion) ? String.Empty :
                    replacement.Suggestion;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="word">The word related to the event</param>
        /// <param name="position">The position related to the event</param>
        /// <param name="startIndex">The start index related to the event</param>
        /// <param name="endIndex">The end index related to the event</param>
        public SpellingEventArgs(string word, int position, int startIndex, int endIndex)
        {
            this.Word = word;
            this.Position = position;
            this.StartIndex = startIndex;
            this.EndIndex = endIndex;
        }
        #endregion
    }
}
