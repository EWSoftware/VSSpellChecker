//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : SpellCheckerConfiguration.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 10/27/2015
// Note    : Copyright 2015, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains the class used to contain the spell checker's configuration settings
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
//===============================================================================================================
// 02/01/2015  EFW  Refactored the configuration settings to allow for solution and project specific settings
// 07/22/2015  EFW  Added support for selecting multiple languages
//===============================================================================================================

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;

namespace VisualStudio.SpellChecker.Configuration
{
    /// <summary>
    /// This class is used to contain the spell checker's configuration
    /// </summary>
    /// <remarks>Settings are stored in an XML file in the user's local application data folder and will be used
    /// by all versions of Visual Studio in which the package is installed.</remarks>
    public class SpellCheckerConfiguration
    {
        #region Private data members
        //=====================================================================

        private CSharpOptions csharpOptions;
        private CodeAnalysisDictionaryOptions cadOptions;

        private HashSet<string> ignoredWords, ignoredXmlElements, spellCheckedXmlAttributes, excludedExtensions,
            recognizedWords;
        private List<CultureInfo> dictionaryLanguages;
        private List<string> additionalDictionaryFolders;
        private List<Regex> exclusionExpressions;
        private Dictionary<string, string> deprecatedTerms, compoundTerms;
        private Dictionary<string, IList<string>> unrecognizedWords;
        #endregion

        #region Properties
        //=====================================================================

        /// <summary>
        /// This read-only property returns a list of dictionary languages to be used when spell checking
        /// </summary>
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
        /// This is used to get or set whether or not to spell check the file as you type
        /// </summary>
        /// <value>This is true by default</value>
        [DefaultValue(true)]
        public bool SpellCheckAsYouType { get; set; }

        /// <summary>
        /// This is used to get or set whether or not to spell check the file as part of the solution/project
        /// spell checking process.
        /// </summary>
        /// <value>This is true by default</value>
        [DefaultValue(true)]
        public bool IncludeInProjectSpellCheck { get; set; }

        /// <summary>
        /// This is used to get or set whether or not to detect doubled words as part of the spell checking
        /// process.
        /// </summary>
        /// <value>This is true by default</value>
        [DefaultValue(true)]
        public bool DetectDoubledWords { get; set; }

        /// <summary>
        /// This is used to get or set whether or not to ignore words containing digits
        /// </summary>
        /// <value>This is true by default</value>
        [DefaultValue(true)]
        public bool IgnoreWordsWithDigits { get; set; }

        /// <summary>
        /// This is used to get or set whether or not to ignore words in all uppercase
        /// </summary>
        /// <value>This is true by default</value>
        [DefaultValue(true)]
        public bool IgnoreWordsInAllUppercase { get; set; }

        /// <summary>
        /// This is used to get or set whether or not to ignore .NET and C-style format string specifiers
        /// </summary>
        /// <value>This is true by default</value>
        [DefaultValue(true)]
        public bool IgnoreFormatSpecifiers { get; set; }

        /// <summary>
        /// This is used to get or set whether or not to ignore words that look like filenames or e-mail
        /// addresses.
        /// </summary>
        /// <value>This is true by default</value>
        [DefaultValue(true)]
        public bool IgnoreFilenamesAndEMailAddresses { get; set; }

        /// <summary>
        /// This is used to get or set whether or not to ignore XML elements in the text being spell checked
        /// (text within '&amp;lt;' and '&amp;gt;').
        /// </summary>
        /// <value>This is true by default</value>
        [DefaultValue(true)]
        public bool IgnoreXmlElementsInText { get; set; }

        /// <summary>
        /// This is used to get or set whether or not to ignore words by character class
        /// </summary>
        /// <remarks>This provides a simplistic way of ignoring some words in mixed language files.  It works
        /// best for spell checking English text in files that also contain Cyrillic or Asian text.  The default
        /// is <c>None</c> to include all words regardless of the characters they contain.</remarks>
        [DefaultValue(IgnoredCharacterClass.None)]
        public IgnoredCharacterClass IgnoreCharacterClass { get; set; }

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
        public bool IgnoreMnemonics { get; set; }

        /// <summary>
        /// This is used to get or set whether or not to try to determine the language for resource files based
        /// on their filename (i.e. LocalizedForm.de-DE.resx).
        /// </summary>
        /// <value>This is true by default</value>
        [DefaultValue(true)]
        public bool DetermineResourceFileLanguageFromName { get; set; }

