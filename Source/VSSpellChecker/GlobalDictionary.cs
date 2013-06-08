//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : GlobalDictionary.cs
// Author  : Eric Woodruff
// Updated : 05/23/2013
// Note    : Copyright 2013, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a class that implements the global dictionary
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code.  It can also be found at the project website: http://VSSpellChecker.CodePlex.com.
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
// Version     Date     Who  Comments
//===============================================================================================================
// 1.0.0.0  04/14/2013  EFW  Created the code
//===============================================================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;

using NHunspell;

using Microsoft.VisualStudio.Text;

using VisualStudio.SpellChecker.Definitions;

namespace VisualStudio.SpellChecker
{
    /// <summary>
    /// This class implements the global dictionary
    /// </summary>
    internal sealed class GlobalDictionary : ISpellingDictionary
    {
        #region Private data members
        //=====================================================================

        private static Dictionary<string, GlobalDictionary> globalDictionaries;
        private static SpellEngine spellEngine;

        private List<WeakReference> registeredServices;
        private HashSet<string> ignoredWords;
        private SpellFactory spellFactory;
        private string ignoredWordsFile;

        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Private constructor
        /// </summary>
        /// <param name="culture">The language to use for the dictionary</param>
        /// <param name="spellFactory">The spell factory to use when checking words</param>
        private GlobalDictionary(CultureInfo culture, SpellFactory spellFactory)
        {
            this.spellFactory = spellFactory;

            registeredServices = new List<WeakReference>();
            ignoredWords = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            ignoredWordsFile = Path.Combine(SpellCheckerConfiguration.ConfigurationFilePath,
                culture.Name + "_Ignored.dic");

            this.LoadIgnoredWordsFile();
        }
        #endregion

        #region ISpellingDictionary Members
        //=====================================================================

        /// <inheritdoc />
        public bool IsSpelledCorrectly(string word)
        {
            try
            {
                if(spellFactory != null && !String.IsNullOrWhiteSpace(word))
                    return spellFactory.Spell(word);
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                // Eat exceptions, there's not much we can do
            }

            return true;
        }

        /// <inheritdoc />
        public IEnumerable<string> SuggestCorrections(string word)
        {
            List<string> suggestions = null;

            try
            {
                if(spellFactory != null && !String.IsNullOrWhiteSpace(word))
                    suggestions = spellFactory.Suggest(word);
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                // Eat exceptions, there's not much we can do
            }

            return (suggestions ?? new List<string>());
        }

        /// <inheritdoc />
        public bool AddWordToDictionary(string word)
        {
            if(String.IsNullOrWhiteSpace(word))
                return false;

            if(this.ShouldIgnoreWord(word))
                return true;

            using(StreamWriter writer = new StreamWriter(ignoredWordsFile, true))
            {
                writer.WriteLine(word);
            }

            return this.IgnoreWord(word);
        }

        /// <inheritdoc />
        /// <remarks>The global dictionary does not support ignoring words once</remarks>
        public void IgnoreWordOnce(ITrackingSpan span)
        {
        }

        /// <inheritdoc />
        public bool IgnoreWord(string word)
        {
            if(String.IsNullOrWhiteSpace(word) || this.ShouldIgnoreWord(word))
                return true;

            lock(ignoredWords)
            {
                ignoredWords.Add(word);
            }

            this.NotifySpellingServicesOfChange(word);

            return true;
        }

        /// <inheritdoc />
        public bool ShouldIgnoreWord(string word)
        {
            lock(ignoredWords)
            {
                return ignoredWords.Contains(word);
            }
        }

#pragma warning disable 0067

        /// <inheritdoc />
        /// <remarks>This event is not used by the global dictionary</remarks>
        public event EventHandler<SpellingEventArgs> DictionaryUpdated;

        /// <inheritdoc />
        /// <remarks>This event is not used by the global dictionary</remarks>
        public event EventHandler<SpellingEventArgs> IgnoreOnce;

        /// <inheritdoc />
        /// <remarks>This event is not used by the global dictionary</remarks>
        public event EventHandler<SpellingEventArgs> ReplaceAll;

#pragma warning restore 0067

        /// <inheritdoc />
        /// <remarks>This method is not used by the global dictionary</remarks>
        public void ReplaceAllOccurrences(string word, string replacement)
        {
        }
        #endregion

