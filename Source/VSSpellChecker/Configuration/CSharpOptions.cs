//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : CSharpOptions.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 02/01/2015
// Note    : Copyright 2015, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains the class used to contain the C# configuration settings
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
//===============================================================================================================
// 02/01/2015  EFW  Refactored configuration settings
//===============================================================================================================

using System.ComponentModel;

namespace VisualStudio.SpellChecker.Configuration
{
    /// <summary>
    /// This class contains the spell checker configuration options for C# source code files
    /// </summary>
    public class CSharpOptions
    {
        /// <summary>
        /// This is used to get or set whether or not to ignore XML documentation comments in C# files
        /// (<c>/** ... */</c> or <c>/// ...</c>)
        /// </summary>
        /// <value>The default is false to include XML documentation comments</value>
        [DefaultValue(false)]
        public bool IgnoreXmlDocComments { get; set; }

        /// <summary>
        /// This is used to get or set whether or not to ignore delimited comments in C# files (<c>/* ... */</c>)
        /// </summary>
        /// <value>The default is false to include delimited comments</value>
        [DefaultValue(false)]
        public bool IgnoreDelimitedComments { get; set; }

        /// <summary>
        /// This is used to get or set whether or not to ignore standard single line comments in C# files
        /// (<c>// ...</c>)
        /// </summary>
        /// <value>The default is false to include standard single line comments</value>
        [DefaultValue(false)]
        public bool IgnoreStandardSingleLineComments { get; set; }

        /// <summary>
        /// This is used to get or set whether or not to ignore quadruple slash comments in C# files
        /// (<c>//// ...</c>)
        /// </summary>
        /// <value>The default is false to include quadruple slash comments</value>
        /// <remarks>This is useful for ignoring commented out blocks of code while still spell checking the
        /// other comment styles.</remarks>
        [DefaultValue(false)]
        public bool IgnoreQuadrupleSlashComments { get; set; }

        /// <summary>
        /// This is used to get or set whether or not to ignore normal strings in C# files (<c>"..."</c>)
        /// </summary>
        /// <value>The default is false to include normal strings</value>
        [DefaultValue(false)]
        public bool IgnoreNormalStrings { get; set; }

        /// <summary>
        /// This is used to get or set whether or not to ignore verbatim strings in C# files (<c>@"..."</c>)
        /// </summary>
        /// <value>The default is false to include verbatim strings</value>
        [DefaultValue(false)]
        public bool IgnoreVerbatimStrings { get; set; }
    }
}