        /// <summary>
        /// This read-only property returns the C# source code file options
        /// </summary>
        public CSharpOptions CSharpOptions
        {
            get { return csharpOptions; }
        }

        /// <summary>
        /// This read-only property returns the code analysis dictionary options
        /// </summary>
        public CodeAnalysisDictionaryOptions CadOptions
        {
            get { return cadOptions; }
        }

        /// <summary>
        /// This is used to indicate whether or not excluded extensions are inherited by other configurations
        /// </summary>
        /// <value>The default is true so that sub-configurations inherit all excluded extensions from higher
        /// level configurations.</value>
        [DefaultValue(true)]
        public bool InheritExcludedExtensions { get; set; }

        /// <summary>
        /// This read-only property returns an enumerable list of excluded filename extensions
        /// </summary>
        /// <remarks>Filenames with an extension in this set will not be spell checked.  An entry consisting of a
        /// single period will exclude files without an extension.</remarks>
        public IEnumerable<string> ExcludedExtensions
        {
            get { return excludedExtensions; }
        }

        /// <summary>
        /// This is used to indicate whether or not additional dictionary folders are inherited by other
        /// configurations.
        /// </summary>
        /// <value>The default is true so that sub-configurations inherit all additional dictionary folders from
        /// higher level configurations.</value>
        [DefaultValue(true)]
        public bool InheritAdditionalDictionaryFolders { get; set; }

        /// <summary>
        /// This read-only property returns an enumerable list of additional dictionary folders
        /// </summary>
        /// <remarks>When searching for dictionaries, these folders will be included in the search.  This allows
        /// for solution and project-specific dictionaries.</remarks>
        public IEnumerable<string> AdditionalDictionaryFolders
        {
            get { return additionalDictionaryFolders; }
        }

        /// <summary>
        /// This is used to indicate whether or not ignored words are inherited by other configurations
        /// </summary>
        /// <value>The default is true so that sub-configurations inherit all ignored words from higher level
        /// configurations.</value>
        [DefaultValue(true)]
        public bool InheritIgnoredWords { get; set; }

        /// <summary>
        /// This read-only property returns an enumerable list of ignored words that will not be spell checked
        /// </summary>
        public IEnumerable<string> IgnoredWords
        {
            get { return ignoredWords; }
        }

        /// <summary>
        /// This is used to indicate whether or not exclusion expressions are inherited by other configurations
        /// </summary>
        /// <value>The default is true so that sub-configurations inherit all exclusion expressions from higher
        /// level configurations.</value>
        [DefaultValue(true)]
        public bool InheritExclusionExpressions { get; set; }

        /// <summary>
        /// This read-only property returns an enumerable list of exclusion regular expressions that will be used
        /// to find ranges of text that should not be spell checked.
        /// </summary>
        public IEnumerable<Regex> ExclusionExpressions
        {
            get { return exclusionExpressions; }
        }

        /// <summary>
        /// This is used to indicate whether or not ignored XML elements and included attributes are inherited by
        /// other configurations.
        /// </summary>
        /// <value>The default is true so that sub-configurations inherit all ignored XML elements and included
        /// attributes from higher level configurations.</value>
        [DefaultValue(true)]
        public bool InheritXmlSettings { get; set; }

        /// <summary>
        /// This read-only property returns an enumerable list of ignored XML element names that will not have
        /// their content spell checked.
        /// </summary>
        public IEnumerable<string> IgnoredXmlElements
        {
            get { return ignoredXmlElements; }
        }

        /// <summary>
        /// This read-only property returns an enumerable list of XML attribute names that will not have their
        /// values spell checked.
        /// </summary>
        public IEnumerable<string> SpellCheckedXmlAttributes
        {
            get { return spellCheckedXmlAttributes; }
        }

        /// <summary>
        /// This read-only property returns the recognized words loaded from code analysis dictionaries
        /// </summary>
        public IEnumerable<string> RecognizedWords
        {
            get { return recognizedWords; }
        }

        /// <summary>
        /// This read-only property returns the unrecognized words loaded from code analysis dictionaries
        /// </summary>
        /// <value>The key is the unrecognized word and the value is the list of spelling alternatives</value>
        public IDictionary<string, IList<string>> UnrecognizedWords
        {
            get { return unrecognizedWords; }
        }

