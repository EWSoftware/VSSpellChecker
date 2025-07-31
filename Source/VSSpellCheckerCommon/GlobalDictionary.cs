//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : GlobalDictionary.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 07/31/2025
// Note    : Copyright 2013-2025, Eric Woodruff, All rights reserved
//
// This file contains a class that implements the global dictionary
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
//===============================================================================================================
// 04/14/2013  EFW  Created the code
//===============================================================================================================

// Ignore Spelling: Resharper Hunspellx

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using NHunspell;

using VisualStudio.SpellChecker.Common.Configuration;

namespace VisualStudio.SpellChecker.Common
{
    /// <summary>
    /// This class implements the global dictionary
    /// </summary>
    public sealed class GlobalDictionary : IDisposable
    {
        #region Private data members
        //=====================================================================

        private static Dictionary<string, GlobalDictionary> globalDictionaries;
        private static SpellEngine spellEngine;

        private readonly List<WeakReference> registeredServices;
        private readonly HashSet<string> dictionaryWords, ignoredWords, recognizedWords;
        private SpellFactory spellFactory;
        private string dictionaryFile, dictionaryWordsFile;
        private bool isDisposed;

        private static readonly object syncRoot = new();

        #endregion

        #region Properties
        //=====================================================================

        /// <summary>
        /// This read-only property returns the dictionary's culture
        /// </summary>
        public CultureInfo Culture { get; }

        /// <summary>
        /// This read-only property indicates whether or not the dictionary is initialized
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// This read-only property returns the name of the user dictionary file
        /// </summary>
        public string UserDictionaryFile => dictionaryWordsFile;

        /// <summary>
        /// This can be used to assign a function that checks to see if the user words file is writable
        /// </summary>
        /// <remarks>The default implementation only checks to see if the file is writable.  It does not take
        /// source control status into consideration and will not add it to the active project.  Assign a
        /// different function to this property that implements that functionality if necessary.</remarks>
        public static Func<string, string, bool, bool> CanWriteToUserWordsFile { get; set; } = CanWriteToUserWordsFileDefault;

        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Private constructor
        /// </summary>
        /// <param name="culture">The language to use for the dictionary</param>
        /// <param name="recognizedWords">An optional list of recognized words that will be added to the
        /// dictionary (i.e. from a code analysis dictionary).</param>
        private GlobalDictionary(CultureInfo culture, IEnumerable<string> recognizedWords)
        {
            this.Culture = culture;

            registeredServices = [];
            dictionaryWords = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            ignoredWords = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

            this.recognizedWords = new HashSet<string>(recognizedWords, StringComparer.InvariantCultureIgnoreCase);
        }
        #endregion

        #region IDisposable members
        //=====================================================================

        /// <inheritdoc />
        public void Dispose()
        {
            lock(syncRoot)
            {
                if(spellFactory != null)
                {
                    spellFactory.Dispose();
                    spellFactory = null;

                    registeredServices.Clear();
                }

                isDisposed = true;

                this.IsInitialized = false;
            }
        }
        #endregion

        #region Dictionary service interaction methods
        //=====================================================================

        /// <summary>
        /// This is used to spell check a word
        /// </summary>
        /// <param name="word">The word to spell check</param>
        /// <returns>True if spelled correctly, false if not</returns>
        public bool IsSpelledCorrectly(string word)
        {
            try
            {
                if(this.IsInitialized && spellFactory != null && !spellFactory.IsDisposed && !String.IsNullOrWhiteSpace(word))
                    return spellFactory.Spell(word);
            }
            catch(Exception ex)
            {
                // Ignore exceptions, there's not much we can do
                System.Diagnostics.Debug.WriteLine(ex);
            }

            return true;
        }

