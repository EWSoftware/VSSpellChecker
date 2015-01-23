//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : SpellCheckerConfiguration.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 06/13/2014
// Note    : Copyright 2013-2014, Eric Woodruff, All rights reserved
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
// 04/26/2013  EFW  Created the code
//===============================================================================================================

using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace VisualStudio.SpellChecker
{
    /// <summary>
    /// This class is used to contain the spell checker's configuration
    /// </summary>
    /// <remarks>Settings are stored in an XML file in the user's local application data folder and will be used
    /// by all versions of Visual Studio in which the package is installed.</remarks>
    internal static class SpellCheckerConfiguration
    {
        #region Private data members
        //=====================================================================

        private static Regex reSplitExtensions = new Regex(@"[^\.\w]");

        private static HashSet<string> ignoredWords, ignoredXmlElements, spellCheckedXmlAttributes,
            extensionExclusions;
        #endregion

        #region Properties
        //=====================================================================

        /// <summary>
        /// This read-only property returns the configuration file path
        /// </summary>
        /// <value>This location is also where custom dictionary files are located</value>
        public static string ConfigurationFilePath
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
        /// This is used to get or set the default language for the spell checker
        /// </summary>
        /// <remarks>The default is to use the English US dictionary (en-US)</remarks>
        public static CultureInfo DefaultLanguage { get; set; }

        /// <summary>
        /// This is used to get or set whether or not to spell checking as you type is enabled
        /// </summary>
        /// <value>This is true by default</value>
        public static bool SpellCheckAsYouType { get; set; }

        /// <summary>
        /// This is used to get or set whether or not to ignore words containing digits
        /// </summary>
        /// <value>This is true by default</value>
        public static bool IgnoreWordsWithDigits { get; set; }

        /// <summary>
        /// This is used to get or set whether or not to ignore words in all uppercase
        /// </summary>
        /// <value>This is true by default</value>
        public static bool IgnoreWordsInAllUppercase { get; set; }

        /// <summary>
        /// This is used to get or set whether or not to ignore .NET and C-style format string specifiers
        /// </summary>
        /// <value>This is true by default</value>
        public static bool IgnoreFormatSpecifiers { get; set; }

        /// <summary>
        /// This is used to get or set whether or not to ignore words that look like filenames or e-mail
        /// addresses.
        /// </summary>
        /// <value>This is true by default</value>
        public static bool IgnoreFilenamesAndEMailAddresses { get; set; }

        /// <summary>
        /// This is used to get or set whether or not to ignore XML elements in the text being spell checked
        /// (text within '&amp;lt;' and '&amp;gt;').
        /// </summary>
        /// <value>This is true by default</value>
        public static bool IgnoreXmlElementsInText { get; set; }

        /// <summary>
        /// This is used to get or set whether or not to ignore words by character class
        /// </summary>
        /// <remarks>This provides a simplistic way of ignoring some words in mixed language files.  It works
        /// best for spell checking English text in files that also contain Cyrillic or Asian text.  The default
        /// is <c>None</c> to include all words regardless of the characters they contain.</remarks>
        public static IgnoredCharacterClass IgnoreCharacterClass { get; set; }

        /// <summary>
        /// This is used to get or set whether or not underscores are treated as a word separator
        /// </summary>
        /// <value>This is false by default</value>
        public static bool TreatUnderscoreAsSeparator { get; set; }

        /// <summary>
        /// This is used to get or set whether or not to ignore XML documentation comments in C# files
        /// (<c>/** ... */</c> or <c>/// ...</c>)
        /// </summary>
        /// <value>The default is false to include XML documentation comments</value>
        public static bool IgnoreXmlDocComments { get; set; }

        /// <summary>
        /// This is used to get or set whether or not to ignore delimited comments in C# files (<c>/* ... */</c>)
        /// </summary>
        /// <value>The default is false to include delimited comments</value>
        public static bool IgnoreDelimitedComments { get; set; }

        /// <summary>
        /// This is used to get or set whether or not to ignore standard single line comments in C# files
        /// (<c>// ...</c>)
        /// </summary>
        /// <value>The default is false to include standard single line comments</value>
        public static bool IgnoreStandardSingleLineComments { get; set; }

        /// <summary>
        /// This is used to get or set whether or not to ignore quadruple slash comments in C# files
        /// (<c>//// ...</c>)
        /// </summary>
        /// <value>The default is false to include quadruple slash comments</value>
        /// <remarks>This is useful for ignoring commented out blocks of code while still spell checking the
        /// other comment styles.</remarks>
        public static bool IgnoreQuadrupleSlashComments { get; set; }

        /// <summary>
        /// This is used to get or set whether or not to ignore normal strings in C# files (<c>"..."</c>)
        /// </summary>
        /// <value>The default is false to include normal strings</value>
        public static bool IgnoreNormalStrings { get; set; }

        /// <summary>
        /// This is used to get or set whether or not to ignore verbatim strings in C# files (<c>@"..."</c>)
        /// </summary>
        /// <value>The default is false to include verbatim strings</value>
        public static bool IgnoreVerbatimStrings { get; set; }

        /// <summary>
        /// This is used to get or set the exclusions by filename extension
        /// </summary>
        /// <remarks>Filenames with an extension in this set will not be spell checked.  Extensions are specified
        /// in a space or comma-separated list with or without a preceding period.  A single period will exclude
        /// files without an extension.</remarks>
        public static string ExcludeByFilenameExtension
        {
            get
            {
                return String.Join(" ", extensionExclusions.OrderBy(e => e));
            }
            set
            {
                if(extensionExclusions == null)
                    extensionExclusions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                else
                    extensionExclusions.Clear();

                if(!String.IsNullOrWhiteSpace(value))
                    foreach(string ext in reSplitExtensions.Split(value))
                        if(!String.IsNullOrEmpty(ext))
                        {
                            string addExt;

                            if(ext[0] != '.')
                                addExt = "." + ext;
                            else
                                addExt = ext;

                            extensionExclusions.Add(addExt);
                        }
            }
        }

        /// <summary>
        /// This read-only property returns an enumerable list of ignored words that will not be spell
        /// checked.
        /// </summary>
        public static IEnumerable<string> IgnoredWords
        {
            get { return ignoredWords; }
        }

        /// <summary>
        /// This read-only property returns an enumerable list of ignored XML element names that will not have
        /// their content spell checked.
        /// </summary>
        public static IEnumerable<string> IgnoredXmlElements
        {
            get { return ignoredXmlElements; }
        }

        /// <summary>
        /// This read-only property returns an enumerable list of XML attribute names that will not have their
        /// values spell checked.
        /// </summary>
        public static IEnumerable<string> SpellCheckedXmlAttributes
        {
            get { return spellCheckedXmlAttributes; }
        }

        /// <summary>
        /// This read-only property returns a list of available dictionary languages
        /// </summary>
        /// <remarks>The returned enumerable list contains the default English (en-US) dictionary along with
        /// any custom dictionaries found in the <see cref="ConfigurationFilePath"/> folder.</remarks>
        public static IEnumerable<CultureInfo> AvailableDictionaryLanguages
        {
            get
            {
                CultureInfo info;

                // This is supplied with the application and is always available
                yield return new CultureInfo("en-US");

                // Culture names can vary in format (en-US, arn, az-Cyrl, az-Cyrl-AZ, az-Latn, az-Latn-AZ, etc.)
                // so look for any affix files with a related dictionary file and see if they are valid cultures.
                // If so, we'll take them.
                foreach(string dictionary in Directory.EnumerateFiles(ConfigurationFilePath, "*.aff"))
                    if(File.Exists(Path.ChangeExtension(dictionary, ".dic")))
                    {
                        try
                        {
                            info = new CultureInfo(Path.GetFileNameWithoutExtension(dictionary).Replace("_", "-"));
                        }
                        catch(CultureNotFoundException )
                        {
                            // Ignore filenames that are not cultures
                            info = null;
                        }

                        if(info != null)
                            yield return info;
                    }
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
        /// Static constructor
        /// </summary>
        static SpellCheckerConfiguration()
        {
            if(!LoadConfiguration())
                ResetConfiguration(false);
        }
        #endregion

        #region Helper methods
        //=====================================================================

        /// <summary>
        /// This is used to determine if a file is to be excluded from spell checking by its extension
        /// </summary>
        /// <param name="extension">The filename extension to check</param>
        /// <returns>True to exclude the file from spell checking, false to include it</returns>
        public static bool IsExcludedByExtension(string extension)
        {
            if(extension == null)
                return false;

            if(extension.Length == 0 || extension[0] != '.')
                extension = "." + extension;

            return extensionExclusions.Contains(extension);
        }

        /// <summary>
        /// This method provides a thread-safe way to check for a globally ignored word
        /// </summary>
        /// <param name="word">The word to check</param>
        /// <returns>True if it should be ignored, false if not</returns>
        public static bool ShouldIgnoreWord(string word)
        {
            if(String.IsNullOrWhiteSpace(word))
                return true;

            lock(ignoredWords)
            {
                return ignoredWords.Contains(word);
            }
        }

        /// <summary>
        /// Set the list of ignored XML elements
        /// </summary>
        /// <param name="ignoredElements">The list of XML elements to ignore</param>
        public static void SetIgnoredXmlElements(IEnumerable<string> ignoredElements)
        {
            ignoredXmlElements = new HashSet<string>(ignoredElements);
        }

        /// <summary>
        /// Set the list of ignored words
        /// </summary>
        /// <param name="ignored">The list of words to ignore</param>
        public static void SetIgnoredWords(IEnumerable<string> ignored)
        {
            ignoredWords = new HashSet<string>(ignored);
        }

        /// <summary>
        /// Set the list of spell checked XML attributes
        /// </summary>
        /// <param name="spellCheckedAttributes">The list of spell checked XML attributes</param>
        public static void SetSpellCheckedXmlAttributes(IEnumerable<string> spellCheckedAttributes)
        {
            spellCheckedXmlAttributes = new HashSet<string>(spellCheckedAttributes);
        }

        /// <summary>
        /// This is used to load the spell checker configuration settings
        /// </summary>
        /// <returns>True if loaded successfully or false if the file does not exist or could not be loaded</returns>
        private static bool LoadConfiguration()
        {
            IgnoredCharacterClass ignoredClass;
            string filename = Path.Combine(ConfigurationFilePath, "SpellChecker.config");
            bool success = true;

            try
            {
                if(!File.Exists(filename))
                    return false;

                var root = XDocument.Load(filename).Root;

                var node = root.Element("DefaultLanguage");

                if(node != null)
                    DefaultLanguage = new CultureInfo(node.Value);
                else
                    DefaultLanguage = new CultureInfo("en-US");

                SpellCheckAsYouType = (root.Element("SpellCheckAsYouType") != null);
                IgnoreWordsWithDigits = (root.Element("IgnoreWordsWithDigits") != null);
                IgnoreWordsInAllUppercase = (root.Element("IgnoreWordsInAllUppercase") != null);
                IgnoreFormatSpecifiers = (root.Element("IgnoreFormatSpecifiers") != null);
                IgnoreFilenamesAndEMailAddresses = (root.Element("IgnoreFilenamesAndEMailAddresses") != null);
                IgnoreXmlElementsInText = (root.Element("IgnoreXmlElementsInText") != null);
                TreatUnderscoreAsSeparator = (root.Element("TreatUnderscoreAsSeparator") != null);

                IgnoreXmlDocComments = (root.Element("IgnoreXmlDocComments") != null);
                IgnoreDelimitedComments = (root.Element("IgnoreDelimitedComments") != null);
                IgnoreStandardSingleLineComments = (root.Element("IgnoreStandardSingleLineComments") != null);
                IgnoreQuadrupleSlashComments = (root.Element("IgnoreQuadrupleSlashComments") != null);
                IgnoreNormalStrings = (root.Element("IgnoreNormalStrings") != null);
                IgnoreVerbatimStrings = (root.Element("IgnoreVerbatimStrings") != null);

                node = root.Element("IgnoredCharacterClass");

                if(node == null || !Enum.TryParse<IgnoredCharacterClass>(node.Value, out ignoredClass))
                    ignoredClass = IgnoredCharacterClass.None;

                IgnoreCharacterClass = ignoredClass;

                node = root.Element("ExcludeByFilenameExtension");

                if(node != null)
                    ExcludeByFilenameExtension = node.Value;
                else
                    ExcludeByFilenameExtension = null;

                node = root.Element("IgnoredWords");

                if(node != null)
                    ignoredWords = new HashSet<string>(node.Descendants().Select(n => n.Value));
                else
                    ignoredWords = new HashSet<string>(DefaultIgnoredWords);

                node = root.Element("IgnoredXmlElements");

                if(node != null)
                    ignoredXmlElements = new HashSet<string>(node.Descendants().Select(n => n.Value));
                else
                    ignoredXmlElements = new HashSet<string>(DefaultIgnoredXmlElements);

                node = root.Element("SpellCheckedXmlAttributes");

                if(node != null)
                    spellCheckedXmlAttributes = new HashSet<string>(node.Descendants().Select(n => n.Value));
                else
                    spellCheckedXmlAttributes = new HashSet<string>(DefaultSpellCheckedAttributes);
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                success = false;
            }

            return success;
        }

        /// <summary>
        /// This is used to save the spell checker configuration settings
        /// </summary>
        /// <remarks>The settings are saved to <b>SpellChecker.config</b> in the <see cref="ConfigurationFilePath"/>
        /// folder.</remarks>
        public static bool SaveConfiguration()
        {
            string filename = Path.Combine(ConfigurationFilePath, "SpellChecker.config");
            bool success = true;

            try
            {
                XElement root = new XElement("SpellCheckerConfiguration",
                    new XElement("DefaultLanguage") { Value = DefaultLanguage.Name },
                    SpellCheckAsYouType ? new XElement("SpellCheckAsYouType") : null,
                    IgnoreWordsWithDigits ? new XElement("IgnoreWordsWithDigits") : null,
                    IgnoreWordsInAllUppercase ? new XElement("IgnoreWordsInAllUppercase") : null,
                    IgnoreFormatSpecifiers ? new XElement("IgnoreFormatSpecifiers") : null,
                    IgnoreFilenamesAndEMailAddresses ? new XElement("IgnoreFilenamesAndEMailAddresses") : null,
                    IgnoreXmlElementsInText ? new XElement("IgnoreXmlElementsInText") : null,
                    TreatUnderscoreAsSeparator ? new XElement("TreatUnderscoreAsSeparator") : null,
                    new XElement("ExcludeByFilenameExtension", ExcludeByFilenameExtension),
                    new XElement("IgnoredCharacterClass", IgnoreCharacterClass.ToString()),
                    IgnoreXmlDocComments ? new XElement("IgnoreXmlDocComments") : null,
                    IgnoreDelimitedComments ? new XElement("IgnoreDelimitedComments") : null,
                    IgnoreStandardSingleLineComments ? new XElement("IgnoreStandardSingleLineComments") : null,
                    IgnoreQuadrupleSlashComments ? new XElement("IgnoreQuadrupleSlashComments") : null,
                    IgnoreNormalStrings ? new XElement("IgnoreNormalStrings") : null,
                    IgnoreVerbatimStrings ? new XElement("IgnoreVerbatimStrings") : null);

                if(ignoredWords.Count != DefaultIgnoredWords.Count() ||
                  DefaultIgnoredWords.Except(ignoredWords).Count() != 0)
                    root.Add(new XElement("IgnoredWords",
                        ignoredWords.Select(i => new XElement("Ignore") { Value = i })));

                if(ignoredXmlElements.Count != DefaultIgnoredXmlElements.Count() ||
                  DefaultIgnoredXmlElements.Except(ignoredXmlElements).Count() != 0)
                    root.Add(new XElement("IgnoredXmlElements",
                        ignoredXmlElements.Select(i => new XElement("Ignore") { Value = i })));

                if(spellCheckedXmlAttributes.Count != DefaultSpellCheckedAttributes.Count() ||
                  DefaultSpellCheckedAttributes.Except(spellCheckedXmlAttributes).Count() != 0)
                    root.Add(new XElement("SpellCheckedXmlAttributes",
                        spellCheckedXmlAttributes.Select(i => new XElement("SpellCheck") { Value = i })));

                root.Save(filename);
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                success = false;
            }

            return success;
        }

        /// <summary>
        /// This is used to reset the configuration to its default state
        /// </summary>
        /// <param name="deleteConfigurationFile">True to delete the configuration file if it exists, false to
        /// just set the default values</param>
        /// <returns></returns>
        public static void ResetConfiguration(bool deleteConfigurationFile)
        {
            SpellCheckAsYouType = IgnoreWordsWithDigits = IgnoreWordsInAllUppercase =
                IgnoreFormatSpecifiers = IgnoreFilenamesAndEMailAddresses = IgnoreXmlElementsInText = true;

            TreatUnderscoreAsSeparator = IgnoreXmlDocComments = IgnoreDelimitedComments =
                IgnoreStandardSingleLineComments = IgnoreQuadrupleSlashComments = IgnoreNormalStrings =
                IgnoreVerbatimStrings = false;

            IgnoreCharacterClass = IgnoredCharacterClass.None;

            ignoredWords = new HashSet<string>(DefaultIgnoredWords, StringComparer.OrdinalIgnoreCase);
            ignoredXmlElements = new HashSet<string>(DefaultIgnoredXmlElements);
            spellCheckedXmlAttributes = new HashSet<string>(DefaultSpellCheckedAttributes);
            extensionExclusions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            DefaultLanguage = new CultureInfo("en-US");

            if(deleteConfigurationFile)
            {
                string filename = Path.Combine(ConfigurationFilePath, "SpellChecker.config");

                try
                {
                    if(File.Exists(filename))
                        File.Delete(filename);
                }
                catch(Exception ex)
                {
                    // Ignore exception encountered while trying to delete the configuration file
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            }
        }
        #endregion
    }
}
