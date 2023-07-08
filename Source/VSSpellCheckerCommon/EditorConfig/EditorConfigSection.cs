//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : EditorConfigSection.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 04/11/2023
// Note    : Copyright 2023, Eric Woodruff, All rights reserved
//
// This file contains the class used to represent and .editorconfig file section
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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace VisualStudio.SpellChecker.Common.EditorConfig
{
    /// <summary>
    /// This class is used to represent and .editorconfig file section
    /// </summary>
    public class EditorConfigSection
    {
        #region Properties
        //=====================================================================

        /// <summary>
        /// This read-only property returns true if the section represents a file section, false if not
        /// </summary>
        public bool IsFileSection => this.SectionLines.Count != 0 &&
            this.SectionLines.Any(l => l.LineType == LineType.SectionHeader);

        /// <summary>
        /// This read-only property is used to see if this section contains the <c>is_global</c> property and it
        /// is set to true.
        /// </summary>
        public bool IsGlobal
        {
            get
            {
                if(this.IsFileSection)
                    return false;

                var rootProperty = this.SectionLines.FirstOrDefault(l => l.LineType == LineType.Property &&
                    l.PropertyName.Equals("is_global", StringComparison.OrdinalIgnoreCase));

                return rootProperty?.PropertyValue?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;
            }
        }

        /// <summary>
        /// This read-only property is used to get the global level property if this is a global file
        /// </summary>
        /// <value>Returns the global level value if the property exists, null if not</value>
        public int? GlobalLevel
        {
            get
            {
                if(this.IsFileSection)
                    return null;

                var levelProperty = this.SectionLines.FirstOrDefault(l => l.LineType == LineType.Property &&
                    l.PropertyName.Equals("global_level", StringComparison.OrdinalIgnoreCase));

                if(levelProperty == null || !Int32.TryParse(levelProperty.PropertyValue, out int level))
                    return null;

                return level;
            }
        }

        /// <summary>
        /// This read-only property is used to see if this section contains the <c>root</c> property and it is
        /// set to true.
        /// </summary>
        public bool IsRoot
        {
            get
            {
                if(this.IsFileSection)
                    return false;

                var rootProperty = this.SectionLines.FirstOrDefault(l => l.LineType == LineType.Property &&
                    l.PropertyName.Equals("root", StringComparison.OrdinalIgnoreCase));

                return rootProperty?.PropertyValue?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;
            }
        }

        /// <summary>
        /// This is used to get the section header containing the file glob
        /// </summary>
        /// <value>Returns the section header line if this is a file section or null if not</value>
        public SectionLine SectionHeader => this.IsFileSection ?
            this.SectionLines.First(l => l.LineType == LineType.SectionHeader) : null;

        /// <summary>
        /// This read-only property returns the section lines
        /// </summary>
        public Collection<SectionLine> SectionLines { get; }

        /// <summary>
        /// This read-only property returns an enumerable list of all spell checker properties in the section
        /// </summary>
        public IEnumerable<SectionLine> SpellCheckerProperties => this.SectionLines.Where(
            l => l.LineType == LineType.Property && l.PropertyName.StartsWith("vsspell_", StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// This read-only property returns an enumerable list of all spell checker comments in the section
        /// </summary>
        /// <remarks>The comments are prefixed with "VSSPELL:".  Typically there will only be one but there
        /// can be multiple comments after conversion or if the user adds them.  When saved, the comments will
        /// be merged into one single comment line.</remarks>
        public IEnumerable<SectionLine> SpellCheckerComments => this.SectionLines.Where(
            l => l.LineType == LineType.Comment && l.LineText.StartsWith("# VSSPELL:", StringComparison.OrdinalIgnoreCase));

        #endregion

        #region Constructors
        //=====================================================================

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <overloads>There are two overloads for the constructor</overloads>
        public EditorConfigSection()
        {
            this.SectionLines = new Collection<SectionLine>();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="lines">An enumerable list of section lines</param>
        public EditorConfigSection(IEnumerable<SectionLine> lines)
        {
            this.SectionLines = new Collection<SectionLine>(lines.ToList());
        }

        /// <summary>
        /// Constructor (used when parsing .editorconfig files)
        /// </summary>
        /// <param name="lines">A collection of section lines</param>
        internal EditorConfigSection(Collection<SectionLine> lines)
        {
            this.SectionLines = lines;
        }
        #endregion

        #region Helper methods
        //=====================================================================

        /// <summary>
        /// This is used to see if the section is a match for the given file
        /// </summary>
        /// <param name="filename">The filename to check</param>
        /// <returns>True if this is a section header and the file is a match for the file glob</returns>
        public bool IsMatchForFile(string filename)
        {
            return this.IsFileSection && this.SectionLines.First(
                l => l.LineType == LineType.SectionHeader).IsMatchForFile(filename);
        }
        #endregion
    }
}
