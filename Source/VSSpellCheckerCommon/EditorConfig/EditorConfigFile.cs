//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : EditorConfigFile.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 03/20/2023
// Note    : Copyright 2023, Eric Woodruff, All rights reserved
//
// This file contains the class used to load and manage an .editorconfig file
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
using System.IO;
using System.Linq;

namespace VisualStudio.SpellChecker.Common.EditorConfig
{
    /// <summary>
    /// This class is used to load and manage an .editorconfig file
    /// </summary>
    public class EditorConfigFile
    {
        #region Properties
        //=====================================================================

        /// <summary>
        /// This property is used to get or set the .editorconfig filename
        /// </summary>
        public string Filename { get; set; }

        /// <summary>
        /// This read-only property is used to get the sections of the .editorconfig file
        /// </summary>
        public Collection<EditorConfigSection> Sections { get; }

        /// <summary>
        /// This read-only property returns true if this is a global file, false if not
        /// </summary>
        public bool IsGlobal => this.Filename != null && Path.GetFileName(this.Filename).Equals(".globalconfig",
            StringComparison.OrdinalIgnoreCase) || this.Sections.Count != 0 && this.Sections[0].IsGlobal;

        /// <summary>
        /// This read-only property is used to get the global level value if this is a global file
        /// </summary>
        /// <value>Returns the global level value if the property exists.  If not and this is the .globalconfig
        /// configuration file, it returns 100.  Otherwise, it returns zero.</value>
        public int GlobalLevel
        {
            get
            {
                int? level = null;

                if(this.IsGlobal && this.Sections.Count != 0)
                    level = this.Sections[0].GlobalLevel;

                if(level == null && this.Filename != null && Path.GetFileName(this.Filename).Equals(".globalconfig",
                  StringComparison.OrdinalIgnoreCase))
                {
                    level = 100;
                }

                return level ?? 0;
            }
        }

        /// <summary>
        /// This read-only property returns true if this is a root file, false if not
        /// </summary>
        public bool IsRoot => this.Sections.Count != 0 && this.Sections[0].IsRoot;

        /// <summary>
        /// Get the section that has the file glob matching the one specified
        /// </summary>
        /// <param name="fileGlob">The file glob to match</param>
        /// <returns>The matching section or null if one is not found</returns>
        public EditorConfigSection this[string fileGlob] => this.Sections.FirstOrDefault(s => s.IsFileSection &&
            s.SectionLines.First(l => l.LineType == LineType.SectionHeader).FileGlob.Equals(fileGlob, StringComparison.OrdinalIgnoreCase));

        #endregion

        #region Constructors
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        public EditorConfigFile()
        {
            this.Sections = new Collection<EditorConfigSection>();
        }
        #endregion

        #region Helper methods
        //=====================================================================

        /// <summary>
        /// This is used to create an .editorconfig instance from a file
        /// </summary>
        /// <param name="filename">The filename to load</param>
        /// <returns>An .editorconfig instance containing the settings from the file</returns>
        public static EditorConfigFile FromFile(string filename)
        {
            var config = new EditorConfigFile { Filename = filename };

            if(!String.IsNullOrWhiteSpace(filename) && File.Exists(filename))
                config.Parse(File.ReadAllLines(filename));

            return config;
        }

        /// <summary>
        /// This is used to create an .editorconfig instance from a block of text
        /// </summary>
        /// <param name="text">The text to load</param>
        /// <returns>An .editorconfig instance containing the settings from the given text</returns>
        public static EditorConfigFile FromText(string text)
        {
            var config = new EditorConfigFile();

            if(!String.IsNullOrWhiteSpace(text))
            {
                var lines = new List<string>();

                using(var sr = new StringReader(text))
                {
                    string line;

                    do
                    {
                        line = sr.ReadLine();

                        if(line != null)
                            lines.Add(line);

                    } while(line != null);
                }

                config.Parse(lines);
            }

            return config;
        }

