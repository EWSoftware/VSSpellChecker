//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : SuggestedActionBase.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 04/10/2019
// Note    : Copyright 2016-2019, Eric Woodruff, All rights reserved
//
// This file contains a class used as an abstract base class for suggested actions
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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace VisualStudio.SpellChecker.SuggestedActions
{
    /// <summary>
    /// This serves as an abstract base class for suggested actions that implements the common functionality
    /// </summary>
    internal abstract class SuggestedActionBase : ISuggestedAction2
    {
        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="displayText">The display text for the suggested action</param>
        /// <param name="span">The span upon which the action is taken</param>
        public SuggestedActionBase(string displayText, ITrackingSpan span)
        {
            this.DisplayText = displayText;
            this.Span = span;
        }
        #endregion

        #region Additional properties
        //=====================================================================

        /// <summary>
        /// This read-only property returns the tracking span upon which the action is taken
        /// </summary>
        public ITrackingSpan Span { get; }

        /// <summary>
        /// This is used to get a preview object for the action
        /// </summary>
        /// <remarks>The preview object must be recreated on each call as prior instances may be hosted in
        /// another control and cannot be reused.</remarks>
        public Func<object> Preview { get; set; }

        #endregion

        #region ISuggestedAction members
        //=====================================================================

        /// <inheritdoc />
        public string DisplayText { get; protected set; }

        /// <inheritdoc />
        public string DisplayTextSuffix { get; protected set; }

        /// <inheritdoc />
        /// <returns>This suggested action has no other action sets and always returns false</returns>
        public bool HasActionSets => false;

        /// <inheritdoc />
        public bool HasPreview => (this.Preview != null);

        /// <inheritdoc />
        /// <returns>This suggested action does not have any icon automation text and always returns null</returns>
        public string IconAutomationText => null;

        /// <inheritdoc />
        /// <returns>This suggested action does not have an icon moniker and always returns a default moniker</returns>
        public ImageMoniker IconMoniker => default;

        /// <inheritdoc />
        /// <returns>This suggested action does not have any input gesture text and always returns null</returns>
        public string InputGestureText => null;

        /// <inheritdoc />
        /// <remarks>This implementation does nothing</remarks>
        public void Dispose()
        {
        }

        /// <inheritdoc />
        /// <returns>This suggested action has no other action sets and always returns a null task</returns>
        public Task<IEnumerable<SuggestedActionSet>> GetActionSetsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IEnumerable<SuggestedActionSet>>(null);
        }

        /// <inheritdoc />
        public Task<object> GetPreviewAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(this.Preview != null ? this.Preview() : null);
        }

        /// <inheritdoc />
        public abstract void Invoke(CancellationToken cancellationToken);

        /// <inheritdoc />
        /// <returns>This action does not participate in telemetry logging and always returns false</returns>
        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }
        #endregion
    }
}
