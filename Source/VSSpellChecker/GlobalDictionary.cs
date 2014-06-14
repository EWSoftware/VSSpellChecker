//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : GlobalDictionary.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 06/13/2014
// Note    : Copyright 2013-2014, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a class that implements the global dictionary
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code.  It can also be found at the project website: http://VSSpellChecker.CodePlex.com.
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
//===============================================================================================================
// 04/14/2013  EFW  Created the code
//===============================================================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;

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
        private HashSet<string> dictionaryWords, ignoredWords;
        private CultureInfo culture;
        private SpellFactory spellFactory;
        private string dictionaryWordsFile;

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
            this.culture = culture;
            this.spellFactory = spellFactory;

            registeredServices = new List<WeakReference>();

            dictionaryWords = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            dictionaryWordsFile = Path.Combine(SpellCheckerConfiguration.ConfigurationFilePath,
                culture.Name + "_User.dic");

            ignoredWords = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

            this.LoadUserDictionaryFile();
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
                // Ignore exceptions, there's not much we can do
                System.Diagnostics.Debug.WriteLine(ex);
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
                // Ignore exceptions, there's not much we can do
                System.Diagnostics.Debug.WriteLine(ex);
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

            using(StreamWriter writer = new StreamWriter(dictionaryWordsFile, true))
            {
                writer.WriteLine(word);
            }

            lock(dictionaryWords)
            {
                dictionaryWords.Add(word);
            }

            this.AddSuggestion(word);
            this.NotifySpellingServicesOfChange(word);

            return true;
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
                if(ignoredWords.Contains(word))
                    return true;
            }

            return SpellCheckerConfiguration.ShouldIgnoreWord(word);
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
            string affixFile, dictionaryFile;

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
                    affixFile = Path.Combine(SpellCheckerConfiguration.ConfigurationFilePath,
                        culture.Name + ".aff");
                    dictionaryFile = Path.ChangeExtension(affixFile, ".dic");

                    // Dictionary file names may use a dash or an underscore as the separator.  Try both ways
                    // in case they aren't named consistently which does happen.
                    if(!File.Exists(affixFile))
                        affixFile = Path.Combine(SpellCheckerConfiguration.ConfigurationFilePath,
                            culture.Name.Replace('-', '_') + ".aff");

                    if(!File.Exists(dictionaryFile))
                        dictionaryFile = Path.Combine(SpellCheckerConfiguration.ConfigurationFilePath,
                            culture.Name.Replace('-', '_') + ".dic");

                    // If not found, default to the English dictionary supplied with the package.  This can at
                    // least clue us in that it didn't find the language-specific dictionary when the suggestions
                    // are in English.
                    if(!File.Exists(affixFile) || !File.Exists(dictionaryFile))
                    {
                        affixFile = Path.Combine(dllPath, "en_US.aff");
                        dictionaryFile = Path.ChangeExtension(affixFile, ".dic");
                    }

                    spellEngine.AddLanguage(new LanguageConfig
                    {
                        LanguageCode = culture.Name,
                        HunspellAffFile = affixFile,
                        HunspellDictFile = dictionaryFile
                    });

                    globalDictionaries.Add(culture.Name, new GlobalDictionary(culture, spellEngine[culture.Name]));
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
        /// This is used to load the user dictionary words file for a specific language if it exists
        /// </summary>
        public static void LoadUserDictionaryFile(CultureInfo language)
        {
            GlobalDictionary g;

            if(globalDictionaries != null && globalDictionaries.TryGetValue(language.Name, out g))
            {
                g.LoadUserDictionaryFile();
                g.NotifySpellingServicesOfChange(null);
            }
        }

        /// <summary>
        /// This is used to load the user dictionary words file
        /// </summary>
        private void LoadUserDictionaryFile()
        {
            dictionaryWords.Clear();

            if(File.Exists(dictionaryWordsFile))
            {
                try
                {
                    foreach(string word in File.ReadLines(dictionaryWordsFile))
                        if(!String.IsNullOrWhiteSpace(word))
                            dictionaryWords.Add(word.Trim());

                    this.AddSuggestions();
                }
                catch(Exception ex)
                {
                    // Ignore exceptions.  Not much we can do, we'll just not ignore anything by default.
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            }
            else
            {
                // Older versions used a different filename.  If found, rename it and use it.
                string oldFilename = dictionaryWordsFile.Replace("_User", "_Ignored");

                try
                {
                    if(File.Exists(oldFilename))
                    {
                        if(File.Exists(dictionaryWordsFile))
                            File.Delete(dictionaryWordsFile);

                        File.Move(oldFilename, dictionaryWordsFile);
                        LoadUserDictionaryFile();
                    }
                }
                catch(Exception ex)
                {
                    // Ignore exceptions.  We just won't load the old file.
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            }
        }

        /// <summary>
        /// Add a new word as a suggestion to the Hunspell instances
        /// </summary>
        /// <remarks>The word is not added to the Hunspell dictionary files, just the speller instances</remarks>
        private void AddSuggestion(string word)
        {
            // Since we're using the factory, we've got to get at the internals using reflection
            Type sf = spellFactory.GetType();

            FieldInfo fi = sf.GetField("processors", BindingFlags.Instance | BindingFlags.NonPublic);

            int releaseCount = 0, processors = (int)fi.GetValue(spellFactory);

            fi = sf.GetField("hunspellSemaphore", BindingFlags.Instance | BindingFlags.NonPublic);

            Semaphore hunspellSemaphore = (Semaphore)fi.GetValue(spellFactory);

            fi = sf.GetField("hunspells", BindingFlags.Instance | BindingFlags.NonPublic);

            Stack<Hunspell> hunspells = (Stack<Hunspell>)fi.GetValue(spellFactory);

            if(hunspellSemaphore != null && hunspells != null)
                try
                {
                    // Make sure we get all semaphores since we will be touching all spellers
                    while(releaseCount < processors)
                    {
                        // Don't wait too long.  If we can't get them all, we just won't add the words
                        // as suggestions this time around.
                        if(!hunspellSemaphore.WaitOne(2000))
                            break;

                        releaseCount++;
                    }

                    if(releaseCount == processors)
                        foreach(var hs in hunspells.ToArray())
                            if(!hs.Spell(word))
                                hs.Add(word.ToLower(culture));
                }
                catch(Exception ex)
                {
                    // Ignore any exceptions.  Worst case, some words won't be added as suggestions.
                    System.Diagnostics.Debug.WriteLine(ex);
                }
                finally
                {
                    if(releaseCount != 0)
                        hunspellSemaphore.Release(releaseCount);
                }
        }

        /// <summary>
        /// Add the user dictionary words as suggestions to the Hunspell instances
        /// </summary>
        /// <remarks>The words are not added to the Hunspell dictionary files, just the speller instances</remarks>
        private void AddSuggestions()
        {
            // Since we're using the factory, we've got to get at the internals using reflection
            Type sf = spellFactory.GetType();

            FieldInfo fi = sf.GetField("processors", BindingFlags.Instance | BindingFlags.NonPublic);

            int releaseCount = 0, processors = (int)fi.GetValue(spellFactory);

            fi = sf.GetField("hunspellSemaphore", BindingFlags.Instance | BindingFlags.NonPublic);

            Semaphore hunspellSemaphore = (Semaphore)fi.GetValue(spellFactory);

            fi = sf.GetField("hunspells", BindingFlags.Instance | BindingFlags.NonPublic);

            Stack<Hunspell> hunspells = (Stack<Hunspell>)fi.GetValue(spellFactory);

            if(hunspellSemaphore != null && hunspells != null)
                try
                {
                    // Make sure we get all semaphores since we will be touching all spellers
                    while(releaseCount < processors)
                    {
                        // Don't wait too long.  If we can't get them all, we just won't add the words
                        // as suggestions this time around.
                        if(!hunspellSemaphore.WaitOne(2000))
                            break;

                        releaseCount++;
                    }

                    if(releaseCount == processors)
                        foreach(var hs in hunspells.ToArray())
                            foreach(string word in dictionaryWords)
                                if(!hs.Spell(word))
                                    hs.Add(word.ToLower(culture));
                }
                catch(Exception ex)
                {
                    // Ignore any exceptions.  Worst case, some words won't be added as suggestions.
                    System.Diagnostics.Debug.WriteLine(ex);
                }
                finally
                {
                    if(releaseCount != 0)
                        hunspellSemaphore.Release(releaseCount);
                }
        }

        /// <summary>
        /// Remove the given word from the global dictionaries
        /// </summary>
        /// <param name="word">The word to remove</param>
        public static void RemoveWord(CultureInfo language, string word)
        {
            GlobalDictionary g;

            if(!String.IsNullOrWhiteSpace(word) && globalDictionaries != null &&
              globalDictionaries.TryGetValue(language.Name, out g))
                g.RemoveWord(word);
        }

        /// <summary>
        /// Remove the given word from the Hunspell instances 
        /// </summary>
        /// <param name="word">The word to remove</param>
        /// <remarks>The word is not removed from the Hunspell dictionary files, just the speller instances</remarks>
        private void RemoveWord(string word)
        {
            // Since we're using the factory, we've got to get at the internals using reflection
            Type sf = spellFactory.GetType();

            FieldInfo fi = sf.GetField("processors", BindingFlags.Instance | BindingFlags.NonPublic);

            int releaseCount = 0, processors = (int)fi.GetValue(spellFactory);

            fi = sf.GetField("hunspellSemaphore", BindingFlags.Instance | BindingFlags.NonPublic);

            Semaphore hunspellSemaphore = (Semaphore)fi.GetValue(spellFactory);

            fi = sf.GetField("hunspells", BindingFlags.Instance | BindingFlags.NonPublic);

            Stack<Hunspell> hunspells = (Stack<Hunspell>)fi.GetValue(spellFactory);

            if(hunspellSemaphore != null && hunspells != null)
                try
                {
                    // Make sure we get all semaphores since we will be touching all spellers
                    while(releaseCount < processors)
                    {
                        // Don't wait too long.  If we can't get them all, we just won't add the words
                        // as suggestions this time around.
                        if(!hunspellSemaphore.WaitOne(2000))
                            break;

                        releaseCount++;
                    }

                    if(releaseCount == processors)
                        foreach(var hs in hunspells.ToArray())
                            if(hs.Spell(word))
                                hs.Remove(word.ToLower(culture));
                }
                catch(Exception ex)
                {
                    // Ignore any exceptions.  Worst case, some words won't be added as suggestions.
                    System.Diagnostics.Debug.WriteLine(ex);
                }
                finally
                {
                    if(releaseCount != 0)
                        hunspellSemaphore.Release(releaseCount);
                }
        }
        #endregion
    }
}
