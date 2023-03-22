//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : SpellCheckerConfiguration.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 03/19/2023
// Note    : Copyright 2015-2023, Eric Woodruff, All rights reserved
//
// This file contains the class used to contain the spell checker's configuration settings
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 02/01/2015  EFW  Refactored the configuration settings to allow for solution and project specific settings
// 07/22/2015  EFW  Added support for selecting multiple languages
// 08/15/2018  EFW  Added support for tracking and excluding classifications using the classification cache
// 02/05/2023  EFW  Reworked for use with .editorconfig files
//===============================================================================================================

// Ignore spelling: lt cebf seealso

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

using System.Xml.Linq;
using System.Xml.XPath;

using VisualStudio.SpellChecker.Common.EditorConfig;

namespace VisualStudio.SpellChecker.Common.Configuration
{
    // TODO: Editor: If the section contains a comment starting with "# VSSPELL:", use that as a section comment
    // for the spell checker settings.

    /// <summary>
    /// This class is used to contain the spell checker's configuration
    /// </summary>
    /// <remarks>Settings are stored in an XML file in the user's local application data folder and will be used
    /// by all versions of Visual Studio in which the package is installed.</remarks>
    public class SpellCheckerConfiguration
    {
        #region Private data members and constants
        //=====================================================================

        /// <summary>
        /// This is the prefix used on all spell checker .editorconfig properties
        /// </summary>
        public const string PropertyPrefix = "vsspell_";

        private readonly HashSet<string> ignoredWords, ignoredWordsFiles, ignoredKeywords, recognizedWords,
            ignoredXmlElements, spellCheckedXmlAttributes;
        private readonly List<CultureInfo> dictionaryLanguages;
        private readonly List<string> additionalDictionaryFolders;
        private readonly List<Regex> exclusionExpressions, visualStudioIdExclusions;
        private readonly Dictionary<string, HashSet<string>> ignoredClassifications;
        private readonly Dictionary<string, string> deprecatedTerms, compoundTerms;
        private readonly Dictionary<string, IList<string>> unrecognizedWords;

        private static readonly Dictionary<string, object> defaultValueCache;
        private static readonly Dictionary<string, EditorConfigPropertyAttribute> editorConfigAttrCache;
        private static readonly Dictionary<string, PropertyInfo> vsspellToPropertyCache;

        #endregion

        #region Properties
        //=====================================================================

        /// <summary>
        /// This read-only property returns the global configuration file path
        /// </summary>
        /// <value>This location is also where custom dictionary files and the default global ignored words file
        /// are located</value>
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
        /// This read-only property is used to return the global configuration filename (VSSpellChecker.editorconfig)
        /// </summary>
        public static string GlobalConfigurationFilename => Path.Combine(GlobalConfigurationFilePath, "VSSpellChecker.editorconfig");

        /// <summary>
        /// This returns the default global .editorconfig settings in string form
        /// </summary>
        public static string DefaultGlobalConfiguration => Properties.Resources.DefaultConfig;

        /// <summary>
        /// This read-only property returns the filename for which the configuration was generated
        /// </summary>
        public string SpellCheckFilename { get; }

        /// <summary>
        /// This is used to get or set a unique ID for all collection properties within an .editorconfig file
        /// section.
        /// </summary>
        /// <value>If not set explicitly, a value will be generated when the properties are saved.  While the
        /// section ID value is used to make the multi-valued properties unique, this property itself is only
        /// used by the spell checker configuration editor and has no effect on the actual configuration
        /// generated for spell checking.</value>
        [EditorConfigProperty("vsspell_section_id")]
        public Guid SectionId { get; set; }

        // TODO: Editor: This property should be the first property for non-global configurations and last
        // property for global configurations.
        /// <summary>
        /// This is used to get or set the name of a configuration file to import
        /// </summary>
        [EditorConfigProperty("vsspell_import_settings_file", true)]
        public string ImportSettingsFile { get; set; }

        /// <summary>
        /// This read-only property returns a list of dictionary languages to be used when spell checking
        /// </summary>
        [EditorConfigProperty("vsspell_dictionary_languages", true)]
        public IList<CultureInfo> DictionaryLanguages
        {
            get
            {
                // Always ensure we have at least the default language if no configuration was loaded
                if(dictionaryLanguages.Count == 0)
                    dictionaryLanguages.Add(new CultureInfo("en-US"));

                return dictionaryLanguages;
            }
        }

        /// <summary>
        /// This is used to get or set whether or not to spell check the file as you type using the
        /// classification taggers.
        /// </summary>
        /// <value>This is true by default.  If code analyzers are enabled, this option will have no effect in
        /// files in which code analyzers are active.</value>
        [DefaultValue(true), EditorConfigProperty("vsspell_spell_check_as_you_type")]
        public bool SpellCheckAsYouType { get; set; } = true;

        // TODO: Editor: This is always true for global configuration files so it is never changed
        /// <summary>
        /// This is used to get or set whether or not to spell check the file as part of the solution/project
        /// spell checking process.
        /// </summary>
        /// <value>This is true by default</value>
        [DefaultValue(true), EditorConfigProperty("vsspell_include_in_project_spell_check")]
        public bool IncludeInProjectSpellCheck { get; set; } = true;

        /// <summary>
        /// This is used to get or set whether or not the Roslyn-based code analyzers are enabled
        /// </summary>
        /// <value>The default is true to enable them</value>
        [DefaultValue(true), EditorConfigProperty("vsspell_code_analyzers_enabled")]
        public bool EnableCodeAnalyzers { get; set; } = true;

        /// <summary>
        /// This is used to get or set whether or not to detect doubled words as part of the spell checking
        /// process.
        /// </summary>
        /// <value>This is true by default</value>
        [DefaultValue(true), EditorConfigProperty("vsspell_detect_doubled_words")]
        public bool DetectDoubledWords { get; set; } = true;

        /// <summary>
        /// This is used to get or set whether or not to ignore words containing digits
        /// </summary>
        /// <value>This is true by default</value>
        [DefaultValue(true), EditorConfigProperty("vsspell_ignore_words_with_digits")]
        public bool IgnoreWordsWithDigits { get; set; } = true;

        /// <summary>
        /// This is used to get or set whether or not to ignore words in all uppercase
        /// </summary>
        /// <value>This is true by default</value>
        [DefaultValue(true), EditorConfigProperty("vsspell_ignore_words_in_all_uppercase")]
        public bool IgnoreWordsInAllUppercase { get; set; } = true;

