//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : SpellingServiceProxy.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 09/04/2022
// Note    : Copyright 2015-2022, Eric Woodruff, All rights reserved
//
// This file contains a class that implements the spelling service interface to expose the spell checker to
// third-party tagger providers.
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 02/19/2015  EFW  Created the code
//===============================================================================================================

using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;

using EnvDTE;
using EnvDTE80;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;

using VisualStudio.SpellChecker.Definitions;
using VisualStudio.SpellChecker.Configuration;
using VisualStudio.SpellChecker.ProjectSpellCheck;

namespace VisualStudio.SpellChecker
{
    /// <summary>
    /// This class implements the spelling service interface to expose the spell checker to third-party tagger
    /// providers
    /// </summary>
    [Export(typeof(ISpellingService))]
    internal sealed class SpellingServiceProxy : ISpellingService
    {
        #region Private data members
        //=====================================================================

        // This serves as a flag indicating that a file is not to be spell checked.  It saves storing the
        // entire configuration as a property when spell checking is not wanted.
        private const string SpellCheckerDisabledKey = "@@VisualStudio.SpellChecker.Disabled";

        #endregion

        #region Properties
        //=====================================================================

        /// <summary>
        /// This is used to track the last solution filename to determine when the global dictionary cache should
        /// be cleared.
        /// </summary>
        internal static string LastSolutionName { get; set; }

        #endregion

        #region ISpellingService Members
        //=====================================================================

        /// <inheritdoc />
        public bool IsEnabled(ITextBuffer buffer)
        {
#pragma warning disable VSTHRD010
            // Getting the configuration determines if spell checking is enabled for this file
            return (buffer != null && GetConfiguration(buffer) != null);
#pragma warning restore VSTHRD010
        }
        #endregion

        #region Helper methods
        //=====================================================================

        /// <summary>
        /// Get the configuration settings for the specified buffer
        /// </summary>
        /// <param name="buffer">The buffer for which to get the configuration settings</param>
        /// <returns>The spell checker configuration settings for the buffer or null if one is not provided or
        /// is disabled for the given buffer.</returns>
        public static SpellCheckerConfiguration GetConfiguration(ITextBuffer buffer)
        {
            SpellCheckerConfiguration config = null;

            // If not given a buffer or already checked for and found to be disabled, don't go any further
            if(buffer != null && !buffer.Properties.TryGetProperty(SpellCheckerDisabledKey, out bool _) &&
              !buffer.Properties.TryGetProperty(typeof(SpellCheckerConfiguration), out config))
            {
#pragma warning disable VSTHRD010
                // Generate the configuration settings unique to the file
                config = GenerateConfiguration(buffer);

                if(config == null || !config.SpellCheckAsYouType || config.ShouldExcludeFile(buffer.GetFilename()))
                {
                    // Mark it as disabled so that we don't have to check again
                    buffer.Properties[SpellCheckerDisabledKey] = true;
                    config = null;
                }
                else
                    buffer.Properties[typeof(SpellCheckerConfiguration)] = config;
#pragma warning restore VSTHRD010
            }

            return config;
        }

        /// <summary>
        /// Get the dictionary for the specified buffer
        /// </summary>
        /// <param name="buffer">The buffer for which to get a dictionary</param>
        /// <returns>The spelling dictionary for the buffer or null if one is not provided</returns>
        public static SpellingDictionary GetDictionary(ITextBuffer buffer)
        {
            SpellingDictionary service = null;

            if(buffer != null && !buffer.Properties.TryGetProperty(typeof(SpellingDictionary), out service))
            {
#pragma warning disable VSTHRD010
                // Get the configuration and create the dictionary based on the configuration
                var config = GetConfiguration(buffer);
#pragma warning restore VSTHRD010

                if(config != null)
                {
                    // Create a dictionary for each configuration dictionary language ignoring any that are
                    // invalid and duplicates caused by missing languages which return the en-US dictionary.
                    var globalDictionaries = config.DictionaryLanguages.Select(l =>
                        GlobalDictionary.CreateGlobalDictionary(l, config.AdditionalDictionaryFolders,
                        config.RecognizedWords)).Where(d => d != null).Distinct().ToList();

                    if(globalDictionaries.Any())
                    {
                        service = new SpellingDictionary(globalDictionaries, config.IgnoredWords);
                        buffer.Properties[typeof(SpellingDictionary)] = service;
                    }
                }
            }

            return service;
        }