        /// <summary>
        /// This read-only property returns the deprecated terms loaded from code analysis dictionaries
        /// </summary>
        /// <value>The key is the deprecated term and the value is the preferred alternate</value>
        public IDictionary<string, string> DeprecatedTerms
        {
            get { return deprecatedTerms; }
        }

        /// <summary>
        /// This read-only property returns the compound terms loaded from code analysis dictionaries
        /// </summary>
        /// <value>The key is the discrete term and the value is the compound alternate</value>
        public IDictionary<string, string> CompoundTerms
        {
            get { return compoundTerms; }
        }

        /// <summary>
        /// This read-only property returns the default list of ignored words
        /// </summary>
        /// <remarks>The default list includes words starting with what looks like an escape sequence such as
        /// various Doxygen documentation tags (i.e. \anchor, \ref, \remarks, etc.).</remarks>
        public static IEnumerable<string> DefaultIgnoredWords
        {
            get
            {
                return new string[] { "\\addindex", "\\addtogroup", "\\anchor", "\\arg", "\\attention",
                    "\\author", "\\authors", "\\brief", "\\bug", "\\file", "\\fn", "\\name", "\\namespace",
                    "\\nosubgrouping", "\\note", "\\ref", "\\refitem", "\\related", "\\relates", "\\relatedalso",
                    "\\relatesalso", "\\remark", "\\remarks", "\\result", "\\return", "\\returns", "\\retval",
                    "\\rtfonly", "\\tableofcontents", "\\test", "\\throw", "\\throws", "\\todo", "\\tparam",
                    "\\typedef", "\\var", "\\verbatim", "\\verbinclude", "\\version", "\\vhdlflow"};
            }
        }

        /// <summary>
        /// This read-only property returns the default list of ignored XML elements
        /// </summary>
        public static IEnumerable<string> DefaultIgnoredXmlElements
        {
            get
            {
                return new string[] { "c", "code", "codeEntityReference", "codeReference", "codeInline",
                    "command", "environmentVariable", "fictitiousUri", "foreignPhrase", "link", "linkTarget",
                    "linkUri", "localUri", "replaceable", "resheader", "see", "seeAlso", "style",
                    "unmanagedCodeEntityReference", "token" };
            }
        }

        /// <summary>
        /// This read-only property returns the default list of spell checked XML attributes
        /// </summary>
        public static IEnumerable<string> DefaultSpellCheckedAttributes
        {
            get
            {
                return new[] { "altText", "Caption", "CompoundAlternate", "Content", "Header", "lead",
                    "PreferredAlternate", "SpellingAlternates", "title", "term", "Text", "ToolTip" };
            }
        }
        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        public SpellCheckerConfiguration()
        {
            csharpOptions = new CSharpOptions();
            cadOptions = new CodeAnalysisDictionaryOptions();

            dictionaryLanguages = new List<CultureInfo>();

            this.SpellCheckAsYouType = this.IncludeInProjectSpellCheck = this.DetectDoubledWords =
                this.IgnoreWordsWithDigits = this.IgnoreWordsInAllUppercase = this.IgnoreFormatSpecifiers =
                this.IgnoreFilenamesAndEMailAddresses = this.IgnoreXmlElementsInText =
                this.DetermineResourceFileLanguageFromName = this.InheritExcludedExtensions =
                this.InheritAdditionalDictionaryFolders = this.InheritIgnoredWords =
                this.InheritExclusionExpressions = this.InheritXmlSettings = this.IgnoreMnemonics = true;

            this.TreatUnderscoreAsSeparator = false;

            ignoredWords = new HashSet<string>(DefaultIgnoredWords, StringComparer.OrdinalIgnoreCase);
            ignoredXmlElements = new HashSet<string>(DefaultIgnoredXmlElements);
            spellCheckedXmlAttributes = new HashSet<string>(DefaultSpellCheckedAttributes);
            excludedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            recognizedWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            additionalDictionaryFolders = new List<string>();

            exclusionExpressions = new List<Regex>();

            deprecatedTerms = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            compoundTerms = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            unrecognizedWords = new Dictionary<string, IList<string>>(StringComparer.OrdinalIgnoreCase);
        }
        #endregion

        #region Helper methods
        //=====================================================================

