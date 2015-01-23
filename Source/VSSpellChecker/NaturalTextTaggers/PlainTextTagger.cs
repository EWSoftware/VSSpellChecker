//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : PlainTextTagger.cs
// Authors : Noah Richards, Roman Golovin, Michael Lehenbauer, Eric Woodruff
// Updated : 06/06/2014
// Note    : Copyright 2010-2014, Microsoft Corporation, All rights reserved
//           Portions Copyright 2013-2014, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a class used to provide tags for plain text files
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 04/14/2013  EFW  Imported the code into the project
// 04/26/2013  EFW  Added support for disabling spell checking as you type
// 06/06/2014  EFW  Added support for excluding from spell checking by filename extension
//===============================================================================================================

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

using VisualStudio.SpellChecker;

namespace VisualStudio.SpellChecker.NaturalTextTaggers
{
    /// <summary>
    /// This class provides tags for plain text files
    /// </summary>
    internal class PlainTextTagger : ITagger<NaturalTextTag>
    {
        #region Private Fields
        //=====================================================================

        private ITextBuffer _buffer;
        #endregion

        #region MEF Imports / Exports
        //=====================================================================

        /// <summary>
        /// Plain text tagger provider
        /// </summary>
        [Export(typeof(ITaggerProvider)), ContentType("plaintext"), ContentType("text"),
          TagType(typeof(NaturalTextTag))]
        internal class PlainTextTaggerProvider : ITaggerProvider
        {
            /// <summary>
            /// Creates a tag provider for the specified buffer
            /// </summary>
            /// <typeparam name="T">The tag type</typeparam>
            /// <param name="buffer">The text buffer</param>
            /// <returns>The tag provider for the specified buffer or null if the buffer is null or spell
            /// checking as you type is disabled.</returns>
            public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
            {
                // If no buffer, not enabled, excluded by extension, or the content type is one of the more
                // derived types, don't use this one.
                if(buffer == null || !SpellCheckerConfiguration.SpellCheckAsYouType ||
                  SpellCheckerConfiguration.IsExcludedByExtension(buffer.GetFilenameExtension()) ||
                  buffer.ContentType.IsOfType("code") || buffer.ContentType.IsOfType("html"))
                    return null;

                return new PlainTextTagger(buffer) as ITagger<T>;
            }
        }
        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor for Natural Text Tagger.
        /// </summary>
        /// <param name="buffer">Relevant buffer.</param>
        public PlainTextTagger(ITextBuffer buffer)
        {
            _buffer = buffer;
        }
        #endregion

        #region ITagger<INaturalTextTag> Members
        //=====================================================================

        /// <inheritdoc />
        public IEnumerable<ITagSpan<NaturalTextTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            foreach(var snapshotSpan in spans)
                yield return new TagSpan<NaturalTextTag>(snapshotSpan, new NaturalTextTag());
        }

#pragma warning disable 67
        /// <inheritdoc />
        /// <remarks>This event is not used by this tagger</remarks>
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
#pragma warning restore 67

        #endregion
    }
}
