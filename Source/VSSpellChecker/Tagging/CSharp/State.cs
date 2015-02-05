//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : State.cs
// Authors : Noah Richards, Roman Golovin, Michael Lehenbauer
// Updated : 06/12/2014
// Note    : Copyright 2010-2014, Microsoft Corporation, All rights reserved
//           Portions Copyright 2014, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains an enumeration used to indicate the line progress state
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 04/14/2013  EFW  Imported the code into the project
// 06/12/2014  EFW  Added support for MultiLineDocComment
//===============================================================================================================

namespace VisualStudio.SpellChecker.Tagging.CSharp
{
    /// <summary>
    /// This enumerated type is used to indicate the line progress state
    /// </summary>
    enum State
    {
        /// <summary>Default start state</summary>
        Default,

        /// <summary>Single-line comment (// ...)</summary>
        Comment,
        /// <summary>Multi-line comment (/*...*/)</summary>
        MultiLineComment,
        /// <summary>Multi-line doc comment (/**...*/)</summary>
        MultiLineDocComment,

        /// <summary>XML doc comment (/// ...)</summary>
        DocComment,
        /// <summary>XML doc comment XML tag (/// &lt;...&gt; blah)</summary>
        DocCommentXml,
        /// <summary>XML doc comment XML string (/// &lt;blah bar="..."&gt;)</summary>
        DocCommentXmlString,

        /// <summary>String ("...")</summary>
        String,
        /// <summary>Multi-line string (@"...")</summary>
        MultiLineString,

        /// <summary>Character ('.')</summary>
        Character
    }
}