        /// <summary>
        /// This is used to determine if a file is to be excluded from spell checking by its extension
        /// </summary>
        /// <param name="extension">The filename extension to check</param>
        /// <returns>True to exclude the file from spell checking, false to include it</returns>
        public bool IsExcludedByExtension(string extension)
        {
            if(extension == null)
                return false;

            if(extension.Length == 0 || extension[0] != '.')
                extension = "." + extension;

            return excludedExtensions.Contains(extension);
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

            return ignoredWords.Contains(word);
        }
        #endregion

        #region Load configuration methods
        //=====================================================================

        /// <summary>
        /// Load the configuration from the given file
        /// </summary>
        /// <param name="filename">The configuration file to load</param>
        /// <remarks>Any properties not in the configuration file retain their current values.  If the file does
        /// not exist, the configuration will remain unchanged.</remarks>
        public void Load(string filename)
        {
            HashSet<string> tempHashSet;

            try
            {
                // Nothing to do if the file doesn't exist
                if(!File.Exists(filename))
                    return;

                var configuration = new SpellingConfigurationFile(filename, this);

                this.SpellCheckAsYouType = configuration.ToBoolean(PropertyNames.SpellCheckAsYouType);
                
                // This option is always true for the global configuration
                if(configuration.ConfigurationType != ConfigurationType.Global)
                    this.IncludeInProjectSpellCheck = configuration.ToBoolean(PropertyNames.IncludeInProjectSpellCheck);

                this.DetectDoubledWords = configuration.ToBoolean(PropertyNames.DetectDoubledWords);
                this.IgnoreWordsWithDigits = configuration.ToBoolean(PropertyNames.IgnoreWordsWithDigits);
                this.IgnoreWordsInAllUppercase = configuration.ToBoolean(PropertyNames.IgnoreWordsInAllUppercase);
                this.IgnoreFormatSpecifiers = configuration.ToBoolean(PropertyNames.IgnoreFormatSpecifiers);
                this.IgnoreFilenamesAndEMailAddresses = configuration.ToBoolean(
                    PropertyNames.IgnoreFilenamesAndEMailAddresses);
                this.IgnoreXmlElementsInText = configuration.ToBoolean(PropertyNames.IgnoreXmlElementsInText);
                this.TreatUnderscoreAsSeparator = configuration.ToBoolean(PropertyNames.TreatUnderscoreAsSeparator);
                this.IgnoreMnemonics = configuration.ToBoolean(PropertyNames.IgnoreMnemonics);
                this.IgnoreCharacterClass = configuration.ToEnum<IgnoredCharacterClass>(
                    PropertyNames.IgnoreCharacterClass);
                this.DetermineResourceFileLanguageFromName = configuration.ToBoolean(
                    PropertyNames.DetermineResourceFileLanguageFromName);

                csharpOptions.IgnoreXmlDocComments = configuration.ToBoolean(
                    PropertyNames.CSharpOptionsIgnoreXmlDocComments);
                csharpOptions.IgnoreDelimitedComments = configuration.ToBoolean(
                    PropertyNames.CSharpOptionsIgnoreDelimitedComments);
                csharpOptions.IgnoreStandardSingleLineComments = configuration.ToBoolean(
                    PropertyNames.CSharpOptionsIgnoreStandardSingleLineComments);
                csharpOptions.IgnoreQuadrupleSlashComments = configuration.ToBoolean(
                    PropertyNames.CSharpOptionsIgnoreQuadrupleSlashComments);
                csharpOptions.IgnoreNormalStrings = configuration.ToBoolean(
                    PropertyNames.CSharpOptionsIgnoreNormalStrings);
                csharpOptions.IgnoreVerbatimStrings = configuration.ToBoolean(
                    PropertyNames.CSharpOptionsIgnoreVerbatimStrings);
                csharpOptions.IgnoreInterpolatedStrings = configuration.ToBoolean(
                    PropertyNames.CSharpOptionsIgnoreInterpolatedStrings);
                csharpOptions.ApplyToAllCStyleLanguages = configuration.ToBoolean(
                    PropertyNames.CSharpOptionsApplyToAllCStyleLanguages);

                cadOptions.ImportCodeAnalysisDictionaries = configuration.ToBoolean(
                    PropertyNames.CadOptionsImportCodeAnalysisDictionaries);
                cadOptions.RecognizedWordHandling = configuration.ToEnum<RecognizedWordHandling>(
                    PropertyNames.CadOptionsRecognizedWordHandling);
                cadOptions.TreatUnrecognizedWordsAsMisspelled = configuration.ToBoolean(
                    PropertyNames.CadOptionsTreatUnrecognizedWordsAsMisspelled);
                cadOptions.TreatDeprecatedTermsAsMisspelled = configuration.ToBoolean(
                    PropertyNames.CadOptionsTreatDeprecatedTermsAsMisspelled);
                cadOptions.TreatCompoundTermsAsMisspelled = configuration.ToBoolean(
                    PropertyNames.CadOptionsTreatCompoundTermsAsMisspelled);
                cadOptions.TreatCasingExceptionsAsIgnoredWords = configuration.ToBoolean(
                    PropertyNames.CadOptionsTreatCasingExceptionsAsIgnoredWords);

                this.InheritExcludedExtensions = configuration.ToBoolean(PropertyNames.InheritExcludedExtensions);

                if(configuration.HasProperty(PropertyNames.ExcludedExtensions))
                {
                    tempHashSet = new HashSet<string>(configuration.ToValues(PropertyNames.ExcludedExtensions,
                        PropertyNames.ExcludedExtensionsItem), StringComparer.OrdinalIgnoreCase);

                    if(this.InheritExcludedExtensions)
                    {
                        if(tempHashSet.Count != 0)
                            foreach(string ext in tempHashSet)
                                excludedExtensions.Add(ext);
                    }
                    else
                        excludedExtensions = tempHashSet;
                }

                this.InheritAdditionalDictionaryFolders = configuration.ToBoolean(
                    PropertyNames.InheritAdditionalDictionaryFolders);

                if(configuration.HasProperty(PropertyNames.AdditionalDictionaryFolders))
                {
                    tempHashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    foreach(string folder in configuration.ToValues(PropertyNames.AdditionalDictionaryFolders,
                      PropertyNames.AdditionalDictionaryFoldersItem))
                    {
                        // Fully qualify relative paths with the configuration file path
                        if(folder.IndexOf('%') != -1 || Path.IsPathRooted(folder))
                            tempHashSet.Add(folder);
                        else
                            tempHashSet.Add(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(filename), folder)));
                    }

                    if(this.InheritAdditionalDictionaryFolders)
                    {
                        if(tempHashSet.Count != 0)
                            foreach(string folder in tempHashSet)
                                additionalDictionaryFolders.Add(folder);
                    }
                    else
                        additionalDictionaryFolders = tempHashSet.ToList();
                }

