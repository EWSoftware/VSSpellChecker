//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : SpellingDictionaryServiceFactory.cs
// Author  : Noah Richards, Roman Golovin, Michael Lehenbauer
// Updated : 05/01/2013
// Note    : Copyright 2010-2013, Microsoft Corporation, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a class that implements the spelling dictionary service factory
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code.  It can also be found at the project website: http://VSSpellChecker.CodePlex.com.
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
// Version     Date     Who  Comments
//===============================================================================================================
// 1.0.0.0  04/14/2013  EFW  Imported the code into the project
//
// Change History
// 04/30/2013 - EFW - Moved the global dictionary creation into the GlobalDictionary class
//===============================================================================================================

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Text;

using VisualStudio.SpellChecker.Definitions;

namespace VisualStudio.SpellChecker
{
    /// <summary>
    /// This class implements the spelling dictionary service factory
    /// </summary>
    [Export(typeof(ISpellingDictionaryService))]
    internal sealed class SpellingDictionaryServiceFactory : ISpellingDictionaryService
    {
        #region Private data members
        //=====================================================================

        // The ImportMany attribute causes the composition container to assign a value to this when an instance
        // is created.  It is not assigned to within this class.
        [ImportMany(typeof(IBufferSpecificDictionaryProvider))]
        private IEnumerable<Lazy<IBufferSpecificDictionaryProvider>> bufferSpecificDictionaryProviders = null;
        #endregion

        #region ISpellingDictionaryService Members
        //=====================================================================

        /// <inheritdoc />
        public ISpellingDictionary GetDictionary(ITextBuffer buffer)
        {
            ISpellingDictionary service = null;

            if(buffer.Properties.TryGetProperty(typeof(SpellingDictionaryService), out service))
                return service;

            List<ISpellingDictionary> bufferSpecificDictionaries = new List<ISpellingDictionary>();

            foreach(var provider in bufferSpecificDictionaryProviders)
            {
                var dictionary = provider.Value.GetDictionary(buffer);

                if(dictionary != null)
                    bufferSpecificDictionaries.Add(dictionary);
            }

            // Create or get the existing global dictionary for the default language
            var globalDictionary = GlobalDictionary.CreateGlobalDictionary(null);

            if(globalDictionary != null)
            {
                service = new SpellingDictionaryService(bufferSpecificDictionaries, globalDictionary);
                buffer.Properties[typeof(SpellingDictionaryService)] = service;
            }

            return service;
        }
        #endregion
    }
}