        /// <summary>
        /// This is used to get or set whether or not to ignore words in mixed/camel case
        /// </summary>
        /// <value>This is true by default</value>
        [DefaultValue(true), EditorConfigProperty("vsspell_ignore_words_in_mixed_case")]
        public bool IgnoreWordsInMixedCase { get; set; } = true;

        /// <summary>
        /// This is used to get or set whether or not to ignore .NET and C-style format string specifiers
        /// </summary>
        /// <value>This is true by default</value>
        [DefaultValue(true), EditorConfigProperty("vsspell_ignore_format_specifiers")]
        public bool IgnoreFormatSpecifiers { get; set; } = true;

        /// <summary>
        /// This is used to get or set whether or not to ignore words that look like filenames or e-mail
        /// addresses.
        /// </summary>
        /// <value>This is true by default</value>
        [DefaultValue(true), EditorConfigProperty("vsspell_ignore_filenames_and_email_addresses")]
        public bool IgnoreFilenamesAndEMailAddresses { get; set; } = true;

        /// <summary>
        /// This is used to get or set whether or not to ignore XML elements in the text being spell checked
        /// (text within '&amp;lt;' and '&amp;gt;').
        /// </summary>
        /// <value>This is true by default</value>
        [DefaultValue(true), EditorConfigProperty("vsspell_ignore_xml_elements_in_text")]
        public bool IgnoreXmlElementsInText { get; set; } = true;

        /// <summary>
        /// This is used to get or set whether or not to ignore words by character class
        /// </summary>
        /// <remarks>This provides a simplistic way of ignoring some words in mixed language files.  It works
        /// best for spell checking English text in files that also contain Cyrillic or Asian text.  The default
        /// is <c>None</c> to include all words regardless of the characters they contain.</remarks>
        [DefaultValue(IgnoredCharacterClass.None), EditorConfigProperty("vsspell_ignored_character_class")]
        public IgnoredCharacterClass IgnoredCharacterClass { get; set; } = IgnoredCharacterClass.None;

        /// <summary>
        /// This is used to get or set whether or not underscores are treated as a word separator
        /// </summary>
        /// <value>This is false by default</value>
        [DefaultValue(false), EditorConfigProperty("vsspell_treat_underscore_as_separator")]
        public bool TreatUnderscoreAsSeparator { get; set; }

        /// <summary>
        /// This is used to get or set whether or not mnemonics are ignored within words
        /// </summary>
        /// <value>This is true by default.  If false, mnemonic characters act as word breaks.</value>
        [DefaultValue(true), EditorConfigProperty("vsspell_ignore_mnemonics")]
        public bool IgnoreMnemonics { get; set; } = true;

        /// <summary>
        /// This is used to get or set whether or not to try to determine the language for resource files based
        /// on their filename (i.e. LocalizedForm.de-DE.resx).
        /// </summary>
        /// <value>This is true by default</value>
        [DefaultValue(true), EditorConfigProperty("vsspell_determine_resource_file_language_from_name")]
        public bool DetermineResourceFileLanguageFromName { get; set; } = true;

        /// <summary>
        /// This read-only property returns the code analyzer options
        /// </summary>
        public CodeAnalyzerOptions CodeAnalyzerOptions { get; } = new CodeAnalyzerOptions();

        /// <summary>
        /// This read-only property returns the code analysis dictionary options
        /// </summary>
        public CodeAnalysisDictionaryOptions CadOptions { get; } = new CodeAnalysisDictionaryOptions();

        /// <summary>
        /// This read-only property returns an enumerable list of additional dictionary folders
        /// </summary>
        /// <remarks>When searching for dictionaries, these folders will be included in the search.  This allows
        /// for solution and project-specific dictionaries.  If the first property value is set to
        /// "clear_inherited", prior folders are cleared rather than inherited.  If not specified, prior folders
        /// are retained.</remarks>
        [EditorConfigProperty("vsspell_additional_dictionary_folders", true)]
        public IEnumerable<string> AdditionalDictionaryFolders => additionalDictionaryFolders;

        // TODO: Editor: For the global configuration this is always marked to clear the list when loaded to
        // remove the default list and replace it with the modified default list.  If not changed, nothing will
        // be written out except for the ignored words file if specified.
        /// <summary>
        /// This read-only property returns an enumerable list of ignored words that will not be spell checked
        /// </summary>
        /// <remarks>If the first property value is set to "clear_inherited", prior ignored words are cleared
        /// rather than inherited.  If not specified, prior ignored words are retained.</remarks>
        [EditorConfigProperty("vsspell_ignored_words", true)]
        public IEnumerable<string> IgnoredWords => ignoredWords;

        // TODO: Editor: If an ignored words file is specified, make sure it exists when the settings are saved.
        // If not, create a blank file.
        /// <summary>
        /// This read-only property returns an enumerable list of ignored words files that were loaded by this
        /// configuration.
        /// </summary>
        /// <remarks>A filename is added if the <see cref="IgnoredWords" /> property value contains an entry
        /// prefixed with "File:".</remarks>
        public IEnumerable<string> IgnoredWordsFiles => ignoredWordsFiles;

        /// <summary>
        /// This read-only property returns an enumerable list of ignored language keywords that will not be
        /// spell checked.
        /// </summary>
        /// <remarks>Keywords in this list are treated as ignored words.  However, they are separate from the
        /// main ignored words list so that they are not cleared.  These are always inherited across all
        /// configuration files.</remarks>
        [EditorConfigProperty("vsspell_ignored_keywords", true)]
        public IEnumerable<string> IgnoredKeywords => ignoredKeywords;

        /// <summary>
        /// This read-only property returns an enumerable list of exclusion regular expressions that will be used
        /// to find ranges of text that should not be spell checked.
        /// </summary>
        /// <remarks>If the first property value is set to "clear_inherited", prior expressions are cleared
        /// rather than inherited.  If not specified, prior expressions are retained.</remarks>
        [EditorConfigProperty("vsspell_exclusion_expressions", true)]
        public IEnumerable<Regex> ExclusionExpressions => exclusionExpressions;

