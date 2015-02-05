//=============================================================================
// System  : Visual Studio Spell Checker Package
// File    : SpellingConfigurationEditorFactory.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 02/06/2015
// Note    : Copyright 2015, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a class used generate spelling configuration file editor instances
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ============================================================================
// 02/06/2015  EFW  Created the code
//=============================================================================

using System.Runtime.InteropServices;

namespace VisualStudio.SpellChecker.Editors
{
    /// <summary>
    /// This is the factory class for spelling configuration file editors
    /// </summary>
    [Guid(GuidList.guidSpellingConfigurationEditorFactoryString)]
    public sealed class SpellingConfigurationEditorFactory : SimpleEditorFactory<SpellingConfigurationEditorPane>
    {
    }
}
