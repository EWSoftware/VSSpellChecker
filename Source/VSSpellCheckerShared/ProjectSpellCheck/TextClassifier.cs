//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : TextClassifier.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 03/22/2023
// Note    : Copyright 2015-2023, Eric Woodruff, All rights reserved
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
// 08/18/2018  EFW  Added support for excluding by range classification
//===============================================================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Microsoft.VisualStudio.Text;

using VisualStudio.SpellChecker.Common.Configuration;

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
        private readonly List<RangeClassification> ignoredClassifications;

        #endregion

        #region Properties
        //=====================================================================

        /// <summary>
        /// This read-only property returns the filename
        /// </summary>
        public string Filename { get; }

        /// <summary>
        /// This read-only property returns the spell checker configuration for the file
        /// </summary>
        public SpellCheckerConfiguration SpellCheckConfiguration { get; }

        /// <summary>
        /// This is used to get an enumerable list of ignored range classifications that should not be spell checked
        /// </summary>
        public IEnumerable<RangeClassification> IgnoredClassifications => ignoredClassifications;

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
        public int LineCount => lineOffsets.Count - 1;

        /// <summary>
        /// This read-only property is used to get the given line from the text
        /// </summary>
        /// <param name="line">The line number to get</param>
        /// <returns></returns>
        public string this[int line]
        {
            get
            {
                if(line < 1 || line > lineOffsets.Count - 1)
                    return String.Empty;

                if(line == lineOffsets.Count - 1)
                    return this.Text.Substring(lineOffsets[line - 1]);

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

            ignoredClassifications = new List<RangeClassification>();

            // Get the ignored classifications based on the extension.  If there are none, check for the
            // file type.
            string ext = Path.GetExtension(filename);

            if(!String.IsNullOrWhiteSpace(ext))
                ext = ext.Substring(1);

            var exclusions = spellCheckConfiguration.IgnoredClassificationsFor(
                SpellCheckerConfiguration.Extension + ext);

            if(!exclusions.Any())
            {
                exclusions = spellCheckConfiguration.IgnoredClassificationsFor(SpellCheckerConfiguration.FileType +
                    ClassifierFactory.ClassifierIdFor(filename));
            }

            foreach(string exclusion in exclusions)
                if(Enum.TryParse(exclusion, out RangeClassification rangeType))
                    ignoredClassifications.Add(rangeType);

            if(!File.Exists(filename))
                this.SetText(String.Empty);
            else
            {
                using(StreamReader sr = new StreamReader(filename, Encoding.Default, true))
                {
                    this.SetText(sr.ReadToEnd());
                }
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
        public virtual void SetText(string text)
        {
            int length = text.Length;

            this.Text = text;

            lineOffsets = new List<int>((length / 10) + 1) { 0 };

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
            this.GetPosition(offset, out int line, out _);

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

            int low = 0, high = lineOffsets.Count - 1;

            while(low < high)
            {
                int mid = (low + high) / 2;

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

        /// <summary>
        /// This is used to split the first span, which completely contains the second, into two or three
        /// contiguous non-overlapping spans.
        /// </summary>
        /// <param name="spans">The span collection</param>
        /// <param name="firstSpanIdx">The first span containing the second span</param>
        /// <param name="secondSpanIdx">The second span contained within the first span</param>
        /// <remarks>This allows the classifier to exclude unwanted spans of text from a larger span that is
        /// wanted.  For example, code spans within a larger span of plain text.</remarks>
        protected static void SplitSpan(IList<SpellCheckSpan> spans, int firstSpanIdx, int secondSpanIdx)
        {
            SpellCheckSpan firstSpan = spans[firstSpanIdx], secondSpan = spans[secondSpanIdx];

            if(secondSpanIdx < firstSpanIdx)
                (secondSpanIdx, firstSpanIdx) = (firstSpanIdx, secondSpanIdx);

            spans.RemoveAt(secondSpanIdx);
            spans.RemoveAt(firstSpanIdx);

            // Two identical spans were classified under different rules or the containing span is undefined
            if(firstSpan.Span == secondSpan.Span || firstSpan.Classification == RangeClassification.Undefined)
            {
                spans.Insert(firstSpanIdx, firstSpan);
                return;
            }

            // The second span is at the start of the first span
            if(firstSpan.Span.Start == secondSpan.Span.Start)
            {
                spans.Insert(firstSpanIdx, secondSpan);
                spans.Insert(secondSpanIdx, new SpellCheckSpan
                {
                    Span = new Span(secondSpan.Span.Start + secondSpan.Span.Length,
                        firstSpan.Span.Length - secondSpan.Span.Length),
                    Text = firstSpan.Text.Substring(secondSpan.Span.Length),
                    Classification = firstSpan.Classification
                });

                return;
            }

            // The second span is at the end of the first span
            if(firstSpan.Span.End == secondSpan.Span.End)
            {
                spans.Insert(firstSpanIdx, new SpellCheckSpan
                {
                    Span = new Span(firstSpan.Span.Start, firstSpan.Span.Length - secondSpan.Span.Length),
                    Text = firstSpan.Text.Substring(0, secondSpan.Span.Start - firstSpan.Span.Start),
                    Classification = firstSpan.Classification
                });
                spans.Insert(secondSpanIdx, secondSpan);

                return;
            }

            // The second span splits the first span into two parts
            spans.Insert(firstSpanIdx, new SpellCheckSpan
            {
                Span = new Span(firstSpan.Span.Start, secondSpan.Span.Start - firstSpan.Span.Start),
                Text = firstSpan.Text.Substring(0, secondSpan.Span.Start - firstSpan.Span.Start),
                Classification = firstSpan.Classification
            });
            spans.Insert(secondSpanIdx, secondSpan);
            spans.Insert(secondSpanIdx + 1, new SpellCheckSpan
            {
                Span = new Span(secondSpan.Span.Start + secondSpan.Span.Length,
                    firstSpan.Span.End - secondSpan.Span.End),
                Text = firstSpan.Text.Substring(secondSpan.Span.Start - firstSpan.Span.Start + secondSpan.Span.Length),
                Classification = firstSpan.Classification
            });
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
