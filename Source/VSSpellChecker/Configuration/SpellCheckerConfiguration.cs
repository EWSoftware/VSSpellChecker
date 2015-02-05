//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : SpellCheckerConfiguration.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 02/18/2015
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
//===============================================================================================================

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;

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
        private HashSet<string> ignoredWords, ignoredXmlElements, spellCheckedXmlAttributes, excludedExtensions;
        private List<string> additionalDictionaryFolders;
        #endregion

        #region Properties
        //=====================================================================

        /// <summary>
        /// This is used to get or set the default language for the spell checker
        /// </summary>
        /// <remarks>The default is to use the English US dictionary (en-US)</remarks>
        [DefaultValue(typeof(CultureInfo), "en-US")]
        public CultureInfo DefaultLanguage { get; set; }

        /// <summary>
        /// This is used to get or set whether or not to spell checking as you type is enabled
        /// </summary>
        /// <value>This is true by default</value>
        [DefaultValue(true)]
        public bool SpellCheckAsYouType { get; set; }

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
                    "linkUri", "localUri", "replaceable", "see", "seeAlso", "unmanagedCodeEntityReference",
                    "token" };
            }
        }

        /// <summary>
        /// This read-only property returns the default list of spell checked XML attributes
        /// </summary>
        public static IEnumerable<string> DefaultSpellCheckedAttributes
        {
            get
            {
                return new[] { "altText", "Caption", "Content", "Header", "lead", "title", "term", "Text",
                    "ToolTip" };
            }
        }
        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// constructor
        /// </summary>
        public SpellCheckerConfiguration()
        {
            csharpOptions = new CSharpOptions();

            this.DefaultLanguage = new CultureInfo("en-US");

            this.SpellCheckAsYouType = this.IgnoreWordsWithDigits = this.IgnoreWordsInAllUppercase =
                this.IgnoreFormatSpecifiers = this.IgnoreFilenamesAndEMailAddresses =
                this.IgnoreXmlElementsInText = this.DetermineResourceFileLanguageFromName =
                this.InheritExcludedExtensions = this.InheritAdditionalDictionaryFolders =
                this.InheritIgnoredWords = this.InheritXmlSettings = true;

            this.TreatUnderscoreAsSeparator = false;
            this.IgnoreCharacterClass = IgnoredCharacterClass.None;

            ignoredWords = new HashSet<string>(DefaultIgnoredWords, StringComparer.OrdinalIgnoreCase);
            ignoredXmlElements = new HashSet<string>(DefaultIgnoredXmlElements);
            spellCheckedXmlAttributes = new HashSet<string>(DefaultSpellCheckedAttributes);
            excludedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            additionalDictionaryFolders = new List<string>();
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

                this.DefaultLanguage = configuration.ToCultureInfo(PropertyNames.DefaultLanguage);
                this.SpellCheckAsYouType = configuration.ToBoolean(PropertyNames.SpellCheckAsYouType);
                this.IgnoreWordsWithDigits = configuration.ToBoolean(PropertyNames.IgnoreWordsWithDigits);
                this.IgnoreWordsInAllUppercase = configuration.ToBoolean(PropertyNames.IgnoreWordsInAllUppercase);
                this.IgnoreFormatSpecifiers = configuration.ToBoolean(PropertyNames.IgnoreFormatSpecifiers);
                this.IgnoreFilenamesAndEMailAddresses = configuration.ToBoolean(
                    PropertyNames.IgnoreFilenamesAndEMailAddresses);
                this.IgnoreXmlElementsInText = configuration.ToBoolean(PropertyNames.IgnoreXmlElementsInText);
                this.TreatUnderscoreAsSeparator = configuration.ToBoolean(PropertyNames.TreatUnderscoreAsSeparator);
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
                        if(Path.IsPathRooted(folder))
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
            }
            catch(Exception ex)
            {
                // Ignore errors and just use the defaults
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }
        #endregion
    }
}
