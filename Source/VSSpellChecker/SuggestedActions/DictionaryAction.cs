//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : DictionaryAction.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 03/12/2020
// Note    : Copyright 2013-2020, Eric Woodruff, All rights reserved
//
// This file contains an enumerated type that defines the various dictionary actions that can be taken
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 05/25/2013  EFW  Created the code
//===============================================================================================================

namespace VisualStudio.SpellChecker.SuggestedActions
{
    /// <summary>
    /// This enumerated type defines the various dictionary actions that can be taken
    /// </summary>
    public enum DictionaryAction
    {
        /// <summary>Ignore the current instance of the misspelled word</summary>
        IgnoreOnce,
        /// <summary>Ignore all instances of the misspelled word</summary>
        IgnoreAll,
        /// <summary>Add the word to the user dictionary</summary>
        AddWord
    }
}