        // TODO: Editor: For the global configuration this is always marked to clear the list when loaded to
        // remove the default list and replace it with the modified default list.  If not changed, nothing will
        // be written out.
        /// <summary>
        /// This read-only property returns an enumerable list of ignored XML element names that will not have
        /// their content spell checked.
        /// </summary>
        /// <remarks>If the first property value is set to "clear_inherited", prior elements are cleared rather
        /// than inherited.  If not specified, prior elements are retained.</remarks>
        [EditorConfigProperty("vsspell_ignored_xml_elements", true)]
        public IEnumerable<string> IgnoredXmlElements => ignoredXmlElements;

        // TODO: Editor: For the global configuration this is always marked to clear the list when loaded to
        // remove the default list and replace it with the modified default list.  If not changed, nothing will
        // be written out.
        /// <summary>
        /// This read-only property returns an enumerable list of XML attribute names that will have their values
        /// spell checked.
        /// </summary>
        /// <remarks>If the first property value is set to "clear_inherited", prior attributes are cleared rather
        /// than inherited.  If not specified, prior attributes are retained.</remarks>
        [EditorConfigProperty("vsspell_spell_checked_xml_attributes", true)]
        public IEnumerable<string> SpellCheckedXmlAttributes => spellCheckedXmlAttributes;

        // TODO: Editor: For the global configuration this is always marked to clear the list when loaded to
        // remove the default list and replace it with the modified default list.  If not changed, nothing will
        // be written out.
        /// <summary>
        /// This read-only property returns the ignored classification settings
        /// </summary>
        /// <remarks>If the first property value is set to "clear_inherited", prior classifications are cleared
        /// rather than inherited.  If not specified, prior classifications are retained.</remarks>
        [EditorConfigProperty("vsspell_ignored_classifications", true)]
        public Dictionary<string, HashSet<string>> IgnoredClassifications => ignoredClassifications;

        /// <summary>
        /// This read-only property returns the recognized words loaded from code analysis dictionaries
        /// </summary>
        public IEnumerable<string> RecognizedWords => recognizedWords;

        /// <summary>
        /// This read-only property returns the unrecognized words loaded from code analysis dictionaries
        /// </summary>
        /// <value>The key is the unrecognized word and the value is the list of spelling alternatives</value>
        public IDictionary<string, IList<string>> UnrecognizedWords => unrecognizedWords;

        /// <summary>
        /// This read-only property returns the deprecated terms loaded from code analysis dictionaries
        /// </summary>
        /// <value>The key is the deprecated term and the value is the preferred alternate</value>
        public IDictionary<string, string> DeprecatedTerms => deprecatedTerms;

        /// <summary>
        /// This read-only property returns the compound terms loaded from code analysis dictionaries
        /// </summary>
        /// <value>The key is the discrete term and the value is the compound alternate</value>
        public IDictionary<string, string> CompoundTerms => compoundTerms;

        /// <summary>
        /// This is used to indicate whether or not to spell check any WPF text box within Visual Studio
        /// </summary>
        /// <value>The default is true.  This option only applies to the global configuration.</value>
        [DefaultValue(true), EditorConfigProperty("vsspell_enable_wpf_text_box_spell_checking")]
        public bool EnableWpfTextBoxSpellChecking { get; set; } = true;

        /// <summary>
        /// This read-only property returns an enumerable list of exclusion regular expressions that will be used
        /// to exclude WPF text boxes in Visual Studio editor and tool windows from being spell checked.
        /// </summary>
        /// <value>This option only applies to the global configuration.</value>
        [EditorConfigProperty("vsspell_visual_studio_id_exclusions")]
        public IEnumerable<Regex> VisualStudioIdExclusions => visualStudioIdExclusions;

        /// <summary>
        /// This read-only property returns the default list of ignored classifications
        /// </summary>
        public static IEnumerable<KeyValuePair<string, IEnumerable<string>>> DefaultIgnoredClassifications =>
            new[] {
                // Only comments are spell checked in EditorConfig files.  Ignore the string classification
                // use by the EditorConfig Language Service extension by Mads Kristensen.
                new KeyValuePair<string, IEnumerable<string>>("EditorConfig", new[] { "string" })
            };

        /// <summary>
        /// This read-only property returns the default list of ignored XML elements
        /// </summary>
        public static IEnumerable<string> DefaultIgnoredXmlElements =>
            new string[] { "c", "code", "codeEntityReference", "codeInline", "codeReference", "command",
                "environmentVariable", "fictitiousUri", "foreignPhrase", "link", "linkTarget", "linkUri",
                "localUri", "replaceable", "resheader", "see", "seealso", "style", "token",
                "unmanagedCodeEntityReference" };

        /// <summary>
        /// This read-only property returns the default list of spell checked XML attributes
        /// </summary>
        public static IEnumerable<string> DefaultSpellCheckedAttributes =>
            new[] { "altText", "Caption", "CompoundAlternate", "Content", "content", "Header", "lead",
                "PreferredAlternate", "SpellingAlternates", "term", "Text", "title", "ToolTip" };

