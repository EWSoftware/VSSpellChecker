//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : PhpTextTaggerProvider.cs
// Authors : Miloslav Beňo (DevSense - http://www.devsense.com/)
// Updated : 06/03/2024
// Note    : Copyright 2016-2024, DevSense, All rights reserved
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
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace VisualStudio.SpellChecker.Tagging
{
    /// <summary>
    /// This class provides tags for PHP files when PHP Tools for Visual Studio are installed
    /// </summary>
    [Export(typeof(IViewTaggerProvider))]
    [ContentType("Phalanger")] // Because of legacy reasons ContentType is Phalanger, not PHP
    [TagType(typeof(NaturalTextTag))]
    internal class PhpTextTaggerProvider : IViewTaggerProvider
    {
        [Import]
        private readonly IViewClassifierAggregatorService classifierAggregatorService = null;

        /// <inheritdoc />
        public ITagger<T> CreateTagger<T>(ITextView view, ITextBuffer buffer) where T : ITag
        {
            if(view == null || buffer == null)
                return null;

            var classifier = classifierAggregatorService.GetClassifier(view);
#pragma warning disable VSTHRD010
            var config = SpellingServiceProxy.GetConfiguration(buffer);
#pragma warning restore VSTHRD010

            return new CommentTextTagger(buffer, classifier, null, null,
                config?.IgnoredClassificationsFor(buffer.ContentType.TypeName)) as ITagger<T>;
        }
    }
}