                this.InheritIgnoredWords = configuration.ToBoolean(PropertyNames.InheritIgnoredWords);

                if(configuration.HasProperty(PropertyNames.IgnoredWords))
                {
                    tempHashSet = new HashSet<string>(configuration.ToValues(PropertyNames.IgnoredWords,
                        PropertyNames.IgnoredWordsItem), StringComparer.OrdinalIgnoreCase);

                    if(this.InheritIgnoredWords)
                    {
                        if(tempHashSet.Count != 0)
                            foreach(string word in tempHashSet)
                                ignoredWords.Add(word);
                    }
                    else
                        ignoredWords = tempHashSet;
                }

                this.InheritExclusionExpressions = configuration.ToBoolean(PropertyNames.InheritExclusionExpressions);

                if(configuration.HasProperty(PropertyNames.ExclusionExpressions))
                {
                    var tempList = new List<Regex>(configuration.ToRegexes(PropertyNames.ExclusionExpressions,
                        PropertyNames.ExclusionExpressionItem));

                    if(this.InheritExclusionExpressions)
                    {
                        if(tempList.Count != 0)
                        {
                            tempHashSet = new HashSet<string>(exclusionExpressions.Select(r => r.ToString()));

                            foreach(Regex exp in tempList)
                                if(!tempHashSet.Contains(exp.ToString()))
                                {
                                    exclusionExpressions.Add(exp);
                                    tempHashSet.Add(exp.ToString());
                                }
                        }
                    }
                    else
                        exclusionExpressions = tempList;
                }

                this.InheritXmlSettings = configuration.ToBoolean(PropertyNames.InheritXmlSettings);

