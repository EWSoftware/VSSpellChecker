//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : Utility.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 08/30/2018
// Note    : Copyright 2013-2018, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a utility class with extension and utility methods.
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 05/25/2013  EFW  Created the code
//===============================================================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

using EnvDTE;
using EnvDTE80;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;

using VisualStudio.SpellChecker.Configuration;
using VisualStudio.SpellChecker.Editors;
using VisualStudio.SpellChecker.ProjectSpellCheck;
using VisualStudio.SpellChecker.Properties;

namespace VisualStudio.SpellChecker
{
    /// <summary>
    /// This class contains utility and extension methods
    /// </summary>
    public static class Utility
    {
        #region Constants and private data members
        //=====================================================================

        /// <summary>
        /// This is an extra <c>PredefinedTextViewRoles</c> value that only exists in VS 2013.  It defines the
        /// Peek Definition window view role which doesn't exist in earlier versions of Visual Studio.  As such,
        /// we define it here.
        /// </summary>
        public const string EmbeddedPeekTextView = "EMBEDDED_PEEK_TEXT_VIEW";

        private static Regex reUppercase = new Regex("([A-Z])"); 
        private static Regex reAutoGenCodeFilename = new Regex("(#ExternalSource\\(|#line\\s\\d*\\s)\"(?<Filename>.*?)\"");

        #endregion

        #region General utility methods
        //=====================================================================

        /// <summary>
        /// Get a service from the Visual Studio Spell Checker package
        /// </summary>
        /// <param name="throwOnError">True to throw an exception if the service cannot be obtained,
        /// false to return null.</param>
        /// <typeparam name="TInterface">The interface to obtain</typeparam>
        /// <typeparam name="TService">The service used to get the interface</typeparam>
        /// <returns>The service or null if it could not be obtained</returns>
        public static TInterface GetServiceFromPackage<TInterface, TService>(bool throwOnError)
            where TInterface : class
            where TService : class
        {
            TInterface service = Package.GetGlobalService(typeof(TService)) as TInterface;

            if(service == null && throwOnError)
                throw new InvalidOperationException("Unable to obtain service of type " + typeof(TService).Name);

            return service;
        }

        /// <summary>
        /// Get the filename from the given text buffer
        /// </summary>
        /// <param name="buffer">The text buffer from which to get the filename</param>
        /// <returns>The filename or null if it could not be obtained</returns>
        public static string GetFilename(this ITextBuffer buffer)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if(buffer != null)
            {
                // Most files have an ITextDocument property
                if(buffer.Properties.TryGetProperty(typeof(ITextDocument), out ITextDocument textDoc))
                {
                    if(textDoc != null && !String.IsNullOrEmpty(textDoc.FilePath))
                        return textDoc.FilePath;
                }

                if(buffer.Properties.TryGetProperty(typeof(IVsTextBuffer), out IVsTextBuffer vsTextBuffer))
                {
                    // Some, like HTML files, don't so we go through the IVsTextBuffer to get it
                    if(vsTextBuffer != null)
                    {
                        if(vsTextBuffer is IPersistFileFormat persistFileFormat)
                        {
                            try
                            {
                                persistFileFormat.GetCurFile(out string ppzsFilename, out uint pnFormatIndex);

                                if(!String.IsNullOrEmpty(ppzsFilename))
                                    return ppzsFilename;
                            }
                            catch(NotImplementedException)
                            {
                                // Secondary buffers throw an exception rather than returning E_NOTIMPL so we'll
                                // ignore these.  They are typically used for inline CSS, script, etc. and can be
                                // safely ignored as they're part of a primary buffer that does have a filename.
                                System.Diagnostics.Debug.WriteLine("Unable to obtain filename, probably a secondary buffer");

                                return null;
                            }
                        }
                    }
                }

                // See if the text in the buffer contains a filename reference from a code generator by looking
                // for some common patterns (stuff like Razor HTML).
                string content = buffer.CurrentSnapshot.GetText(0, Math.Min(buffer.CurrentSnapshot.Length, 4096));

                var m = reAutoGenCodeFilename.Match(content);

                if(m.Success)
                    return m.Groups["Filename"].Value;
            }

