//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : CommonUtilities.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 04/15/2023
// Note    : Copyright 2013-2023, Eric Woodruff, All rights reserved
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

// Ignore Spelling: za tp

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace VisualStudio.SpellChecker.Common
{
    /// <summary>
    /// This class contains common utility and extension methods
    /// </summary>
    public static class CommonUtilities
    {
        #region Constants and private data members
        //=====================================================================

        private static readonly Regex reUppercase = new Regex("([A-Z])");
        private static readonly Regex reRegexSeparator = new Regex(@"\(\?\#/Options/(.*?)\)");

        /// <summary>
        /// This defines the minimum identifier length
        /// </summary>
        /// <remarks>An identifier or identifier part less than this will not be spell checked</remarks>
        public const int MinimumIdentifierLength = 3;

        /// <summary>
        /// This defines the regular expression used to find Ignore Spelling directives in code comments
        /// </summary>
        public static readonly Regex IgnoreSpellingDirectiveRegex = new Regex(
            @"Ignore spelling:\s*?(?<IgnoredWords>[^\r\n/]+)(?<CaseSensitive>/matchCase)?", RegexOptions.IgnoreCase);

        /// <summary>
        /// This defines the regular expression used to find XML elements in text
        /// </summary>
        public static readonly Regex XmlElement = new Regex(@"<[A-Za-z/]+?.*?>");

        /// <summary>
        /// This defines the regular expression used to find URLs in text
        /// </summary>
        public static readonly Regex Url = new Regex(@"(ht|f)tp(s?)\:\/\/[0-9a-z]([-.\w]*[0-9a-z])*(:(0-9)*)*(\/?)" +
            @"([a-z0-9\-\.\?\,\'\/\\\+&amp;%\$#_]*)?", RegexOptions.IgnoreCase);

        #endregion

        #region General utility methods
        //=====================================================================

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
            {
                if(!absolutePath.Contains("*") && !absolutePath.Contains("?"))
                    absolutePath = Path.GetFullPath(absolutePath);
                else
                {
                    absolutePath = Path.Combine(Path.GetFullPath(Path.GetDirectoryName(absolutePath)),
                        Path.GetFileName(absolutePath));
                }
            }

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
            {
                if(String.Compare(baseParts[idx], absParts[idx], StringComparison.OrdinalIgnoreCase) != 0)
                    break;
            }

            // Use the absolute path if there's nothing in common (i.e. they are on different drives or network
            // shares or it would go all the way up to the root anyway).
            if(idx == 0 || (idx == 2 && absolutePath.Length > 2 && Char.IsLetter(absolutePath[0]) &&
              absolutePath[1] == Path.VolumeSeparatorChar))
            {
                relPath = absolutePath;
            }
            else
            {
                // If equal to the base path, it doesn't have to go anywhere.  Otherwise, work up from the base
                // path to the common root.
                if(idx == baseParts.Length)
                    relPath = String.Empty;
                else
                {
                    relPath = new String(' ', baseParts.Length - idx).Replace(" ", ".." +
                        Path.DirectorySeparatorChar);
                }

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

        /// <summary>
        /// Convert a configuration value to an enumerable list of regular expressions or return an empty
        /// enumeration if the value is null or whitespace.
        /// </summary>
        /// <param name="value">The value to convert to regular expressions.  This consists of a string
        /// containing one or more regular expressions separated by a regular expression comment in the format
        /// "(?#/Options/[options values])".  "[option values]" will be the applicable <see cref="RegexOptions"/>
        /// option values for the preceding expression.</param>
        /// <returns>An enumerable list of the regular expressions</returns>
        /// <remarks>All expressions are created with a 100 millisecond time out.</remarks>
        public static IEnumerable<Regex> ToRegexes(this string value)
        {
            if(!String.IsNullOrWhiteSpace(value))
            {
                var matches = reRegexSeparator.Split(value);

                if(matches.Length != 0)
                {
                    for(int idx = 0; idx < matches.Length; idx += 2)
                    {
                        Regex regex = null;

                        try
                        {
                            if(!String.IsNullOrWhiteSpace(matches[idx]))
                            {
                                if(idx + 1 >= matches.Length || String.IsNullOrWhiteSpace(matches[idx + 1]) ||
                                  !Enum.TryParse(matches[idx + 1], out RegexOptions regexOpts))
                                {
                                    regexOpts = RegexOptions.None;
                                }

                                regex = new Regex(matches[idx], regexOpts, TimeSpan.FromMilliseconds(100));
                            }
                        }
                        catch(Exception ex)
                        {
                            // Ignore invalid expressions
                            System.Diagnostics.Debug.WriteLine(ex);
                        }

                        if(regex != null)
                            yield return regex;
                    }
                }
            }
        }

        /// <summary>
        /// Escape .editorconfig property values if needed to work around a bug in Visual Studio
        /// </summary>
        /// <param name="value">The value to escape</param>
        /// <returns>The escaped value</returns>
        /// <remarks>There is currently a bug in Visual Studio's handling of .editorconfig files.  It treats '#'
        /// and ';' within a property value as a comment start but shouldn't per the latest spec.  To work around
        /// this, we escape them ourself by replacing them with a marker that is unlikely to appear normally.
        /// Escaping the characters with a backslash isn't supported.</remarks>
        public static string EscapeEditorConfigValue(this string value)
        {
            if(value == null || (value.IndexOf('#') == -1 && value.IndexOf(';') == -1))
                return value;

            return value.Replace("#", "@@PND@@").Replace(";", "@@SEMI@@");
        }

        /// <summary>
        /// Unescape .editorconfig property values if needed to work around a bug in Visual Studio
        /// </summary>
        /// <param name="value">The value to unescape</param>
        /// <returns>The unescaped value</returns>
        /// <remarks>There is currently a bug in Visual Studio's handling of .editorconfig files.  It treats '#'
        /// and ';' within a property value as a comment start but shouldn't per the latest spec.  To work around
        /// this, we escape them ourself by replacing them with a marker that is unlikely to appear normally.
        /// Escaping the characters with a backslash isn't supported.</remarks>
        public static string UnescapeEditorConfigValue(this string value)
        {
            if(value == null || (value.IndexOf("@@PND@@", StringComparison.Ordinal) == -1 &&
              value.IndexOf("@@SEMI@@", StringComparison.Ordinal) == -1))
            {
                return value;
            }

            return value.Replace("@@PND@@", "#").Replace("@@SEMI@@", ";");
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
            char[] escapedLetters = new[] { 'a', 'b', 'f', 'n', 'r', 't', 'v', 'x', 'u', 'U' };

            if(!File.Exists(filename))
                return Enumerable.Empty<string>();

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
                {
                    if(doc.Root.Name == "StyleCopSettings")
                    {
                        var recognizedWords = doc.Descendants("CollectionProperty").FirstOrDefault(
                            c => (string)c.Attribute("Name") == "RecognizedWords");

                        if(recognizedWords != null)
                        {
                            words = recognizedWords.Elements("Value").Where(
                                w => !String.IsNullOrWhiteSpace(w.Value)).Select(w => w.Value.Trim());
                        }
                    }
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
                        {
                            if(!onlyAddedWords)
                            {
                                if(escapedLetters.Contains(w[1]))
                                    wordList.Add(w);
                                else
                                    wordList.Add(w.Substring(1));
                            }
                        }
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

            if(words == null)
                throw new ArgumentNullException(nameof(words));

            try
            {
                // For existing files, see if it's XML.  Don't assume the type by the extension.  If creating a
                // new file, we will only use the XML format if it's got a ".xml" extension though.
                if(File.Exists(filename))
                    dictionary = XDocument.Load(filename);
                else
                {
                    if(Path.GetExtension(filename).Equals(".xml", StringComparison.OrdinalIgnoreCase))
                        dictionary = XDocument.Parse("<Dictionary />");
                }
            }
            catch(Exception ex)
            {
                // Ignore any exceptions and treat the file as plain text
                System.Diagnostics.Debug.WriteLine(ex);
            }

            if(dictionary == null)
            {
                // Sort and write all the words to the file.  If under source control, this should minimize the
                // number of merge conflicts that could result if multiple people added words and they were all
                // written to the end of the file.
                File.WriteAllLines(filename, words.OrderBy(w => w));
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
            {
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
            }

            dictionary.Save(filename);
        }
        #endregion
    }
}