        /// <summary>
        /// This read-only property returns the default list of excluded Visual Studio text box IDs
        /// </summary>
        public static IEnumerable<string> DefaultVisualStudioIdExclusions => new[] {
            @".*?\.(Placement\.PART_SearchBox|Placement\.PART_EditableTextBox|ServerNameTextBox|" +
                "filterTextBox|searchTextBox|tboxFilter|txtSearchText)(?# Various search text boxes)",
            @"Microsoft\.VisualStudio\.Dialogs\.NewProjectDialog.*(?# New Project dialog box)",
            @"Microsoft\.VisualStudio\.Web\.Publish\.PublishUI\.PublishDialog.*(?# Website publishing dialog box)",
            @"131369f2-062d-44a2-8671-91ff31efb4f4.*?\.globalSettingsSectionView.*(?# Git global settings)",
            @"fbcae063-e2c0-4ab1-a516-996ea3dafb72.*(?# SQL Server object explorer)",
            @"1c79180c-bb93-46d2-b4d3-f22e7015a6f1\.txtFindID(?# SHFB resource item editor)",
            @"581e89c0-e423-4453-bde3-a0403d5f380d\.ucEntityReferences\.txtFindName(?# SHFB entity references)",
            @"7aad2922-72a2-42c1-a077-85f5097a8fa7\.txtFindID(?# SHFB content layout editor)",
            @"d481fb70-9bf0-4868-9d4c-5db33c6565e1\.(txtFindID|txtTokenName)(?# SHFB Token editor)",
            @"b270807c-d8c6-49eb-8ebe-8e8d566637a1\.(.*\.txtFolder|.*\.txtFile|txtHtmlHelpName|" +
                "txtWebsiteAdContent|txtCatalogProductId|txtCatalogName|txtVendorName|txtValue|" +
                "pgProps.*|txtPreBuildEvent|txtPostBuildEvent)(?# SHFB property page and form controls)",
            @"(SandcastleBuilder\.Components\.UI\.|Microsoft\.Ddue\.Tools\.UI\.|SandcastleBuilder\.PlugIns\.).*" +
                "(?# SHFB build component and plug-in configuration forms)",
            @"64debe95-07ea-48ac-8744-af87605d624a.*(?# Spell checker solution/project tool window)",
            @"837501d0-c07d-47c6-aab7-9ba4d78d0038\.pnlPages\.(txtAdditionalFolder|txtAttributeName|" +
                "txtFilePattern|txtIgnoredElement|txtIgnoredWord|txtImportSettingsFile)(?# Spell checker config editor)",
            @"fd92f3d8-cebf-47b9-bb98-674a1618f364.*(?# Spell checker interactive tool window)",
            @"VisualStudio\.SpellChecker\.Editors\.Pages\.ExclusionExpressionAddEditForm\.txtExpression" +
                "(?# Spell checker exclusion expression editor)",
            @"da95c001-7ed0-4f46-b5f0-351125ab8bda.*(?# Web publishing dialog box)",
            @"Microsoft\.VisualStudio\.Web\.Publish\.PublishUI\.AdvancedPreCompileOptionsDialog.*" +
                "(?# Web publishing compile options dialog box)"
        };
        #endregion

        #region Constructors
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="spellCheckFilename">The name of the file for which the configuration was generated</param>
        private SpellCheckerConfiguration(string spellCheckFilename)
        {
            this.SpellCheckFilename = spellCheckFilename;

            dictionaryLanguages = new List<CultureInfo>();
            ignoredWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            ignoredKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            ignoredXmlElements = new HashSet<string>(DefaultIgnoredXmlElements);
            spellCheckedXmlAttributes = new HashSet<string>(DefaultSpellCheckedAttributes);
            recognizedWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            ignoredWordsFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            additionalDictionaryFolders = new List<string>();
            exclusionExpressions = new List<Regex>();
            visualStudioIdExclusions = new List<Regex>(DefaultVisualStudioIdExclusions.Select(p => new Regex(p)));
            deprecatedTerms = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            compoundTerms = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            unrecognizedWords = new Dictionary<string, IList<string>>(StringComparer.OrdinalIgnoreCase);
            ignoredClassifications = new Dictionary<string, HashSet<string>>();

            foreach(var kv in DefaultIgnoredClassifications)
                ignoredClassifications.Add(kv.Key, new HashSet<string>(kv.Value));
        }

        /// <summary>
        /// Static constructor
        /// </summary>
        static SpellCheckerConfiguration()
        {
            defaultValueCache = new Dictionary<string, object>();
            editorConfigAttrCache = new Dictionary<string, EditorConfigPropertyAttribute>();
            vsspellToPropertyCache = new Dictionary<string, PropertyInfo>();

            foreach(PropertyInfo property in typeof(SpellCheckerConfiguration).GetProperties(
              BindingFlags.Public | BindingFlags.Instance).Concat(typeof(CodeAnalyzerOptions).GetProperties(
              BindingFlags.Public | BindingFlags.Instance)).Concat(typeof(CodeAnalysisDictionaryOptions).GetProperties(
              BindingFlags.Public | BindingFlags.Instance)))
            {
                var defaultValue = property.GetCustomAttribute<DefaultValueAttribute>();

                if(defaultValue != null)
                    defaultValueCache.Add(property.Name, defaultValue.Value);

                var editorConfigAttr = property.GetCustomAttribute<EditorConfigPropertyAttribute>();

                if(editorConfigAttr != null)
                {
                    editorConfigAttrCache.Add(property.Name, editorConfigAttr);
                    vsspellToPropertyCache.Add(editorConfigAttr.PropertyName, property);
                }
            }
        }
        #endregion

        #region Helper methods
        //=====================================================================

        /// <summary>
        /// This is used to get the default value for the given configuration property name
        /// </summary>
        /// <param name="propertyName">The property name for which to get the default value</param>
        /// <returns>The default value for the property or null if there is no default</returns>
        public static object DefaultValueFor(string propertyName)
        {
            if(defaultValueCache.TryGetValue(propertyName, out object value))
                return value;

            return null;
        }

        /// <summary>
        /// This is used to get the .editorconfig property settings for the given configuration property name
        /// </summary>
        /// <param name="propertyName">The property name for which to get the .editorconfig property settings</param>
        /// <returns>The .editorconfig settings for the property or null if there are none</returns>
        public static EditorConfigPropertyAttribute EditorConfigSettingsFor(string propertyName)
        {
            if(editorConfigAttrCache.TryGetValue(propertyName, out EditorConfigPropertyAttribute value))
                return value;

            return null;
        }

        /// <summary>
        /// This is used to get the configuration property name for the given .editorconfig setting name
        /// </summary>
        /// <param name="settingName">The .editorconfig setting name for which to get the property name</param>
        /// <returns>The property name for the .editorconfig setting name.  The .editorconfig setting name can be
        /// the base name or can contain a section ID suffix.  If not found, it returns an empty string.</returns>
        public static string PropertyNameForEditorConfigSetting(string settingName)
        {
            if(settingName == null)
                throw new ArgumentNullException(nameof(settingName));

            if(vsspellToPropertyCache.TryGetValue(settingName, out var configProperty))
                return configProperty.Name;

            string propName = settingName;
            int separator = propName.LastIndexOf('_');

            while(separator != -1)
            {
                propName = propName.Substring(0, separator);

                if(vsspellToPropertyCache.TryGetValue(propName, out configProperty))
                    break;

                separator = propName.LastIndexOf('_');
            }

            if(separator == -1)
            {
                System.Diagnostics.Debug.WriteLine("Unknown .editorconfig property name: " + settingName);
                System.Diagnostics.Debugger.Break();
                return String.Empty;
            }

            return configProperty.Name;
        }

