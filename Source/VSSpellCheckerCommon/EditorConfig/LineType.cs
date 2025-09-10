//===============================================================================================================
// System  : Spell Check My Code Package
// File    : LineType.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 01/30/2023
// Note    : Copyright 2023, Eric Woodruff, All rights reserved
//
// This file contains the enumeration used to define .editorconfig file line types
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
//===============================================================================================================
// 01/30/2023  EFW  Created the code
//===============================================================================================================

using System;

namespace VisualStudio.SpellChecker.Common.EditorConfig
{
    /// <summary>
    /// This enumerated type is used to define .editorconfig file line types
    /// </summary>
    [Serializable]
    public enum LineType
    {
        /// <summary>
        /// Undefined
        /// </summary>
        None,
        /// <summary>
        /// A blank line
        /// </summary>
        Blank,
        /// <summary>
        /// A comment
        /// </summary>
        Comment,
        /// <summary>
        /// A section header (file glob)
        /// </summary>
        SectionHeader,
        /// <summary>
        /// A property
        /// </summary>
        Property
    }
}
