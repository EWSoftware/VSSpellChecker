//===============================================================================================================
// System  : Visual Studio Spell Checker Definitions
// File    : IMisspellingTag.cs
// Authors : Noah Richards, Roman Golovin, Michael Lehenbauer
// Updated : 06/06/2014
// Note    : Copyright 2010-2014, Microsoft Corporation, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a class used to implement a misspelling tag
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code.  It can also be found at the project website: http://VSSpellChecker.CodePlex.com.
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 04/14/2013  EFW  Imported the code into the project
// 06/06/2014  EFW  Added support for doubled word tags
//===============================================================================================================

using System.Collections.Generic;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace VisualStudio.SpellChecker.Definitions
{
    /// <summary>
    /// This is used to implement a misspelling tag
    /// </summary>
    public interface IMisspellingTag : ITag
    {
        /// <summary>
        /// Returns true if this represents a doubled word rather than a misspelled word
        /// </summary>
        bool IsMisspelling { get; }

        /// <summary>
        /// Returns the delete word span for doubled words
        /// </summary>
        ITrackingSpan DeleteWordSpan { get; }

        /// <summary>
        /// Returns an enumerable list of suggestions for misspelled words
        /// </summary>
        IEnumerable<string> Suggestions { get; }
    }
}
