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
    internal static class GlobEvaluator
    {
        public static bool Eval(Segment[] segments, int segmentIndex, string[] input, int inputIndex, bool caseSensitive)
        {
            while (true)
            {
                // no segments left
                if (segmentIndex == segments.Length)
                    return inputIndex == input.Length;

                var consumedAllInput = inputIndex >= input.Length;
                if (consumedAllInput)
                    return false;

                switch (segments[segmentIndex])
                {
                    case DirectoryWildcard _:
                        var isLastInput = inputIndex == input.Length - 1;
                        var isLastSegment = segmentIndex == segments.Length - 1;

                        // simple match last input and segment
                        if (isLastSegment && isLastInput)
                            return true;

                        // match 0
                        var matchConsumesWildCard = !isLastSegment && Eval(segments, segmentIndex + 1, input, inputIndex, caseSensitive);
                        if (matchConsumesWildCard)
                            return true;

                        // match 1+
                        var skipInput = !isLastInput && Eval(segments, segmentIndex, input, inputIndex + 1, caseSensitive);

                        return skipInput;

                    case Root root:
                        if (inputIndex < input.Length && String.Equals(input[inputIndex], root.Text, caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase))
                        {
                            segmentIndex++;
                            inputIndex++;
                            continue;
                        }
                        else
                        {
                            return false;
                        }

                    case DirectorySegment dir:
                        if (inputIndex < input.Length && dir.MatchesSegment(input[inputIndex], caseSensitive))
                        {
                            segmentIndex++;
                            inputIndex++;
                            continue;
                        }
                        else
                        {
                            return false;
                        }
                }

                return false;
            }
        }
    }
}
