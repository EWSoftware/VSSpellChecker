//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : SpellSmartTag.cs
// Authors : Noah Richards, Roman Golovin, Michael Lehenbauer
// Updated : 04/14/2013
// Note    : Copyright 2010-2013, Microsoft Corporation, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a class used to represent a spelling smart tag
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 04/14/2013  EFW  Imported the code into the project
//===============================================================================================================

using System.Collections.ObjectModel;

using Microsoft.VisualStudio.Language.Intellisense;

namespace VisualStudio.SpellChecker.SmartTags
{
    /// <summary>
    /// This class is used to represent a spelling smart tag
    /// </summary>
    internal class SpellSmartTag : SmartTag
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="actionSets">The action sets for the smart tag</param>
        public SpellSmartTag(ReadOnlyCollection<SmartTagActionSet> actionSets) :
          base(SmartTagType.Factoid, actionSets)
        {
        }
    }
}