            return null;
        }

        /// <summary>
        /// Get the filename extension from the given text buffer
        /// </summary>
        /// <param name="buffer">The text buffer from which to get the filename extension</param>
        /// <returns>The filename extension or null if it could not be obtained</returns>
        public static string GetFilenameExtension(this ITextBuffer buffer)
        {
#pragma warning disable VSTHRD010
            string path = GetFilename(buffer);
#pragma warning restore VSTHRD010

            return String.IsNullOrEmpty(path) ? null : Path.GetExtension(path);
        }

        /// <summary>
        /// This returns the given absolute file path relative to the given base path
        /// </summary>
        /// <param name="absolutePath">The file path to convert to a relative path</param>
        /// <param name="basePath">The base path to which the absolute path is made relative</param>
        /// <returns>The file path relative to the given base path</returns>
        public static string ToRelativePath(this string absolutePath, string basePath)
        {
            bool hasBackslash = false;
            string relPath;
            int minLength, idx;

            // If not specified, use the current folder as the base path
            if(basePath == null || basePath.Trim().Length == 0)
                basePath = Directory.GetCurrentDirectory();
            else
                basePath = Path.GetFullPath(basePath);

            if(absolutePath == null)
                absolutePath = String.Empty;

            // Expand environment variables if necessary
            if(absolutePath.IndexOf('%') != -1)
            {
                absolutePath = Environment.ExpandEnvironmentVariables(absolutePath);

                if(absolutePath.IndexOf('%') != -1)
                    return absolutePath;
            }

            // Just in case, make sure the path is absolute
            if(!Path.IsPathRooted(absolutePath))
                if(!absolutePath.Contains("*") && !absolutePath.Contains("?"))
                    absolutePath = Path.GetFullPath(absolutePath);
                else
                    absolutePath = Path.Combine(Path.GetFullPath(Path.GetDirectoryName(absolutePath)),
                        Path.GetFileName(absolutePath));

            if(absolutePath.Length > 1 && absolutePath[absolutePath.Length - 1] == '\\')
            {
                absolutePath = absolutePath.Substring(0, absolutePath.Length - 1);
                hasBackslash = true;
            }

            // Split the paths into their component parts
            char[] separators = { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar,
                Path.VolumeSeparatorChar };
            string[] baseParts = basePath.Split(separators);
            string[] absParts = absolutePath.Split(separators);

            // Find the common base path
            minLength = Math.Min(baseParts.Length, absParts.Length);

            for(idx = 0; idx < minLength; idx++)
                if(String.Compare(baseParts[idx], absParts[idx], StringComparison.OrdinalIgnoreCase) != 0)
                    break;

            // Use the absolute path if there's nothing in common (i.e. they are on different drives or network
            // shares.
            if(idx == 0)
                relPath = absolutePath;
            else
            {
                // If equal to the base path, it doesn't have to go anywhere.  Otherwise, work up from the base
                // path to the common root.
                if(idx == baseParts.Length)
                    relPath = String.Empty;
                else
                    relPath = new String(' ', baseParts.Length - idx).Replace(" ", ".." +
                        Path.DirectorySeparatorChar);

                // And finally, add the path from the common root to the absolute path
                relPath += String.Join(Path.DirectorySeparatorChar.ToString(), absParts, idx,
                    absParts.Length - idx);
            }

            return (hasBackslash) ? relPath + "\\" : relPath;
        }

        /// <summary>
        /// Convert a camel cased term to one or more space-separated words
        /// </summary>
        /// <param name="term">The term to convert</param>
        /// <returns>The term with spaces inserted before each word</returns>
        public static string ToWords(this string term)
        {
            if(String.IsNullOrWhiteSpace(term))
                return term;

            return reUppercase.Replace(term, " $1").Trim();
        }
        #endregion