        #region Methods
        //=====================================================================

        /// <summary>
        /// Create a global dictionary for the specified culture
        /// </summary>
        /// <param name="culture">The language to use for the dictionary</param>
        /// <returns>The spell factory to use or null if one could not be created</returns>
        public static GlobalDictionary CreateGlobalDictionary(CultureInfo culture)
        {
            GlobalDictionary globalDictionary = null;

            try
            {
                if(globalDictionaries == null)
                    globalDictionaries = new Dictionary<string, GlobalDictionary>();

                // If no culture is specified, use the default culture
                if(culture == null)
                    culture = SpellCheckerConfiguration.DefaultLanguage;

                // If not already loaded, create the dictionary and the thread-safe spell factory instance for
                // the given culture.
                if(!globalDictionaries.ContainsKey(culture.Name))
                {
                    string dllPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                        "NHunspell");

                    if(spellEngine == null)
                    {
                        Hunspell.NativeDllPath = dllPath;
                        spellEngine = new SpellEngine();
                    }

                    // Look in the configuration folder first for user-supplied dictionaries
                    string dictionaryFile = Path.Combine(SpellCheckerConfiguration.ConfigurationFilePath,
                        culture.Name.Replace("-", "_") + ".aff");

                    // If not found, default to the English dictionary supplied with the package.  This can at
                    // least clue us in that it didn't find the language-specific dictionary when the suggestions
                    // are in English.
                    if(!File.Exists(dictionaryFile))
                        dictionaryFile = Path.Combine(dllPath, "en_US.aff");

                    LanguageConfig lc = new LanguageConfig();
                    lc.LanguageCode = culture.Name;
                    lc.HunspellAffFile = dictionaryFile;
                    lc.HunspellDictFile = Path.ChangeExtension(dictionaryFile, ".dic");

                    spellEngine.AddLanguage(lc);

                    globalDictionaries.Add(culture.Name, new GlobalDictionary(culture,
                        spellEngine[culture.Name]));
                }

                globalDictionary = globalDictionaries[culture.Name];
            }
            catch(Exception ex)
            {
                // Ignore exceptions.  Not much we can do, we'll just not spell check anything.
                System.Diagnostics.Debug.WriteLine(ex);
            }

            return globalDictionary;
        }

        /// <summary>
        /// This is used to register a spelling dictionary service with the global dictionary so that it is
        /// notified of changes to the global dictionary.
        /// </summary>
        /// <param name="service">The dictionary service to register</param>
        public void RegisterSpellingDictionaryService(SpellingDictionaryService service)
        {
            registeredServices.Add(new WeakReference(service));
        }

        /// <summary>
        /// This is used to notify all registered spelling dictionary services of a change to the global
        /// dictionary.
        /// </summary>
        /// <param name="word">The word that triggered the change</param>
        private void NotifySpellingServicesOfChange(string word)
        {
            var referencesToRemove = new List<WeakReference>();

            foreach(var service in registeredServices)
            {
                var target = service.Target as SpellingDictionaryService;

                if(target != null)
                    target.GlobalDictionaryUpdated(word);
                else
                    referencesToRemove.Add(service);
            }

            foreach(var reference in referencesToRemove)
                registeredServices.Remove(reference);
        }

        /// <summary>
        /// This is used to load the ignored words file for a specific language if it exists
        /// </summary>
        public static void LoadIgnoredWordsFile(CultureInfo language)
        {
            GlobalDictionary g;

            if(globalDictionaries != null && globalDictionaries.TryGetValue(language.Name, out g))
            {
                g.LoadIgnoredWordsFile();
                g.NotifySpellingServicesOfChange(null);
            }
        }

        /// <summary>
        /// This is used to load the ignored words file
        /// </summary>
        private void LoadIgnoredWordsFile()
        {
            ignoredWords.Clear();

            if(File.Exists(ignoredWordsFile))
            {
                try
                {
                    foreach(string word in File.ReadLines(ignoredWordsFile))
                        if(!String.IsNullOrWhiteSpace(word))
                            ignoredWords.Add(word.Trim());
                }
                catch(Exception ex)
                {
                    // Ignore exceptions.  Not much we can do, we'll just not ignore anything by default.
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            }
        }
        #endregion
    }
}
