//===============================================================================================================
// System  : Spell Check My Code Package
// File    : PlainTextClassifier.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 08/30/2025
// Note    : Copyright 2015-2025, Eric Woodruff, All rights reserved
//
// This file contains a class used to classify plain text file content
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 08/26/2015  EFW  Created the code
//===============================================================================================================

using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.Text;

using VisualStudio.SpellChecker.Common.Configuration;

namespace VisualStudio.SpellChecker.ProjectSpellCheck
{
    /// <summary>
    /// This class is used to classify plain text file content
    /// </summary>
    /// <remarks>This one is as simple as it gets.  It simply returns the entire file contents.</remarks>
    internal class PlainTextClassifier : TextClassifier
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="filename">The filename to load</param>
        /// <param name="spellCheckConfiguration">The spell checker configuration for the file</param>
        public PlainTextClassifier(string filename, SpellCheckerConfiguration spellCheckConfiguration) :
          base(filename, spellCheckConfiguration)
        {
        }

        /// <inheritdoc />
        public override IEnumerable<SpellCheckSpan> Parse()
        {
            // This would be odd but it is possible to do
            if(this.IgnoredClassifications.Contains(RangeClassification.PlainText))
                return [];

            return 
            [
                new SpellCheckSpan
                {
                    Span = new Span(0, this.Text.Length),
                    Text = this.Text,
                    Classification = RangeClassification.PlainText
                }
            ];
        }
    }
}
