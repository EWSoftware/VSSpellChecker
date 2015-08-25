//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : RangeClassification.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 08/31/2015
// Note    : Copyright 2015, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains an enumerated type that defines the classification for a range of text
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 08/29/2015  EFW  Created the code
//===============================================================================================================

namespace VisualStudio.SpellChecker.ProjectSpellCheck
{
    /// <summary>
    /// This enumerated type defines the classification for a range of text
    /// </summary>
    internal enum RangeClassification
    {
        /// <summary>Plain text</summary>
        PlainText,

        /// <summary>XML/HTML file comment</summary>
        XmlFileComment,
        /// <summary>XML file CDATA</summary>
        XmlFileCData,
        /// <summary>XML/HTML element attribute value</summary>
        AttributeValue,
        /// <summary>XML/HTML element inner text</summary>
        InnerText,

        /// <summary>Delimited comment in code</summary>
        DelimitedComments,
        /// <summary>Single line comment in code</summary>
        SingleLineComment,
        /// <summary>XML documentation comments in code</summary>
        XmlDocComments,
        /// <summary>Quadruple slash comment in code</summary>
        QuadSlashComment,
        /// <summary>Normal string literal in code</summary>
        NormalStringLiteral,
        /// <summary>Verbatim string literal in code</summary>
        VerbatimStringLiteral,
        /// <summary>Interpolated string literal in code</summary>
        InterpolatedStringLiteral,

        /// <summary>Region directive</summary>
        RegionDirective
    }
}
