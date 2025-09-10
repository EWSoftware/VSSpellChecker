//===============================================================================================================
// System  : Spell Check My Code Package
// File    : Guids.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 01/08/2017
// Note    : Copyright 2013-2017, Eric Woodruff, All rights reserved
//
// This file contains various GUIDs for the package
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 05/20/2013  EFW  Created the code
//===============================================================================================================

using System;

namespace VisualStudio.SpellChecker
{
    /// <summary>
    /// This class defines the GUIDs for the package
    /// </summary>
    internal static class GuidList
    {
        /// <summary>
        /// VSSpellChecker Package GUID (string form)
        /// </summary>
        public const string guidVSSpellCheckerPkgString = "86b8a6ea-6a96-4e31-b31d-943e86581421";
        /// <summary>
        /// VSSpellCheckEverywhere Package GUID (string form)
        /// </summary>
        public const string guidVSSpellCheckEverywherePkgString = "A447DC7A-A901-442C-B183-87DCBF015C1E";
        /// <summary>
        /// Command set GUID (string form) - VS2017 and later package
        /// </summary>
        public const string guidVSSpellCheckerCmdSetString = "43EA967E-0DE2-4136-8E52-C6DCFB5C2748";
        /// <summary>
        /// Command set GUID
        /// </summary>
        public static readonly Guid guidVSSpellCheckerCmdSet = new(guidVSSpellCheckerCmdSetString);
        /// <summary>
        /// Spelling configuration file editor factory GUID string
        /// </summary>
        public const string guidSpellingConfigurationEditorFactoryString = "837501D0-C07D-47C6-AAB7-9BA4D78D0038";
    };
}
