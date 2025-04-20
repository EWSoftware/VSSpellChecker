//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : DoubledWordSuggestedAction.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 12/07/2016
// Note    : Copyright 2016, Eric Woodruff, All rights reserved
//
// This file contains a class used to provide a suggested action for deleting doubled words
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 12/06/2016  EFW  Created the code
//===============================================================================================================

using System;
using System.Threading;

using Microsoft.VisualStudio.Text;

namespace VisualStudio.SpellChecker.SuggestedActions
{
    /// <summary>
    /// Suggested action for deleting doubled words
    /// </summary>
    internal class DoubledWordSuggestedAction : SuggestedActionBase
    {
        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="span">The word span to delete</param>
        public DoubledWordSuggestedAction(ITrackingSpan span) : base("Delete Word", span)
        {
        }
        #endregion

        #region Abstract method implementations
        //=====================================================================

        /// <inheritdoc />
        public override void Invoke(CancellationToken cancellationToken)
        {
            this.Span.TextBuffer.Replace(this.Span.GetSpan(this.Span.TextBuffer.CurrentSnapshot), String.Empty);
        }
        #endregion
    }
}
