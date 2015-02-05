//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : SpellCheckerDictionary.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 02/13/2015
// Note    : Copyright 2015, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains the class used to contain information about the available spell checker dictionaries
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
//===============================================================================================================
// 02/11/2015  EFW  Created the code
//===============================================================================================================

using System;
using System.Globalization;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace VisualStudio.SpellChecker.Configuration
{
    /// <summary>
    /// This class is used to contain information about the available spell checker dictionaries
    /// </summary>
    public class SpellCheckerDictionary
    {
        #region Properties
        //=====================================================================

        /// <summary>
        /// This read-only property returns the dictionary culture information
        /// </summary>
        public CultureInfo Culture { get; private set; }

        /// <summary>
        /// This read-only property returns the affix file path
        /// </summary>
        public string AffixFilePath { get; private set; }

        /// <summary>
        /// This read-only property returns the dictionary file path
        /// </summary>
        public string DictionaryFilePath { get; private set; }

        /// <summary>
        /// This read-only property returns the user dictionary file path
        /// </summary>
        public string UserDictionaryFilePath { get; private set; }

        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="culture">The dictionary culture</param>
        /// <param name="affixPath">The affix file path</param>
        /// <param name="dictionaryPath">The dictionary file path</param>
        /// <param name="userDictionaryPath">The user dictionary file path</param>
        public SpellCheckerDictionary(CultureInfo culture, string affixPath, string dictionaryPath,
          string userDictionaryPath)
        {
            this.Culture = culture;
            this.AffixFilePath = affixPath;
            this.DictionaryFilePath = dictionaryPath;
            this.UserDictionaryFilePath = userDictionaryPath;
        }
        #endregion

        #region Equality, hash code, ToString
        //=====================================================================

        /// <summary>
        /// Returns a value indicating whether two specified instances of <c>SpellCheckerDictionary</c> are equal
        /// </summary>
        /// <param name="d1">The first dictionary to compare</param>
        /// <param name="d2">The second dictionary to compare</param>
        /// <returns>Returns true if the dictionaries are equal, false if they are not</returns>
        public static bool Equals(SpellCheckerDictionary d1, SpellCheckerDictionary d2)
        {
            if((object)d1 == null && (object)d2 == null)
                return true;

            if((object)d1 == null)
                return false;

            return d1.Equals(d2);
        }

        /// <summary>
        /// This is overridden to allow proper comparison of <c>SpellCheckerDictionary</c> objects
        /// </summary>
        /// <param name="obj">The object to which this instance is compared</param>
        /// <returns>Returns true if the object equals this instance, false if it does not</returns>
        public override bool Equals(object obj)
        {
            SpellCheckerDictionary d = obj as SpellCheckerDictionary;

            return (d != null && this.Culture.Name == d.Culture.Name);
        }

        /// <summary>
        /// Get a hash code for the dictionary object
        /// </summary>
        /// <returns>Returns the hash code for the culture</returns>
        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }

        /// <summary>
        /// Convert the spell checker dictionary to its string form, a description of the culture
        /// </summary>
        /// <returns>Returns the culture description string</returns>
        public override string ToString()
        {
            // The invariant culture is used in the configuration editor to represent the inherited state
            if(this.Culture == CultureInfo.InvariantCulture)
                return "Inherited";

            // Replace parentheses with a comma and append the language ID in parentheses
            string description = this.Culture.EnglishName.Replace(" (", ", ").Replace(")", String.Empty);

            if(!String.IsNullOrEmpty(this.Culture.Name))
                description += " (" + this.Culture.Name + ")";

            return description;
        }
        #endregion

        #region Helper methods
        //=====================================================================

        /// <summary>
        /// This returns an enumerable list of available dictionaries
        /// </summary>
        /// <remarks>The returned enumerable list contains the default English (en-US) dictionary along with
        /// any custom dictionaries found in the <see cref="SpellingConfigurationFile.GlobalConfigurationFilePath"/>
        /// folder and any optional additional search folders specified.</remarks>
        public static IDictionary<string, SpellCheckerDictionary> AvailableDictionaries(
          IEnumerable<string> additionalSearchFolders)
        {
            Dictionary<string, SpellCheckerDictionary> availableDictionaries = new Dictionary<string,
                SpellCheckerDictionary>(StringComparer.OrdinalIgnoreCase);
            CultureInfo info;
            string dllPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // This is supplied with the application and is always available.  It may be replaced by a
            // user-supplied dictionary below.
            availableDictionaries["en-US"] = new SpellCheckerDictionary(new CultureInfo("en-US"),
                Path.Combine(dllPath, "en_US.aff"), Path.Combine(dllPath, "en_US.dic"),
                Path.Combine(SpellingConfigurationFile.GlobalConfigurationFilePath, "en-US_User.dic"));

            var searchFolders = new List<string>();
            searchFolders.Add(SpellingConfigurationFile.GlobalConfigurationFilePath);

            if(additionalSearchFolders != null)
                searchFolders.AddRange(additionalSearchFolders);

            foreach(string folder in searchFolders)
            {
                try
                {
                    // Culture names can vary in format (en-US, arn, az-Cyrl, az-Cyrl-AZ, az-Latn, az-Latn-AZ,
                    // etc.) so look for any affix files with a related dictionary file and see if they are valid
                    // cultures.  If so, we'll take them.
                    if(Directory.Exists(folder))
                        foreach(string affixFile in Directory.EnumerateFiles(folder, "*.aff"))
                            if(File.Exists(Path.ChangeExtension(affixFile, ".dic")))
                            {
                                try
                                {
                                    info = new CultureInfo(Path.GetFileNameWithoutExtension(affixFile).Replace("_", "-"));
                                }
                                catch(CultureNotFoundException)
                                {
                                    // Ignore filenames that are not cultures
                                    info = null;
                                }

                                if(info != null)
                                    availableDictionaries[info.Name] = new SpellCheckerDictionary(info, affixFile,
                                        Path.ChangeExtension(affixFile, ".dic"),
                                        Path.Combine(Path.GetDirectoryName(affixFile), info.Name + "_User.dic"));
                            }
                }
                catch(Exception ex)
                {
                    // Ignore exceptions due to inaccessible folders
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            }

            return availableDictionaries;
        }
        #endregion
    }
}
