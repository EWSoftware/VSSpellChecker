//===============================================================================================================
// System  : Visual Studio Spell Checker Definitions
// File    : INaturalTextTag.cs
// Authors : Noah Richards, Roman Golovin, Michael Lehenbauer
// Updated : 04/14/2013
// Note    : Copyright 2010-2013, Microsoft Corporation, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains an interface used to represent a tag for natural text regions
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code.  It can also be found at the project website: http://VSSpellChecker.CodePlex.com.
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 04/14/2013  EFW  Imported the code into the project
//===============================================================================================================

using Microsoft.VisualStudio.Text.Tagging;

namespace VisualStudio.SpellChecker.Definitions
{
    /// <summary>
    /// This represents a tag for natural text regions
    /// </summary>
    public interface INaturalTextTag : ITag
    {
    }
}