        /// <summary>
        /// This method provides a thread-safe way to check for a globally ignored word
        /// </summary>
        /// <param name="word">The word to check</param>
        /// <returns>True if it should be ignored, false if not</returns>
        public bool ShouldIgnoreWord(string word)
        {
            if(String.IsNullOrWhiteSpace(word))
                return true;

            return ignoredWords.Contains(word) || ignoredKeywords.Contains(word);
        }

        /// <summary>
        /// This is used to get a set of ignored classifications for the given content type
        /// </summary>
        /// <param name="contentType">The content type for which to get ignored classifications</param>
        /// <returns>An enumerable list of ignored classifications or an empty set if there are none</returns>
        public IEnumerable<string> IgnoredClassificationsFor(string contentType)
        {
            if(!ignoredClassifications.TryGetValue(contentType, out HashSet<string> classifications))
                classifications = new HashSet<string>();

            return classifications;
        }
        #endregion

        #region Load configuration methods
        //=====================================================================

        /// <summary>
        /// This is used to generate an enumerable list of spell checking properties for the given file loaded
        /// from the global spell checker configuration file.
        /// </summary>
        /// <param name="filename">The filename for which to get the global spell checker properties</param>
        /// <remarks>Settings from the global spell checker configuration file serve as the base set of properties
        /// and can be overridden by settings from .globalconfig and .editorconfig files.</remarks>
        public static IEnumerable<(string PropertyName, string Value)> GlobalConfigurationPropertiesFor(
          string filename)
        {
            var editorConfigProperties = new List<SectionLine>();
            var replaceValues = new Dictionary<string, SectionLine>();
            var editorConfig = EditorConfigFile.FromFile(GlobalConfigurationFilename);

            foreach(var section in editorConfig.Sections.Where(s => s.IsMatchForFile(filename)))
            {
                foreach(var property in section.SpellCheckerProperties)
                {
                    // If it doesn't exist, add it
                    if(!replaceValues.TryGetValue(property.PropertyName, out var existingProperty))
                    {
                        editorConfigProperties.Add(property);
                        replaceValues.Add(property.PropertyName, property);
                    }
                    else
                    {
                        // If it does exist, replace the value
                        existingProperty.PropertyValue = property.PropertyValue;
                    }
                }
            }

            // If the global configuration doesn't contain any explicit ignore words file, include the default one
            string ignoredWordsPropName = EditorConfigSettingsFor(nameof(IgnoredWords)).PropertyName;
            string file = Path.Combine(GlobalConfigurationFilePath, "IgnoredWords.dic");

            if(File.Exists(file))
            {
                var iwp = editorConfigProperties.FirstOrDefault(p => p.PropertyName.StartsWith(ignoredWordsPropName,
                    StringComparison.OrdinalIgnoreCase)) ??
                    new SectionLine { PropertyName = ignoredWordsPropName, PropertyValue = String.Empty };

                if(iwp.PropertyValue.IndexOf("File:", StringComparison.OrdinalIgnoreCase) == -1)
                {
                    if(!String.IsNullOrWhiteSpace(iwp.PropertyValue))
                        iwp.PropertyValue = "File:" + file + "|" + iwp.PropertyValue;
                    else
                        iwp.PropertyValue = "File:" + file;
                }
            }

            return editorConfigProperties.Select(p => (p.PropertyName, p.PropertyValue));
        }

        /// <summary>
        /// This is used to generate an enumerable list of spell checking properties for the given file loaded
        /// from .globalconfig and .editorconfig files that apply to it.
        /// </summary>
        /// <param name="filename">The filename for which to get the spell checker properties</param>
        /// <param name="additionalGlobalConfigFiles">Additional .globalconfig files to include such as those
        /// from <c>GlobalAnalyzerConfigFiles</c> project items.</param>
        /// <param name="additionalEditorConfigFiles">Additional .editorconfig files to include such as those
        /// from <c>EditorConfigFiles</c> project items.</param>
        /// <remarks><para>Settings from the global spell checker configuration will not be returned.  They will
        /// be included when an actual configuration is generated.</para>
        /// 
        /// <para>.globalconfig files are handled first in any order for those found in the file's path from its
        /// current folder to the root plus any additional ones passed to the method.  A <c>global_level</c>
        /// property is used to give precedence to duplicate properties in the global files.  If a level is not
        /// specified, it defaults to 100 for files named .globalconfig and 0 for all others.  If a property
        /// appear in multiple global files, the one with the higher global level wins.  If their levels are
        /// equal, the properties are ignored and neither is included.</para>
        ///
        /// <para>.editorconfig files are handled after that in folder order for any found in the root folder to
        /// the file folder followed by any additional ones passed to the method.  Anything in the preamble is
        /// ignored.  If a section glob has no folder, it matches on the filename alone regardless of where the
        /// config file is in the folder hierarchy.  If a folder is present in the glob, subfolders in the
        /// filename being compared will be taken into consideration along with the filename.</para>
        /// </remarks>
        public static IEnumerable<(string PropertyName, string Value)> SpellCheckingPropertiesFor(string filename,
          IEnumerable<string> additionalGlobalConfigFiles, IEnumerable<string> additionalEditorConfigFiles)
        {
            if(additionalGlobalConfigFiles == null)
                additionalGlobalConfigFiles = Enumerable.Empty<string>();

            if(additionalEditorConfigFiles == null)
                additionalEditorConfigFiles = Enumerable.Empty<string>();

            filename = Path.GetFullPath(filename);

            string path = Path.GetDirectoryName(filename);
            var globalProperties = new Dictionary<string, (SectionLine Property, int GlobalLevel)>(
                StringComparer.OrdinalIgnoreCase);
            var discardedGlobals = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Get global properties first as the files aren't processed in any particular order.  The global
            // level in each determines which copies of duplicate properties are kept or discarded.
            foreach(var file in EditorConfigFile.GlobalConfigFilesIn(path).Concat(additionalGlobalConfigFiles))
            {
                var globalConfig = EditorConfigFile.FromFile(file);

                if(globalConfig.Sections.Count != 0 && globalConfig.Sections[0].IsGlobal)
                {
                    foreach(var property in globalConfig.Sections[0].SpellCheckerProperties)
                    {
                        // If it's not there, add it
                        if(!globalProperties.TryGetValue(property.PropertyName, out var existingProperty))
                            globalProperties.Add(property.PropertyName, (property, globalConfig.GlobalLevel));
                        else
                        {
                            // If it exists and the new property has a higher global level, use it instead
                            if(existingProperty.GlobalLevel < globalConfig.GlobalLevel)
                            {
                                globalProperties[property.PropertyName] = (property, globalConfig.GlobalLevel);

                                // If discarded earlier due to a lower matching global level, remove it from the
                                // discard set.
                                if(discardedGlobals.Contains(property.PropertyName))
                                    discardedGlobals.Remove(property.PropertyName);
                            }
                            else
                            {
                                // If the global levels are equal, mark it for discarding.  We keep it in the
                                // dictionary though in case a later file has a copy with a higher global level
                                // that we will keep.
                                if(existingProperty.GlobalLevel == globalConfig.GlobalLevel)
                                    discardedGlobals.Add(property.PropertyName);
                            }
                        }
                    }
                }
            }

            // Use the global properties as the starting set of .editorconfig properties excluding discarded ones
            foreach(string discard in discardedGlobals)
                globalProperties.Remove(discard);

            var editorConfigProperties = new List<SectionLine>(globalProperties.Values.Select(p => p.Property));
            var replaceValues = editorConfigProperties.ToDictionary(k => k.PropertyName, v => v);
            bool addedProperties = false;

            // Load .editorconfig file properties next in folder order.  Additional files are processed last in
            // folder order.
            foreach(var file in EditorConfigFile.EditorConfigFilesIn(path).OrderBy(f => f).Concat(
              additionalEditorConfigFiles.OrderBy(f => f)))
            {
                var editorConfig = EditorConfigFile.FromFile(file);

                // If it's a root configuration and we've added properties from other files, reset the collection
                // to the global set.
                if(addedProperties && editorConfig.IsRoot)
                {
                    editorConfigProperties.Clear();
                    replaceValues.Clear();

                    editorConfigProperties.AddRange(globalProperties.Values.Select(p => p.Property));

                    foreach(var p in editorConfigProperties)
                        replaceValues.Add(p.PropertyName, p);
                }

                foreach(var section in editorConfig.Sections.Where(s => s.IsMatchForFile(filename)))
                {
                    foreach(var property in section.SpellCheckerProperties)
                    {
                        addedProperties = true;

                        // If it doesn't exist, add it
                        if(!replaceValues.TryGetValue(property.PropertyName, out var existingProperty))
                        {
                            editorConfigProperties.Add(property);
                            replaceValues.Add(property.PropertyName, property);
                        }
                        else
                        {
                            // If it does exist, replace the value
                            existingProperty.PropertyValue = property.PropertyValue;
                        }
                    }
                }
            }

            return editorConfigProperties.Select(p => (p.PropertyName, p.PropertyValue));
        }

