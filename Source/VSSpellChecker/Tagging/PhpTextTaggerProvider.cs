﻿//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : PhpTextTaggerProvider.cs
// Authors : Miloslav Beňo (DevSense - http://www.devsense.com/)
// Updated : 08/15/2018
// Note    : Copyright 2016-2018, DevSense, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a class used to provide tags for PHP files when PHP Tools for Visual Studio are installed
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 04/14/2016  MB   Added support for PHP files in the editor
// 08/17/2018  EFW  Added support for tracking and excluding classifications using the classification cache
//===============================================================================================================

using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace VisualStudio.SpellChecker.Tagging
{
    /// <summary>
    /// This class provides tags for PHP files when PHP Tools for Visual Studio are installed
    /// </summary>
    [Export(typeof(ITaggerProvider))]
    [ContentType("Phalanger")] // Because of legacy reasons ContentType is Phalanger, not PHP
    [TagType(typeof(NaturalTextTag))]
    class PhpTextTaggerProvider : ITaggerProvider
    {
        [Import]
        private IClassifierAggregatorService classifierAggregatorService = null;

        [Import]
        private SpellingServiceFactory spellingService = null;

        /// <inheritdoc />
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            var classifier = classifierAggregatorService.GetClassifier(buffer);
#pragma warning disable VSTHRD010
            var config = spellingService.GetConfiguration(buffer);
#pragma warning restore VSTHRD010

            // Use existing comment text tagger, it works well with PHP classifier
            if(config == null)
                return new CommentTextTagger(buffer, classifier, null, null, null) as ITagger<T>;

            return new CommentTextTagger(buffer, classifier, null, null,
                config.IgnoredClassificationsFor(buffer.ContentType.TypeName)) as ITagger<T>;
        }
    }
}
