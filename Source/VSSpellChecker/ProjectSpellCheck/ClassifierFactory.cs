//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : ClassifierFactory.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 08/30/2025
// Note    : Copyright 2015-2025, Eric Woodruff, All rights reserved
//
// This file contains a class used to generate classifiers for files that need to be spell checked
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 08/31/2015  EFW  Created the code
//===============================================================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

using VisualStudio.SpellChecker.Common.Configuration;

namespace VisualStudio.SpellChecker.ProjectSpellCheck
{
    /// <summary>
    /// This class is used to generate classifiers for files that need to be spell checked
    /// </summary>
    internal static class ClassifierFactory
    {
        #region Classifier definition class
        //=====================================================================

        /// <summary>
        /// This contains classifier definition settings
        /// </summary>
        private class ClassifierDefinition
        {
            /// <summary>
            /// This is used to get or set the classifier type
            /// </summary>
            public string ClassifierType { get; set; }

            /// <summary>
            /// The mnemonic character used by the file type
            /// </summary>
            public char Mnemonic { get; set; }

            /// <summary>
            /// This is used to get or set the classifier configuration, if any
            /// </summary>
            public XElement Configuration { get; set; }
        }
        #endregion

        #region Private data members
        //=====================================================================

        private static Dictionary<string, ClassifierDefinition> definitions;
        private static Dictionary<string, string> extensionMap;

        #endregion

        #region Properties
        //=====================================================================

        /// <summary>
        /// This read-only property returns an enumerable list of classifier IDs
        /// </summary>
        public static IEnumerable<string> ClassifierIds
        {
            get
            {
                if(extensionMap == null)
                    LoadClassifierConfiguration();

                return definitions.Keys.OrderBy(k => k);
            }
        }
        #endregion

        #region Helper methods
        //=====================================================================

        /// <summary>
        /// This is used to return an enumerable list of file extensions for the given classifier ID
        /// </summary>
        /// <param name="classifierId">The classifier ID for which to get file extensions</param>
        /// <returns>An enumerable list of file extensions that use the given classifier ID</returns>
        public static IEnumerable<string> ExtensionsFor(string classifierId)
        {
            if(extensionMap == null)
                LoadClassifierConfiguration();

            return extensionMap.Where(kv => kv.Value == classifierId).Select(kv => kv.Key).OrderBy(v => v); ;
        }

        /// <summary>
        /// This is used to get a classifier ID for the given filename extension
        /// </summary>
        /// <param name="extension">The filename for which to get a classifier ID</param>
        /// <returns>The classifier ID for the given filename's extension</returns>
        public static string ClassifierIdFor(string filename)
        {
            string extension = Path.GetExtension(filename);

            if(extensionMap == null)
                LoadClassifierConfiguration();

            if(!String.IsNullOrWhiteSpace(extension))
                extension = extension.Substring(1);

            if(!extensionMap.TryGetValue(extension, out string id))
                id = FileIsXml(filename) ? "XML" : "PlainText";

            return id;
        }

        /// <summary>
        /// This is used to determine if the file contains C-style code based on its extension
        /// </summary>
        /// <param name="filename">The filename to check</param>
        /// <returns>True if it does, false if not</returns>
        public static bool IsCStyleCode(string filename)
        {
            if(filename == null)
                return false;

            string extension = Path.GetExtension(filename);

            if(extensionMap == null)
                LoadClassifierConfiguration();

            if(!String.IsNullOrWhiteSpace(extension))
                extension = extension.Substring(1);

            return (extensionMap.TryGetValue(extension, out string id) && id.StartsWith("CStyle", StringComparison.Ordinal));
        }

        /// <summary>
        /// This is used to see if a C-style language support old style XML documentation comments (/** ... */)
        /// </summary>
        /// <param name="filename">The filename to check</param>
        /// <returns>True if it does, false if not</returns>
        public static bool SupportsOldStyleXmlDocComments(string filename)
        {
            if(filename == null)
                return false;

            string extension = Path.GetExtension(filename);

            if(extensionMap == null)
                LoadClassifierConfiguration();

            if(!String.IsNullOrWhiteSpace(extension))
                extension = extension.Substring(1);

            return extensionMap.TryGetValue(extension, out string id) && id != "None" &&
              definitions.TryGetValue(id, out ClassifierDefinition definition) &&
              !String.IsNullOrWhiteSpace((string)definition.Configuration.Attribute("OldStyleDocCommentDelimiter"));
        }

        /// <summary>
        /// This is used to determine if apostrophes are escaped such as in SQL literal strings
        /// </summary>
        /// <param name="filename">The filename to check</param>
        /// <returns>True if they are, false if not</returns>
        public static bool EscapesApostrophes(string filename)
        {
            if(filename == null)
                return false;

            string extension = Path.GetExtension(filename);

            if(extensionMap == null)
                LoadClassifierConfiguration();

            if(!String.IsNullOrWhiteSpace(extension))
                extension = extension.Substring(1);

            return (extensionMap.TryGetValue(extension, out string id) && id == "SQL");
        }