        /// <summary>
        /// Parse the given .editorconfig file
        /// </summary>
        /// <param name="editorConfigLines">The .editorconfig file lines to parse</param>
        private void Parse(IEnumerable<string> editorConfigLines)
        {
            this.Sections.Clear();

            var sectionLines = new Collection<SectionLine>();

            foreach(var line in editorConfigLines)
            {
                int idx = 0;

                while(idx < line.Length)
                {
                    if(!Char.IsWhiteSpace(line[idx]))
                        break;

                    idx++;
                }

                // New section?
                if(idx < line.Length && line[idx] == '[')
                {
                    if(sectionLines.Count != 0)
                    {
                        var priorSectionLines = sectionLines;
                            
                        sectionLines = new Collection<SectionLine>();

                        // Comments immediately before the new section are most likely associated with the
                        // next section so keep them together.
                        while(priorSectionLines.Count != 0 &&
                            priorSectionLines[priorSectionLines.Count - 1].LineType == LineType.Comment)
                        {
                            var commentLine = priorSectionLines[priorSectionLines.Count - 1];

                            sectionLines.Insert(0, commentLine);
                            priorSectionLines.RemoveAt(priorSectionLines.Count - 1);
                        }

                        if(priorSectionLines.Count != 0)
                            this.Sections.Add(new EditorConfigSection(priorSectionLines));
                    }

                    sectionLines.Add(new SectionLine(line));
                }
                else
                    sectionLines.Add(new SectionLine(line));
            }

            if(sectionLines.Count != 0)
                this.Sections.Add(new EditorConfigSection(sectionLines));
        }

        /// <summary>
        /// Save the contents of the .editorconfig file
        /// </summary>
        public void Save()
        {
            if(String.IsNullOrWhiteSpace(this.Filename))
                throw new InvalidOperationException("A filename is required to save the .editorconfig settings");

            File.WriteAllLines(this.Filename, this.Sections.SelectMany(s => s.SectionLines.Select(l => l.LineText)));
        }

        /// <summary>
        /// This returns an enumerable list of sections for the given filename
        /// </summary>
        /// <param name="filename">The filename to check</param>
        /// <returns>An enumerable list of sections for the file</returns>
        public IEnumerable<EditorConfigSection> SectionsForFile(string filename)
        {
            foreach(var s in this.Sections.Where(s => s.IsFileSection && s.IsMatchForFile(filename)))
                yield return s;
        }

        /// <summary>
        /// This is used to get an enumerable list of .globalconfig files starting in the given path and moving
        /// up to the root of the file system.
        /// </summary>
        /// <param name="path">The starting path</param>
        /// <returns>An enumerable list of .globalconfig files if any are found</returns>
        public static IEnumerable<string> GlobalConfigFilesIn(string path)
        {
            if(!String.IsNullOrWhiteSpace(path))
            {
                if(path[path.Length - 1] == Path.DirectorySeparatorChar)
                    path = path.Substring(0, path.Length - 2);

                while(path != null)
                {
                    string configFile = Path.Combine(path, ".globalconfig");

                    if(File.Exists(configFile))
                        yield return configFile;

                    path = Path.GetDirectoryName(path);
                }
            }
        }

        /// <summary>
        /// This is used to get an enumerable list of .editorconfig files starting in the given path and moving
        /// up to the root of the file system.
        /// </summary>
        /// <param name="path">The starting path</param>
        /// <returns>An enumerable list of .editorconfig files if any are found</returns>
        public static IEnumerable<string> EditorConfigFilesIn(string path)
        {
            if(!String.IsNullOrWhiteSpace(path))
            {
                if(path[path.Length - 1] == Path.DirectorySeparatorChar)
                    path = path.Substring(0, path.Length - 2);

                while(path != null)
                {
                    string configFile = Path.Combine(path, ".editorconfig");

                    if(File.Exists(configFile))
                        yield return configFile;

                    path = Path.GetDirectoryName(path);
                }
            }
        }
        #endregion
    }
}