        #region Property state conversion methods
        //=====================================================================

        /// <summary>
        /// This is used to convert a wildcard file pattern to an equivalent regular expression
        /// </summary>
        /// <param name="pattern">The wildcard file pattern</param>
        /// <returns>A regular expression that can be used to match the given file wildcard pattern</returns>
        public static Regex RegexFromFilePattern(this string pattern)
        {
            if(String.IsNullOrWhiteSpace(pattern))
                pattern = @"\--";

            // Make sure it starts with a backslash so that it doesn't match the end of an unrelated filename
            // (i.e. "Utilities.*" doesn't match "FileUtilities.*").
            if(pattern[0] != '\\')
                pattern = @"\" + pattern;

            return new Regex(Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".") + "$", RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Convert the named property value to the appropriate selection state
        /// </summary>
        /// <param name="configuration">The configuration file from which to obtain the property value</param>
        /// <param name="propertyName">The name of the property to get</param>
        /// <returns>The selection state based on the specified property's value</returns>
        public static PropertyState ToPropertyState(this SpellingConfigurationFile configuration,
          string propertyName)
        {
            return !configuration.HasProperty(propertyName) &&
                configuration.ConfigurationType != ConfigurationType.Global ? PropertyState.Inherited :
                configuration.ToBoolean(propertyName) ? PropertyState.Yes : PropertyState.No;
        }

        /// <summary>
        /// Convert the selection state value to a property value
        /// </summary>
        /// <param name="state">The selection state to convert</param>
        /// <returns>The appropriate property value to store</returns>
        public static bool? ToPropertyValue(this PropertyState state)
        {
            return (state == PropertyState.Inherited) ? (bool?)null : state == PropertyState.Yes ? true : false;
        }
        #endregion

        #region Project and source control interaction
        //=====================================================================

        /// <summary>
        /// This is used to find configuration file project items more efficiently
        /// </summary>
        /// <param name="solution">The solution to search</param>
        /// <param name="filename">The filename to find</param>
        /// <returns>The project item of the configuration file if found or null if not found</returns>
        public static ProjectItem FindProjectItemForFile(this Solution solution, string filename)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if(String.IsNullOrWhiteSpace(filename))
                return null;

            if(!Path.IsPathRooted(filename))
            {
                // We're making an assumption that the path is in or just below the solution folder.  This may
                // not be the case if the project has a folder too.  In such cases, we'll never find the file.
                System.Diagnostics.Debug.WriteLine("**** FindProjectItemForFile called with a relative path.  " +
                    "Assuming the file is in or below the solution folder.  This may not be correct.");
                filename = Path.Combine(Path.GetDirectoryName(solution.FullName), filename);
            }

            // If the file doesn't exist, we don't need to look any further.  This saves searching the solution
            // which can be slow for extremely large projects.
            if(!File.Exists(filename))
                return null;

            return solution.FindProjectItem(filename);
        }

