//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : CSharpCommentTextTagger.cs
// Author  : Noah Richards, Roman Golovin, Michael Lehenbauer
// Updated : 04/14/2013
// Note    : Copyright 2010-2013, Microsoft Corporation, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a class used to provide tags for C# code
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code.  It can also be found at the project website: http://VSSpellChecker.CodePlex.com.
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
// Version     Date     Who  Comments
//===============================================================================================================
// 1.0.0.0  04/14/2013  EFW  Imported the code into the project.
//
// Change History
// 04/14/2013 - EFW - Added a check for #region directives so that the region title is spell checked too
//===============================================================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace VisualStudio.SpellChecker.NaturalTextTaggers.CSharp
{
    /// <summary>
    /// This class is used to provide tags for C# code
    /// </summary>
    /// <remarks>Due to issues with the built-in C# classifier, we write our own NaturalTextTagger that looks for 
    /// comments (single, multi-line, and doc comment) and strings (single and multi-line) and tags them with
    /// NaturalTextTag.</remarks>
    internal class CSharpCommentTextTagger : ITagger<NaturalTextTag>, IDisposable
    {
        #region Private data members
        //=====================================================================

        private ITextBuffer _buffer;
        private ITextSnapshot _lineCacheSnapshot = null;
        private List<State> _lineCache;
        #endregion

        #region Constructor
        //=====================================================================


        public CSharpCommentTextTagger(ITextBuffer buffer)
        {
            _buffer = buffer;

            // Populate our cache initially.
            ITextSnapshot snapshot = _buffer.CurrentSnapshot;
            int lines = snapshot.LineCount;
            _lineCache = new List<State>(snapshot.LineCount);
            _lineCache.AddRange(Enumerable.Repeat(State.Default, snapshot.LineCount));

            RescanLines(snapshot, startLine: 0, lastDirtyLine: snapshot.LineCount - 1);
            _lineCacheSnapshot = snapshot;

            // Listen for text changes so we can stay up-to-date.
            _buffer.Changed += OnTextBufferChanged;
        }
        #endregion

        #region IDisposable implementation
        //=====================================================================

        public void Dispose()
        {
            _buffer.Changed -= OnTextBufferChanged;
        }
        #endregion

        #region ITagger<INaturalTextTag> Members
        //=====================================================================

        /// <inheritdoc />
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        /// <inheritdoc />
        public IEnumerable<ITagSpan<NaturalTextTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            foreach(SnapshotSpan span in spans)
            {
                // If we're called on the non-current snapshot, return nothing.
                if(span.Snapshot != _lineCacheSnapshot)
                    yield break;

                SnapshotPoint lineStart = span.Start;

                while(lineStart < span.End)
                {
                    ITextSnapshotLine line = lineStart.GetContainingLine();
                    State state = _lineCache[line.LineNumber];

                    List<SnapshotSpan> naturalTextSpans = new List<SnapshotSpan>();
                    state = ScanLine(state, line, naturalTextSpans);

                    foreach(SnapshotSpan naturalTextSpan in naturalTextSpans)
                        if(naturalTextSpan.IntersectsWith(span))
                            yield return new TagSpan<NaturalTextTag>(naturalTextSpan, new NaturalTextTag());

                    // Advance to next line
                    lineStart = line.EndIncludingLineBreak;
                }
            }
        }

        private void OnTextBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            ITextSnapshot snapshot = e.After;

            // First update _lineCache so its size matches snapshot.LineCount.
            foreach(ITextChange change in e.Changes)
            {
                if(change.LineCountDelta > 0)
                {
                    int line = snapshot.GetLineFromPosition(change.NewPosition).LineNumber;
                    _lineCache.InsertRange(line, Enumerable.Repeat(State.Default, change.LineCountDelta));
                }
                else if(change.LineCountDelta < 0)
                {
                    int line = snapshot.GetLineFromPosition(change.NewPosition).LineNumber;
                    _lineCache.RemoveRange(line, -change.LineCountDelta);
                }
            }

            // Now that _lineCache is the appropriate size we can safely start rescanning.
            // If we hadn't updated _lineCache, then rescanning could walk off the edge.
            List<SnapshotSpan> changedSpans = new List<SnapshotSpan>();

            foreach(ITextChange change in e.Changes)
            {
                ITextSnapshotLine startLine = snapshot.GetLineFromPosition(change.NewPosition);
                ITextSnapshotLine endLine = snapshot.GetLineFromPosition(change.NewPosition);
                int lastUpdatedLine = RescanLines(snapshot, startLine.LineNumber, endLine.LineNumber);

                changedSpans.Add(new SnapshotSpan(startLine.Start,
                    snapshot.GetLineFromLineNumber(lastUpdatedLine).End));
            }

            _lineCacheSnapshot = snapshot;

            var tagsChanged = TagsChanged;

            if(tagsChanged != null)
            {
                foreach(SnapshotSpan span in changedSpans)
                    tagsChanged(this, new SnapshotSpanEventArgs(span));
            }
        }
        #endregion

        #region Helper methods
        //=====================================================================

        // Returns last line updated (will be greater than or equal to lastDirtyLine).
        private int RescanLines(ITextSnapshot snapshot, int startLine, int lastDirtyLine)
        {
            int currentLine = startLine;
            State state = _lineCache[currentLine];
            bool updatedStateForCurrentLine = true;

            // Go until we have covered all of the dirty lines and we get to a line where our
            // new state matches the old state.
            while(currentLine < lastDirtyLine || (updatedStateForCurrentLine && currentLine < snapshot.LineCount))
            {
                ITextSnapshotLine line = snapshot.GetLineFromLineNumber(currentLine);
                state = ScanLine(state, line);

                // Advance to next line.
                currentLine++;
                if(currentLine < snapshot.LineCount)
                {
                    updatedStateForCurrentLine = (state != _lineCache[currentLine]);
                    _lineCache[currentLine] = state;
                }
            }

            return currentLine - 1; // last line updated.
        }

        private State ScanLine(State state, ITextSnapshotLine line, List<SnapshotSpan> naturalTextSpans = null)
        {
            LineProgress p = new LineProgress(line, state, naturalTextSpans);

            while(!p.EndOfLine)
            {
                if(p.State == State.Default)
                    ScanDefault(p);
                else if(p.State == State.MultiLineComment)
                    ScanMultiLineComment(p);
                else if(p.State == State.MultiLineString)
                    ScanMultiLineString(p);
                else
                    Debug.Fail("Invalid state at beginning of line.");
            }

            // End Of Line state must be one of these.
            Debug.Assert(p.State == State.Default || p.State == State.MultiLineString || p.State == State.MultiLineComment);

            return p.State;
        }

        private void ScanDefault(LineProgress p)
        {
            while(!p.EndOfLine)
            {
                if(p.Char() == '/' && p.NextChar() == '/' && p.NextNextChar() == '/') // doc comment.
                {
                    p.Advance(3);
/*
                    // TODO: Make this an option
                    // If "////", allow skipping it (i.e. commented out code)
                    if(p.Char() == '/')
                    {
                        p.AdvanceToEndOfLine();
                        return;
                    }*/

                    p.State = State.DocComment;
                    ScanDocComment(p);
                }
                else if(p.Char() == '/' && p.NextChar() == '/') // single line comment
                {
                    p.Advance(2);
                    p.StartNaturalText();
                    p.AdvanceToEndOfLine();
                    p.EndNaturalText();

                    p.State = State.Default;
                    return;
                }
                else if(p.Char() == '/' && p.NextChar() == '*') // multi-line comment
                {
                    p.Advance(2);
                    p.State = State.MultiLineComment;
                    ScanMultiLineComment(p);
                }
                else if(p.Char() == '@' && p.NextChar() == '"') // multi-line string
                {
                    p.Advance(2);
                    p.State = State.MultiLineString;
                    ScanMultiLineString(p);
                }
                else if(p.Char() == '"') // single-line string
                {
                    p.Advance(1);
                    p.State = State.String;
                    ScanString(p);
                }
                else if(p.Char() == '\'') // character literal
                {
                    p.Advance(1);
                    p.State = State.Character;
                    ScanCharacter(p);
                }
                else if(p.Char() == '#')    // Possible preprocessor keyword, check for #region
                {
                    p.Advance(1);

                    // If found, treat it like a single line comment
                    if(p.NextSegment(6) == "region")
                    {
                        p.StartNaturalText();
                        p.AdvanceToEndOfLine();
                        p.EndNaturalText();

                        p.State = State.Default;
                        return;
                    }
                }
                else
                {
                    p.Advance();
                }
            }
        }

        private void ScanDocComment(LineProgress p)
        {
            p.StartNaturalText();
            while(!p.EndOfLine)
            {
                if(p.Char() == '<')
                {
                    p.EndNaturalText();

                    p.Advance();
                    p.State = State.DocCommentXml;
                    ScanDocCommentXml(p);

                    p.StartNaturalText();
                }
                else
                {
                    p.Advance();
                }
            }

            // End of line.  Record what we have and revert to default state.
            p.EndNaturalText();
            p.State = State.Default;
        }

        private void ScanDocCommentXml(LineProgress p)
        {
            while(!p.EndOfLine)
            {
                if(p.Char() == '"')
                {
                    p.Advance(1);
                    p.State = State.DocCommentXmlString;
                    ScanDocCommentXmlString(p);
                }
                else if(p.Char() == '>')
                {
                    p.Advance();
                    p.State = State.DocComment;

                    return; // Done with xml tag in doc comment.
                }
                else
                {
                    p.Advance();
                }
            }

            // End of line.  Never found the '>' for the tag, but whatever.  We revert to default state.
            p.State = State.Default;
        }

        private void ScanDocCommentXmlString(LineProgress p)
        {
            while(!p.EndOfLine)
            {
                if(p.Char() == '"')
                {
                    p.Advance(1);
                    p.State = State.DocCommentXml;

                    return; // Done with string in doc comment xml.
                }
                else
                {
                    p.Advance();
                }
            }

            // End of line.  Never found the '"' to close the string, but whatever.  We revert to default state.
            p.State = State.Default;
        }

        private void ScanMultiLineComment(LineProgress p)
        {
            p.StartNaturalText();

            while(!p.EndOfLine)
            {
                if(p.Char() == '*' && p.NextChar() == '/') // close comment
                {
                    p.EndNaturalText();
                    p.Advance(2);
                    p.State = State.Default;

                    return; // done with multi-line comment.
                }
                else
                {
                    p.Advance();
                }
            }

            // End of line.  Emit as human readable, but remain in MultiLineComment state.
            p.EndNaturalText();
            Debug.Assert(p.State == State.MultiLineComment);
        }

        private void ScanMultiLineString(LineProgress p)
        {
            p.StartNaturalText();

            while(!p.EndOfLine)
            {
                if(p.Char() == '"' && p.NextChar() == '"') // "" is allowed within multiline string.
                {
                    p.Advance(2);
                }
                else if(p.Char() == '"') // end of multi-line string
                {
                    p.EndNaturalText();

                    p.Advance();
                    p.State = State.Default;
                    return;
                }
                else
                {
                    p.Advance();
                }
            }

            // End of line.  Emit as human readable, but remain in MultiLineString state.
            p.EndNaturalText();
            Debug.Assert(p.State == State.MultiLineString);
        }

        private void ScanString(LineProgress p)
        {
            p.StartNaturalText();

            while(!p.EndOfLine)
            {
                if(p.Char() == '\\') // escaped character.  Skip over it.
                {
                    p.Advance(2);
                }
                else if(p.Char() == '"') // end of string.
                {
                    p.EndNaturalText();
                    p.Advance();
                    p.State = State.Default;

                    return;
                }
                else
                {
                    p.Advance();
                }
            }

            // End of line.  String wasn't closed.  Oh well.  Revert to Default state.
            p.EndNaturalText();
            p.State = State.Default;
        }

        private void ScanCharacter(LineProgress p)
        {
            if(!p.EndOfLine && p.Char() == '\\') // escaped character.  Eat it.
            {
                p.Advance(2);
            }
            else if(!p.EndOfLine && p.Char() != '\'') // non-escaped character.  Eat it.
            {
                p.Advance(1);
            }

            if(!p.EndOfLine && p.Char() == '\'') // closing ' for character, as expected.
            {
                p.Advance(1);
                p.State = State.Default;
                return;
            }

            // Didn't find closing ' for character.  Oh well.
            p.State = State.Default;
        }
        #endregion
    }
}