        /// <summary>
        /// This is used to get the mnemonic character used by the file based on its extension
        /// </summary>
        /// <param name="filename">The filename for which to get the mnemonic character</param>
        /// <returns>The mnemonic character for the file type.  The ampersand and underscore are the only
        /// supported mnemonic characters.</returns>
        public static char GetMnemonic(string filename)
        {
            if(filename != null)
            {
                string extension = Path.GetExtension(filename);

                if(extensionMap == null)
                    LoadClassifierConfiguration();

                if(!String.IsNullOrWhiteSpace(extension))
                    extension = extension.Substring(1);

                if(!extensionMap.TryGetValue(extension, out string id))
                    id = FileIsXml(filename) ? "XML" : "PlainText";

                if(id != "None" && definitions.TryGetValue(id, out ClassifierDefinition definition) && (definition.Mnemonic == '&' ||
                  definition.Mnemonic == '_'))
                {
                    return definition.Mnemonic;
                }
            }

            return '&';
        }

        /// <summary>
        /// This is used to get the classifier for the given file
        /// </summary>
        /// <param name="filename">The file for which to get a classifier</param>
        /// <param name="spellCheckConfiguration">The spell checker configuration that the classifier can use to
        /// determine what elements to return for spell checking if needed.</param>
        /// <returns>The classifier to use or null if the file should not be processed</returns>
        public static TextClassifier GetClassifier(string filename, SpellCheckerConfiguration spellCheckConfiguration)
        {
            TextClassifier classifier = null;
            string extension = Path.GetExtension(filename);

            if(extensionMap == null)
                LoadClassifierConfiguration();

            if(!String.IsNullOrWhiteSpace(extension))
                extension = extension.Substring(1);

            if(!extensionMap.TryGetValue(extension, out string id))
                id = FileIsXml(filename) ? "XML" : "PlainText";

            if(id != "None" && definitions.TryGetValue(id, out ClassifierDefinition definition))
            {
                switch(definition.ClassifierType)
                {
                    case "PlainTextClassifier":
                        classifier = new PlainTextClassifier(filename, spellCheckConfiguration);
                        break;

                    case "XmlClassifier":
                        classifier = new XmlClassifier(filename, spellCheckConfiguration);
                        break;

                    case "ReportingServicesClassifier":
                        classifier = new ReportingServicesClassifier(filename, spellCheckConfiguration);
                        break;

                    case "ResourceFileClassifier":
                        classifier = new ResourceFileClassifier(filename, spellCheckConfiguration);
                        break;

                    case "HtmlClassifier":
                        classifier = new HtmlClassifier(filename, spellCheckConfiguration);
                        break;

                    case "MarkdownClassifier":
                        classifier = new MarkdownClassifier(filename, spellCheckConfiguration);
                        break;

                    case "CodeClassifier":
                        classifier = new CodeClassifier(filename, spellCheckConfiguration, definition.Configuration);
                        break;

                    case "RegexClassifier":
                        classifier = new RegexClassifier(filename, spellCheckConfiguration, definition.Configuration);
                        break;

                    case "ScriptWithHtmlClassifier":
                        classifier = new ScriptWithHtmlClassifier(filename, spellCheckConfiguration, definition.Configuration);
                        break;

                    default:
                        break;
                }
            }

            return classifier;
        }

        /// <summary>
        /// This is used to load the classifier configuration settings
        /// </summary>
        /// <remarks>The default configuration is loaded first,  If an overrides file exists in the global
        /// configuration file path, it is loaded too.</remarks>
        private static void LoadClassifierConfiguration()
        {
            definitions = new Dictionary<string, ClassifierDefinition>(StringComparer.OrdinalIgnoreCase);
            extensionMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            List<string> locations =
            [
                Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    "Classifications.config"),
                Path.Combine(SpellCheckerConfiguration.GlobalConfigurationFilePath, "Classifications.config")
            ];

            try
            {
                foreach(string configFile in locations)
                {
                    if(File.Exists(configFile))
                    {
                        var config = XDocument.Load(configFile);

                        foreach(var classifier in config.Descendants("Classifier"))
                        {
                            definitions[(string)classifier.Attribute("Id")] = new ClassifierDefinition
                            {
                                ClassifierType = (string)classifier.Attribute("Type"),
                                Mnemonic = ((string)classifier.Attribute("Mnemonic") ?? "&")[0],
                                Configuration = classifier
                            };
                        }

                        foreach(var extension in config.Descendants("Extension"))
                            extensionMap[(string)extension.Attribute("Value")] = (string)extension.Attribute("Classifier");
                    }
                }
            }
            catch(Exception ex)
            {
                // Ignore configuration exceptions, there's not much we can do
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        /// <summary>
        /// This is used to determine whether or not a file is XML by attempting to load it
        /// </summary>
        /// <param name="filename">The filename to test</param>
        /// <returns>True if the file is XML, false if not</returns>
        private static bool FileIsXml(string filename)
        {
            try
            {
                // If it doesn't exist, it won't matter as there won't be any content to classify
                if(!File.Exists(filename))
                    return false;

                XDocument.Load(filename);
            }
            catch(Exception ex)
            {
                // Ignore any exceptions and treat the file as plain text
                System.Diagnostics.Debug.WriteLine(ex);
                return false;
            }

            return true;
        }
        #endregion
    }
}
