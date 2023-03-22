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
using System.Text;

namespace GlobExpressions.AST
{
    internal sealed class CharacterSet : SubSegment
    {
        public bool Inverted { get; }
        public string Characters { get; }
        public string ExpandedCharacters { get; }

        public CharacterSet(string characters, bool inverted)
            : base(GlobNodeType.CharacterSet)
        {
            Characters = characters;
            Inverted = inverted;
            this.ExpandedCharacters = CalculateExpandedForm(characters);
        }

        public bool Matches(char c, bool caseSensitive) => Contains(c, caseSensitive) != this.Inverted;

        private bool Contains(char c, bool caseSensitive) => ExpandedCharacters.IndexOf(c.ToString(), caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase) >= 0;

        private static string CalculateExpandedForm(string chars)
        {
            var sb = new StringBuilder();
            var i = 0;
            var len = chars.Length;

            // if first character is special, add it
            if (chars.StartsWith("-", StringComparison.Ordinal) || chars.StartsWith("[", StringComparison.Ordinal) ||
              chars.StartsWith("]", StringComparison.Ordinal))
            {
                sb.Append(chars[0]);
                i++;
            }

            while (true)
            {
                if (i >= len)
                    break;

                if (chars[i] == '-')
                {
                    if (i == len - 1)
                    {
                        // - is last character so just add it
                        sb.Append('-');
                    }
                    else
                    {
                        for (var c = chars[i - 1] + 1; c <= chars[i + 1]; c++)
                        {
                            sb.Append((char)c);
                        }
                        i++; // skip trailing range
                    }
                }
                else if (chars[i] == '/')
                {
                    i++; // skip
                }
                else
                {
                    sb.Append(chars[i]);
                }
                i++;
            }

            return sb.ToString();
        }
    }
}
