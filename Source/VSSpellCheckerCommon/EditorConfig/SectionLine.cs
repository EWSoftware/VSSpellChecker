//===============================================================================================================
// System  : Spell Check My Code Package
// File    : SectionLine.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 04/13/2023
// Note    : Copyright 2023, Eric Woodruff, All rights reserved
//
// This file contains the enumeration used to represent a line within an .editorconfig file section
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
//===============================================================================================================
// 01/30/2023  EFW  Created the code
//===============================================================================================================

using System;
using GlobExpressions;

namespace VisualStudio.SpellChecker.Common.EditorConfig
{
    /// <summary>
    /// This class is used to represent a line within an .editorconfig file section
    /// </summary>
    public class SectionLine
    {
        #region Private data members
        //=====================================================================

        private string lineText, fileGlob, propertyName, propertyValue;
        private LineType lineType;
        private Glob glob;

        #endregion

        #region Properties
        //=====================================================================

        /// <summary>
        /// This property is used to get or set the line text
        /// </summary>
        /// <value>Setting the line text will determine the line type and set the other properties.  Setting
        /// any of the other properties will update the line type and line text.</value>
        public string LineText
        {
            get => lineText;
            set
            {
                lineText = value;
                this.DetermineLineType();
            }
        }

        /// <summary>
        /// This read-only property returns the line type
        /// </summary>
        public LineType LineType => lineType;

        /// <summary>
        /// This is used to get or set the file glob for section headers
        /// </summary>
        public string FileGlob
        {
            get => fileGlob;
            set
            {
                fileGlob = value;
                lineText = $"[{fileGlob}]";
                lineType = LineType.SectionHeader;
            }
        }

        /// <summary>
        /// This is used to get or set the property name for properties
        /// </summary>
        public string PropertyName
        {
            get => propertyName;
            set
            {
                propertyName = value;
                lineText = $"{propertyName} = {propertyValue.EscapeEditorConfigValue()}";
                lineType = LineType.Property;
            }
        }

        /// <summary>
        /// This is used to get or set the property value for properties
        /// </summary>
        public string PropertyValue
        {
            get => propertyValue;
            set
            {
                propertyValue = value;
                lineText = $"{propertyName} = {propertyValue.EscapeEditorConfigValue()}";
                lineType = LineType.Property;
            }
        }

        /// <summary>
        /// An optional tag to mark the line in some way or store user-defined data for the editor to use
        /// </summary>
        public object Tag { get; set; }

        #endregion

        #region Constructors
        //=====================================================================

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <overloads>There are two overloads for the constructor</overloads>
        public SectionLine()
        {
            this.LineText = String.Empty;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="lineText">The section line's text</param>
        public SectionLine(string lineText)
        {
            this.LineText = lineText;
        }
        #endregion

        #region Helper methods
        //=====================================================================

        /// <summary>
        /// This is used to determine the line type
        /// </summary>
        private void DetermineLineType()
        {
            glob = null;
            fileGlob = propertyName = propertyValue = null;

            if(String.IsNullOrWhiteSpace(lineText))
            {
                lineType = LineType.Blank;
                return;
            }

            int idx = 0;

            lineType = LineType.None;

            while(idx < lineText.Length && lineType == LineType.None)
            {
                switch(lineText[idx])
                {
                    case '#':
                    case ';':
                        lineType = LineType.Comment;
                        break;

                    case '[':
                        lineType = LineType.SectionHeader;
                        fileGlob = lineText.Substring(idx + 1, lineText.Length - idx - 2);
                        glob = new Glob(fileGlob);
                        break;

                    case '=':
                        lineType = LineType.Property;
                        propertyName = lineText.Substring(0, idx).Trim();
                        propertyValue = lineText.Substring(idx + 1).Trim().UnescapeEditorConfigValue();
                        break;

                    default:
                        break;
                }

                idx++;
            }
        }

        /// <summary>
        /// For section headers, this is used to see if the given filename is a match for the file glob
        /// </summary>
        /// <param name="filename">The filename to check.  The path should be relative to the base folder for
        /// the project.</param>
        /// <returns>True if it matches, false if not.  If the glob does not contain a directory separator, the
        /// filename alone is used to make the match.  If a directory separator is present in the glob,
        /// subfolders in the filename being compared will be taken into consideration along with the filename.</returns>
        public bool IsMatchForFile(string filename)
        {
            if(glob != null && filename != null)
            {
                try
                {
                    return glob.IsMatch(filename);
                }
                catch(Exception ex)
                {
                    // Ignore exceptions, just treat it as if it was not a match
                    System.Diagnostics.Debug.WriteLine("Error parsing glob: {0}  Message: {1}", glob.Pattern, ex.Message);
                }
            }

            return false;
        }
        #endregion
    }
}
