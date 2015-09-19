//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : ResourceFileClassifier.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 09/10/2015
// Note    : Copyright 2015, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a class used to classify XML resource file content
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 09/10/2015  EFW  Created the code
//===============================================================================================================

using System;
using System.Xml;

using VisualStudio.SpellChecker.Configuration;

namespace VisualStudio.SpellChecker.ProjectSpellCheck
{
    /// <summary>
    /// This classifier is used to parse resource (.resx) files and exclude elements and comment text that should
    /// not be spell checked.
    /// </summary>
    internal class ResourceFileClassifier : XmlClassifier
    {
        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="filename">The filename to load</param>
        /// <param name="spellCheckConfiguration">The spell checker configuration for the file</param>
        public ResourceFileClassifier(string filename, SpellCheckerConfiguration spellCheckConfiguration) :
          base(filename, spellCheckConfiguration)
        {
        }
        #endregion

        #region Method overrides
        //=====================================================================

        /// <inheritdoc />
        /// <remarks>This classifier removes certain element names that appear in the comments that would
        /// otherwise cause false misspelling reports.</remarks>
        protected override string AdjustCommentText(string comments)
        {
            // Replace with equivalent lengths of spaces to keep the positions of all other words accurate
            return comments.Replace("mimetype", "        ").Replace("resheader", "         ").Replace(
                "microsoft-resx", "              ");
        }

        /// <inheritdoc />
        /// <remarks>This classifier ignores <c>data</c> and <c>metadata</c> elements that have either a
        /// <c>type</c> attribute with any value or a <c>mimetype</c> attribute containing "base64" in its
        /// value.  These never contain anything that should be spell checked.</remarks>
        protected override bool ShouldSkipElement(XmlReader reader)
        {
            if(reader.LocalName != "data" && reader.LocalName != "metadata")
                return false;

            string mimeType = reader.GetAttribute("mimetype");

            if(!String.IsNullOrWhiteSpace(mimeType) && mimeType.IndexOf("base64", StringComparison.Ordinal) != -1)
                return true;

            return !String.IsNullOrWhiteSpace(reader.GetAttribute("type"));
        }
        #endregion
    }
}