        /// <summary>
        /// Generate the configuration to use when spell checking the given text buffer
        /// </summary>
        /// <param name="buffer">The text buffer for which to generate a configuration</param>
        /// <returns>The generated configuration to use</returns>
        /// <remarks>The configuration is a merger of the global settings plus any solution, project, folder, and
        /// file settings related to the text buffer.</remarks>
        private static SpellCheckerConfiguration GenerateConfiguration(ITextBuffer buffer)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            ProjectItem projectItem, fileItem;
            string bufferFilename, filename, projectPath, projectFilename = null;

            // Start with the global configuration
            var config = new SpellCheckerConfiguration();

            try
            {
                config.Load(SpellingConfigurationFile.GlobalConfigurationFilename);

                if(Package.GetGlobalService(typeof(SDTE)) is DTE2 dte2 && dte2.Solution != null &&
                  !String.IsNullOrWhiteSpace(dte2.Solution.FullName))
                {
                    var solution = dte2.Solution;

                    // Clear the global dictionary cache when a change in solution is detected.  This handles
                    // cases where only the MEF components are loaded and not the package (i.e. a configuration
                    // has not been edited).  See VSSpellCheckerPackage.solutionEvents_AfterClosing().
                    if(LastSolutionName == null || !LastSolutionName.Equals(solution.FullName,
                      StringComparison.OrdinalIgnoreCase))
                    {
                        WpfTextBox.WpfTextBoxSpellChecker.ClearCache();
                        GlobalDictionary.ClearDictionaryCache();
                        LastSolutionName = solution.FullName;
                    }

                    // See if there is a solution configuration
                    filename = solution.FullName + ".vsspell";
                    projectItem = solution.FindProjectItemForFile(filename);

                    // Allow for solution configuration files to be named ".vsspell"
                    if(projectItem == null)
                    {
                        filename = Path.Combine(Path.GetDirectoryName(filename), ".vsspell");
                        projectItem = solution.FindProjectItemForFile(filename);
                    }

                    if(projectItem != null)
                        config.Load(filename);

                    // Find the project item for the file we are opening
                    bufferFilename = buffer.GetFilename();
                    projectItem = solution.FindProjectItemForFile(bufferFilename);

                    if(projectItem != null)
                    {
                        fileItem = projectItem;

                        // If we have a project (we should), see if it has settings
                        if(projectItem.ContainingProject != null &&
                          !String.IsNullOrWhiteSpace(projectItem.ContainingProject.FullName))
                        {
                            projectFilename = projectItem.ContainingProject.FullName;

                            // Website projects are named after the folder
                            if(projectFilename.Length > 1 && projectFilename[projectFilename.Length - 1] == '\\')
                            {
                                filename = Path.GetFileName(projectFilename.Substring(0, projectFilename.Length - 1));
                                filename = projectFilename + filename + ".vsspell";
                            }
                            else
                                filename = projectFilename + ".vsspell";

                            projectItem = solution.FindProjectItemForFile(filename);

                            if(projectItem != null)
                                config.Load(filename);

                            // Get the full path based on the project.  The buffer filename will refer to the actual
                            // path which may be to a linked file outside the project's folder structure.
                            projectPath = Path.GetDirectoryName(filename);
                            filename = Path.GetDirectoryName((string)fileItem.Properties.Item("FullPath").Value);

                            // Search for folder-specific configuration files
                            if(filename.StartsWith(projectPath, StringComparison.OrdinalIgnoreCase))
                            {
                                // Then check subfolders.  No need to check the root folder as the project
                                // settings cover it.
                                if(filename.Length > projectPath.Length)
                                    foreach(string folder in filename.Substring(projectPath.Length + 1).Split('\\'))
                                    {
                                        projectPath = Path.Combine(projectPath, folder);
                                        filename = Path.Combine(projectPath, folder + ".vsspell");
                                        projectItem = solution.FindProjectItemForFile(filename);

                                        if(projectItem != null)
                                            config.Load(filename);
                                    }
                            }

                            // If the item looks like a dependent file item, look for a settings file related to
                            // the parent file item.
                            if(fileItem.Collection != null && fileItem.Collection.Parent != null)
                            {
                                projectItem = fileItem.Collection.Parent as ProjectItem;

                                if(projectItem != null && projectItem.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFile)
                                {
                                    filename = (string)projectItem.Properties.Item("FullPath").Value + ".vsspell";
                                    projectItem = solution.FindProjectItemForFile(filename);

                                    if(projectItem != null)
                                        config.Load(filename);
                                }
                            }

                            // And finally, look for file-specific settings for the item itself
                            filename = (string)fileItem.Properties.Item("FullPath").Value + ".vsspell";
                            projectItem = solution.FindProjectItemForFile(filename);

                            if(projectItem != null)
                                config.Load(filename);
                        }
                        else
                            if(projectItem.Kind == EnvDTE.Constants.vsProjectItemKindSolutionItems)
                        {
                            // Looks like a solution item, see if a related setting file exists
                            filename = bufferFilename + ".vsspell";

                            projectItem = solution.FindProjectItemForFile(filename);

                            if(projectItem != null)
                                config.Load(filename);
                        }
                    }

                    // Load code analysis dictionaries if wanted
                    if(projectFilename != null && config.CadOptions.ImportCodeAnalysisDictionaries)
                    {
                        // Typically there is only one but multiple files are supported
                        foreach(var cad in SpellCheckFileInfo.ProjectCodeAnalysisDictionaries(projectFilename))
                        {
                            if(File.Exists(cad.CanonicalName))
                                config.ImportCodeAnalysisDictionary(cad.CanonicalName);
                        }
                    }

                    if(bufferFilename != null && config.DetermineResourceFileLanguageFromName &&
                      Path.GetExtension(bufferFilename).Equals(".resx", StringComparison.OrdinalIgnoreCase))
                    {
                        // Localized resource files are expected to have filenames in the format
                        // BaseName.Language.resx (i.e. LocalizedForm.de-DE.resx).
                        bufferFilename = Path.GetExtension(Path.GetFileNameWithoutExtension(bufferFilename));

                        if(bufferFilename.Length > 1)
                        {
                            bufferFilename = bufferFilename.Substring(1);

                            if(SpellCheckerDictionary.AvailableDictionaries(
                              config.AdditionalDictionaryFolders).TryGetValue(bufferFilename,
                              out SpellCheckerDictionary match))
                            {
                                // Clear any existing dictionary languages and use just the one that matches the
                                // file's language.
                                config.DictionaryLanguages.Clear();
                                config.DictionaryLanguages.Add(match.Culture);
                            }
                        }
                    }
                }
                else
                    if(LastSolutionName != null)
                {
                    // A solution was closed and a file has been opened outside of a solution so clear the
                    // cache and use the global dictionaries.
                    WpfTextBox.WpfTextBoxSpellChecker.ClearCache();
                    GlobalDictionary.ClearDictionaryCache();
                    LastSolutionName = null;
                }
            }
            catch(Exception ex)
            {
                // Ignore errors, we just won't load the configurations after the point of failure
                System.Diagnostics.Debug.WriteLine(ex);
            }

            return config;
        }
        #endregion
    }
}
