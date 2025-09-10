//===============================================================================================================
// System  : Spell Check My Code Package
// File    : ConvertedConfiguration.cs
// Authors : Eric Woodruff  (Eric@EWoodruff.us), Franz Alex Gaisie-Essilfie
// Updated : 03/16/2023
// Note    : Copyright 2023, Eric Woodruff, All rights reserved
//
// This file contains the class used to contain information about a converted spell checker configuration file
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who   Comments
// ==============================================================================================================
// 03/16/2023  EFW   Created the code
//===============================================================================================================

using System.Collections.Generic;
using System.Linq;

using VisualStudio.SpellChecker.Common.Configuration.Legacy;
using VisualStudio.SpellChecker.Common.EditorConfig;

namespace VisualStudio.SpellChecker.ToolWindows
{
    /// <summary>
    /// This class is used to contain information about a converted configuration file
    /// </summary>
    public class ConvertedConfiguration
    {
        #region Properties
        //=====================================================================

        /// <summary>
        /// This read-only property returns the legacy configuration
        /// </summary>
        public SpellCheckerLegacyConfiguration LegacyConfiguration { get; }

        /// <summary>
        /// This read-only property returns an enumerable list of the .editorconfig sections to merge
        /// </summary>
        public IEnumerable<EditorConfigSection> Sections { get; }

        #endregion

        #region Constructor
        //=====================================================================
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="legacyConfigurationFilename">The legacy configuration filename</param>
        public ConvertedConfiguration(string legacyConfigurationFilename)
        {
            this.LegacyConfiguration = new SpellCheckerLegacyConfiguration(legacyConfigurationFilename);
            this.Sections = [.. this.LegacyConfiguration.ConvertLegacyConfiguration()];
        }
        #endregion

    }
}