        /// <summary>
        /// This is used to spell check a word case insensitively
        /// </summary>
        /// <param name="word">The word to spell check</param>
        /// <returns>True if spelled correctly, false if not</returns>
        /// <remarks>The word is converted to uppercase to bypass the case sensitive setting in the dictionary</remarks>
        public bool IsSpelledCorrectlyIgnoreCase(string word)
        {
            try
            {
                if(this.IsInitialized && spellFactory != null && !spellFactory.IsDisposed && !String.IsNullOrWhiteSpace(word))
                    return spellFactory.Spell(word.ToUpper(this.Culture));
            }
            catch(Exception ex)
            {
                // Ignore exceptions, there's not much we can do
                System.Diagnostics.Debug.WriteLine(ex);
            }

            return true;
        }

        /// <summary>
        /// This is used to suggest corrections for a misspelled word
        /// </summary>
        /// <param name="word">The misspelled word for which to get suggestions</param>
        /// <returns>An enumerable list of zero or more suggested correct spellings</returns>
        public IEnumerable<SpellingSuggestion> SuggestCorrections(string word)
        {
            try
            {
                if(this.IsInitialized && spellFactory != null && !spellFactory.IsDisposed && !String.IsNullOrWhiteSpace(word))
                    return spellFactory.Suggest(word).Select(w => new SpellingSuggestion(this.Culture, w));
            }
            catch(Exception ex)
            {
                // Ignore exceptions, there's not much we can do
                System.Diagnostics.Debug.WriteLine(ex);
            }

            return [];
        }

        /// <summary>
        /// Add the given word to the dictionary so that it will no longer show up as an incorrect spelling.
        /// </summary>
        /// <param name="word">The word to add to the dictionary.</param>
        /// <returns>True if the word was successfully added to the dictionary, was an ignored word, or was
        /// already in the main or user dictionary.  False if the word to add is blank or the user dictionary
        /// could not be updated.</returns>
        public bool AddWordToDictionary(string word)
        {
            if(!this.IsInitialized || String.IsNullOrWhiteSpace(word))
                return false;

            string originalWord = word;

            // Remove mnemonics
            word = word.Replace("&", String.Empty).Replace("_", String.Empty);

            if(this.ShouldIgnoreWord(word) || this.IsSpelledCorrectly(word))
                return true;

            if(!(CanWriteToUserWordsFile ?? CanWriteToUserWordsFileDefault)(dictionaryWordsFile, dictionaryFile, true))
                return false;

            bool multipleWordsAdded = false;

            lock(dictionaryWords)
            {
                var currentDictionary = new HashSet<string>(CommonUtilities.LoadUserDictionary(dictionaryWordsFile, false,
                    false), StringComparer.OrdinalIgnoreCase);

                dictionaryWords.Add(word);

                // The word may have been added by another instance of Visual Studio.  If so, don't save the change.
                if(!currentDictionary.Contains(word))
                {
                    int wordCount = dictionaryWords.Count;

                    // Add words added by other instances of Visual Studio not already in this copy of the
                    // dictionary so that we don't lose them when the file is saved.  All new ones will be
                    // added as suggestions below.
                    dictionaryWords.UnionWith(currentDictionary);

                    multipleWordsAdded = (wordCount != dictionaryWords.Count);

                    // Sort and write all the words to the file.  If under source control, this should minimize the
                    // number of merge conflicts that could result if multiple people added words and they were all
                    // written to the end of the file.
                    File.WriteAllLines(dictionaryWordsFile, dictionaryWords.OrderBy(w => w));
                }
            }

            if(multipleWordsAdded)
                this.AddSuggestions();
            else
                this.AddSuggestion(word);

            // Must pass the original word with mnemonics as it must match the span text.  If multiple words
            // were added from other instances, pass null to respell all text.
            this.NotifySpellingServicesOfChange(multipleWordsAdded ? null : originalWord);

            return true;
        }

