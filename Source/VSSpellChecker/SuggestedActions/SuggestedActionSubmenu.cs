//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : SuggestedActionSubmenu.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 12/07/2016
// Note    : Copyright 2016, Eric Woodruff, All rights reserved
//
// This file contains a suggested action that serves as a submenu container for other suggested actions
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

namespace VisualStudio.SpellChecker.SuggestedActions
{
    /// <summary>
    /// This serves as a submenu container for other suggested actions
    /// </summary>
    internal class SuggestedActionSubmenu : ISuggestedAction
    {
        #region Private data members
        //=====================================================================

        private readonly IEnumerable<SuggestedActionSet> actions;

        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="displayText">The display text for the suggested action</param>
        /// <param name="actions">The actions to show in the submenu</param>
        public SuggestedActionSubmenu(string displayText, IEnumerable<SuggestedActionSet> actions)
        {
            this.DisplayText = displayText;
            this.actions = actions;
        }
        #endregion

        #region ISuggestedAction members
        //=====================================================================

        /// <inheritdoc />
        public string DisplayText { get; }

        /// <inheritdoc />
        /// <returns>This suggested action always has other action sets and always returns true</returns>
        public bool HasActionSets => true;

        /// <inheritdoc />
        /// <returns>This suggested action never has a preview and always returns false</returns>
        public bool HasPreview => false;

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
        public Task<IEnumerable<SuggestedActionSet>> GetActionSetsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(actions);
        }

        /// <inheritdoc />
        /// <returns>This suggested action has no preview and always returns a null task</returns>
        public Task<object> GetPreviewAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<object>(null);
        }

        /// <inheritdoc />
        /// <remarks>Since this serves as a container for other actions, this does nothing</remarks>
        public void Invoke(CancellationToken cancellationToken)
        {
        }

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
