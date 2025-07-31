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

02/22/2023 - EFW - Search for EFW to find changes made to support .editorconfig matching similar to Visual Studio

*/

using System;
using System.Linq;

using GlobExpressions.AST;

namespace GlobExpressions
{
    /// <summary>
    /// This class is used to compare filenames to file globs
    /// </summary>
    public partial class Glob
    {
        /// <summary>
        /// The glob pattern
        /// </summary>
        public string Pattern { get; }

        private Tree _root;
        private Segment[] _segments;
        private readonly bool _caseSensitive;
        private readonly bool _matchFilenameOnly;
        //!EFW - Added support for .editorconfig style matching
        private readonly bool _editorConfigMatching;

        //!EFW - Made the default options CaseInsensitive and EditorConfigMatching
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="pattern">The glob pattern</param>
        /// <param name="options">The options</param>
        public Glob(string pattern, GlobOptions options = GlobOptions.CaseInsensitive | GlobOptions.EditorConfigMatching)
        {
            this.Pattern = pattern;
            _caseSensitive = !options.HasFlag(GlobOptions.CaseInsensitive);
            _matchFilenameOnly = options.HasFlag(GlobOptions.MatchFilenameOnly);
            _editorConfigMatching = options.HasFlag(GlobOptions.EditorConfigMatching);

            if(options.HasFlag(GlobOptions.Compiled))
            {
                this.Compile();
            }
        }

        private void Compile()
        {
            if (_root != null)
                return;

            if (_segments != null)
                return;

            var parser = new Parser(this.Pattern);
            _root = parser.ParseTree();
            _segments = _root.Segments;
        }

        /// <summary>
        /// See if the file path is a match for this glob
        /// </summary>
        /// <param name="input">The file path to check</param>
        /// <returns>True if it is a match, false if not</returns>
        public bool IsMatch(string input)
        {
            this.Compile();

            if(input == null)
                throw new ArgumentNullException(nameof(input));

            var pathSegments = input.Split('/', '\\');

            // match filename only
            if (_matchFilenameOnly && _segments.Length == 1)
            {
                var last = pathSegments.LastOrDefault();
                var tail = (last == null) ? [] : new[] { last };

                if (GlobEvaluator.Eval(_segments, 0, tail, 0, _caseSensitive))
                    return true;
            }

            // !EFW - Support matching on partial paths starting with a folder (an Ends With match).  This
            // allows globs like Folder/File.ext in lieu of **/Folder/File.ext.
            if(_editorConfigMatching && _segments.Length > 1 && _segments[0].Type == GlobNodeType.DirectorySegment)
            {
                // If there are no directory wildcards, we can just compare the last set of segments
                int idx = pathSegments.Length - _segments.Length;

                if(idx > -1 && !_segments.Any(s => s.Type == GlobNodeType.DirectoryWildcard))
                    return GlobEvaluator.Eval(_segments, 0, pathSegments, idx, _caseSensitive);

                // If there are, try to match segments from the end until we run out.  This allows for things
                // like TestFolder/**/File.ext as well.
                while(idx >= 0)
                {
                    if(GlobEvaluator.Eval(_segments, 0, pathSegments, idx, _caseSensitive))
                        return true;

                    idx--;
                }

                return false;
            }

            return GlobEvaluator.Eval(_segments, 0, pathSegments, 0, _caseSensitive);
        }
    }
}