        /// <summary>
        /// This is used to generate a spell checker configuration for the given file from the given set of
        /// spell checker properties.
        /// </summary>
        /// <param name="filename">The filename for which to generate the configuration</param>
        /// <param name="properties">The properties used to create the configuration</param>
        /// <returns>A spell checker configuration instance</returns>
        public static SpellCheckerConfiguration CreateSpellCheckerConfigurationFor(string filename,
          IEnumerable<(string PropertyName, string Value)> properties)
        {
            filename = Path.GetFullPath(filename);

            var configuration = new SpellCheckerConfiguration(filename);
            var globalProperties = GlobalConfigurationPropertiesFor(filename);
            var importedConfigFiles = new HashSet<string>();

            configuration.ApplyProperties(Path.GetDirectoryName(filename), importedConfigFiles,
                globalProperties.Concat(properties));

            // Always add the Ignore Spelling directive expression as we don't want the directive words included
            // when spell checking with non-English dictionaries.
            string directiveExp = Utility.IgnoreSpellingDirectiveRegex.ToString();

            if(!configuration.exclusionExpressions.Any(e => e.ToString().Equals(directiveExp, StringComparison.Ordinal)))
                configuration.exclusionExpressions.Add(new Regex(directiveExp, Utility.IgnoreSpellingDirectiveRegex.Options));

            // Always ensure we have at least the default language if none are specified in the global
            // configuration file.
            if(configuration.dictionaryLanguages.Count == 0)
                configuration.dictionaryLanguages.Add(new CultureInfo("en-US"));

            return configuration;
        }

