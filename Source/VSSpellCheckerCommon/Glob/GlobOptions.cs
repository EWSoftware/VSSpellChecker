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

02/22/2023 - EFW - Added support for .editorconfig style matching

*/

using System;

namespace GlobExpressions
{
    /// <summary>
    /// Glob options
    /// </summary>
    [Flags]
    public enum GlobOptions
    {
        /// <summary>
        /// None
        /// </summary>
        None = 0,
        /// <summary>
        /// Compiled
        /// </summary>
        Compiled = 1 << 1,
        /// <summary>
        /// Compare case-insensitively
        /// </summary>
        CaseInsensitive = 1 << 2,
        /// <summary>
        /// Match filenames only (no paths)
        /// </summary>
        MatchFilenameOnly = 1 << 3,
        // !EFW - Added .editorconfig matching options
        /// <summary>
        /// .editorconfig option
        /// </summary>
        EditorConfig = 1 << 4,
        /// <summary>
        /// Adjust matching behavior to match .editorconfig glob matching
        /// </summary>
        /// <remarks>This adjusts the matching behavior to conform to how .editorconfig globs are matched, at
        /// least in Visual Studio.  A glob that is a single segment will always match on the filename alone.
        /// Globs starting with a folder will match starting at that folder if present (an Ends With match).</remarks>
        EditorConfigMatching = MatchFilenameOnly | EditorConfig
    }
}
