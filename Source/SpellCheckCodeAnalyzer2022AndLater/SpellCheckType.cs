//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : SpellCheckType.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 04/26/2023
// Note    : Copyright 2023, Eric Woodruff, All rights reserved
//
// This file contains an enumerated type that defines the spell checked items
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 02/25/2023  EFW  Created the code
//===============================================================================================================

using System;

namespace VisualStudio.SpellChecker.CodeAnalyzer
{
    /// <summary>
    /// This enumerated type defines the spell checked items
    /// </summary>
    [Flags, Serializable]
    public enum SpellCheckType
    {
        // ====================================================================
        // Main types
        
        /// <summary>
        /// None
        /// </summary>
        None                = 0x0000,
        /// <summary>
        /// An identifier
        /// </summary>
        Identifier          = 0x0001,
        /// <summary>
        /// A comment
        /// </summary>
        Comment             = 0x0002,
        /// <summary>
        /// A string literal
        /// </summary>
        StringLiteral       = 0x0004,
        /// <summary>
        /// An attribute value
        /// </summary>
        AttributeValue      = 0x0008,

        // ====================================================================
        // Subtypes

        /// <summary>
        /// A type parameter
        /// </summary>
        TypeParameter = 0x0010,

        /// <summary>
        /// A normal string literal
        /// </summary>
        NormalString        = 0x0020,
        /// <summary>
        /// An interpolated string literal
        /// </summary>
        InterpolatedString  = 0x0040,
        /// <summary>
        /// A verbatim string literal
        /// </summary>
        VerbatimString      = 0x0080,
        /// <summary>
        /// A raw string literal
        /// </summary>
        RawString           = 0x0100,

        /// <summary>
        /// A delimited comment
        /// </summary>
        DelimitedComment    = 0x0200,
        /// <summary>
        /// A single-line comment
        /// </summary>
        SingleLineComment   = 0x0400,
        /// <summary>
        /// A quad-slash comment
        /// </summary>
        QuadSlashComment    = 0x0800,
        /// <summary>
        /// An XML documentation comment
        /// </summary>
        XmlDocComment       = 0x1000
    }
}
