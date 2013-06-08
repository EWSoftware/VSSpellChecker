//===============================================================================================================
// System  : Visual Studio Spell Checker Definitions
// File    : IBufferSpecificDictionaryProvider.cs
// Author  : Noah Richards, Roman Golovin, Michael Lehenbauer
// Updated : 04/14/2013
// Note    : Copyright 2010-2013, Microsoft Corporation, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains an interface that is used to retrieve the buffer-specific spelling dictionary for a given
// text buffer.
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code.  It can also be found at the project website: http://VSSpellChecker.CodePlex.com.
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
// Version     Date     Who  Comments
//===============================================================================================================
// 1.0.0.0  04/14/2013  EFW  Imported the code into the project
//===============================================================================================================

using Microsoft.VisualStudio.Text;

namespace VisualStudio.SpellChecker.Definitions
{
    /// <summary>
    /// This is used to retrieve the buffer-specific spelling dictionary for a given text buffer
    /// </summary>
    /// <exampe>
    /// This is a MEF component, and should be imported with the following code:
    /// <code>
    /// [ImportMany(typeof(IBufferSpecificDictionaryProvider))]
    /// private IEnumerable&lt;Lazy&lt;IBufferSpecificDictionaryProvider&gt;&gt; bufferSpecificDictionaryProviders = null;
    /// </code>
    /// </example>
    public interface IBufferSpecificDictionaryProvider
    {
        /// <summary>
        /// Get a buffer-specific dictionary
        /// </summary>
        /// <param name="buffer">The buffer</param>
        /// <returns>The buffer-specific dictionary or null if one is not provided</returns>
        ISpellingDictionary GetDictionary(ITextBuffer buffer);
    }
}
