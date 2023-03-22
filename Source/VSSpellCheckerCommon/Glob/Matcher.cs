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

using GlobExpressions.AST;

namespace GlobExpressions
{
    internal static class Matcher
    {
        public static bool MatchesSegment(this DirectorySegment segment, string pathSegment, bool caseSensitive) =>
            MatchesSubSegment(segment.SubSegments, 0, -1, pathSegment, 0, caseSensitive);

        private static bool MatchesSubSegment(SubSegment[] segments, int segmentIndex, int literalSetIndex, string pathSegment, int pathIndex, bool caseSensitive)
        {
            var nextSegment = segmentIndex + 1;
            if (nextSegment > segments.Length)
                return pathIndex == pathSegment.Length;

            var head = segments[segmentIndex];
            if (head is LiteralSet ls)
            {
                if (literalSetIndex == -1)
                {
                    for (int i = 0; i < ls.Literals.Length; i++)
                    {
                        if (MatchesSubSegment(segments, segmentIndex, i, pathSegment, pathIndex, caseSensitive))
                            return true;
                    }

                    return false;
                }

                head = ls.Literals[literalSetIndex];
            }

            switch (head)
            {
                // match zero or more chars
                case StringWildcard _:
                    return MatchesSubSegment(segments, nextSegment, -1, pathSegment, pathIndex, caseSensitive) // zero
                           || (pathIndex < pathSegment.Length &&
                               MatchesSubSegment(segments, segmentIndex, -1, pathSegment, pathIndex + 1, caseSensitive)); // or one+

                case CharacterWildcard _:
                    return pathIndex < pathSegment.Length && MatchesSubSegment(segments, nextSegment, -1, pathSegment, pathIndex + 1, caseSensitive);

                case Identifier ident:
                    var len = ident.Value.Length;
                    if (len + pathIndex > pathSegment.Length)
                        return false;

                    if (!SubstringEquals(pathSegment, pathIndex, ident.Value, caseSensitive))
                        return false;

                    return MatchesSubSegment(segments, nextSegment, -1, pathSegment, pathIndex + len, caseSensitive);

                case CharacterSet set:
                    if (pathIndex == pathSegment.Length)
                        return false;

                    var inThere = set.Matches(pathSegment[pathIndex], caseSensitive);
                    return inThere && MatchesSubSegment(segments, nextSegment, -1, pathSegment, pathIndex + 1, caseSensitive);

                default:
                    return false;
            }
        }

        private static bool SubstringEquals(string segment, int segmentIndex, string search, bool caseSensitive) =>
            String.Compare(segment, segmentIndex, search, 0, search.Length, caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase) == 0;
    }
}