        /// <summary>
        /// Ignore all occurrences of the given word, but don't add it to the dictionary.
        /// </summary>
        /// <param name="word">The word to be ignored.</param>
        /// <returns><c>true</c> if the word was successfully marked as ignored.</returns>
        public bool IgnoreWord(string word)
        {
            if(String.IsNullOrWhiteSpace(word) || this.ShouldIgnoreWord(word))
                return true;

            lock(ignoredWords)
            {
                // Remove mnemonics here but pass the original word below as it must match the span text
                ignoredWords.Add(word.Replace("&", String.Empty).Replace("_", String.Empty));
            }

            this.NotifySpellingServicesOfChange(word);

            return true;
        }

        /// <summary>
        /// Check the ignored words dictionary for the given word.
        /// </summary>
        /// <param name="word">The word for which to check</param>
        /// <returns>True if the word should be ignored, false if not</returns>
        public bool ShouldIgnoreWord(string word)
        {
            lock(ignoredWords)
            {
                return word == null || ignoredWords.Contains(word.Replace("&", String.Empty).Replace("_", String.Empty));
            }
        }
        #endregion

        #region General methods
        //=====================================================================

        /// <summary>
        /// This is used to determine if the given user dictionary words file can be written to
        /// </summary>
        /// <param name="dictionaryWordsFile">The user dictionary words file</param>
        /// <param name="dictionaryFile">The related dictionary file or null if there isn't one</param>
        /// <param name="checkOutForEdit">This is unused but is needed to match the package version that has a
        /// parameter used to indicate whether or not the file should be checked out for editing.</param>
        /// <returns>True if it can, false if not.</returns>
        /// <remarks>This only checks to see if the file is writable.  It does not take source control status
        /// into consideration and will not add it to the active project.  Use the
        /// <see cref="CanWriteToUserWordsFile"/> property to assign a different function that does if necessary.</remarks>
        private static bool CanWriteToUserWordsFileDefault(string dictionaryWordsFile, string dictionaryFile,
          bool checkOutForEdit)
        {
            if(String.IsNullOrWhiteSpace(dictionaryWordsFile))
                throw new ArgumentException("Dictionary words file cannot be null or empty", nameof(dictionaryWordsFile));

            if(dictionaryFile != null && dictionaryFile.Trim().Length == 0)
                throw new ArgumentException("Dictionary file cannot be empty", nameof(dictionaryFile));

            // The file must exist
            if(!File.Exists(dictionaryWordsFile))
                File.WriteAllText(dictionaryWordsFile, String.Empty);

            // If it's in the global configuration folder, we can write to it if not read-only
            if(Path.GetDirectoryName(dictionaryWordsFile).StartsWith(
              SpellCheckerConfiguration.GlobalConfigurationFilePath, StringComparison.OrdinalIgnoreCase))
            {
                return ((File.GetAttributes(dictionaryWordsFile) & FileAttributes.ReadOnly) == 0);
            }

            return ((File.GetAttributes(dictionaryWordsFile) & FileAttributes.ReadOnly) == 0);
        }

