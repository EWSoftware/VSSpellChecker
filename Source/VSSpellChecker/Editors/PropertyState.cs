﻿//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : PropertyState.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 02/07/2015
// Note    : Copyright 2015, Eric Woodruff, All rights reserved
//
// This file contains an enumerated type that defines the various configuration property states
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 02/07/2015  EFW  Created the code
//===============================================================================================================

namespace VisualStudio.SpellChecker.Editors
{
    /// <summary>
    /// This enumerated type defines the available configuration property states
    /// </summary>
    public enum PropertyState
    {
        /// <summary>
        /// Inherit the setting from the prior settings level
        /// </summary>
        Inherited,
        /// <summary>
        /// The property is enabled
        /// </summary>
        Yes,
        /// <summary>
        /// The property is disabled
        /// </summary>
        No
    }
}
