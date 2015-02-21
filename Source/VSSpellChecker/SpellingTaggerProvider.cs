//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : SpellingTaggerProvider.cs
// Authors : Noah Richards, Roman Golovin, Michael Lehenbauer, Eric Woodruff
// Updated : 02/19/2015
// Note    : Copyright 2010-2015, Microsoft Corporation, All rights reserved
//           Portions Copyright 2013-2015, Eric Woodruff, All rights reserved
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

using VisualStudio.SpellChecker.Configuration;
using VisualStudio.SpellChecker.Definitions;
using VisualStudio.SpellChecker.Tagging;

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

        [Import]
        private IViewTagAggregatorFactoryService aggregatorFactory = null;

        [Import]
        private SpellingServiceFactory spellingService = null;
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
            SpellingTagger spellingTagger = null;

            // Make sure we are only tagging the top buffer
            if(textView == null || buffer == null || spellingService == null || textView.TextBuffer != buffer)
                return null;

            if(!textView.Properties.TryGetProperty(typeof(SpellingTagger), out spellingTagger))
            {
                // Getting the configuration determines if spell checking is enabled for this file
                var config = spellingService.GetConfiguration(buffer);

                if(config != null)
                {
                    var dictionary = spellingService.GetDictionary(buffer);

                    if(dictionary != null)
                    {
                        var naturalTextAggregator = aggregatorFactory.CreateTagAggregator<INaturalTextTag>(textView,
                            TagAggregatorOptions.MapByContentType);
                        var urlAggregator = aggregatorFactory.CreateTagAggregator<IUrlTag>(textView);

                        spellingTagger = new SpellingTagger(buffer, textView, naturalTextAggregator, urlAggregator,
                            config, dictionary);
                        textView.Properties[typeof(SpellingTagger)] = spellingTagger;
                    }
                }
            }

            return spellingTagger as ITagger<T>;
        }
        #endregion
    }
}