        /// <summary>
        /// Enumerate all of the projects within the given solution
        /// </summary>
        /// <param name="solution">The solution from which to get the projects</param>
        /// <returns>An enumerable list of all projects in the solution including subprojects nested within
        /// solution item folders.</returns>
        public static IEnumerable<Project> EnumerateProjects(this Solution solution)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            return solution.Projects.Cast<Project>().SelectMany(EnumerateProjects);
        }

        /// <summary>
        /// This handles enumeration of subprojects when necessary and ignores unmodeled projects
        /// </summary>
        /// <param name="project">The project to return, ignore, or enumerate</param>
        /// <returns>An enumerable list of zero or more projects based on the kind of project passed in</returns>
        public static IEnumerable<Project> EnumerateProjects(this Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            switch(project.Kind)
            {
                case EnvDTE.Constants.vsProjectKindSolutionItems:
                    foreach(ProjectItem projectItem in project.ProjectItems)
                        if(projectItem.SubProject != null)
                            foreach(var result in EnumerateProjects(projectItem.SubProject))
                                yield return result;
                    break;

                case EnvDTE.Constants.vsProjectKindUnmodeled:
                    break;

                default:
                    if(!String.IsNullOrWhiteSpace(project.FullName))
                        yield return project;
                    break;
            }
        }

        /// <summary>
        /// This is used to determine if the given user dictionary words file can be written to
        /// </summary>
        /// <param name="dictionaryWordsFile">The user dictionary words file</param>
        /// <param name="dictionaryFile">The related dictionary file or null if there isn't one</param>
        /// <param name="serviceProvider">The service provider to use for interacting with the solution/project</param>
        /// <returns>True if it can, false if not</returns>
        public static bool CanWriteToUserWordsFile(this string dictionaryWordsFile, string dictionaryFile)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if(String.IsNullOrWhiteSpace(dictionaryWordsFile))
                throw new ArgumentException("Dictionary words file cannot be null or empty", "dictionaryWordsFile");

            if(dictionaryFile != null && dictionaryFile.Trim().Length == 0)
                throw new ArgumentException("Dictionary file cannot be empty", "dictionaryFile");

            // The file must exist
            if(!File.Exists(dictionaryWordsFile))
                File.WriteAllText(dictionaryWordsFile, String.Empty);

            // If it's in the global configuration folder, we can write to it if not read-only
            if(Path.GetDirectoryName(dictionaryWordsFile).StartsWith(
              SpellingConfigurationFile.GlobalConfigurationFilePath, StringComparison.OrdinalIgnoreCase))
            {
                return ((File.GetAttributes(dictionaryWordsFile) & FileAttributes.ReadOnly) == 0);
            }

            // If not part of an active solution, we can write to it if not read-only
            if(!(Package.GetGlobalService(typeof(SDTE)) is DTE2 dte) || dte.Solution == null ||
              String.IsNullOrWhiteSpace(dte.Solution.FullName))
            {
                return ((File.GetAttributes(dictionaryWordsFile) & FileAttributes.ReadOnly) == 0);
            }

            // See if the user file or its related dictionary is part of the solution.  If neither are, we can
            // write to it if not read-only.
            var userItem = dte.Solution.FindProjectItemForFile(dictionaryWordsFile);
            var dictItem = (dictionaryFile == null) ? null : dte.Solution.FindProjectItemForFile(dictionaryFile);

            if(dictItem == null && userItem == null)
                return ((File.GetAttributes(dictionaryWordsFile) & FileAttributes.ReadOnly) == 0);

            // If the dictionary is part of a project but the user file isn't, add it to the dictionary file's
            // containing project.
            if(dictItem != null && dictItem.ContainingProject != null && userItem == null)
            {
                userItem = dictItem.ContainingProject.ProjectItems.AddFromFile(dictionaryWordsFile);

                if(userItem == null)
                    return false;
            }

            // If under source control, check it out
            if(dte.SourceControl.IsItemUnderSCC(dictionaryWordsFile) &&
              !dte.SourceControl.IsItemCheckedOut(dictionaryWordsFile))
                return dte.SourceControl.CheckOutItem(dictionaryWordsFile);

            return ((File.GetAttributes(dictionaryWordsFile) & FileAttributes.ReadOnly) == 0);
        }
        #endregion

        #region To CSV field extension method
        //=====================================================================

        /// <summary>
        /// This converts a value to a string suitable for writing to a text file as a comma-separated value (CSV)
        /// </summary>
        /// <param name="value">The value to write</param>
        /// <param name="writeSeparator">True to add a comma separator to the value, false if not</param>
        /// <returns>The value in a form suitable for writing to a CSV file</returns>
        public static string ToCsvField(this object value, bool writeSeparator)
        {
            string fieldValue;

            if(value == null)
                fieldValue = String.Empty;
            else
                fieldValue = value.ToString();

            if(fieldValue.IndexOfAny(new[] { ',', '\"'}) != -1)
                fieldValue = "\"" + fieldValue.Replace("\"", "\"\"") + "\"";

            if(writeSeparator)
                fieldValue += ",";

            return fieldValue;
        }
        #endregion

        #region User dictionary import/export methods
        //=====================================================================

        /// <summary>
        /// This loads all of the words from the given file based on the determined type (an XML or text user
        /// dictionary file or a StyleCop settings file).
        /// </summary>
        /// <param name="filename">The file to load.</param>
        /// <param name="onlyAddedWords">True to only load words from an XML user dictionary file where the
        /// <c>Spelling</c> attribute is set to "Add" or false to only load words where the <c>Spelling</c>
        /// attribute is set to "Ignore".  Words without a <c>Spelling</c> attribute will be added to the list
        /// regardless of this setting.</param>
        /// <param name="allWords">True to return all words including duplicates and those containing digits or
        /// false to return only unique words without digits.</param>
        /// <returns>An enumerable list of words from the file or an empty enumeration if the file type is XML
        /// but is not a recognized format.</returns>
        public static IEnumerable<string> LoadUserDictionary(string filename, bool onlyAddedWords, bool allWords)
        {
            IEnumerable<string> words = Enumerable.Empty<string>();
            string action = onlyAddedWords ? "Add" : "Ignore";

            try
            {
                var doc = XDocument.Load(filename);

                if(doc.Root.Name == "Dictionary")
                {
                    var recognizedWords = doc.Descendants("Recognized").FirstOrDefault();

                    if(recognizedWords != null)
                        words = recognizedWords.Elements("Word").Where(
                            w => ((string)w.Attribute("Spelling") == action ||
                                (string)w.Attribute("Spelling") == null) &&
                                !String.IsNullOrWhiteSpace(w.Value)).Select(w => w.Value.Trim());
                }
                else
                    if(doc.Root.Name == "StyleCopSettings")
                    {
                        var recognizedWords = doc.Descendants("CollectionProperty").Where(
                            c => (string)c.Attribute("Name") == "RecognizedWords").FirstOrDefault();

                        if(recognizedWords != null)
                            words = recognizedWords.Elements("Value").Where(
                                w => !String.IsNullOrWhiteSpace(w.Value)).Select(w => w.Value.Trim());
                    }
            }
            catch
            {
                // If it doesn't look like an XML file, assume it's text of some sort.  Convert anything that
                // isn't a letter or a digit to a space and get each word.
                var wordList = (new String(File.ReadAllText(filename).ToCharArray().Select(
                    c => (c == '\\' || Char.IsLetterOrDigit(c)) ? c : ' ').ToArray())).Split(new[] { ' ' },
                    StringSplitOptions.RemoveEmptyEntries).ToList();

                // Handle escaped words and split words containing the escape anywhere other than the start
                foreach(string w in wordList.Where(wd => wd.IndexOf('\\') != -1).ToArray())
                {
                    wordList.Remove(w);

                    if(w.Length > 2)
                    {
                        if(w.IndexOf('\\', 1) > 0)
                            wordList.AddRange(w.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries));
                        else
                            if(!onlyAddedWords)
                                wordList.Add(w);
                    }
                }

                words = wordList;
            }

            if(allWords)
                return words;

            return words.Distinct().Where(w => w.Length > 1 && w.IndexOfAny(
                new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' }) == -1).ToList();
        }

        /// <summary>
        /// This is used to save an enumerable list of words to a user dictionary file
        /// </summary>
        /// <param name="filename">The user dictionary filename.  This can be a text or XML user dictionary file.</param>
        /// <param name="replaceWords">True to replace the words in the file with the new list or false to
        /// merge the words into the existing list.  This only applies to XML user dictionaries.  For text user
        /// dictionaries, the word list contains all words including those from the current file if wanted so it
        /// is always overwritten.</param>
        /// <param name="addedWords">For XML user dictionaries, this indicates the <c>Spelling</c> attribute
        /// setting (true for added words, false for ignored words).</param>
        /// <param name="words">The list of words to save to the user dictionary file.</param>
        public static void SaveCustomDictionary(string filename, bool replaceWords, bool addedWords,
          IEnumerable<string> words)
        {
            XDocument dictionary = null;
            string action = addedWords ? "Add" : "Ignore";

            try
            {
                // For existing files, see if it's XML.  Don't assume the type by the extension.  If creating a
                // new file, we will only use the XML format if it's got a ".xml" extension though.
                if(File.Exists(filename))
                    dictionary = XDocument.Load(filename);
                else
                    if(Path.GetExtension(filename).Equals(".xml", StringComparison.OrdinalIgnoreCase))
                        dictionary = XDocument.Parse("<Dictionary />");
            }
            catch(Exception ex)
            {
                // Ignore any exceptions and treat the file as plain text
                System.Diagnostics.Debug.WriteLine(ex);
            }

            if(dictionary == null)
            {
                File.WriteAllLines(filename, words);
                return;
            }

            // This only supports the code analysis custom dictionary format
            if(dictionary.Root.Name != "Dictionary")
                throw new InvalidOperationException("Only code analysis format XML custom dictionaries are supported");

            var wordsElement = dictionary.Root.Element("Words");

            if(wordsElement == null)
            {
                wordsElement = new XElement("Words");
                dictionary.Root.Add(wordsElement);
            }

            var recognizedElement = wordsElement.Element("Recognized");

            if(recognizedElement == null)
            {
                recognizedElement = new XElement("Recognized");
                wordsElement.Add(recognizedElement);
            }

            var existingWords = recognizedElement.Elements("Word").Where(
                w => (string)w.Attribute("Spelling") == action || words.Contains(w.Value)).ToList();

            if(replaceWords && existingWords.Count != 0)
            {
                foreach(var word in existingWords)
                    word.Remove();

                existingWords.Clear();
            }

            // Escaped words are ignored as they aren't supported in XML user dictionary files
            foreach(string w in words)
                if(w.Length > 0 && w[0] != '\\')
                {
                    var match = existingWords.FirstOrDefault(m => m.Value.Equals(w, StringComparison.OrdinalIgnoreCase));

                    if(match != null)
                    {
                        if(match.Attribute("Spelling") != null)
                            match.Attribute("Spelling").Value = action;
                        else
                            match.Add(new XAttribute("Spelling", action));
                    }
                    else
                    {
                        var newWord = new XElement("Word", new XAttribute("Spelling", action), w);
                        existingWords.Add(newWord);
                        recognizedElement.Add(newWord);
                    }
                }

            dictionary.Save(filename);
        }
        #endregion

        #region Range classification helpers
        //=====================================================================

        /// <summary>
        /// This is used to see if a range classification is one of the string literal types and is followed by
        /// another of the same type.
        /// </summary>
        /// <param name="classification">The classification to check</param>
        /// <returns>True if the classification is an interpolated, normal, or verbatim string literal followed
        /// by another of the same type, false if not.</returns>
        internal static bool ConsecutiveStringLiterals(this RangeClassification classification, RangeClassification nextClassification)
        {
            return ((classification == RangeClassification.InterpolatedStringLiteral ||
                classification == RangeClassification.NormalStringLiteral ||
                classification == RangeClassification.VerbatimStringLiteral) && classification == nextClassification);
        }

        /// <summary>
        /// This is used to see if a range classification is one of the string literal types
        /// </summary>
        /// <param name="classification">The classification to check</param>
        /// <returns>True if the classification is an interpolated, normal, or verbatim string literal, false
        /// if not.</returns>
        internal static bool IsStringLiteral(this RangeClassification classification)
        {
            return (classification == RangeClassification.InterpolatedStringLiteral ||
                classification == RangeClassification.NormalStringLiteral ||
                classification == RangeClassification.VerbatimStringLiteral);
        }
        #endregion
    }
}
