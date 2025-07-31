//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : SpellCheckerLegacyConfiguration.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 07/31/2025
// Note    : Copyright 2015-2025, Eric Woodruff, All rights reserved
//
// This file contains the class used to convert the legacy spell checker configuration settings to the new
// .editorconfig format.
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 02/05/2023  EFW  Created the code
//===============================================================================================================

// Ignore spelling: lt proj Regexes

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;

using VisualStudio.SpellChecker.Common.EditorConfig;

namespace VisualStudio.SpellChecker.Common.Configuration.Legacy
{
    /// <summary>
    /// This class is used to convert the legacy spell checker configuration settings to the new .editorconfig
    /// format.
    /// </summary>
    public class SpellCheckerLegacyConfiguration
    {
        #region Private data members
        //=====================================================================

        private readonly Dictionary<string, PropertyInfo> propertyCache;
        private readonly PropertyDescriptorCollection configCache, csoCache, cadCache;

        private readonly XDocument document;
        private readonly XElement root;

        private static readonly char[] wordSeparators = [',', ' ', '\t', '\r', '\n'];
        private static readonly char[] propertySeparator = ['.'];

        #endregion

        #region Legacy configuration constants
        //=====================================================================

        // Configuration file schema version
        private const string ConfigSchemaVersion = "2018.8.16.0";

        // Selected languages list
        private const string SelectedLanguages = "SelectedLanguages";

        // Selected languages list item
        private const string SelectedLanguagesItem = "LanguageName";

        // Ignored file pattern item
        private const string IgnoredFilePatternItem = "FilePattern";

        // Additional dictionary folders list item
        private const string AdditionalDictionaryFoldersItem = "Folder";

        // Ignored item
        private const string IgnoredItem = "Ignore";

        // Exclusion expression item
        private const string ExclusionExpressionItem = "Expression";

        // Spell checked XML attributes item
        private const string SpellCheckedXmlAttributesItem = "SpellCheck";

        // Content type item
        private const string ContentType = "ContentType";

        // Content type name attribute
        private const string ContentTypeName = "Name";

        // Ignored classification item
        private const string Classification = "Classification";

        // File type classification prefix
        private const string FileType = "File Type: ";

        // Visual Studio ID Exclusion expression item
        private const string VisualStudioIdExclusionItem = "IdExpression";

        #endregion

        #region Properties
        //=====================================================================

        /// <summary>
        /// This read-only property returns the global configuration file path
        /// </summary>
        /// <value>This location is also where custom dictionary files are located</value>
        public static string GlobalConfigurationFilePath
        {
            get
            {
                string configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    @"EWSoftware\Visual Studio Spell Checker");

                if(!Directory.Exists(configPath))
                    Directory.CreateDirectory(configPath);

                return configPath;
            }
        }

        /// <summary>
        /// This is used to return the global configuration filename (VSSpellChecker.vsspell)
        /// </summary>
        /// <value>If the legacy configuration file exists, its name will be returned instead if the newer style
        /// configuration file does not exist.</value>
        public static string GlobalConfigurationFilename
        {
            get
            {
                string legacyConfig = Path.Combine(GlobalConfigurationFilePath, "SpellChecker.config"),
                    currentConfig = Path.Combine(GlobalConfigurationFilePath, "VSSpellChecker.vsspell");

                if(!File.Exists(currentConfig) && File.Exists(legacyConfig))
                    return legacyConfig;

                return currentConfig;
            }
        }

        /// <summary>
        /// This read-only property returns the name of the legacy configuration file
        /// </summary>
        public string LegacyConfigurationFilename { get; }

        /// <summary>
        /// This read-only property returns true if this is the global configuration, false if not
        /// </summary>
        public bool IsGlobalConfiguration { get; }

        /// <summary>
        /// This read-only property returns true if this is a solution configuration, false if not
        /// </summary>
        public bool IsSolutionConfiguration { get; }

        /// <summary>
        /// This read-only property returns true if this is a project configuration, false if not
        /// </summary>
        public bool IsProjectConfiguration { get; }

        /// <summary>
        /// This read-only property returns true if this is a folder configuration, false if not
        /// </summary>
        public bool IsFolderConfiguration { get; }

        /// <summary>
        /// This read-only property returns true if this is a file configuration, false if not
        /// </summary>
        public bool IsFileConfiguration { get; }

        /// <summary>
        /// This read-only property returns the file glob to use for the settings in this file
        /// </summary>
        public string FileGlob { get; }

        /// <summary>
        /// This read-only property returns the name of the file into which the settings will be merged
        /// </summary>
        /// <value>If the file exists, the settings are merged into it.  If not, it is created</value>
        public string EditorConfigFilename { get; }

        /// <summary>
        /// This is used to get or set the import setting file
        /// </summary>
        public string ImportSettingsFile { get; set; }

        /// <summary>
        /// This is used to get or set the ignored words file
        /// </summary>
        public string IgnoredWordsFile { get; set; }

        /// <summary>
        /// This read-only property returns an enumerable list of dictionary languages to be used when spell
        /// checking.
        /// </summary>
        public List<string> DictionaryLanguages { get; } = [];

        /// <summary>
        /// This is used to get or set whether or not to spell check the file as you type
        /// </summary>
        /// <value>This is true by default</value>
        [DefaultValue(true)]
        public bool SpellCheckAsYouType { get; set; } = true;

        /// <summary>
        /// This is used to get or set whether or not to spell check the file as part of the solution/project
        /// spell checking process.
        /// </summary>
        /// <value>This is true by default</value>
        [DefaultValue(true)]
        public bool IncludeInProjectSpellCheck { get; set; } = true;

        /// <summary>
        /// This is used to get or set whether or not to detect doubled words as part of the spell checking
        /// process.
        /// </summary>
        /// <value>This is true by default</value>
        [DefaultValue(true)]
        public bool DetectDoubledWords { get; set; } = true;

        /// <summary>
        /// This is used to get or set whether or not to ignore words containing digits
        /// </summary>
        /// <value>This is true by default</value>
        [DefaultValue(true)]
        public bool IgnoreWordsWithDigits { get; set; } = true;

        /// <summary>
        /// This is used to get or set whether or not to ignore words in all uppercase
        /// </summary>
        /// <value>This is true by default</value>
        [DefaultValue(true)]
        public bool IgnoreWordsInAllUppercase { get; set; } = true;

        /// <summary>
        /// This is used to get or set whether or not to ignore words in mixed/camel case
        /// </summary>
        /// <value>This is true by default</value>
        [DefaultValue(true)]
        public bool IgnoreWordsInMixedCase { get; set; } = true;

        /// <summary>
        /// This is used to get or set whether or not to ignore .NET and C-style format string specifiers
        /// </summary>
        /// <value>This is true by default</value>
        [DefaultValue(true)]
        public bool IgnoreFormatSpecifiers { get; set; } = true;

        /// <summary>
        /// This is used to get or set whether or not to ignore words that look like filenames or e-mail
        /// addresses.
        /// </summary>
        /// <value>This is true by default</value>
        [DefaultValue(true)]
        public bool IgnoreFilenamesAndEMailAddresses { get; set; } = true;

        /// <summary>
        /// This is used to get or set whether or not to ignore XML elements in the text being spell checked
        /// (text within '&amp;lt;' and '&amp;gt;').
        /// </summary>
        /// <value>This is true by default</value>
        [DefaultValue(true)]
        public bool IgnoreXmlElementsInText { get; set; } = true;

        /// <summary>
        /// This is used to get or set whether or not to ignore words by character class
        /// </summary>
        /// <remarks>This provides a simplistic way of ignoring some words in mixed language files.  It works
        /// best for spell checking English text in files that also contain Cyrillic or Asian text.  The default
        /// is <c>None</c> to include all words regardless of the characters they contain.</remarks>
        [DefaultValue(IgnoredCharacterClass.None)]
        public IgnoredCharacterClass IgnoreCharacterClass { get; set; } = IgnoredCharacterClass.None;

        /// <summary>
        /// This is used to get or set whether or not underscores are treated as a word separator
        /// </summary>
        /// <value>This is false by default</value>
        [DefaultValue(false)]
        public bool TreatUnderscoreAsSeparator { get; set; }

