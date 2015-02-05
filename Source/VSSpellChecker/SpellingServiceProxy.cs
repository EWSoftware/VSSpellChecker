//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : SpellingServiceProxy.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 02/19/2015
// Note    : Copyright 2015, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a class that implements the spelling service interface to expose the spell checker to
// third-party tagger providers.
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 02/19/2015  EFW  Created the code
//===============================================================================================================

using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Text;

using VisualStudio.SpellChecker.Definitions;

namespace VisualStudio.SpellChecker
{
    /// <summary>
    /// This class implements the spelling service interface to expose the spell checker to third-party tagger
    /// providers
    /// </summary>
    [Export(typeof(ISpellingService))]
    internal sealed class SpellingServiceProxy : ISpellingService
    {
        #region Private data members
        //=====================================================================

        [Import]
        private SpellingServiceFactory spellingService = null;

        #endregion

        #region ISpellingService Members

        /// <inheritdoc />
        public bool IsEnabled(ITextBuffer buffer)
        {
            // Getting the configuration determines if spell checking is enabled for this file
            return (buffer != null && spellingService != null && spellingService.GetConfiguration(buffer) != null);
        }
        #endregion
    }
}
