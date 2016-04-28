//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : PhpTextTaggerProvider.cs
// Authors : Noah Richards, Roman Golovin, Michael Lehenbauer, Eric Woodruff
// Updated : 04/28/2016
// Note    : Copyright 2010-2015, Microsoft Corporation, All rights reserved
//           Portions Copyright 2013-2015, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a class that represents provider for php text tagger
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 04/14/2016  MB   Added support for PHP files in the editor
//===============================================================================================================


using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

using VisualStudio.SpellChecker.Definitions;
using Microsoft.VisualStudio.Text.Classification;
using VisualStudio.SpellChecker.Tagging;

namespace VisualStudio.SpellChecker.Tagging
{
	/// <summary>
	/// This class provides text tagger PHP Code when PHP Tools for Visual Studio are installed
	/// </summary>
	[Export(typeof(ITaggerProvider))]
	[ContentType("Phalanger")] // Because of legacy reasons ContentType is Phalanger, not PHP
	[TagType(typeof(NaturalTextTag))]
	class PhpTextTaggerProvider : ITaggerProvider
	{
		[Import]
		private IClassifierAggregatorService classifierAggregatorService = null;

		/// <inheritdoc />
		public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
		{
			var classifier = classifierAggregatorService.GetClassifier(buffer);

			//Use existing comment text tagger, it works well with PHP classifier
			return new CommentTextTagger(buffer, classifier, null, null) as ITagger<T>;
		}
	}
}