                if(configuration.HasProperty(PropertyNames.IgnoredXmlElements))
                {
                    tempHashSet = new HashSet<string>(configuration.ToValues(PropertyNames.IgnoredXmlElements,
                        PropertyNames.IgnoredXmlElementsItem));

                    if(this.InheritXmlSettings)
                    {
                        if(tempHashSet.Count != 0)
                            foreach(string element in tempHashSet)
                                ignoredXmlElements.Add(element);
                    }
                    else
                        ignoredXmlElements = tempHashSet;
                }

                if(configuration.HasProperty(PropertyNames.SpellCheckedXmlAttributes))
                {
                    tempHashSet = new HashSet<string>(configuration.ToValues(PropertyNames.SpellCheckedXmlAttributes,
                        PropertyNames.SpellCheckedXmlAttributesItem));

                    if(this.InheritXmlSettings)
                    {
                        if(tempHashSet.Count != 0)
                            foreach(string attr in tempHashSet)
                                spellCheckedXmlAttributes.Add(attr);
                    }
                    else
                        spellCheckedXmlAttributes = tempHashSet;
                }

                // Load the dictionary languages and, if merging settings, handle inheritance
                if(configuration.HasProperty(PropertyNames.SelectedLanguages))
                {
                    var languages = configuration.ToValues(PropertyNames.SelectedLanguages,
                      PropertyNames.SelectedLanguagesItem, true).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

                    // Is there a blank entry that marks the inherited languages placeholder?
                    int idx = languages.IndexOf(String.Empty);

                    if(idx != -1)
                    {
                        languages.RemoveAt(idx);

                        // If there are other languages, insert the inherited languages at the desired location.
                        // If an inherited language matches a language in the configuration file, it is left at
                        // its new location this overriding the inherited language location.
                        if(languages.Count != 0)
                            foreach(var lang in dictionaryLanguages)
                                if(!languages.Contains(lang.Name))
                                {
                                    languages.Insert(idx, lang.Name);
                                    idx++;
                                }
                    }

                    if(languages.Count != 0)
                        dictionaryLanguages = languages.Select(l => new CultureInfo(l)).ToList();
                }
            }
            catch(Exception ex)
            {
                // Ignore errors and just use the defaults
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                // Always ensure we have at least the default language if none are specified in the global
                // configuration file.
                if(dictionaryLanguages.Count == 0)
                    dictionaryLanguages.Add(new CultureInfo("en-US"));
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
                foreach(var word in option.Elements("Word"))
                    if(!String.IsNullOrWhiteSpace(word.Value))
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
                                    if((string)word.Attribute("Spelling") == "Ignore")
                                        ignoredWords.Add(word.Value);

                                // Any other value is treated as None and it passes through to the spell checker
                                // like any other word.
                                break;
                        }

            option = root.XPathSelectElement("Words/Unrecognized");

            if(this.CadOptions.TreatUnrecognizedWordsAsMisspelled && option != null)
                foreach(var word in option.Elements("Word"))
                    if(!String.IsNullOrWhiteSpace(word.Value))
                    {
                        unrecognizedWords[word.Value] = new List<string>(
                            ((string)word.Attribute("SpellingAlternates") ?? String.Empty).Split(
                                new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries));
                    }

            option = root.XPathSelectElement("Words/Deprecated");

            if(this.CadOptions.TreatDeprecatedTermsAsMisspelled && option != null)
                foreach(var term in option.Elements("Term"))
                    if(!String.IsNullOrWhiteSpace(term.Value))
                        deprecatedTerms[term.Value] = ((string)term.Attribute("PreferredAlternate")).ToWords();

            option = root.XPathSelectElement("Words/Compound");

            if(this.CadOptions.TreatCompoundTermsAsMisspelled && option != null)
                foreach(var term in option.Elements("Term"))
                    if(!String.IsNullOrWhiteSpace(term.Value))
                        compoundTerms[term.Value] = ((string)term.Attribute("CompoundAlternate")).ToWords();

            option = root.XPathSelectElement("Acronyms/CasingExceptions");

            if(this.CadOptions.TreatCasingExceptionsAsIgnoredWords && option != null)
                foreach(var acronym in option.Elements("Acronym"))
                    if(!String.IsNullOrWhiteSpace(acronym.Value))
                        ignoredWords.Add(acronym.Value);
        }
        #endregion
    }
}
