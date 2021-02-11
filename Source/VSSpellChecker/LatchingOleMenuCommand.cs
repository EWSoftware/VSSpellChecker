//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : LatchingOleMenuCommand.cs
// Author  : Walter Goodwin  (walterpg@github)
// Created : 02/10/2021
// Note    : Copyright 2013-2021, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a class derived from OleMenuCommand to provide its missing "Latched" property.
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
//===============================================================================================================
//===============================================================================================================

using System;
using System.ComponentModel.Design;

using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;

namespace VisualStudio.SpellChecker
{
    internal class LatchingOleMenuCommand : OleMenuCommand
    {
        //
        // Summary:
        //     Builds a new LatchingOleMenuCommand.
        //
        // Parameters:
        //   invokeHandler:
        //     The event handler called to execute the command.
        //
        //   id:
        //     ID of the command.
        public LatchingOleMenuCommand(EventHandler invokeHandler, CommandID id)
            : base(invokeHandler, id)
        {
        }

        //
        // Summary:
        //     Builds a new LatchingOleMenuCommand.
        //
        // Parameters:
        //   invokeHandler:
        //     The event handler called to execute the command.
        //
        //   id:
        //     ID of the command.
        //
        //   Text:
        //     The text of the command.
        public LatchingOleMenuCommand(EventHandler invokeHandler, CommandID id, string Text) :
          base(invokeHandler, id, Text)
        {
        }

        //
        // Parameters:
        //   invokeHandler:
        //     The event handler called to execute the command.
        //
        //   changeHandler:
        //     The event handler called when the command's status changes.
        //
        //   id:
        //     ID of the command.
        public LatchingOleMenuCommand(EventHandler invokeHandler, EventHandler changeHandler, CommandID id) :
          base(invokeHandler, changeHandler, id)
        {
        }

        //
        // Parameters:
        //   invokeHandler:
        //     The event handler called to execute the command.
        //
        //   changeHandler:
        //     The event handler called when the command's status changes.
        //
        //   id:
        //     ID of the command.
        //
        //   Text:
        //     The text of the command.
        public LatchingOleMenuCommand(EventHandler invokeHandler, EventHandler changeHandler, CommandID id, string Text) :
          base(invokeHandler, changeHandler, id, Text)
        {
        }

        //
        // Parameters:
        //   invokeHandler:
        //     The event handler called to execute the command.
        //
        //   changeHandler:
        //     The event handler called when the command's status changes.
        //
        //   beforeQueryStatus:
        //     Event handler called when a client asks for the command status.
        //
        //   id:
        //     ID of the command.
        public LatchingOleMenuCommand(EventHandler invokeHandler, EventHandler changeHandler, EventHandler beforeQueryStatus,
            CommandID id) :
          base(invokeHandler, changeHandler, beforeQueryStatus, id)
        {
        }

        //
        // Parameters:
        //   invokeHandler:
        //     The event handler called to execute the command.
        //
        //   changeHandler:
        //     The event handler called when the command's status changes.
        //
        //   beforeQueryStatus:
        //     Event handler called when a client asks for the command status.
        //
        //   id:
        //     ID of the command.
        //
        //   Text:
        //     The text of the command.
        public LatchingOleMenuCommand(EventHandler invokeHandler, EventHandler changeHandler, EventHandler beforeQueryStatus,
            CommandID id, string Text) :
          base(invokeHandler, changeHandler, beforeQueryStatus, id, Text)
        {
        }

        public override int OleStatus
        {
            get
            {
                if(Latched)
                    return base.OleStatus | (int)OLECMDF.OLECMDF_LATCHED;
                else
                    return base.OleStatus & ~(int)OLECMDF.OLECMDF_LATCHED;
            }
        }

        public bool Latched { get; set; }
    }
}