        /// <summary>
        /// This is used to get or set whether or not mnemonics are ignored within words
        /// </summary>
        /// <value>This is true by default.  If false, mnemonic characters act as word breaks.</value>
        [DefaultValue(true)]
        public bool IgnoreMnemonics { get; set; } = true;

        /// <summary>
        /// This is used to get or set whether or not to try to determine the language for resource files based
        /// on their filename (i.e. LocalizedForm.de-DE.resx).
        /// </summary>
        /// <value>This is true by default</value>
        [DefaultValue(true)]
        public bool DetermineResourceFileLanguageFromName { get; set; } = true;

        /// <summary>
        /// This read-only property returns the C# source code file options
        /// </summary>
        public CSharpOptions CSharpOptions { get; } = new CSharpOptions();

        /// <summary>
        /// This read-only property returns the code analysis dictionary options
        /// </summary>
        public CodeAnalysisDictionaryOptions CadOptions { get; } = new CodeAnalysisDictionaryOptions();

        /// <summary>
        /// This is used to indicate whether or not ignored file patterns are inherited by other configurations
        /// </summary>
        /// <value>The default is true so that sub-configurations inherit all ignored files from higher level
        /// configurations.</value>
        [DefaultValue(true)]
        public bool InheritIgnoredFilePatterns { get; set; } = true;

        /// <summary>
        /// This read-only property returns an enumerable list of ignored file patterns
        /// </summary>
        /// <remarks>Filenames matching the patterns in this set will not be spell checked</remarks>
        public List<string> IgnoredFilePatterns { get; } = [];

        /// <summary>
        /// This is used to indicate whether or not additional dictionary folders are inherited by other
        /// configurations.
        /// </summary>
        /// <value>The default is true so that sub-configurations inherit all additional dictionary folders from
        /// higher level configurations.</value>
        [DefaultValue(true)]
        public bool InheritAdditionalDictionaryFolders { get; set; } = true;

        /// <summary>
        /// This read-only property returns an enumerable list of additional dictionary folders
        /// </summary>
        /// <remarks>When searching for dictionaries, these folders will be included in the search.  This allows
        /// for solution and project-specific dictionaries.</remarks>
        public List<string> AdditionalDictionaryFolders { get; } = [];

        /// <summary>
        /// This is used to indicate whether or not ignored words are inherited by other configurations
        /// </summary>
        /// <value>The default is true so that sub-configurations inherit all ignored words from higher level
        /// configurations.</value>
        [DefaultValue(true)]
        public bool InheritIgnoredWords { get; set; } = true;

