//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : CommandTargetArguments.cs
// Author  : Istvan Novak
// Updated : 04/27/2023
// Source  : http://learnvsxnow.codeplex.com/
// Note    : Copyright 2008-2023, Istvan Novak, All rights reserved
//
// This file contains classes that define event arguments used by the SimpleEditorPane class
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 02/06/2015  EFW  Added the code to the project
//===============================================================================================================

// Ignore Spelling: pva

using System;
using Microsoft.VisualStudio.OLE.Interop;

namespace VisualStudio.SpellChecker.Editors
{
    #region ExecArgs class
    //=====================================================================

    /// <summary>
    /// This class represents the arguments of an IOleCommandTarget.Exec method.
    /// </summary>
    public class ExecArgs
    {
        /// <summary>
        /// Gets the ID of the command group.
        /// </summary>
        public Guid GroupId { get; }

        /// <summary>
        /// Gets the ID of the command within the group.
        /// </summary>
        public uint CommandId { get; }

        /// <summary>
        /// Options for the command
        /// </summary>
        public uint CommandExecOpt { get; set; }

        /// <summary>
        /// Pointer to input arguments
        /// </summary>
        public IntPtr PvaIn { get; set; }

        /// <summary>
        /// Pointer to output arguments
        /// </summary>
        public IntPtr PvaOut { get; set; }

        /// <summary>
        /// Creates a new instance of this class with the specified command identifiers.
        /// </summary>
        /// <param name="groupId">ID of the command group.</param>
        /// <param name="commandId">ID of the command within the group.</param>
        public ExecArgs(Guid groupId, uint commandId)
        {
            GroupId = groupId;
            CommandId = commandId;
        }
    }
    #endregion

    #region QueryStatusArgs class
    //=====================================================================

    /// <summary>
    /// This class represents the arguments of an IOleCommandTarget.QueryStatus method.
    /// </summary>
    public sealed class QueryStatusArgs
    {
        /// <summary>
        /// The group ID
        /// </summary>
        public Guid GroupId { get; }

        /// <summary>
        /// The command count
        /// </summary>
        public uint CommandCount { get; set; }

        /// <summary>
        /// The commands
        /// </summary>
        public OLECMD[] Commands { get; set; }

        /// <summary>
        /// The command text
        /// </summary>
        public IntPtr PCmdText { get; set; }

        /// <summary>
        /// The first command ID
        /// </summary>
        public uint FirstCommandId => this.Commands[0].cmdID;

        /// <summary>
        /// The first command status
        /// </summary>
        public uint FirstCommandStatus
        {
            get => this.Commands[0].cmdf;
            set => this.Commands[0].cmdf = value;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="groupId">The group ID</param>
        public QueryStatusArgs(Guid groupId)
        {
            this.GroupId = groupId;
        }
    }
    #endregion
}
