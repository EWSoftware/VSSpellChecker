//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : TextClassifier.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 09/10/2015
// Note    : Copyright 2015, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains an abstract base class used to implement text classification for the content of various
// file types.
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 08/26/2015  EFW  Created the code
//===============================================================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using VisualStudio.SpellChecker.Configuration;

namespace VisualStudio.SpellChecker.ProjectSpellCheck
{
    /// <summary>
    /// This abstract base class is used to implement text classification for the content of various file types
    /// </summary>
    internal abstract class TextClassifier
    {
        #region Private data members
        //=====================================================================

        private List<int> lineOffsets;

        #endregion

        #region Properties
        //=====================================================================

        /// <summary>
        /// This read-only property returns the filename
        /// </summary>
        public string Filename { get; private set; }

        /// <summary>
        /// This read-only property returns the spell checker configuration for the file
        /// </summary>
        public SpellCheckerConfiguration SpellCheckConfiguration { get; private set; }

        /// <summary>
        /// This read-only property returns the text contained in the file
        /// </summary>
        public string Text { get; private set; }

        /// <summary>
        /// This can be used to set a line offset if parsing a subsection of text
        /// </summary>
        protected int LineOffset { get; set; }

        /// <summary>
        /// This can be used to set a column offset for the <see cref="LineOffset"/> line number if parsing a
        /// subsection of text.
        /// </summary>
        protected int ColumnOffset { get; set; }

        /// <summary>
        /// This read-only property returns the line count
        /// </summary>
        public int LineCount
        {
            get
            {
                return lineOffsets.Count - 1;
            }
        }

        /// <summary>
        /// This read-only property is used to get the given line from the text
        /// </summary>
        /// <param name="line">The line number to get</param>
        /// <returns></returns>
        public string this[int line]
        {
            get
            {
                if(line < 1 || line > lineOffsets.Count - 2)
                    return String.Empty;

                line--;

                return this.Text.Substring(lineOffsets[line], lineOffsets[line + 1] - lineOffsets[line]).TrimEnd();
            }
        }
        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="filename">The filename to load</param>
        /// <param name="spellCheckConfiguration">The spell checker configuration for the file</param>
        protected TextClassifier(string filename, SpellCheckerConfiguration spellCheckConfiguration)
        {
            this.Filename = filename;
            this.SpellCheckConfiguration = spellCheckConfiguration;

            using(StreamReader sr = new StreamReader(filename, Encoding.Default, true))
            {
                this.SetText(sr.ReadToEnd());
            }
        }
        #endregion

        #region Methods
        //=====================================================================

        /// <summary>
        /// This is used to set the text that will be classified
        /// </summary>
        /// <param name="text">The text to use</param>
        /// <remarks>By default, the constructor calls this to set the text to the file content.  It can be
        /// called by other code to set alternate text such as from an open editor.</remarks>
        public void SetText(string text)
        {
            int length = text.Length;

            this.Text = text;

            lineOffsets = new List<int>(length / 10 + 1);

            lineOffsets.Add(0);

            for(int i = 0; i < length; i++)
                switch(text[i])
                {
                    case '\r':
                        if(i + 1 < length && text[i + 1] == '\n')
                            i++;

                        lineOffsets.Add(i + 1);
                        break;

                    case '\n':
                        lineOffsets.Add(i + 1);
                        break;
                }

            lineOffsets.Add(length + 1);
        }

        /// <summary>
        /// Given a start line and start column this returns the corresponding starting position within the text
        /// </summary>
        /// <param name="line">The number of the line containing the first character</param>
        /// <param name="column">The position of the first character relative to the start of the line</param>
        /// <returns>The position in the text of the line and column</returns>
        public int GetOffset(int line, int column)
        {
            if(this.LineOffset != 0)
                line += this.LineOffset - 1;

            if(line == this.LineOffset)
                column += this.ColumnOffset - 1;

            // If line and/or column are less than zero, adjust them to be valid.  This may not produce a valid
            // offset for the text but it does prevent an exception.
            if(line < 1)
                line = 1;

            if(column < 1)
                column = 1;

            int offset = lineOffsets[line - 1] + column - 1;

            if(offset > this.Text.Length)
                offset = this.Text.Length;

            return offset;
        }

        /// <summary>
        /// This gets the line number containing the given offset
        /// </summary>
        /// <param name="offset">The offset for which to get the line number</param>
        /// <returns>The line number containing the offset</returns>
        public int GetLineNumber(int offset)
        {
            int line, column;

            this.GetPosition(offset, out line, out column);

            return line;
        }

        /// <summary>
        /// This converts the given zero based character position in the text to a line/column pair corresponding
        /// to the same position.
        /// </summary>
        /// <param name="offset">The offset for which to get the line and column</param>
        /// <param name="line">The returns the line number</param>
        /// <param name="column">This returns the column number</param>
        public void GetPosition(int offset, out int line, out int column)
        {
            line = column = 0;

            if(offset >= 0 && this.Text != null && offset < this.Text.Length)
            {
                int index = this.Search(offset);

                if(index >= 0 && index < lineOffsets.Count)
                {
                    line = index + 1;
                    column = offset - lineOffsets[index] + 1;
                }
            }
        }

        /// <summary>
        /// This is used to get an adjusted offset for cases where a parser's reported line/column position does
        /// not exactly match the given text's location.
        /// </summary>
        /// <param name="offset">The reported offset</param>
        /// <param name="matchText">The text to match</param>
        /// <returns>The adjusted offset if the given text could be found at or within the length of the given
        /// text from the original position.  If no match could be found, the original offset is returned.</returns>
        public int AdjustedOffset(int offset, string matchText)
        {
            if(offset < 0)
                offset = 0;

            int originalOffset = offset, length = matchText.Length, maxOffset = offset + length;

            while(offset < maxOffset)
            {
                if(this.Text.Substring(offset, length) == matchText)
                    return offset;

                offset++;
            }

            return originalOffset;
        }

        /// <summary>
        /// This returns the index in the line offsets list such that the line offset value is less than or equal
        /// to offset and offset is less than the following line offset.
        /// </summary>
        private int Search(int offset)
        {
            if(offset < 0)
                return -1;

            int mid = 0, low = 0, high = lineOffsets.Count - 1;

            while(low < high)
            {
                mid = (low + high) / 2;

                if(lineOffsets[mid] <= offset)
                {
                    if(offset < lineOffsets[mid + 1])
                        return mid;

                    low = mid + 1;
                }
                else
                    high = mid;
            }

            return low;
        }
        #endregion

        #region Abstract methods
        //=====================================================================

        /// <summary>
        /// Parse the given text and return the classified spans that should be spell checked
        /// </summary>
        /// <returns>An enumerable list of spans to spell check</returns>
        public abstract IEnumerable<SpellCheckSpan> Parse();

        #endregion
    }
}
