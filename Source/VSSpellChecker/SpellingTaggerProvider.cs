//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : SpellingTaggerProvider.cs
// Authors : Noah Richards, Roman Golovin, Michael Lehenbauer, Eric Woodruff
// Updated : 06/06/2014
// Note    : Copyright 2010-2014, Microsoft Corporation, All rights reserved
//           Portions Copyright 2013-2014, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a class used to create the spelling tagger
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
//===============================================================================================================

using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

using VisualStudio.SpellChecker.Definitions;

namespace VisualStudio.SpellChecker
{
    /// <summary>
    /// This class is used to create the spelling tagger
    /// </summary>
    [Export(typeof(IViewTaggerProvider)), ContentType("any"), TagType(typeof(MisspellingTag))]
    internal sealed class SpellingTaggerProvider : IViewTaggerProvider
    {
        #region Private data members
        //=====================================================================

        // The Import attribute causes the composition container to assign a value to this when an instance is
        // created.  It is not assigned to within this class.
        [Import]
        private IViewTagAggregatorFactoryService aggregatorFactory = null;

        [Import]
        private ISpellingDictionaryService spellingDictionaryFactory = null;
        #endregion

        #region IViewTaggerProvider Members
        //=====================================================================

        /// <summary>
        /// Creates a tag provider for the specified view and buffer
        /// </summary>
        /// <typeparam name="T">The tag type</typeparam>
        /// <param name="textView">The text view</param>
        /// <param name="buffer">The text buffer</param>
        /// <returns>The tag provider for the specified view and buffer or null if the buffer does not match the
        /// one in the view or spell checking as you type is disabled.</returns>
        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            SpellingTagger spellingTagger;

            // Make sure we only tagging top buffer and only if wanted
            if(textView.TextBuffer != buffer || !SpellCheckerConfiguration.SpellCheckAsYouType ||
              SpellCheckerConfiguration.IsExcludedByExtension(buffer.GetFilenameExtension()))
                return null;

            if(textView.Properties.TryGetProperty(typeof(SpellingTagger), out spellingTagger))
                return spellingTagger as ITagger<T>;

            var dictionary = spellingDictionaryFactory.GetDictionary(buffer);

            if(dictionary == null)
                return null;

            var naturalTextAggregator = aggregatorFactory.CreateTagAggregator<INaturalTextTag>(textView,
                TagAggregatorOptions.MapByContentType);
            var urlAggregator = aggregatorFactory.CreateTagAggregator<IUrlTag>(textView);

            spellingTagger = new SpellingTagger(buffer, textView, naturalTextAggregator, urlAggregator, dictionary);
            textView.Properties[typeof(SpellingTagger)] = spellingTagger;

            return spellingTagger as ITagger<T>;
        }
        #endregion
    }
}