        /// <summary>
        /// This is used to apply the given set of properties to the configuration
        /// </summary>
        /// <param name="basePath">The base path used to resolve relative paths in the configuration options
        /// for ignored words files and imported configuration files.</param>
        /// <param name="importedConfigFiles">This is used to track previously imported configuration files
        /// to prevent circular references.</param>
        /// <param name="properties">The properties used to create the configuration</param>
        private void ApplyProperties(string basePath, HashSet<string> importedConfigFiles,
          IEnumerable<(string PropertyName, string Value)> properties)
        {
            foreach(var p in properties)
            {
                bool wasHandled = false;

                // Handle simple properties
                if(vsspellToPropertyCache.TryGetValue(p.PropertyName, out var configProperty) &&
                  !EditorConfigSettingsFor(configProperty.Name).CanHaveMultipleInstances)
                {
                    object target;

                    if(configProperty.ReflectedType == typeof(SpellCheckerConfiguration))
                        target = this;
                    else
                    {
                        if(configProperty.ReflectedType == typeof(CodeAnalyzerOptions))
                            target = this.CodeAnalyzerOptions;
                        else
                            target = this.CadOptions;
                    }

                    switch(configProperty.PropertyType)
                    {
                        case Type t when t == typeof(bool):
                            if(Boolean.TryParse(p.Value, out bool b))
                                configProperty.SetValue(target, b);

                            wasHandled = true;
                            break;

                        case Type t when t == typeof(string):
                            configProperty.SetValue(target, p.Value);
                            wasHandled = true;
                            break;

                        case Type t when t == typeof(IgnoredCharacterClass):
                            if(Enum.TryParse(p.Value, out IgnoredCharacterClass i))
                                configProperty.SetValue(target, i);

                            wasHandled = true;
                            break;

                        case Type t when t == typeof(RecognizedWordHandling):
                            if(Enum.TryParse(p.Value, out RecognizedWordHandling r))
                                configProperty.SetValue(target, r);

                            wasHandled = true;
                            break;

                        case Type t when t == typeof(Guid):
                            if(Guid.TryParse(p.Value, out Guid g))
                                configProperty.SetValue(target, g);

                            wasHandled = true;
                            break;

                        default:
                            // Ignore unhandled types unless debugging
                            System.Diagnostics.Debug.WriteLine("Unknown property type for: " + p.PropertyName);
                            System.Diagnostics.Debugger.Break();
                            wasHandled = true;
                            break;
                    }
                }

                if(wasHandled)
                    continue;

                // Find a multi-valued property by its base name
                if(configProperty == null)
                {
                    string propName = p.PropertyName;
                    int separator = propName.LastIndexOf('_');

                    while(separator != -1)
                    {
                        propName = propName.Substring(0, separator);

                        if(vsspellToPropertyCache.TryGetValue(propName, out configProperty))
                            break;

                        separator = propName.LastIndexOf('_');
                    }

                    if(separator == -1)
                    {
                        System.Diagnostics.Debug.WriteLine("Unknown property: " + p.PropertyName);
                        System.Diagnostics.Debugger.Break();
                        continue;
                    }
                }

                switch(configProperty.Name)
                {
                    case nameof(ImportSettingsFile):
                        this.ImportSettingsFrom(p.Value, basePath, importedConfigFiles);
                        break;

                    case nameof(VisualStudioIdExclusions):
                        // This is only applicable to the global configuration and never inherits values
                        visualStudioIdExclusions.Clear();
                        visualStudioIdExclusions.AddRange(p.Value.ToRegexes());
                        break;

                    case nameof(ExclusionExpressions):
                        var tempRegexes = p.Value.ToRegexes();

                        if(tempRegexes.Any())
                        {
                            if(!tempRegexes.First().ToString().Equals("clear_inherited",
                              StringComparison.OrdinalIgnoreCase))
                            {
                                var tempHashSet = new HashSet<string>(exclusionExpressions.Select(r => r.ToString()));

                                foreach(Regex exp in tempRegexes)
                                    if(!tempHashSet.Contains(exp.ToString()))
                                    {
                                        exclusionExpressions.Add(exp);
                                        tempHashSet.Add(exp.ToString());
                                    }
                            }
                            else
                            {
                                exclusionExpressions.Clear();
                                exclusionExpressions.AddRange(tempRegexes);
                            }
                        }
                        break;

                    case nameof(IgnoredWords):
                        foreach(string word in p.Value.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            if(word.Equals("clear_inherited", StringComparison.OrdinalIgnoreCase))
                                ignoredWords.Clear();
                            else
                            {
                                if(!word.StartsWith("File:", StringComparison.OrdinalIgnoreCase))
                                    ignoredWords.Add(word);
                                else
                                {
                                    string ignoredWordsFile = ResolveFilePath(word.Substring(5), basePath);

                                    if(ignoredWordsFile != null)
                                    {
                                        ignoredWords.UnionWith(Utility.LoadUserDictionary(ignoredWordsFile, false, false));

                                        if(!ignoredWordsFiles.Contains(ignoredWordsFile))
                                            ignoredWordsFiles.Add(ignoredWordsFile);
                                    }
                                }
                            }
                        }
                        break;

                    case nameof(IgnoredKeywords):
                        foreach(string word in p.Value.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
                            ignoredKeywords.Add(word);
                        break;

                    case nameof(AdditionalDictionaryFolders):
                        foreach(string folder in p.Value.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            if(folder.Equals("clear_inherited", StringComparison.OrdinalIgnoreCase))
                                additionalDictionaryFolders.Clear();
                            else
                            {
                                string additionalFolder = ResolveFolderPath(folder, basePath);

                                if(additionalFolder != null)
                                    additionalDictionaryFolders.Add(additionalFolder);
                            }
                        }
                        break;

                    case nameof(IgnoredXmlElements):
                        var tempElements = p.Value.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);

                        if(tempElements.Length != 0)
                        {
                            if(tempElements[0].Equals("clear_inherited", StringComparison.OrdinalIgnoreCase))
                                ignoredXmlElements.Clear();
                            
                            ignoredXmlElements.UnionWith(tempElements);
                        }
                        break;

                    case nameof(SpellCheckedXmlAttributes):
                        var tempAttrs = p.Value.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);

                        if(tempAttrs.Length != 0)
                        {
                            if(tempAttrs[0].Equals("clear_inherited", StringComparison.OrdinalIgnoreCase))
                                spellCheckedXmlAttributes.Clear();

                            spellCheckedXmlAttributes.UnionWith(tempAttrs);
                        }
                        break;

                    case nameof(IgnoredClassifications):
                        foreach(string classification in p.Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            if(classification.Equals("clear_inherited", StringComparison.OrdinalIgnoreCase))
                                ignoredClassifications.Clear();
                            else
                            {
                                var types = classification.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

                                if(types.Length > 1)
                                {
                                    if(!ignoredClassifications.TryGetValue(types[0],
                                      out HashSet<string> classifications))
                                    {
                                        classifications = new HashSet<string>();
                                        ignoredClassifications.Add(types[0], classifications);
                                    }

                                    classifications.UnionWith(types.Skip(1));
                                }
                            }
                        }
                        break;

                    case nameof(DictionaryLanguages):
                        var tempLanguages = p.Value.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                        var languageSet = new HashSet<string>(tempLanguages, StringComparer.OrdinalIgnoreCase);

                        // If there is an "inherited" entry that marks the inherited languages placeholder,
                        // insert the inherited languages at that point
                        for(int idx = 0; idx < tempLanguages.Count; idx++)
                        {
                            if(tempLanguages[idx].Equals("inherited", StringComparison.OrdinalIgnoreCase))
                            {
                                tempLanguages.RemoveAt(idx);

                                // If an inherited language matches a language in the configuration file, it is
                                // left at its new location thus overriding the inherited language location.
                                foreach(var lang in dictionaryLanguages)
                                {
                                    if(!languageSet.Contains(lang.Name))
                                        tempLanguages.Insert(idx++, lang.Name);
                                }

                                break;
                            }
                        }

                        if(tempLanguages.Count != 0)
                        {
                            dictionaryLanguages.Clear();
                            dictionaryLanguages.AddRange(tempLanguages.Select(l => new CultureInfo(l)));
                        }
                        break;

                    default:
                        System.Diagnostics.Debug.WriteLine(configProperty.Name);
                        break;
                }
            }
        }

