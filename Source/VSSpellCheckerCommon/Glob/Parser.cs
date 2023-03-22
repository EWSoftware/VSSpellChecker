/*
Project URL: https://github.com/kthompson/glob

The NuGet package is unsigned and doesn't support earlier versions of the .NET
Framework.  It's small enough so I'm just including the code directly. - Eric

The MIT License (MIT)

Copyright (c) 2013-2023 Kevin Thompson and Glob contributors

Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in
the Software without restriction, including without limitation the rights to
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
the Software, and to permit persons to whom the Software is furnished to do so,
subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Text;

using GlobExpressions.AST;

namespace GlobExpressions
{
    internal class Parser
    {
        private readonly string _source;
        private int _sourceIndex;
        private int _currentCharacter;
        private readonly StringBuilder _spelling;

        public Parser(string pattern = null)
        {
            this._source = pattern ?? String.Empty;
            this._sourceIndex = 0;
            _spelling = new StringBuilder();
            SetCurrentCharacter();
        }

        private string GetSpelling()
        {
            try
            {
                if (_spelling.Length == 0)
                {
                    // Did not consume a character
                    throw new GlobPatternException($"Unexpected character {(char)_currentCharacter} at index {_sourceIndex}");
                }
                return _spelling.ToString();
            }
            finally
            {
                _spelling.Clear();
            }
        }

        private StringWildcard ParseStar()
        {
            this.SkipIt();
            return StringWildcard.Default;
        }

        private CharacterWildcard ParseCharacterWildcard()
        {
            this.SkipIt();
            return CharacterWildcard.Default;
        }

        private CharacterSet ParseCharacterSet()
        {
            this.SkipIt();

            var inverted = false;
            if(_currentCharacter == '!')
            {
                this.SkipIt();
                inverted = true;
            }

            // first token is special and we allow more things like ] or [ at the beginning
            if(_currentCharacter == ']')
            {
                this.Accept();
            }

            while (_currentCharacter != ']' && _currentCharacter != -1)
            {
                this.Accept();
            }

            this.SkipIt(); // ]
            var spelling = GetSpelling();
            return new CharacterSet(spelling, inverted);
        }

        private LiteralSet ParseLiteralSet()
        {
            var items = new List<Identifier>();
            this.SkipIt(); // {

            if (this._currentCharacter == '}')
            {
                throw new GlobPatternException($"Expected literal at index {_sourceIndex}. Literal sets cannot be empty.");
            }

            items.Add(this.ParseIdentifier(true));
            while (this._currentCharacter == ',')
            {
                this.SkipIt(); // ,
                items.Add(this.ParseIdentifier(true));
            }
            this.SkipIt('}');
            return new LiteralSet(items);
        }

        private void Accept()
        {
            _spelling.Append((char)_currentCharacter);

            SkipIt();
        }

        private void Accept(int expected)
        {
            if (_currentCharacter != expected)
            {
                if (expected == -1)
                {
                    throw new GlobPatternException($"Expected end of input at index {_sourceIndex}");
                }

                throw new GlobPatternException($"Expected {(char)expected} at index {_sourceIndex}");
            }

            Accept();
        }

        private void SkipIt(int expected)
        {
            if (_currentCharacter != expected)
            {
                if (expected == -1)
                {
                    throw new GlobPatternException($"Expected end of input at index {_sourceIndex}");
                }

                throw new GlobPatternException($"Expected {(char)expected} at index {_sourceIndex}");
            }

            SkipIt();
        }

        private void SkipIt()
        {
            this._sourceIndex++;

            SetCurrentCharacter();
        }

        private void SetCurrentCharacter()
        {
            if (this._sourceIndex >= this._source.Length)
                this._currentCharacter = -1;
            else
                this._currentCharacter = this._source[this._sourceIndex];
        }

        private int PeekChar()
        {
            var sourceIndex = this._sourceIndex + 1;
            if (sourceIndex >= this._source.Length)
                return -1;

            return this._source[sourceIndex];
        }

        private Identifier ParseIdentifier(bool inLiteralSet)
        {
            // if we are in a literal set then we wont consume commas unless they are escaped
            while (true)
            {
                switch (_currentCharacter)
                {
                    case -1:
                    case '[':
                    case ']':
                    case '{':
                    case '}':
                    case '?':
                    case '*':
                    case '/':
                    case ',' when inLiteralSet:
                        // terminate
                        break;
                    case '\\':
                        ParseEscapeSequence(inLiteralSet);
                        continue;

                    default:
                        Accept();
                        continue;
                }

                break;
            }

            return new Identifier(GetSpelling());
        }

        private void ParseEscapeSequence(bool inLiteralSet)
        {
            this.SkipIt(); // don't append to our text
            switch (this._currentCharacter)
            {
                case '*':
                case '?':
                case '{':
                case '}':
                case '[':
                case ']':
                case '(':
                case ')':
                case ' ':
                case ',' when inLiteralSet:
                    this.Accept(); // escaped char
                    return;

                default:
                    throw new GlobPatternException(
                        $"Expected escape sequence at index pattern `{_sourceIndex - 1}` but found `\\{(char)this._currentCharacter}`");
            }
        }

        // SubSegment := Identifier | CharacterSet | LiteralSet | CharacterWildcard | Wildcard
        private SubSegment ParseSubSegment()
        {
            // stub support for extended globs if we ever want to support it
            if (PeekChar() == '(')
            {
                switch (this._currentCharacter)
                {
                    case '?':
                    case '*':
                    case '+':
                    case '@':
                    case '!':
                        throw new GlobPatternException("Extended glob patterns are not currently supported");
                }
            }

            switch (this._currentCharacter)
            {
                case '[':
                    return this.ParseCharacterSet();

                case '{':
                    return this.ParseLiteralSet();

                case '?':
                    return this.ParseCharacterWildcard();

                case '*':
                    return this.ParseStar();

                default:
                    return this.ParseIdentifier(false);
            }
        }

        // Segment := DirectoryWildcard | DirectorySegment
        // DirectorySegment := SubSegment SubSegment*
        private Segment ParseSegment()
        {
            var items = new List<SubSegment>();

            var lastWasStar = false;
            var prevWasStar = false;
            while (_currentCharacter != '/' && _currentCharacter != -1)
            {
                var subSegment = this.ParseSubSegment();
                var isStar = subSegment is StringWildcard;
                // convert ** to *
                if (!lastWasStar || !isStar)
                {
                    items.Add(subSegment);
                }

                prevWasStar = lastWasStar;
                lastWasStar = isStar;
            }


            // if we had a ** return **
            if(items.Count == 1 && lastWasStar && prevWasStar)
                return DirectoryWildcard.Default;

            return new DirectorySegment(items);
        }

        private Root ParseRoot()
        {
            if (_currentCharacter == '/')
            {
                return new Root(); // don't consume it so we can leave it for the segments
            }

            // windows root
            this.Accept();
            this.Accept(':');

            return new Root(GetSpelling());
        }

        // Tree := ( Root | Segment ) ( '/' Segment )*
        protected internal Tree ParseTree()
        {
            var items = new List<Segment>();

            switch (this._currentCharacter)
            {
                case -1:
                    break;

                case '/':
                    items.Add(this.ParseRoot());
                    break;

                default:
                    // windows root
                    if (Char.IsLetter((char)_currentCharacter) && PeekChar() == ':')
                    {
                        items.Add(this.ParseRoot());
                        break;
                    }

                    items.Add(this.ParseSegment());
                    break;
            }

            while (this._currentCharacter == '/')
            {
                SkipIt();
                items.Add(this.ParseSegment());
            }

            Accept(-1);

            return new Tree(items);
        }

        public GlobNode Parse() => this.ParseTree();
    }
}
