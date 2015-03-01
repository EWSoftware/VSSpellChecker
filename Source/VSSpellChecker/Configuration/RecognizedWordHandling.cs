//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : RecognizedWordHandling.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 02/28/2015
// Note    : Copyright 2015, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains an enumerated type that defines how recognized words are handled when imported from a code
// analysis dictionary file.
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
//===============================================================================================================
// 02/26/2015  EFW  Created the code
//===============================================================================================================

namespace VisualStudio.SpellChecker.Configuration
{
    /// <summary>
    /// This enumerated type defines how recognized words are handled when imported from a code analysis
    /// dictionary file.
    /// </summary>
    public enum RecognizedWordHandling
    {
        /// <summary>None.  Recognized word elements are not loaded.</summary>
        None,
        /// <summary>Treat all as ignored words</summary>
        IgnoreAllWords,
        /// <summary>Add all words to the dictionary</summary>
        AddAllWords,
        /// <summary>The <c>Spelling</c> attribute on each word element determines its usage</summary>
        AttributeDeterminesUsage
    }
}