        /// <summary>
        /// Create a global dictionary for the specified culture
        /// </summary>
        /// <param name="culture">The language to use for the dictionary.</param>
        /// <param name="additionalDictionaryFolders">An enumerable list of additional folders to search for
        /// other dictionaries.</param>
        /// <param name="recognizedWords">An optional list of recognized words that will be added to the
        /// dictionary (i.e. from a code analysis dictionary).</param>
        /// <param name="initializeAsynchronously">True to initialize the dictionary asynchronously, false to
        /// wait for it to initialize before returning.</param>
        /// <returns>The global dictionary to use or null if one could not be created.</returns>
        public static GlobalDictionary CreateGlobalDictionary(CultureInfo culture,
          IEnumerable<string> additionalDictionaryFolders, IEnumerable<string> recognizedWords,
          bool initializeAsynchronously = true)
        {
            GlobalDictionary globalDictionary = null;

            try
            {
                globalDictionaries ??= [];

                if(spellEngine == null)
                {
                    // If other packages are installed that use NHunSpell (Resharper for instance), don't set
                    // the native DLL path again if we see the native Hunspell DLLs in the existing path or it
                    // tries to load them a second time and fails.  We'll just use their copy which should be
                    // fine since it hasn't changed in a long time (I know, never say never).
                    if(String.IsNullOrWhiteSpace(Hunspell.NativeDllPath) || !Directory.EnumerateFiles(
                      Hunspell.NativeDllPath, "Hunspellx*.dll").Any())
                    {
                        Hunspell.NativeDllPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    }

                    spellEngine = new SpellEngine();
                }

                // The configuration editor should disallow creating a configuration without at least one
                // language but if someone edits the file manually, they could remove them all.  If that
                // happens, just use the English-US dictionary.
                culture ??= new CultureInfo("en-US");

                // If not already loaded, create the dictionary and the thread-safe spell factory instance for
                // the given culture.
                if(!globalDictionaries.TryGetValue(culture.Name, out globalDictionary))
                {
                    globalDictionary = new GlobalDictionary(culture, recognizedWords);

                    if(initializeAsynchronously)
                    {
                        // Initialize the dictionaries asynchronously.  We don't care about the result here.
                        _ = Task.Run(() => globalDictionary.InitializeDictionary(additionalDictionaryFolders));
                    }
                    else
                        globalDictionary.InitializeDictionary(additionalDictionaryFolders);

                    globalDictionaries.Add(culture.Name, globalDictionary);
                }
                else
                {
                    // Add recognized words that are not already there and include the user dictionary in case it
                    // was modified by another process.
                    var dw = new HashSet<string>();

                    try
                    {
                        if(File.Exists(globalDictionary.UserDictionaryFile))
                        {
                            foreach(string word in File.ReadLines(globalDictionary.UserDictionaryFile))
                            {
                                if(!String.IsNullOrWhiteSpace(word))
                                    dw.Add(word.Trim());
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        // Ignore exceptions.  Not much we can do, we'll just not include those words for now.
                        System.Diagnostics.Debug.WriteLine(ex);
                    }

                    dw.UnionWith(recognizedWords);

                    globalDictionary.AddRecognizedWords(dw);
                }
            }
            catch(Exception ex)
            {
                // Ignore exceptions.  Not much we can do, we just won't spell check anything in this language.
                System.Diagnostics.Debug.WriteLine(ex);
            }

            return globalDictionary;
        }

        /// <summary>
        /// This is used to initialize the global dictionary
        /// </summary>
        /// <param name="additionalDictionaryFolders">An enumerable list of additional folders to search for
        /// other dictionaries.</param>
        private void InitializeDictionary(IEnumerable<string> additionalDictionaryFolders)
        {
            string affixFile, userWordsFile, dllPath = Path.Combine(Path.GetDirectoryName(
                Assembly.GetExecutingAssembly().Location), "NHunspell");

            // Look for all available dictionaries and get the one for the requested culture
            var dictionaries = SpellCheckerDictionary.AvailableDictionaries(additionalDictionaryFolders);

            if(dictionaries.TryGetValue(this.Culture.Name, out SpellCheckerDictionary userDictionary))
            {
                affixFile = userDictionary.AffixFilePath;
                dictionaryFile = userDictionary.DictionaryFilePath;
                userWordsFile = userDictionary.UserDictionaryFilePath;
            }
            else
                affixFile = dictionaryFile = userWordsFile = null;

            // If not found, default to the US English dictionary supplied with the package.  This can at
            // least clue us in that it didn't find the language-specific dictionary when the suggestions
            // are in US English.
            if(affixFile == null || dictionaryFile == null || !File.Exists(affixFile) ||
              !File.Exists(dictionaryFile))
            {
                affixFile = Path.Combine(dllPath, "en_US.aff");
                dictionaryFile = Path.ChangeExtension(affixFile, ".dic");
                userWordsFile = Path.Combine(SpellCheckerConfiguration.GlobalConfigurationFilePath,
                    "en-US_User.dic");
            }

            lock(syncRoot)
            {
                if(spellEngine != null && !isDisposed)
                {
                    spellEngine.AddLanguage(new LanguageConfig
                    {
                        LanguageCode = this.Culture.Name,
                        HunspellAffFile = affixFile,
                        HunspellDictFile = dictionaryFile
                    });

                    spellFactory = spellEngine[this.Culture.Name];

                    if(String.IsNullOrWhiteSpace(userWordsFile))
                    {
                        dictionaryWordsFile = Path.Combine(SpellCheckerConfiguration.GlobalConfigurationFilePath,
                            this.Culture.Name + "_User.dic");
                    }
                    else
                        dictionaryWordsFile = userWordsFile;

                    this.LoadUserDictionaryFile();
#if DEBUG
                    // Add an artificial delay to allow for testing readiness checks
                    //Thread.Sleep(5000);
#endif
                    this.IsInitialized = true;
                }
            }
        }

        /// <summary>
        /// This is called to clear the dictionary cache and dispose of the spelling engine
        /// </summary>
        /// <remarks>This is done whenever a change in solution is detected.  This allows for solution and
        /// project-specific dictionaries to override global dictionaries that may have been in use in a previous
        /// solution.</remarks>
        public static void ClearDictionaryCache()
        {
            lock(syncRoot)
            {
                if(globalDictionaries != null)
                {
                    foreach(var gd in globalDictionaries.Values)
                        gd.Dispose();

                    globalDictionaries.Clear();
                }

                if(spellEngine != null)
                {
                    spellEngine.Dispose();
                    spellEngine = null;
                }
            }
        }

        /// <summary>
        /// This is used to register a spelling dictionary service with the global dictionary so that it is
        /// notified of changes to the global dictionary.
        /// </summary>
        /// <param name="service">The dictionary service to register</param>
        public void RegisterSpellingDictionaryService(SpellingDictionary service)
        {
            lock(syncRoot)
            {
                // Clear out ones that have been disposed of
                foreach(var svc in registeredServices.Where(s => !s.IsAlive).ToArray())
                    registeredServices.Remove(svc);

                System.Diagnostics.Debug.WriteLine("Registered services count: {0}", registeredServices.Count);

                registeredServices.Add(new WeakReference(service));
            }
        }

        /// <summary>
        /// This is used to notify all registered spelling dictionary services of a change to the global
        /// dictionary.
        /// </summary>
        /// <param name="word">The word that triggered the change</param>
        private void NotifySpellingServicesOfChange(string word)
        {
            lock(syncRoot)
            {
                // Clear out ones that have been disposed of
                foreach(var service in registeredServices.Where(s => !s.IsAlive).ToArray())
                    registeredServices.Remove(service);

                System.Diagnostics.Debug.WriteLine("Registered services count: {0}", registeredServices.Count);

                foreach(var service in registeredServices)
                {
                    if(service.Target is SpellingDictionary target)
                        target.GlobalDictionaryUpdated(word);
                }
            }
        }

        /// <summary>
        /// This is used to load the user dictionary words file for a specific language if it exists
        /// </summary>
        /// <param name="language">The language of the dictionary for which to load the user dictionary file</param>
        public static void LoadUserDictionaryFile(CultureInfo language)
        {
            if(language == null)
                throw new ArgumentNullException(nameof(language));

            if(globalDictionaries != null && globalDictionaries.TryGetValue(language.Name, out GlobalDictionary g) &&
              g.IsInitialized)
            {
                g.LoadUserDictionaryFile();
                g.NotifySpellingServicesOfChange(null);
            }
        }

        /// <summary>
        /// This is used to see if the dictionary for the given culture is ready for use if loaded
        /// </summary>
        /// <param name="language">The language of the dictionary for which to check readiness</param>
        public static bool IsReadyForUse(CultureInfo language)
        {
            if(language == null)
                throw new ArgumentNullException(nameof(language));

            if(globalDictionaries != null && globalDictionaries.TryGetValue(language.Name, out GlobalDictionary g))
                return g.IsInitialized;

            return true;
        }

        /// <summary>
        /// This is used to notify all registered spelling dictionary services of a change in status
        /// </summary>
        /// <remarks>This is used when turning the interactive spell checking on and off for the session</remarks>
        public static void NotifyChangeOfStatus()
        {
            if(globalDictionaries != null)
            {
                foreach(var g in globalDictionaries.Values)
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
                    {
                        if(!String.IsNullOrWhiteSpace(word))
                            dictionaryWords.Add(word.Trim());
                    }

                    this.AddSuggestions();
                }
                catch(Exception ex)
                {
                    // Ignore exceptions.  Not much we can do, we'll just not include those words for now.
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
                        this.LoadUserDictionaryFile();
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
        /// This is used to add an enumerable list of recognized words from a source such as a code analysis
        /// dictionary to the Hunspell dictionaries.
        /// </summary>
        /// <param name="words">The list of words to add</param>
        /// <remarks>Note that since dictionaries are global, the recognized words will be aggregated from all
        /// sources.  As such, you may see words from a code analysis dictionary in one project suggested as
        /// corrections while in another unrelated project.</remarks>
        public void AddRecognizedWords(IEnumerable<string> words)
        {
            bool addSuggestions = false;

            if(words == null)
                throw new ArgumentNullException(nameof(words));

            lock(recognizedWords)
            {
                foreach(string word in words)
                {
                    if(recognizedWords.Add(word))
                        addSuggestions = true;
                }

                if(addSuggestions && this.IsInitialized)
                    this.AddSuggestions();
            }
        }

        /// <summary>
        /// Add a new word as a suggestion to the Hunspell instances
        /// </summary>
        /// <remarks>The word is not added to the Hunspell dictionary files, just the speller instances</remarks>
        private void AddSuggestion(string word)
        {
            lock(syncRoot)
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
                {
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
                        {
                            foreach(var hs in hunspells.ToArray())
                            {
                                if(!hs.Spell(word))
                                    hs.Add(word.ToLower(this.Culture));
                            }
                        }
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
            }
        }

        /// <summary>
        /// Add the user dictionary and recognized words as suggestions to the Hunspell instances
        /// </summary>
        /// <remarks>The words are not added to the Hunspell dictionary files, just the speller instances</remarks>
        private void AddSuggestions()
        {
            lock(syncRoot)
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
                {
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

                        lock(recognizedWords)
                        {
                            if(releaseCount == processors)
                            {
                                foreach(var hs in hunspells.ToArray())
                                {
                                    foreach(string word in dictionaryWords.Concat(recognizedWords))
                                    {
                                        if(!hs.Spell(word))
                                            hs.Add(word.ToLower(this.Culture));
                                    }
                                }
                            }
                        }
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
            }
        }

        /// <summary>
        /// Remove the given word from a global dictionary
        /// </summary>
        /// <param name="language">The language of the dictionary from which to remove the word</param>
        /// <param name="word">The word to remove</param>
        public static void RemoveWord(CultureInfo language, string word)
        {
            if(language == null)
                throw new ArgumentNullException(nameof(language));

            if(!String.IsNullOrWhiteSpace(word) && globalDictionaries != null &&
              globalDictionaries.TryGetValue(language.Name, out GlobalDictionary g) && g.IsInitialized)
            {
                g.RemoveWord(word);
            }
        }

        /// <summary>
        /// Remove the given word from the Hunspell instances 
        /// </summary>
        /// <param name="word">The word to remove</param>
        /// <remarks>The word is not removed from the Hunspell dictionary files, just the speller instances</remarks>
        private void RemoveWord(string word)
        {
            lock(syncRoot)
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
                {
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
                        {
                            foreach(var hs in hunspells.ToArray())
                            {
                                if(hs.Spell(word))
                                    hs.Remove(word.ToLower(this.Culture));
                            }
                        }
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
            }
        }
        #endregion
    }
}
