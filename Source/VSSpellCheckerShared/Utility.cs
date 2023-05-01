//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : Utility.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 04/17/2023
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

using EnvDTE;
using EnvDTE80;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;

using VisualStudio.SpellChecker.Common.Configuration;
using VisualStudio.SpellChecker.Editors;
using VisualStudio.SpellChecker.ProjectSpellCheck;

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

        private static readonly Regex reAutoGenCodeFilename = new Regex(
            "(#ExternalSource\\(|#line\\s\\d*\\s)\"(?<Filename>.*?)\"");

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

                // If it's TypeScript, we can get it through the script block property
                string filename = FilenameFromScriptBlock(buffer);

                if(filename != null)
                    return filename;

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
        /// This is used to see if the buffer contains a script block property present in HTML files
        /// </summary>
        /// <param name="buffer">The text buffer from which to get the filename</param>
        /// <returns>The filename if it could be obtained from the script context property, null if not</returns>
        /// <remarks>There doesn't appear to be any reference assemblies for the script language services so
        /// reflection is used to obtain the property and its value.</remarks>
        private static string FilenameFromScriptBlock(ITextBuffer buffer)
        {
            if(buffer.ContentType.TypeName == "TypeScript")
            {
                foreach(var p in buffer.Properties.PropertyList)
                {
                    if(p.Key is Type t && t.FullName == "Microsoft.VisualStudio.LanguageServices.TypeScript.ScriptContexts.ScriptBlock")
                    {
                        var filename = t.GetProperty("FileName");

                        if(filename != null)
                            return filename.GetValue(p.Value) as string;
                    }
                }
            }

            if(buffer.ContentType.TypeName == "JavaScript")
            {
                foreach(var p in buffer.Properties.PropertyList)
                {
                    if(p.Value is System.Collections.IDictionary d)
                    {
                        foreach(var v in d.Values)
                        {
                            if(v != null)
                            {
                                Type t = v.GetType();

                                if(t.FullName == "Microsoft.VisualStudio.JSLS.Engine.ScriptContext")
                                {
                                    var primarySource = t.GetProperty("PrimarySource");

                                    if(primarySource != null)
                                    {
                                        var ps = primarySource.GetValue(v);

                                        if(ps != null)
                                        {
                                            t = ps.GetType();

                                            var authorFile = t.GetProperty("AuthorFile", BindingFlags.NonPublic |
                                                BindingFlags.Instance);

                                            if(authorFile != null)
                                            {
                                                var af = authorFile.GetValue(ps);

                                                if(af != null)
                                                {
                                                    t = af.GetType();

                                                    var displayName = t.GetProperty("DisplayName");

                                                    if(displayName != null)
                                                        return displayName.GetValue(af) as string;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
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
        #endregion

        #region Property state conversion methods
        //=====================================================================

        /// <summary>
        /// Convert the named property value to the appropriate selection state based on whether or not it is
        /// present in the given dictionary and it's current value.
        /// </summary>
        /// <param name="properties">The dictionary from which to obtain the property value.</param>
        /// <param name="propertyName">The name of the property to get.</param>
        /// <param name="isGlobal">True if the properties came from the global spell checker configuration, false
        /// if not.</param>
        /// <returns>The selection state based on the specified property's value.</returns>
        public static PropertyState ToPropertyState(this IDictionary<string, SpellCheckPropertyInfo> properties,
          string propertyName, bool isGlobal)
        {
            if(properties != null && properties.TryGetValue(propertyName, out var p) &&
              Boolean.TryParse(p.EditorConfigPropertyValue, out bool value))
            {
                return value ? PropertyState.Yes : PropertyState.No;
            }

            if(!isGlobal)
                return PropertyState.Inherited;

            return SpellCheckerConfiguration.DefaultValueFor(propertyName) is
                Boolean defaultValue && defaultValue ? PropertyState.Yes : PropertyState.No;
        }

        /// <summary>
        /// Convert the selection state value to an .editorconfig property value
        /// </summary>
        /// <param name="state">The selection state to convert</param>
        /// <param name="propertyName">The property name from the configuration</param>
        /// <param name="isGlobal">True if this is for the global configuration, false if not</param>
        /// <returns>Null if the property is inherited (non-global) or set to the default (global) or the
        /// .editorconfig property name and value to store if not.</returns>
        public static (string PropertyName, string PropertyValue) ToPropertyValue(this PropertyState state,
          string propertyName, bool isGlobal)
        {
            if(state != PropertyState.Inherited)
            {
                bool newState = (state == PropertyState.Yes);

                if(!isGlobal || SpellCheckerConfiguration.DefaultValueFor(propertyName) is Boolean defaultValue &&
                  (defaultValue != newState))
                {
                    return (SpellCheckerConfiguration.EditorConfigSettingsFor(propertyName).PropertyName,
                        (newState ? Boolean.TrueString : Boolean.FalseString).ToLowerInvariant());
                }
            }

            return (null, null);
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
        /// <param name="checkOutForEdit">True if the file should be checked out for editing, false to check
        /// for the file's existence and, if it doesn't exist, create it and add it to the project.</param>
        /// <returns>True if it can, false if not</returns>
        public static bool CanWriteToUserWordsFile(this string dictionaryWordsFile, string dictionaryFile,
          bool checkOutForEdit)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

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

            // If not part of an active solution, we can write to it if not read-only
            if(!(Package.GetGlobalService(typeof(SDTE)) is DTE2 dte) || dte.Solution == null ||
              String.IsNullOrWhiteSpace(dte.Solution.FullName) ||
              !dictionaryWordsFile.StartsWith(Path.GetDirectoryName(dte.Solution.FullName), StringComparison.OrdinalIgnoreCase))
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

            // If under source control, check it out if requested
            if(checkOutForEdit && dte.SourceControl.IsItemUnderSCC(dictionaryWordsFile) &&
              !dte.SourceControl.IsItemCheckedOut(dictionaryWordsFile))
            {
                return dte.SourceControl.CheckOutItem(dictionaryWordsFile);
            }

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
        #endregion
    }
}
