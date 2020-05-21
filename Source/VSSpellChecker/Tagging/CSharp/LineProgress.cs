﻿//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : LineProgress.cs
// Authors : Noah Richards, Roman Golovin, Michael Lehenbauer, Eric Woodruff
// Updated : 09/02/2018
// Note    : Copyright 2010-2018, Microsoft Corporation, All rights reserved
//           Portions Copyright 2013-2018, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a class used to track line progress while parsing C# code for natural text regions
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 04/14/2013  EFW  Imported the code into the project
// 04/14/2013  EFW  Added the NextSegment() method
// 09/18/2015  EFW  Added methods to ignore a span, determine an XML element name, and XML attribute name
//===============================================================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.VisualStudio.Text;

namespace VisualStudio.SpellChecker.Tagging.CSharp
{
    /// <summary>
    /// This class is used to track line progress while parsing C# code for natural text regions
    /// </summary>
    /// <remarks>This is used in place of the normal classifier to work around issues in how the default
    /// classifier works.</remarks>
    class LineProgress
    {
        #region Private data members
        //=====================================================================

        private ITextSnapshotLine snapshotLine;
        private List<SnapshotSpan> naturalTextSpans;

        private string lineText;
        private int linePosition, naturalTextStart = -1;

        #endregion

        #region Properties
        //=====================================================================

        /// <summary>
        /// This is used to get or set the current line progress state
        /// </summary>
        public State State { get; set; }

        /// <summary>
        /// This read-only property returns true if at the end of the line, false if not
        /// </summary>
        public bool EndOfLine => linePosition >= snapshotLine.Length;

        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="snapshotLine">The line snapshot</param>
        /// <param name="state">The starting state</param>
        /// <param name="naturalTextSpans">The collection of natural text spans</param>
        public LineProgress(ITextSnapshotLine snapshotLine, State state, List<SnapshotSpan> naturalTextSpans)
        {
            this.snapshotLine = snapshotLine;
            this.lineText = snapshotLine.GetText();
            this.naturalTextSpans = naturalTextSpans;
            this.State = state;
        }
        #endregion

        #region Methods
        //=====================================================================

        /// <summary>
        /// This returns the character at the current line position
        /// </summary>
        /// <returns>The character at the current line position</returns>
        public char Char()
        {
            return lineText[linePosition];
        }

        /// <summary>
        /// This returns the next character after the current line position
        /// </summary>
        /// <returns>The next character after the current line position or a null character if it is past the
        /// end of the line.</returns>
        public char NextChar()
        {
            return linePosition < snapshotLine.Length - 1 ? lineText[linePosition + 1] : (char)0;
        }

        /// <summary>
        /// This returns the character two positions after the current line position
        /// </summary>
        /// <returns>The character two positions after the current line position or a null character if it is
        /// past the end of the line.</returns>
        public char NextNextChar()
        {
            return linePosition < snapshotLine.Length - 2 ? lineText[linePosition + 2] : (char)0;
        }

        /// <summary>
        /// Get a segment of the string starting at the current position and extending for the given length
        /// </summary>
        /// <param name="length">The length of the segment to return</param>
        /// <returns>The string segment.  If fewer characters are present than requested, only the remaining
        /// characters are returned.</returns>
        public string NextSegment(int length)
        {
            if(length < 1)
                return String.Empty;

            if(linePosition < snapshotLine.Length - length)
                return lineText.Substring(linePosition, length);

            return lineText.Substring(linePosition);
        }

        /// <summary>
        /// Advance the line position by the given number of characters
        /// </summary>
        /// <param name="count">The number of characters to advance</param>
        public void Advance(int count = 1)
        {
            linePosition += count;
        }

        /// <summary>
        /// Advance the position to the end of the line
        /// </summary>
        public void AdvanceToEndOfLine()
        {
            linePosition = snapshotLine.Length;
        }

        /// <summary>
        /// Mark the start of a natural text region
        /// </summary>
        public void StartNaturalText()
        {
            Debug.Assert(naturalTextStart == -1, "Called StartNaturalText() twice without call to EndNaturalText()?");
            naturalTextStart = linePosition;
        }

        /// <summary>
        /// Mark the end of a natural text region and add it to the collection of natural text spans
        /// </summary>
        public void EndNaturalText()
        {
            Debug.Assert(naturalTextStart != -1, "Called EndNaturalText() without StartNaturalText()?");

            if(naturalTextSpans != null && linePosition > naturalTextStart)
                naturalTextSpans.Add(new SnapshotSpan(snapshotLine.Start + naturalTextStart,
                    linePosition - naturalTextStart));

            naturalTextStart = -1;
        }

        /// <summary>
        /// Ignore a span
        /// </summary>
        public void IgnoreSpan()
        {
            naturalTextStart = -1;
        }

        /// <summary>
        /// This is used to determine an XML doc comment element name if possible so that we can exclude
        /// unwanted content from being spell checked.
        /// </summary>
        /// <returns>The element name if possible or an empty string if not</returns>
        public string DetermineElementName()
        {
            int start, pos = naturalTextStart;

            while(pos > 0 && lineText[pos] != '<' && lineText[pos] != '/')
                pos--;

            if(pos < 1 || lineText[pos] == '/')
                return String.Empty;

            start = ++pos;

            while(pos < naturalTextStart && lineText[pos] != '>' && !System.Char.IsWhiteSpace(lineText[pos]))
                pos++;

            if(pos >= naturalTextStart)
                return String.Empty;

            return lineText.Substring(start, pos - start);
        }

        /// <summary>
        /// This is used to determine an XML doc comment attribute name if possible so that we can exclude
        /// unwanted value strings from being spell checked.
        /// </summary>
        /// <returns>The attribute name if possible or an empty string if not</returns>
        public string DetermineAttributeName()
        {
            int end, pos = naturalTextStart;

            while(pos > 0 && lineText[pos] != '=')
                pos--;

            if(pos < 1)
                return String.Empty;

            end = pos--;

            while(pos > 0 && lineText[pos] != '<' && !System.Char.IsWhiteSpace(lineText[pos]))
                pos--;

            if(pos < 1)
                return String.Empty;

            return lineText.Substring(pos + 1, end - pos - 1);
        }
        #endregion
    }
}