        /// <summary>
        /// This read-only property returns an enumerable list of ignored words that will not be spell checked
        /// </summary>
        public HashSet<string> IgnoredWords { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// This is used to indicate whether or not exclusion expressions are inherited by other configurations
        /// </summary>
        /// <value>The default is true so that sub-configurations inherit all exclusion expressions from higher
        /// level configurations.</value>
        [DefaultValue(true)]
        public bool InheritExclusionExpressions { get; set; } = true;

        /// <summary>
        /// This read-only property returns an enumerable list of exclusion regular expressions that will be used
        /// to find ranges of text that should not be spell checked.
        /// </summary>
        public List<Regex> ExclusionExpressions { get; } = [];

        /// <summary>
        /// This is used to indicate whether or not ignored XML elements and included attributes are inherited by
        /// other configurations.
        /// </summary>
        /// <value>The default is true so that sub-configurations inherit all ignored XML elements and included
        /// attributes from higher level configurations.</value>
        [DefaultValue(true)]
        public bool InheritXmlSettings { get; set; } = true;

        /// <summary>
        /// This read-only property returns an enumerable list of ignored XML element names that will not have
        /// their content spell checked.
        /// </summary>
        public HashSet<string> IgnoredXmlElements { get; } = [];

        /// <summary>
        /// This read-only property returns an enumerable list of XML attribute names that will have their values
        /// spell checked.
        /// </summary>
        public HashSet<string> SpellCheckedXmlAttributes { get; } = [];

        /// <summary>
        /// This is used to indicate whether or not ignored classifications are inherited by other configurations
        /// </summary>
        /// <value>The default is true so that sub-configurations inherit all ignored classifications from higher
        /// level configurations.</value>
        [DefaultValue(true)]
        public bool InheritIgnoredClassifications { get; set; } = true;

        /// <summary>
        /// This read-only property returns the ignored classifications
        /// </summary>
        public Dictionary<string, HashSet<string>> IgnoredClassifications { get; } = [];

        /// <summary>
        /// This is used to indicate whether or not to spell check any WPF text box within Visual Studio
        /// </summary>
        /// <value>The default is true.  This option only applies to the global configuration.</value>
        [DefaultValue(true)]
        public bool EnableWpfTextBoxSpellChecking { get; set; } = true;

        /// <summary>
        /// This read-only property returns an enumerable list of exclusion regular expressions that will be used
        /// to exclude WPF text boxes in Visual Studio editor and tool windows from being spell checked.
        /// </summary>
        /// <value>This option only applies to the global configuration.</value>
        public List<Regex> VisualStudioIdExclusions { get; } = [];

        /// <summary>
        /// This read-only property returns the old default list of ignored words
        /// </summary>
        /// <remarks>These are removed if found in any converted configuration as they are included using a
        /// file-specific ignored words property in the new global configuration file.</remarks>
        public static IEnumerable<string> OldDefaultIgnoredWords { get; } =
        [
            "\\addindex", "\\addtogroup", "\\anchor", "\\arg", "\\attention", "\\author", "\\authors", "\\brief",
            "\\bug", "\\file", "\\fn", "\\name", "\\namespace", "\\nosubgrouping", "\\note", "\\ref", "\\refitem",
            "\\related", "\\relates", "\\relatedalso", "\\relatesalso", "\\remark", "\\remarks", "\\result",
            "\\return", "\\returns", "\\retval", "\\rtfonly", "\\tableofcontents", "\\test", "\\throw",
            "\\throws", "\\todo", "\\tparam", "\\typedef", "\\var", "\\verbatim", "\\verbinclude", "\\version",
            "\\vhdlflow"
        ];
        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="filename">The filename to use</param>
        /// <remarks>Any properties not in the configuration file retain their current values.  If the file does
        /// not exist, the configuration will remain unchanged.</remarks>
        public SpellCheckerLegacyConfiguration(string filename)
        {
            if(String.IsNullOrWhiteSpace(filename))
                throw new ArgumentNullException(nameof(filename), "Filename cannot be null or empty");

            string relatedFile = Path.GetFileNameWithoutExtension(filename),
                folder = Path.GetDirectoryName(filename);

            this.LegacyConfigurationFilename = filename;
            this.IsGlobalConfiguration = filename.Equals(GlobalConfigurationFilename, StringComparison.OrdinalIgnoreCase);
            this.IsSolutionConfiguration = (relatedFile.Length == 0 ||
                relatedFile.EndsWith(".sln", StringComparison.OrdinalIgnoreCase));
            this.IsProjectConfiguration = Path.GetExtension(relatedFile).EndsWith("proj", StringComparison.OrdinalIgnoreCase);
            this.IsFolderConfiguration = (relatedFile.Length != 0 &&
                folder.EndsWith(relatedFile, StringComparison.OrdinalIgnoreCase));
            this.IsFileConfiguration = !this.IsGlobalConfiguration && !this.IsSolutionConfiguration &&
                !this.IsProjectConfiguration && !this.IsFolderConfiguration;

            if(this.IsGlobalConfiguration)
                this.EditorConfigFilename = Path.Combine(folder, Path.GetFileName(SpellCheckerConfiguration.GlobalConfigurationFilename));
            else
                this.EditorConfigFilename = Path.Combine(folder, ".editorconfig");

            // Determine the file glob to use for the main settings.  Global, folder, solution, and project
            // configurations use "*".
            if(!this.IsFileConfiguration)
                this.FileGlob = "*";
            else
            {
                relatedFile = Path.GetFileNameWithoutExtension(relatedFile);

                // For file configurations, use the filename.  If there appear to be multiple related files
                // such as designer and resource files, use a wildcard.
                var matchingFiles = Directory.EnumerateFiles(folder, relatedFile + "*").Where(f =>
                    !Path.GetExtension(f).Equals(".vsspell", StringComparison.OrdinalIgnoreCase) &&
                    !Path.GetExtension(f).Equals(".editorconfig", StringComparison.OrdinalIgnoreCase)).ToList();

                if(matchingFiles.Count > 1)
                    this.FileGlob = relatedFile + "*";
                else
                {
                    if(matchingFiles.Count == 1)
                        this.FileGlob = Path.GetFileName(matchingFiles[0]);
                    else
                    {
                        // If it gets here, we've got a configuration file that doesn't match up to anything.
                        // Just use the "*" and flag it as unmatched.  This may be an imported configuration file
                        // in which case, being unmatched is okay.
                        this.FileGlob = "*";
                        this.EditorConfigFilename = Path.Combine(folder, $"{relatedFile}.editorconfig");
                    }
                }
            }

            try
            {
                // Get the property cache for finding current and default values
                propertyCache = [];
                configCache = TypeDescriptor.GetProperties(typeof(SpellCheckerLegacyConfiguration));
                csoCache = TypeDescriptor.GetProperties(typeof(CSharpOptions));
                cadCache = TypeDescriptor.GetProperties(typeof(CodeAnalysisDictionaryOptions));

                foreach(PropertyInfo property in typeof(SpellCheckerLegacyConfiguration).GetProperties(
                  BindingFlags.Public | BindingFlags.Instance))
                {
                    propertyCache.Add(property.Name, property);
                }

                foreach(PropertyInfo property in typeof(CSharpOptions).GetProperties(
                  BindingFlags.Public | BindingFlags.Instance))
                {
                    propertyCache.Add(property.Name, property);
                }

                foreach(PropertyInfo property in typeof(CodeAnalysisDictionaryOptions).GetProperties(
                  BindingFlags.Public | BindingFlags.Instance))
                {
                    propertyCache.Add(property.Name, property);
                }

                if(File.Exists(filename))
                {
                    document = XDocument.Load(filename);
                    root = document.Root;

                    // If it's an older configuration file, upgrade it to the new format
                    if(root.Attribute("Format") == null || root.Attribute("Format").Value != ConfigSchemaVersion)
                        this.UpgradeConfiguration();

                    this.Load();
                }
            }
            catch(Exception ex)
            {
                // Ignore exceptions, we'll just use a blank configuration
                System.Diagnostics.Debug.WriteLine(ex);
            }

            if(document == null)
            {
                root = new XElement("SpellCheckerConfiguration", new XAttribute("Format", ConfigSchemaVersion));
                document = new XDocument(root);
            }
        }
        #endregion

        #region Helper methods
        //=====================================================================

        /// <summary>
        /// Convert the legacy .vsspell configuration file into a set of .editorconfig sections
        /// </summary>
        /// <returns>An enumerable list of the .editorconfig sections that should be merged into the
        /// .editorconfig file.</returns>
        public IEnumerable<EditorConfigSection> ConvertLegacyConfiguration()
        {
            var (properties, filePatterns) = this.ConvertFromLegacyConfiguration();

            if(properties.Any() || filePatterns.Any())
            {
                string importSettingsFile = SpellCheckerConfiguration.EditorConfigSettingsFor(
                    nameof(SpellCheckerConfiguration.ImportSettingsFile)).PropertyName;

                // Create a section for the main properties
                var section = new EditorConfigSection();

                section.SectionLines.Add(new SectionLine { FileGlob = this.FileGlob });
                section.SectionLines.Add(new SectionLine(
                    $"# VSSPELL: Spell checker settings from {Path.GetFileName(this.LegacyConfigurationFilename)}"));

                foreach(var p in properties)
                {
                    // Imported settings filenames may need correcting or may not be needed if they
                    // are in a folder higher than the current folder as they will be merged automatically.
                    // It should always be the first property so that others override its settings.
                    if(p.PropertyName.StartsWith(importSettingsFile, StringComparison.OrdinalIgnoreCase))
                    {
                        section.SectionLines.Insert(1, new SectionLine("# VSSPELL: TO DO: Review imported settings " +
                            "file.  Is it still needed?  Does the path need correcting?"));

                        section.SectionLines.Insert(2, new SectionLine
                        {
                            PropertyName = p.PropertyName,
                            PropertyValue = p.Value,
                        });
                    }
                    else
                    {
                        section.SectionLines.Add(new SectionLine
                        {
                            PropertyName = p.PropertyName,
                            PropertyValue = p.Value,
                        });
                    }
                }

                yield return section;

                // Ignored file patterns are converted to sections that disable the spell checker
                string spellCheckAsYouType = SpellCheckerConfiguration.EditorConfigSettingsFor(
                    nameof(SpellCheckerConfiguration.SpellCheckAsYouType)).PropertyName,
                    includeInProjectSpellCheck = SpellCheckerConfiguration.EditorConfigSettingsFor(
                    nameof(SpellCheckerConfiguration.IncludeInProjectSpellCheck)).PropertyName;

                foreach(string f in filePatterns)
                {
                    section = new EditorConfigSection();

                    section.SectionLines.Add(new SectionLine { FileGlob = f });
                    section.SectionLines.Add(new SectionLine(
                        $"# VSSPELL: Ignored file pattern from {Path.GetFileName(this.LegacyConfigurationFilename)}"));

                    section.SectionLines.Add(new SectionLine
                    {
                        PropertyName = spellCheckAsYouType,
                        PropertyValue = "false",
                    });

                    section.SectionLines.Add(new SectionLine
                    {
                        PropertyName = includeInProjectSpellCheck,
                        PropertyValue = "false",
                    });

                    yield return section;
                }
            }
        }

        /// <summary>
        /// This is used to load settings from a legacy .vsspell configuration file and convert them to the new
        /// .editorconfig format.
        /// </summary>
        /// <returns>An enumerable list of file patterns that should be ignored.  These will be converted to
        /// sections in a related .editorconfig file with the spell check as you type and project spell checking
        /// properties set to false.</returns>
        private (IEnumerable<(string PropertyName, string Value)> Properties, IEnumerable<string> FilePatterns) ConvertFromLegacyConfiguration()
        {
            var properties = new List<(string PropertyName, string Value)>();
            string sectionId = null;

            // For non-global settings files, settings in this file will override the settings in the imported
            // file so it should appear as the first property.  If this is the global settings file, the imported
            // settings will be loaded last and will override the global settings since the global file doesn't
            // inherit settings from anything else.
            if(!this.IsGlobalConfiguration && this.HasProperty(nameof(ImportSettingsFile)))
            {
                sectionId = "_" + Guid.NewGuid().ToString("N");

                properties.Add((SpellCheckerConfiguration.EditorConfigSettingsFor(
                    nameof(ImportSettingsFile)).PropertyName + sectionId, this.ImportSettingsFile));
            }

            if(this.HasProperty(nameof(SpellCheckAsYouType)))
            {
                properties.Add((SpellCheckerConfiguration.EditorConfigSettingsFor(
                    nameof(SpellCheckAsYouType)).PropertyName,
                    this.SpellCheckAsYouType.ToString().ToLowerInvariant()));
            }

            if(this.HasProperty(nameof(IncludeInProjectSpellCheck)))
            {
                properties.Add((SpellCheckerConfiguration.EditorConfigSettingsFor(
                    nameof(IncludeInProjectSpellCheck)).PropertyName,
                    this.IncludeInProjectSpellCheck.ToString().ToLowerInvariant()));
            }

            if(this.HasProperty(nameof(DetectDoubledWords)))
            {
                properties.Add((SpellCheckerConfiguration.EditorConfigSettingsFor(
                    nameof(DetectDoubledWords)).PropertyName,
                    this.DetectDoubledWords.ToString().ToLowerInvariant()));
            }

            if(this.HasProperty(nameof(IgnoreWordsWithDigits)))
            {
                properties.Add((SpellCheckerConfiguration.EditorConfigSettingsFor(
                    nameof(IgnoreWordsWithDigits)).PropertyName,
                    this.IgnoreWordsWithDigits.ToString().ToLowerInvariant()));
            }

            if(this.HasProperty(nameof(IgnoreWordsInAllUppercase)))
            {
                properties.Add((SpellCheckerConfiguration.EditorConfigSettingsFor(
                    nameof(IgnoreWordsInAllUppercase)).PropertyName,
                    this.IgnoreWordsInAllUppercase.ToString().ToLowerInvariant()));
            }

            if(this.HasProperty(nameof(IgnoreWordsInMixedCase)))
            {
                properties.Add((SpellCheckerConfiguration.EditorConfigSettingsFor(
                    nameof(IgnoreWordsInMixedCase)).PropertyName,
                    this.IgnoreWordsInMixedCase.ToString().ToLowerInvariant()));
            }

            if(this.HasProperty(nameof(IgnoreFormatSpecifiers)))
            {
                properties.Add((SpellCheckerConfiguration.EditorConfigSettingsFor(
                    nameof(IgnoreFormatSpecifiers)).PropertyName,
                    this.IgnoreFormatSpecifiers.ToString().ToLowerInvariant()));
            }

            if(this.HasProperty(nameof(IgnoreFilenamesAndEMailAddresses)))
            {
                properties.Add((SpellCheckerConfiguration.EditorConfigSettingsFor(
                    nameof(IgnoreFilenamesAndEMailAddresses)).PropertyName,
                    this.IgnoreFilenamesAndEMailAddresses.ToString().ToLowerInvariant()));
            }

            if(this.HasProperty(nameof(IgnoreXmlElementsInText)))
            {
                properties.Add((SpellCheckerConfiguration.EditorConfigSettingsFor(
                    nameof(IgnoreXmlElementsInText)).PropertyName,
                    this.IgnoreXmlElementsInText.ToString().ToLowerInvariant()));
            }

            if(this.HasProperty(nameof(TreatUnderscoreAsSeparator)))
            {
                properties.Add((SpellCheckerConfiguration.EditorConfigSettingsFor(
                    nameof(TreatUnderscoreAsSeparator)).PropertyName,
                    this.TreatUnderscoreAsSeparator.ToString().ToLowerInvariant()));
            }

            if(this.HasProperty(nameof(IgnoreMnemonics)))
            {
                properties.Add((SpellCheckerConfiguration.EditorConfigSettingsFor(
                    nameof(IgnoreMnemonics)).PropertyName,
                    this.IgnoreMnemonics.ToString().ToLowerInvariant()));
            }

            if(this.HasProperty(nameof(SpellCheckerLegacyConfiguration.IgnoreCharacterClass)))
            {
                properties.Add((SpellCheckerConfiguration.EditorConfigSettingsFor(
                    nameof(IgnoredCharacterClass)).PropertyName,
                    this.IgnoreCharacterClass.ToString()));
            }

            if(this.HasProperty(nameof(DetermineResourceFileLanguageFromName)))
            {
                properties.Add((SpellCheckerConfiguration.EditorConfigSettingsFor(
                    nameof(DetermineResourceFileLanguageFromName)).PropertyName,
                    this.DetermineResourceFileLanguageFromName.ToString().ToLowerInvariant()));
            }

            if(this.HasProperty(nameof(EnableWpfTextBoxSpellChecking)))
            {
                properties.Add((SpellCheckerConfiguration.EditorConfigSettingsFor(
                    nameof(EnableWpfTextBoxSpellChecking)).PropertyName,
                    this.EnableWpfTextBoxSpellChecking.ToString().ToLowerInvariant()));
            }

            // If both of these are disabled, assume no spell checking is wanted and turn off the code analyzers
            // as well.
            if(!this.SpellCheckAsYouType && !this.IncludeInProjectSpellCheck)
            {
                properties.Add((SpellCheckerConfiguration.EditorConfigSettingsFor(
                    nameof(SpellCheckerConfiguration.EnableCodeAnalyzers)).PropertyName, "false"));
            }

            if(this.HasProperty(nameof(CSharpOptions) + "." +
              nameof(Legacy.CSharpOptions.IgnoreXmlDocComments)))
            {
                properties.Add((SpellCheckerConfiguration.EditorConfigSettingsFor(
                    nameof(CodeAnalyzerOptions.IgnoreXmlDocComments)).PropertyName,
                    this.CSharpOptions.IgnoreXmlDocComments.ToString().ToLowerInvariant()));
            }

            if(this.HasProperty(nameof(CSharpOptions) + "." +
              nameof(Legacy.CSharpOptions.IgnoreDelimitedComments)))
            {
                properties.Add((SpellCheckerConfiguration.EditorConfigSettingsFor(
                    nameof(CodeAnalyzerOptions.IgnoreDelimitedComments)).PropertyName,
                    this.CSharpOptions.IgnoreDelimitedComments.ToString().ToLowerInvariant()));
            }

            if(this.HasProperty(nameof(CSharpOptions) + "." +
              nameof(Legacy.CSharpOptions.IgnoreStandardSingleLineComments)))
            {
                properties.Add((SpellCheckerConfiguration.EditorConfigSettingsFor(
                    nameof(CodeAnalyzerOptions.IgnoreStandardSingleLineComments)).PropertyName,
                    this.CSharpOptions.IgnoreStandardSingleLineComments.ToString().ToLowerInvariant()));
            }

            if(this.HasProperty(nameof(CSharpOptions) + "." +
              nameof(Legacy.CSharpOptions.IgnoreQuadrupleSlashComments)))
            {
                properties.Add((SpellCheckerConfiguration.EditorConfigSettingsFor(
                    nameof(CodeAnalyzerOptions.IgnoreQuadrupleSlashComments)).PropertyName,
                    this.CSharpOptions.IgnoreQuadrupleSlashComments.ToString().ToLowerInvariant()));
            }

            if(this.HasProperty(nameof(CSharpOptions) + "." +
              nameof(Legacy.CSharpOptions.IgnoreNormalStrings)))
            {
                properties.Add((SpellCheckerConfiguration.EditorConfigSettingsFor(
                    nameof(CodeAnalyzerOptions.IgnoreNormalStrings)).PropertyName,
                    this.CSharpOptions.IgnoreNormalStrings.ToString().ToLowerInvariant()));
            }

            if(this.HasProperty(nameof(CSharpOptions) + "." +
              nameof(Legacy.CSharpOptions.IgnoreVerbatimStrings)))
            {
                properties.Add((SpellCheckerConfiguration.EditorConfigSettingsFor(
                    nameof(CodeAnalyzerOptions.IgnoreVerbatimStrings)).PropertyName,
                    this.CSharpOptions.IgnoreVerbatimStrings.ToString().ToLowerInvariant()));
            }

            if(this.HasProperty(nameof(CSharpOptions) + "." +
              nameof(Legacy.CSharpOptions.IgnoreInterpolatedStrings)))
            {
                properties.Add((SpellCheckerConfiguration.EditorConfigSettingsFor(
                    nameof(CodeAnalyzerOptions.IgnoreInterpolatedStrings)).PropertyName,
                    this.CSharpOptions.IgnoreInterpolatedStrings.ToString().ToLowerInvariant()));
            }

            if(this.HasProperty(nameof(CSharpOptions) + "." +
              nameof(Legacy.CSharpOptions.ApplyToAllCStyleLanguages)))
            {
                properties.Add((SpellCheckerConfiguration.EditorConfigSettingsFor(
                    nameof(CodeAnalyzerOptions.ApplyToAllCStyleLanguages)).PropertyName,
                    this.CSharpOptions.ApplyToAllCStyleLanguages.ToString().ToLowerInvariant()));
            }

            if(this.HasProperty(nameof(CadOptions) + "." +
              nameof(CodeAnalysisDictionaryOptions.ImportCodeAnalysisDictionaries)))
            {
                properties.Add((SpellCheckerConfiguration.EditorConfigSettingsFor(
                    nameof(CodeAnalysisDictionaryOptions.ImportCodeAnalysisDictionaries)).PropertyName,
                    this.CadOptions.ImportCodeAnalysisDictionaries.ToString().ToLowerInvariant()));
            }

            if(this.HasProperty(nameof(CadOptions) + "." +
              nameof(CodeAnalysisDictionaryOptions.RecognizedWordHandling)))
            {
                properties.Add((SpellCheckerConfiguration.EditorConfigSettingsFor(
                    nameof(CodeAnalysisDictionaryOptions.RecognizedWordHandling)).PropertyName,
                    this.CadOptions.RecognizedWordHandling.ToString()));
            }

            if(this.HasProperty(nameof(CadOptions) + "." +
              nameof(CodeAnalysisDictionaryOptions.TreatUnrecognizedWordsAsMisspelled)))
            {
                properties.Add((SpellCheckerConfiguration.EditorConfigSettingsFor(
                    nameof(CodeAnalysisDictionaryOptions.TreatUnrecognizedWordsAsMisspelled)).PropertyName,
                    this.CadOptions.TreatUnrecognizedWordsAsMisspelled.ToString().ToLowerInvariant()));
            }

            if(this.HasProperty(nameof(CadOptions) + "." +
              nameof(CodeAnalysisDictionaryOptions.TreatDeprecatedTermsAsMisspelled)))
            {
                properties.Add((SpellCheckerConfiguration.EditorConfigSettingsFor(
                    nameof(CodeAnalysisDictionaryOptions.TreatDeprecatedTermsAsMisspelled)).PropertyName,
                    this.CadOptions.TreatDeprecatedTermsAsMisspelled.ToString().ToLowerInvariant()));
            }

            if(this.HasProperty(nameof(CadOptions) + "." +
              nameof(CodeAnalysisDictionaryOptions.TreatCompoundTermsAsMisspelled)))
            {
                properties.Add((SpellCheckerConfiguration.EditorConfigSettingsFor(
                    nameof(CodeAnalysisDictionaryOptions.TreatCompoundTermsAsMisspelled)).PropertyName,
                    this.CadOptions.TreatCompoundTermsAsMisspelled.ToString().ToLowerInvariant()));
            }

            if(this.HasProperty(nameof(CadOptions) + "." + nameof(
                CodeAnalysisDictionaryOptions.TreatCasingExceptionsAsIgnoredWords)))
            {
                properties.Add((SpellCheckerConfiguration.EditorConfigSettingsFor(
                    nameof(CodeAnalysisDictionaryOptions.TreatCasingExceptionsAsIgnoredWords)).PropertyName,
                    this.CadOptions.TreatCasingExceptionsAsIgnoredWords.ToString().ToLowerInvariant()));
            }

            if(this.VisualStudioIdExclusions.Count != 0)
            {
                // Regular expressions are a bit tricky to specify on one line.  We'll use the options comment
                // as the separator.  This only appears in the global configuration file so it doesn't need a
                // section ID suffix.
                properties.Add((SpellCheckerConfiguration.EditorConfigSettingsFor(
                    nameof(VisualStudioIdExclusions)).PropertyName,
                    String.Concat(this.VisualStudioIdExclusions.Select(r => $"{r}(?#/Options/{r.Options})"))));
            }

            if((!this.IsGlobalConfiguration && !this.InheritAdditionalDictionaryFolders) ||
              this.AdditionalDictionaryFolders.Count != 0)
            {
                sectionId ??= "_" + Guid.NewGuid().ToString("N");

                var values = new List<string>(this.AdditionalDictionaryFolders);

                if(!this.IsGlobalConfiguration && !this.InheritAdditionalDictionaryFolders)
                    values.Insert(0, SpellCheckerConfiguration.ClearInherited);

                // It's unlikely, but a folder could contain a comma so use a pipe to separate the values
                properties.Add((SpellCheckerConfiguration.EditorConfigSettingsFor(
                    nameof(AdditionalDictionaryFolders)).PropertyName + sectionId, String.Join("|", values)));
            }

            if((!this.IsGlobalConfiguration && !this.InheritIgnoredWords) || this.IgnoredWords.Count != 0 ||
              !String.IsNullOrWhiteSpace(this.IgnoredWordsFile))
            {
                sectionId ??= "_" + Guid.NewGuid().ToString("N");

                var values = new List<string>(this.IgnoredWords);
                int insertIdx = 0;

                if(!this.IsGlobalConfiguration && !this.InheritIgnoredWords)
                    values.Insert(insertIdx++, SpellCheckerConfiguration.ClearInherited);

                if(!String.IsNullOrWhiteSpace(this.IgnoredWordsFile))
                    values.Insert(insertIdx++, SpellCheckerConfiguration.FilePrefix + this.IgnoredWordsFile);

                // It's unlikely, but an ignored words filename could contains a comma so use a pipe to separate
                // the values.
                properties.Add((SpellCheckerConfiguration.EditorConfigSettingsFor(
                    nameof(IgnoredWords)).PropertyName + sectionId, String.Join("|", values)));
            }

            if((!this.IsGlobalConfiguration && !this.InheritExclusionExpressions) ||
              this.ExclusionExpressions.Count != 0)
            {
                sectionId ??= "_" + Guid.NewGuid().ToString("N");

                // Regular expressions are a bit tricky to specify on one line.  We'll use the options comment
                // as the separator.
                var values = new List<string>(this.ExclusionExpressions.Select(
                    r => $"{r}(?#/Options/{r.Options})"));

                if(!this.IsGlobalConfiguration && !this.InheritExclusionExpressions)
                    values.Insert(0, SpellCheckerConfiguration.ClearInherited + "(?#/Options/)");

                properties.Add((SpellCheckerConfiguration.EditorConfigSettingsFor(
                    nameof(ExclusionExpressions)).PropertyName + sectionId, String.Concat(values)));
            }

            if(this.IsGlobalConfiguration || !this.InheritXmlSettings || this.IgnoredXmlElements.Count != 0)
            {
                sectionId ??= "_" + Guid.NewGuid().ToString("N");

                var values = new List<string>(this.IgnoredXmlElements);

                // The global configuration always clears the default set
                if(this.IsGlobalConfiguration || !this.InheritXmlSettings)
                    values.Insert(0, SpellCheckerConfiguration.ClearInherited);

                properties.Add((SpellCheckerConfiguration.EditorConfigSettingsFor(
                    nameof(IgnoredXmlElements)).PropertyName + sectionId, String.Join(",", values)));
            }

            if(this.IsGlobalConfiguration || !this.InheritXmlSettings || this.SpellCheckedXmlAttributes.Count != 0)
            {
                sectionId ??= "_" + Guid.NewGuid().ToString("N");

                var values = new List<string>(this.SpellCheckedXmlAttributes);

                // The global configuration always clears the default set
                if(this.IsGlobalConfiguration || !this.InheritXmlSettings)
                    values.Insert(0, SpellCheckerConfiguration.ClearInherited);

                properties.Add((SpellCheckerConfiguration.EditorConfigSettingsFor(
                    nameof(SpellCheckedXmlAttributes)).PropertyName + sectionId, String.Join(",", values)));
            }

            if((!this.IsGlobalConfiguration && !this.InheritIgnoredClassifications) ||
              this.IgnoredClassifications.Count != 0)
            {
                sectionId ??= "_" + Guid.NewGuid().ToString("N");

                var values = new List<string>(this.IgnoredClassifications.Select(kv =>
                    $"{kv.Key}|{String.Join("|", kv.Value)}"));

                if(this.IsGlobalConfiguration && values.Count == 1 && values[0].Equals("EditorConfig|string",
                  StringComparison.OrdinalIgnoreCase))
                {
                    // Special case: If set to the default in the global configuration, don't add it to the
                    // .editorconfig file.
                    values.Clear();
                }
                else
                {
                    // The global configuration always clears the default set
                    if(this.IsGlobalConfiguration || !this.InheritIgnoredClassifications)
                        values.Insert(0, SpellCheckerConfiguration.ClearInherited);
                }

                if(values.Count != 0)
                {
                    properties.Add((SpellCheckerConfiguration.EditorConfigSettingsFor(
                        nameof(IgnoredClassifications)).PropertyName + sectionId, String.Join(",", values)));
                }
            }

            if(this.DictionaryLanguages.Count != 0)
            {
                sectionId ??= "_" + Guid.NewGuid().ToString("N");

                properties.Add((SpellCheckerConfiguration.EditorConfigSettingsFor(
                    nameof(DictionaryLanguages)).PropertyName + sectionId,
                    String.Join(",", this.DictionaryLanguages)));
            }

            // As noted above, if global, any import file should be handled last to override the global settings
            if(this.IsGlobalConfiguration && this.HasProperty(nameof(ImportSettingsFile)))
            {
                sectionId = "_" + Guid.NewGuid().ToString("N");

                properties.Add((SpellCheckerConfiguration.EditorConfigSettingsFor(
                    nameof(ImportSettingsFile)).PropertyName + sectionId, this.ImportSettingsFile));
            }

            if(sectionId != null)
            {
                properties.Insert(0, (SpellCheckerConfiguration.EditorConfigSettingsFor(
                    nameof(SpellCheckerConfiguration.SectionId)).PropertyName, sectionId.Substring(1)));
            }

            return (properties, this.IgnoredFilePatterns);
        }

        /// <summary>
        /// Load the configuration from the given file
        /// </summary>
        private void Load()
        {
            try
            {
                this.ImportSettingsFile = this.ToString(nameof(ImportSettingsFile));

                if(!String.IsNullOrWhiteSpace(this.ImportSettingsFile))
                    this.ImportSettingsFile = Path.ChangeExtension(this.ImportSettingsFile, ".editorconfig");

                this.SpellCheckAsYouType = this.ToBoolean(nameof(SpellCheckAsYouType));
                this.IncludeInProjectSpellCheck = this.ToBoolean(nameof(IncludeInProjectSpellCheck));
                this.DetectDoubledWords = this.ToBoolean(nameof(DetectDoubledWords));
                this.IgnoreWordsWithDigits = this.ToBoolean(nameof(IgnoreWordsWithDigits));
                this.IgnoreWordsInAllUppercase = this.ToBoolean(nameof(IgnoreWordsInAllUppercase));
                this.IgnoreWordsInMixedCase = this.ToBoolean(nameof(IgnoreWordsInMixedCase));
                this.IgnoreFormatSpecifiers = this.ToBoolean(nameof(IgnoreFormatSpecifiers));
                this.IgnoreFilenamesAndEMailAddresses = this.ToBoolean(nameof(IgnoreFilenamesAndEMailAddresses));
                this.IgnoreXmlElementsInText = this.ToBoolean(nameof(IgnoreXmlElementsInText));
                this.TreatUnderscoreAsSeparator = this.ToBoolean(nameof(TreatUnderscoreAsSeparator));
                this.IgnoreMnemonics = this.ToBoolean(nameof(IgnoreMnemonics));
                this.IgnoreCharacterClass = this.ToEnum<IgnoredCharacterClass>(nameof(IgnoreCharacterClass));
                this.DetermineResourceFileLanguageFromName = this.ToBoolean(nameof(DetermineResourceFileLanguageFromName));
                this.EnableWpfTextBoxSpellChecking = this.ToBoolean(nameof(EnableWpfTextBoxSpellChecking));

                this.CSharpOptions.IgnoreXmlDocComments = this.ToBoolean(
                    nameof(CSharpOptions) + "." + nameof(CSharpOptions.IgnoreXmlDocComments));
                this.CSharpOptions.IgnoreDelimitedComments = this.ToBoolean(
                    nameof(CSharpOptions) + "." + nameof(CSharpOptions.IgnoreDelimitedComments));
                this.CSharpOptions.IgnoreStandardSingleLineComments = this.ToBoolean(
                    nameof(CSharpOptions) + "." + nameof(CSharpOptions.IgnoreStandardSingleLineComments));
                this.CSharpOptions.IgnoreQuadrupleSlashComments = this.ToBoolean(
                    nameof(CSharpOptions) + "." + nameof(CSharpOptions.IgnoreQuadrupleSlashComments));
                this.CSharpOptions.IgnoreNormalStrings = this.ToBoolean(
                    nameof(CSharpOptions) + "." + nameof(CSharpOptions.IgnoreNormalStrings));
                this.CSharpOptions.IgnoreVerbatimStrings = this.ToBoolean(
                    nameof(CSharpOptions) + "." + nameof(CSharpOptions.IgnoreVerbatimStrings));
                this.CSharpOptions.IgnoreInterpolatedStrings = this.ToBoolean(
                    nameof(CSharpOptions) + "." + nameof(CSharpOptions.IgnoreInterpolatedStrings));
                this.CSharpOptions.ApplyToAllCStyleLanguages = this.ToBoolean(
                    nameof(CSharpOptions) + "." + nameof(CSharpOptions.ApplyToAllCStyleLanguages));

                this.CadOptions.ImportCodeAnalysisDictionaries = this.ToBoolean(
                    nameof(CadOptions) + "." + nameof(CadOptions.ImportCodeAnalysisDictionaries));
                this.CadOptions.RecognizedWordHandling = this.ToEnum<RecognizedWordHandling>(
                    nameof(CadOptions) + "." + nameof(CadOptions.RecognizedWordHandling));
                this.CadOptions.TreatUnrecognizedWordsAsMisspelled = this.ToBoolean(
                    nameof(CadOptions) + "." + nameof(CadOptions.TreatUnrecognizedWordsAsMisspelled));
                this.CadOptions.TreatDeprecatedTermsAsMisspelled = this.ToBoolean(
                    nameof(CadOptions) + "." + nameof(CadOptions.TreatDeprecatedTermsAsMisspelled));
                this.CadOptions.TreatCompoundTermsAsMisspelled = this.ToBoolean(
                    nameof(CadOptions) + "." + nameof(CadOptions.TreatCompoundTermsAsMisspelled));
                this.CadOptions.TreatCasingExceptionsAsIgnoredWords = this.ToBoolean(
                    nameof(CadOptions) + "." + nameof(CadOptions.TreatCasingExceptionsAsIgnoredWords));

                if(this.HasProperty(nameof(VisualStudioIdExclusions)))
                {
                    this.VisualStudioIdExclusions.AddRange(this.ToRegexes(nameof(VisualStudioIdExclusions),
                        VisualStudioIdExclusionItem));
                }

                this.InheritAdditionalDictionaryFolders = this.ToBoolean(nameof(InheritAdditionalDictionaryFolders));

                if(this.HasProperty(nameof(AdditionalDictionaryFolders)))
                {
                    this.AdditionalDictionaryFolders.AddRange(this.ToValues(
                        nameof(AdditionalDictionaryFolders), AdditionalDictionaryFoldersItem));
                }

                this.InheritIgnoredWords = this.ToBoolean(nameof(InheritIgnoredWords));

                if(this.HasProperty(nameof(IgnoredWords)))
                    this.IgnoredWords.UnionWith(this.ToValues(nameof(IgnoredWords), IgnoredItem).Except(OldDefaultIgnoredWords));

                this.IgnoredWordsFile = this.ToString(nameof(IgnoredWordsFile));

                this.InheritExclusionExpressions = this.ToBoolean(nameof(InheritExclusionExpressions));

                if(this.HasProperty(nameof(ExclusionExpressions)))
                {
                    this.ExclusionExpressions.AddRange(this.ToRegexes(nameof(ExclusionExpressions),
                        ExclusionExpressionItem));
                }

                this.InheritIgnoredFilePatterns = this.ToBoolean(nameof(InheritIgnoredFilePatterns));

                if(this.HasProperty(nameof(IgnoredFilePatterns)))
                {
                    // Convert the file patterns to glob patterns.  These will be converted to sections with the
                    // spell checking options set to false.
                    foreach(string pattern in this.ToValues(nameof(IgnoredFilePatterns), IgnoredFilePatternItem))
                    {
                        string convertedPattern = pattern.Replace("\\", "/").Replace("/*/", "/**/");

                        if(convertedPattern.Length > 1 && convertedPattern[0] == '/')
                            convertedPattern = convertedPattern.Substring(1);

                        if(convertedPattern.Length != 0)
                            this.IgnoredFilePatterns.Add(convertedPattern);
                    }
                }

                this.InheritXmlSettings = this.ToBoolean(nameof(InheritXmlSettings));

                if(this.HasProperty(nameof(IgnoredXmlElements)))
                    this.IgnoredXmlElements.UnionWith(this.ToValues(nameof(IgnoredXmlElements), IgnoredItem));

                if(this.HasProperty(nameof(SpellCheckedXmlAttributes)))
                {
                    this.SpellCheckedXmlAttributes.UnionWith(this.ToValues(nameof(SpellCheckedXmlAttributes),
                        SpellCheckedXmlAttributesItem));
                }

                this.InheritIgnoredClassifications = this.ToBoolean(nameof(InheritIgnoredClassifications));

                if(this.HasProperty(nameof(IgnoredClassifications)))
                {
                    foreach(var type in root.Element(nameof(IgnoredClassifications)).Elements(ContentType))
                    {
                        string typeName = type.Attribute(ContentTypeName).Value;

                        if(!this.IgnoredClassifications.TryGetValue(typeName, out HashSet<string> classifications))
                        {
                            classifications = [];
                            this.IgnoredClassifications.Add(typeName, classifications);
                        }

                        foreach(var c in type.Elements(Classification))
                            classifications.Add(c.Value);
                    }
                }

                if(this.HasProperty(SelectedLanguages))
                {
                    this.DictionaryLanguages.AddRange(this.ToValues(SelectedLanguages,
                      SelectedLanguagesItem, true).Distinct(StringComparer.OrdinalIgnoreCase));

                    int inheritedEntry = this.DictionaryLanguages.IndexOf(String.Empty);

                    if(inheritedEntry != -1)
                        this.DictionaryLanguages[inheritedEntry] = SpellCheckerConfiguration.Inherited;
                }
            }
            catch(Exception ex)
            {
                // Ignore errors and just use the defaults
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        /// <summary>
        /// Upgrade an older configuration file to the latest format
        /// </summary>
        private void UpgradeConfiguration()
        {
            var format = root.Attribute("Format");

            if(format != null)
            {
                Version fileFormat = new(format.Value), currentVersion = new(ConfigSchemaVersion);

                // If they've downgraded, there's nothing to convert.  Any property settings from newer
                // configurations will have to be added back to the older configuration file format by
                // editing the configuration file.
                if(fileFormat < currentVersion)
                {
                    if(fileFormat < new Version(2016, 3, 10))
                    {
                        this.ConvertDefaultLanguage();
                        this.ConvertExcludedExtensionsToIgnoredFilePatterns();
                    }

                    this.ConvertCommentExclusions();
                }

                format.Value = ConfigSchemaVersion;
            }
            else
                this.ConvertFromOriginalFormat();
        }

        /// <summary>
        /// Convert from the very first format to the latest format
        /// </summary>
        private void ConvertFromOriginalFormat()
        {
            string[] propertyNames = [ nameof(SpellCheckAsYouType),
                nameof(IgnoreWordsWithDigits), nameof(IgnoreWordsInAllUppercase),
                nameof(IgnoreFormatSpecifiers), nameof(IgnoreFilenamesAndEMailAddresses),
                nameof(IgnoreXmlElementsInText), nameof(TreatUnderscoreAsSeparator) ];

            root.Add(new XAttribute("Format", ConfigSchemaVersion));

            // Set values on these elements
            foreach(string name in propertyNames)
            {
                var property = root.Element(name);

                if(property != null && String.IsNullOrWhiteSpace(property.Value))
                    property.Value = "True";
            }

            // Move the C# options into the parent element
            XElement ignoreXmlDocComments = root.Element("IgnoreXmlDocComments"),
                ignoreDelimitedComments = root.Element("IgnoreDelimitedComments"),
                ignoreStandardSingleLineComments = root.Element("IgnoreStandardSingleLineComments"),
                ignoreQuadrupleSlashComments = root.Element("IgnoreQuadrupleSlashComments"),
                ignoreNormalStrings = root.Element("IgnoreNormalStrings"),
                ignoreVerbatimStrings = root.Element("IgnoreVerbatimStrings"),
                ignoredCharacterClass = root.Element("IgnoredCharacterClass"),
                csharpOptions = new("CSharpOptions");

            if(ignoreXmlDocComments != null)
            {
                ignoreXmlDocComments.Value = "True";
                ignoreXmlDocComments.Remove();
                csharpOptions.Add(ignoreXmlDocComments);
            }

            if(ignoreDelimitedComments != null)
            {
                ignoreDelimitedComments.Value = "True";
                ignoreDelimitedComments.Remove();
                csharpOptions.Add(ignoreDelimitedComments);
            }

            if(ignoreStandardSingleLineComments != null)
            {
                ignoreStandardSingleLineComments.Value = "True";
                ignoreStandardSingleLineComments.Remove();
                csharpOptions.Add(ignoreStandardSingleLineComments);
            }

            if(ignoreQuadrupleSlashComments != null)
            {
                ignoreQuadrupleSlashComments.Value = "True";
                ignoreQuadrupleSlashComments.Remove();
                csharpOptions.Add(ignoreQuadrupleSlashComments);
            }

            if(ignoreNormalStrings != null)
            {
                ignoreNormalStrings.Value = "True";
                ignoreNormalStrings.Remove();
                csharpOptions.Add(ignoreNormalStrings);
            }

            if(ignoreVerbatimStrings != null)
            {
                ignoreVerbatimStrings.Value = "True";
                ignoreVerbatimStrings.Remove();
                csharpOptions.Add(ignoreVerbatimStrings);
            }

            if(csharpOptions.HasElements)
                root.Add(csharpOptions);

            // Rename the ignored character class element
            if(ignoredCharacterClass != null)
            {
                ignoredCharacterClass.Remove();
                root.Add(new XElement(nameof(IgnoreCharacterClass), ignoredCharacterClass.Value));
            }

            // Convert excluded extensions to a list of ignored file patterns
            var excludeExts = root.Element("ExcludeByFilenameExtension");

            if(excludeExts != null)
            {
                excludeExts.Remove();

                string excluded = excludeExts.Value;

                if(!String.IsNullOrWhiteSpace(excluded))
                {
                    excludeExts = new XElement(nameof(IgnoredFilePatterns));
                    root.Add(excludeExts);

                    foreach(string ext in excluded.Split(wordSeparators, StringSplitOptions.RemoveEmptyEntries))
                    {
                        string addExt;

                        if(ext[0] != '.')
                            addExt = "." + ext;
                        else
                            addExt = ext;

                        if(addExt != ".")
                            excludeExts.Add(new XElement(IgnoredFilePatternItem, "*" + addExt));
                    }
                }
            }
        }

        /// <summary>
        /// Convert DefaultLanguage which got replaced by SelectedLanguages in schema version 2015.7.24.0
        /// </summary>
        private void ConvertDefaultLanguage()
        {
            var defaultLanguage = root.Element("DefaultLanguage");

            if(defaultLanguage != null)
            {
                if(!String.IsNullOrWhiteSpace(defaultLanguage.Value))
                {
                    // If downgraded and then upgraded, the element may already exist
                    var selectedLanguages = root.Element(SelectedLanguages);

                    if(selectedLanguages == null)
                    {
                        root.Add(new XElement(SelectedLanguages,
                            new XElement(SelectedLanguagesItem, defaultLanguage.Value)));
                    }
                    else
                    {
                        selectedLanguages.RemoveNodes();
                        selectedLanguages.Add(new XElement(SelectedLanguagesItem, defaultLanguage.Value));
                    }
                }

                defaultLanguage.Remove();
            }
        }

        /// <summary>
        /// Convert ExcludedExtensions which got replaced by IgnoredFilePatterns in schema version 2016.3.10.0
        /// </summary>
        private void ConvertExcludedExtensionsToIgnoredFilePatterns()
        {
            var excludeExts = root.Element("InheritExcludedExtensions");

            if(excludeExts != null)
            {
                excludeExts.Remove();

                var inheritIgnored = root.Element(nameof(InheritIgnoredFilePatterns));

                // If downgraded and then upgraded, the element may already exist
                if(inheritIgnored != null)
                    inheritIgnored.Value = excludeExts.Value;
                else
                    root.Add(new XElement(nameof(InheritIgnoredFilePatterns), excludeExts.Value));
            }

            excludeExts = root.Element("ExcludedExtension");

            if(excludeExts != null)
            {
                excludeExts.Remove();

                // If downgraded and then upgraded, the element may already exist
                var ignoredPatterns = root.Element(nameof(IgnoredFilePatterns));

                if(ignoredPatterns == null)
                {
                    ignoredPatterns = new XElement(nameof(IgnoredFilePatterns));
                    root.Add(ignoredPatterns);
                }
                else
                    ignoredPatterns.RemoveNodes();

                foreach(var ext in excludeExts.Descendants("Exclude"))
                {
                    if(ext.Value != ".")
                        ignoredPatterns.Add(new XElement(IgnoredFilePatternItem, "*" + ext.Value));
                }
            }
        }

        /// <summary>
        /// Convert ExcludedExtensions which got replaced by IgnoredFilePatterns in schema version 2016.3.10.0
        /// </summary>
        private void ConvertCommentExclusions()
        {
            var ignoredClassifications = new XElement(nameof(IgnoredClassifications));
            var ignoreComments = root.Element("IgnoreHtmlComments");

            if(ignoreComments != null && Convert.ToBoolean(ignoreComments.Value, CultureInfo.InvariantCulture))
            {
                ignoreComments.Remove();

                ignoredClassifications.Add(
                    new XElement(ContentType,
                        new XAttribute(ContentTypeName, "HTML"),
                        new XElement(Classification, "html comment")),
                    new XElement(ContentType,
                        new XAttribute(ContentTypeName, "htmlx"),
                        new XElement(Classification, "html comment")),
                    new XElement(ContentType,
                        new XAttribute(ContentTypeName, FileType + "HTML"),
                        new XElement(Classification, "XmlFileComment")));
            }

            ignoreComments = root.Element("IgnoreXmlComments");

            if(ignoreComments != null && Convert.ToBoolean(ignoreComments.Value, CultureInfo.InvariantCulture))
            {
                ignoreComments.Remove();

                ignoredClassifications.Add(
                    new XElement(ContentType,
                        new XAttribute(ContentTypeName, "XML"),
                        new XElement(Classification, "xml comment")),
                    new XElement(ContentType,
                        new XAttribute(ContentTypeName, FileType + "XML"),
                        new XElement(Classification, "XmlFileComment")));
            }

            if(ignoredClassifications.HasElements)
            {
                // If downgraded and then upgraded, the element may already exist
                root.Element(nameof(IgnoredClassifications))?.Remove();
                root.Add(ignoredClassifications);
            }
        }

        /// <summary>
        /// Get the default value for the specified property
        /// </summary>
        /// <param name="propertyName">The property name</param>
        /// <returns>The default value if the property is found or null if not found and no default value
        /// attribute is defined.</returns>
        private object DefaultValueFor(string propertyName)
        {
            if(configCache != null && csoCache != null && cadCache != null && propertyCache != null)
            {
                try
                {
                    string[] parts = propertyName.Split(propertySeparator, StringSplitOptions.RemoveEmptyEntries);
                    PropertyInfo property = null;
                    object propertyValue = this;

                    // Try to get the value from the default configuration if there is one
                    foreach(string name in parts)
                    {
                        if(!propertyCache.TryGetValue(name, out property))
                        {
                            property = null;
                            break;
                        }

                        propertyValue = property.GetValue(propertyValue, null);
                    }

                    if(property != null)
                        return propertyValue;

                    // If not found, get it from the default value attribute if defined
                    var prop = configCache[parts[parts.Length - 1]];

                    if(prop == null)
                    {
                        prop = csoCache[parts[parts.Length - 1]];

                        if(prop == null)
                        {
                            prop = cadCache[parts[parts.Length - 1]];

                            if(prop == null)
                                return null;
                        }
                    }


                    return (prop.Attributes[typeof(DefaultValueAttribute)] is DefaultValueAttribute defValue) ?
                        defValue.Value : null;
                }
                catch
                {
                    // Ignore errors retrieving values
                }
            }

            return null;
        }

        /// <summary>
        /// Get a property element from the XML document
        /// </summary>
        /// <param name="propertyName">The property name</param>
        /// <returns>Returns the property name if found, or null if not.  If the property name contains dot
        /// separators, the requested nested element is retrieved.</returns>
        private XElement GetPropertyElement(string propertyName)
        {
            string[] elementNames = propertyName.Split(propertySeparator, StringSplitOptions.RemoveEmptyEntries);
            XElement property = null, current = root;

            foreach(string name in elementNames)
            {
                property = current.Element(name);

                if(property == null)
                    break;

                current = property;
            }

            return property;
        }

        /// <summary>
        /// This is used to see if the configuration file contains the named property
        /// </summary>
        /// <param name="propertyName">The property name for which to check</param>
        /// <returns>Returns true if the property name is found, or false if not.  If the property name contains
        /// dot separators, the requested nested element is checked for existence.</returns>
        public bool HasProperty(string propertyName)
        {
            return this.GetPropertyElement(propertyName ?? String.Empty) != null;
        }

        /// <summary>
        /// Convert a configuration element to a new <see cref="String"/> instance or return the default value
        /// </summary>
        /// <param name="propertyName">The property name to retrieve</param>
        /// <returns>The requested <c>String</c> value or the default if not found</returns>
        private string ToString(string propertyName)
        {
            var property = this.GetPropertyElement(propertyName);

            if(property != null && !String.IsNullOrWhiteSpace(property.Value))
                return property.Value;

            return (string)this.DefaultValueFor(propertyName);
        }

        /// <summary>
        /// Convert a configuration element to a new <see cref="Boolean"/> instance or return the default value
        /// </summary>
        /// <param name="propertyName">The property name to retrieve</param>
        /// <returns>The requested <c>Boolean</c> value or the default if not found</returns>
        private bool ToBoolean(string propertyName)
        {
            var property = this.GetPropertyElement(propertyName);

            if(property == null || String.IsNullOrWhiteSpace(property.Value) ||
              !Boolean.TryParse(property.Value, out bool value))
            {
                object defaultValue = this.DefaultValueFor(propertyName);
                return (defaultValue != null) && (bool)defaultValue;
            }

            return value;
        }

        /// <summary>
        /// Convert a configuration element to a new instance of the specified enumeration type or return the
        /// default value.
        /// </summary>
        /// <param name="propertyName">The property name to retrieve</param>
        /// <returns>The property value as the requested enum type value</returns>
        private TEnum ToEnum<TEnum>(string propertyName) where TEnum : struct
        {
            var property = this.GetPropertyElement(propertyName);

            if(property == null || String.IsNullOrWhiteSpace(property.Value) ||
              !Enum.TryParse(property.Value, true, out TEnum value))
            {
                object defaultValue = this.DefaultValueFor(propertyName);
                value = (defaultValue != null) ? (TEnum)defaultValue : default;
            }

            return value;
        }

        /// <summary>
        /// Convert a configuration element to an enumerable list of string values or return an empty enumeration
        /// if not found.
        /// </summary>
        /// <param name="propertyName">The property name to retrieve</param>
        /// <param name="valueName">The value element name of the sub-elements within the parent property</param>
        /// <param name="allowBlankValues">False (the default) to exclude blank values or true to allow them</param>
        /// <returns>An enumerable list of the values</returns>
        private IEnumerable<string> ToValues(string propertyName, string valueName, bool allowBlankValues = false)
        {
            var property = this.GetPropertyElement(propertyName);

            if(property != null && property.HasElements)
                foreach(var value in property.Descendants(valueName))
                    if(allowBlankValues || !String.IsNullOrWhiteSpace(value.Value))
                        yield return value.Value;
        }

        /// <summary>
        /// Convert a configuration element to an enumerable list of regular expressions or return an empty
        /// enumeration if not found.
        /// </summary>
        /// <param name="propertyName">The property name to retrieve</param>
        /// <param name="valueName">The value element name of the sub-elements within the parent property</param>
        /// <returns>An enumerable list of the regular expressions</returns>
        /// <remarks>Each expression item is expected to have a <c>Match</c> attribute that defines the regular
        /// expression and an optional <c>Options</c> attribute that defines the regular expression options,
        /// if any.  All expressions are created with a 100 millisecond time out.</remarks>
        public IEnumerable<Regex> ToRegexes(string propertyName, string valueName)
        {
            string match, options;
            Regex regex;

            var property = this.GetPropertyElement(propertyName ?? String.Empty);

            if(property != null && property.HasElements)
            {
                foreach(var value in property.Descendants(valueName))
                {
                    regex = null;

                    try
                    {
                        match = (string)value.Attribute("Match");
                        options = (string)value.Attribute("Options");

                        if(String.IsNullOrWhiteSpace(options) || !Enum.TryParse(options,
                          out RegexOptions regexOpts))
                        {
                            regexOpts = RegexOptions.None;
                        }

                        if(!String.IsNullOrWhiteSpace(match))
                            regex = new Regex(match, regexOpts, TimeSpan.FromMilliseconds(100));
                    }
                    catch(Exception ex)
                    {
                        // Ignore invalid expressions
                        System.Diagnostics.Debug.WriteLine(ex);
                    }

                    if(regex != null)
                        yield return regex;
                }
            }
        }
        #endregion
    }
}