        /// <summary>
        /// This is used to try and find a file from its relative path based on the folder hierarchy of the given
        /// base path.
        /// </summary>
        /// <param name="filename">The filename for which to find the full path</param>
        /// <param name="basePath">The base path</param>
        /// <returns>The full path to the file if found or null if not</returns>
        private static string ResolveFilePath(string filename, string basePath)
        {
            if(String.IsNullOrWhiteSpace(filename))
                return null;

            if(filename.IndexOf('%') != -1)
                filename = Environment.ExpandEnvironmentVariables(filename);

            if(!Path.IsPathRooted(filename))
            {
                string path = basePath, file;

                do
                {
                    file = Path.GetFullPath(Path.Combine(path, filename));
                    path = Path.GetDirectoryName(path);

                } while(!File.Exists(file) && path != null);

                filename = file;
            }

            return File.Exists(filename) ? filename : null;
        }

        /// <summary>
        /// This is used to try and find a folder from its relative path based on the folder hierarchy of the
        /// given base path.
        /// </summary>
        /// <param name="folder">The folder for which to find the full path</param>
        /// <param name="basePath">The base path</param>
        /// <returns>The full path to the folder if found or null if not</returns>
        private static string ResolveFolderPath(string folder, string basePath)
        {
            if(String.IsNullOrWhiteSpace(folder))
                return null;

            if(folder.IndexOf('%') != -1)
                folder = Environment.ExpandEnvironmentVariables(folder);

            if(!Path.IsPathRooted(folder))
            {
                string path = basePath, subfolder;

                do
                {
                    subfolder = Path.GetFullPath(Path.Combine(path, folder));
                    path = Path.GetDirectoryName(path);

                } while(!Directory.Exists(subfolder) && path != null);

                folder = subfolder;
            }

            return Directory.Exists(folder) ? folder : null;
        }

        /// <summary>
        /// This is used to recursively import settings from another configuration file
        /// </summary>
        /// <param name="configFile">The starting configuration file from which to import settings</param>
        /// <param name="basePath">The base path to use when searching for configuration files with a relative
        /// path</param>
        /// <param name="importedConfigFiles">A list of previously imported configuration files use to prevent
        /// circular references</param>
        private void ImportSettingsFrom(string configFile, string basePath, HashSet<string> importedConfigFiles)
        {
            configFile = ResolveFilePath(configFile, basePath);

            if(configFile != null && !importedConfigFiles.Contains(configFile))
            {
                importedConfigFiles.Add(configFile);

                var editorConfigProperties = new List<SectionLine>();
                var replaceValues = new Dictionary<string, SectionLine>();
                var editorConfig = EditorConfigFile.FromFile(configFile);

                foreach(var section in editorConfig.Sections.Where(s => s.IsMatchForFile(this.SpellCheckFilename)))
                {
                    foreach(var property in section.SpellCheckerProperties)
                    {
                        // If it doesn't exist, add it
                        if(!replaceValues.TryGetValue(property.PropertyName, out var existingProperty))
                        {
                            editorConfigProperties.Add(property);
                            replaceValues.Add(property.PropertyName, property);
                        }
                        else
                        {
                            // If it does exist, replace the value
                            existingProperty.PropertyValue = property.PropertyValue;
                        }
                    }
                }

                if(editorConfigProperties.Count != 0)
                {
                    this.ApplyProperties(Path.GetDirectoryName(configFile), importedConfigFiles,
                        editorConfigProperties.Select(p => (p.PropertyName, p.PropertyValue)));
                }
            }
        }

        /// <summary>
        /// This is used to import spelling words and ignored words from a code analysis dictionary file
        /// </summary>
        /// <param name="filename">The code analysis dictionary file to import</param>
        public void ImportCodeAnalysisDictionary(string filename)
        {
            XDocument settings = XDocument.Load(filename);
            XElement root = settings.Root, option;

            option = root.XPathSelectElement("Words/Recognized");

            if(this.CadOptions.RecognizedWordHandling != RecognizedWordHandling.None && option != null)
            {
                foreach(var word in option.Elements("Word"))
                {
                    if(!String.IsNullOrWhiteSpace(word.Value))
                    {
                        switch(this.CadOptions.RecognizedWordHandling)
                        {
                            case RecognizedWordHandling.IgnoreAllWords:
                                ignoredWords.Add(word.Value);
                                break;

                            case RecognizedWordHandling.AddAllWords:
                                recognizedWords.Add(word.Value);
                                break;

                            default:    // Attribute determines usage
                                if((string)word.Attribute("Spelling") == "Add")
                                    recognizedWords.Add(word.Value);
                                else
                                {
                                    if((string)word.Attribute("Spelling") == "Ignore")
                                        ignoredWords.Add(word.Value);
                                }

                                // Any other value is treated as None and it passes through to the spell checker
                                // like any other word.
                                break;
                        }
                    }
                }
            }

            option = root.XPathSelectElement("Words/Unrecognized");

            if(this.CadOptions.TreatUnrecognizedWordsAsMisspelled && option != null)
            {
                foreach(var word in option.Elements("Word"))
                {
                    if(!String.IsNullOrWhiteSpace(word.Value))
                    {
                        unrecognizedWords[word.Value] = new List<string>(
                            ((string)word.Attribute("SpellingAlternates") ?? String.Empty).Split(
                                new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries));
                    }
                }
            }

            option = root.XPathSelectElement("Words/Deprecated");

            if(this.CadOptions.TreatDeprecatedTermsAsMisspelled && option != null)
            {
                foreach(var term in option.Elements("Term"))
                {
                    if(!String.IsNullOrWhiteSpace(term.Value))
                        deprecatedTerms[term.Value] = ((string)term.Attribute("PreferredAlternate")).ToWords();
                }
            }

            option = root.XPathSelectElement("Words/Compound");

            if(this.CadOptions.TreatCompoundTermsAsMisspelled && option != null)
            {
                foreach(var term in option.Elements("Term"))
                {
                    if(!String.IsNullOrWhiteSpace(term.Value))
                        compoundTerms[term.Value] = ((string)term.Attribute("CompoundAlternate")).ToWords();
                }
            }

            option = root.XPathSelectElement("Acronyms/CasingExceptions");

            if(this.CadOptions.TreatCasingExceptionsAsIgnoredWords && option != null)
            {
                foreach(var acronym in option.Elements("Acronym"))
                {
                    if(!String.IsNullOrWhiteSpace(acronym.Value))
                        ignoredWords.Add(acronym.Value);
                }
            }
        }
        #endregion
    }
}
