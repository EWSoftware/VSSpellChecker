//===============================================================================================================
// System  : Spell Check My Code Package
// File    : SpellCheckFileInfo.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 08/30/2025
// Note    : Copyright 2015-2025, Eric Woodruff, All rights reserved
//
// This file contains a class used to hold information about a file that will be spell checked
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 08/26/2015  EFW  Created the code
//===============================================================================================================

// Ignore spelling: proj

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using EnvDTE;
using EnvDTE80;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

using VisualStudio.SpellChecker.Common.Configuration;

namespace VisualStudio.SpellChecker.ProjectSpellCheck
{
    internal class SpellCheckFileInfo
    {
        #region Private data members
        //=====================================================================

        private static readonly SpellCheckFileInfo IgnoredHierarchyItem = new();
        private static readonly char[] validChars = ['\b', '\t', '\r', '\n', '\x07', '\x0B', '\x0C'];

        #endregion

        #region Properties
        //=====================================================================

        /// <summary>
        /// This read-only property returns the containing solution filename
        /// </summary>
        public string SolutionFile { get; private set; }

        /// <summary>
        /// This read-only property returns the containing project filename
        /// </summary>
        public string ProjectFile { get; private set; }

        /// <summary>
        /// This read-only property returns the filename (no path)
        /// </summary>
        public string Filename { get; private set; }

        /// <summary>
        /// This read-only property returns the file's canonical name (full path)
        /// </summary>
        public string CanonicalName { get; private set; }

        /// <summary>
        /// This read-only property returns true if this item represents a .globalconfig file item, false if not
        /// </summary>
        public bool IsGlobalConfigItem { get; private set; }

        /// <summary>
        /// This read-only property returns true if this item represents an .editorconfig file item, false if not
        /// </summary>
        public bool IsEditorConfigItem { get; private set; }

        /// <summary>
        /// This read-only property returns true if this item represents a code analysis dictionary item, false if not
        /// </summary>
        public bool IsCodeAnalysisDictionary { get; private set; }

        /// <summary>
        /// This returns a description of the item with the solution and relative path to the file
        /// </summary>
        public string Description
        {
            get
            {
                string projectPath = Path.GetDirectoryName(this.ProjectFile), filePath = Path.GetDirectoryName(this.CanonicalName);

                if(projectPath.Length == 0 || !filePath.StartsWith(projectPath, StringComparison.OrdinalIgnoreCase))
                {
                    projectPath = Path.GetDirectoryName(this.SolutionFile ?? ".");

                    if(projectPath.Length == 0 || !filePath.StartsWith(projectPath, StringComparison.OrdinalIgnoreCase))
                        return Path.GetFileName(this.ProjectFile) + " / " + this.Filename;

                    return Path.GetFileName(this.ProjectFile) + " / " + this.CanonicalName.Substring(projectPath.Length + 1);
                }

                return Path.GetFileName(this.ProjectFile) + " / " + this.CanonicalName.Substring(projectPath.Length + 1);
            }
        }

        /// <summary>
        /// This read-only property returns the configuration files used to load the spell checker settings
        /// </summary>
        public IEnumerable<string> ConfigurationFiles { get; private set; }

        /// <summary>
        /// This read-only property returns an enumerable list of ignored words files that were loaded by the
        /// configuration.
        /// </summary>
        public IEnumerable<string> IgnoredWordsFiles { get; private set; }

        #endregion

        #region Helper methods
        //=====================================================================

        /// <summary>
        /// Return spell check file info for an open document
        /// </summary>
        /// <param name="filename">The filename of the open document</param>
        /// <returns>An instance for an open document</returns>
        public static SpellCheckFileInfo ForOpenDocument(string filename)
        {
            return new SpellCheckFileInfo
            {
                ProjectFile = "Open Document",
                Filename = Path.GetFileName(filename),
                CanonicalName = filename
            };
        }

        /// <summary>
        /// This is used to get the additional global configuration, editor configuration, and code analysis
        /// dictionaries from the named project.
        /// </summary>
        /// <param name="projectName">The project from which to get the files</param>
        /// <returns>An enumerable list of the additional global configuration, editor configuration, and code
        /// analysis dictionary files if any.</returns>
        public static IEnumerable<SpellCheckFileInfo> ProjectAdditionalConfigurationFiles(string projectName)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            List<SpellCheckFileInfo> projectFiles = [];

            try
            {
                var solution = Utility.GetServiceFromPackage<IVsSolution, IVsSolution>(false);

                if(solution != null)
                {
                    // Use the IVsHierarchy interface as it is reportedly significantly faster than using the
                    // automation interfaces for very large projects.
                    var hierarchy = (IVsHierarchy)solution;

                    if((projectName == null || solution.GetProjectOfUniqueName(projectName,
                        out hierarchy) == VSConstants.S_OK) && hierarchy != null)
                    {
                        ProcessHierarchyNodeRecursively(hierarchy, VSConstants.VSITEMID_ROOT, projectFiles);
                    }
                }
            }
            catch(Exception ex)
            {
                // Ignore exceptions, just return what we could get
                System.Diagnostics.Debug.WriteLine(ex);
            }

            return projectFiles.Where(f => f.IsGlobalConfigItem || f.IsEditorConfigItem || f.IsCodeAnalysisDictionary);
        }

        /// <summary>
        /// This is used to get information for all files in the solution or a specific project
        /// </summary>
        /// <param name="projectName">The project filename from which to get the file information or null to
        /// return information for all files in all projects in the solution</param>
        /// <returns>An enumerable list of project file information</returns>
        public static IEnumerable<SpellCheckFileInfo> AllProjectFiles(string projectName)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            List<SpellCheckFileInfo> projectFiles = [];

            try
            {
                var solution = Utility.GetServiceFromPackage<IVsSolution, IVsSolution>(false);

                if(solution != null && solution.GetSolutionInfo(out _, out string solutionFile, out _) == VSConstants.S_OK)
                {
                    // Use the IVsHierarchy interface as it is reportedly significantly faster than using the
                    // automation interfaces for very large projects.
                    var hierarchy = (IVsHierarchy)solution;

                    if(projectName != null && solution.GetProjectOfUniqueName(projectName,
                      out hierarchy) != VSConstants.S_OK)
                    {
                        hierarchy = null;
                    }

                    if(hierarchy != null)
                    {
                        ProcessHierarchyNodeRecursively(hierarchy, VSConstants.VSITEMID_ROOT, projectFiles);

                        projectFiles = [.. projectFiles.OrderBy(
                            p => Path.GetFileName(p.ProjectFile)).ThenBy(p => p.Filename)];

                        // Set the solution file for each project file
                        foreach(var file in projectFiles)
                            file.SolutionFile = solutionFile;
                    }
                }
            }
            catch(Exception ex)
            {
                // Ignore exceptions, just return what we could get
                System.Diagnostics.Debug.WriteLine(ex);
            }

            return projectFiles;
        }

        /// <summary>
        /// This is used to get information for only the files related to the currently selected items in the
        /// Solution Explorer.
        /// </summary>
        /// <returns>An enumerable list of all selected project file information.  If the solution node is
        /// selected, all files are returned.  If a project is selected, all files in the project are returned.
        /// If a folder is selected, all files in the folder are returned.  If a file is selected that has
        /// dependency items, those are returned as well.</returns>
        public static IEnumerable<SpellCheckFileInfo> SelectedProjectFiles()
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

#pragma warning disable VSTHRD010
            List<SpellCheckFileInfo> projectFiles = [];
            var dte2 = Utility.GetServiceFromPackage<DTE2, SDTE>(false);

            if(dte2 == null)
                return projectFiles;

            List<string> projects = [], folders = [], files = [];
            bool entireSolution = false;

            // This is a bit complicated but we need to figure out which items are selected and then filter down
            // the entire set based on what was selected.
            foreach(SelectedItem item in dte2.SelectedItems)
            {
                // For vsProjectKindSolutionItems, enumerate projects first if there are any and then fall
                // through to handle solution items.
                if(item.Project != null && item.Project.Kind == EnvDTE.Constants.vsProjectKindSolutionItems)
                    projects.AddRange(item.Project.EnumerateProjects().Select(p => p.FullName));

                if(item.Project != null && item.Project.Kind != EnvDTE.Constants.vsProjectKindSolutionItems &&
                  item.Project.Kind != EnvDTE.Constants.vsProjectKindUnmodeled &&
                  item.Project.Kind != EnvDTE.Constants.vsProjectKindMisc)
                {
                    string path = null;

                    // Looks like a project.  Not all of them implement properties though.
                    if(!String.IsNullOrWhiteSpace(item.Project.FullName) && item.Project.FullName.EndsWith(
                      "proj", StringComparison.OrdinalIgnoreCase))
                    {
                        path = item.Project.FullName;
                    }

                    if(path == null && item.Project.Properties != null)
                    {
                        Property fullPath;

                        try
                        {
                            fullPath = item.Project.Properties.Item("FullPath");
                        }
                        catch
                        {
                            // C++ projects use a different property name and throw an exception above
                            try
                            {
                                fullPath = item.Project.Properties.Item("ProjectFile");
                            }
                            catch
                            {
                                // If that fails, give up
                                fullPath = null;
                            }
                        }

                        if(fullPath != null && fullPath.Value != null)
                            path = (string)fullPath.Value;
                    }

                    if(!String.IsNullOrWhiteSpace(path))
                    {
                        var project = dte2.Solution.EnumerateProjects().FirstOrDefault(p => p.Name == item.Name);

                        if(project != null)
                            projects.Add(project.FullName);
                    }
                }
                else
                    if(item.ProjectItem == null || item.ProjectItem.ContainingProject == null)
                    {
                        // Looks like a solution or a solution items folder
                        if(Path.GetFileNameWithoutExtension(dte2.Solution.FullName) == item.Name)
                        {
                            entireSolution = true;
                            break;
                        }

                        string folderName = Path.Combine(Path.GetDirectoryName(dte2.Solution.FullName), item.Name);

                        // If the folder exists, it's a folder.  If not, it's probably the Solution Items
                        // container node.  However, ignore the References container node.
                        if(Directory.Exists(folderName))
                            folders.Add(folderName + "\\");
                        else
                            if(item.Name != "References")
                                projects.Add("Solution Items");
                    }
                    else
                        if(item.ProjectItem.Properties != null)
                        {
                            // Looks like a folder or file item
                            Property fullPath = null;

                            if(item.ProjectItem.Kind != EnvDTE.Constants.vsProjectItemKindVirtualFolder)
                                fullPath = item.ProjectItem.Properties.Item("FullPath");

                            if(fullPath != null && fullPath.Value != null)
                            {
                                string path = (string)fullPath.Value;

                                if(!String.IsNullOrWhiteSpace(path))
                                {
                                    // Folder items have a trailing backslash in some project systems, others don't
                                    if(path[path.Length - 1] == '\\' || (!File.Exists(path) && Directory.Exists(path)))
                                    {
                                        if(path[path.Length - 1] != '\\')
                                            path += @"\";

                                        folders.Add(path);
                                    }
                                    else
                                    {
                                        files.Add(path);

                                        // If the file has dependency items, add them too
                                        if(item.ProjectItem.ProjectItems != null &&
                                          item.ProjectItem.ProjectItems.Count != 0)
                                            foreach(ProjectItem dep in item.ProjectItem.ProjectItems)
                                            {
                                                files.Add(dep.get_FileNames(1));
                                            }
                                    }
                                }
                            }
                        }
                        else
                            if(item.ProjectItem.Kind == EnvDTE.Constants.vsProjectItemKindSolutionItems)
                            {
                                // Looks like a solution item file
                                files.Add(item.ProjectItem.get_FileNames(1));
                            }
            }
#pragma warning restore VSTHRD010

            var allFiles = AllProjectFiles(null);

            if(entireSolution)
                return allFiles;

            HashSet<string> filenames = new(StringComparer.OrdinalIgnoreCase);

            foreach(string projectName in projects)
            {
                var pf = allFiles.Where(f => !filenames.Contains(f.CanonicalName) &&
                    f.ProjectFile.Equals(projectName, StringComparison.OrdinalIgnoreCase)).ToList();

                filenames.UnionWith(pf.Select(f => f.CanonicalName));

                projectFiles.AddRange(pf);
            }

            foreach(string folderName in folders)
            {
                var pf = allFiles.Where(f => !filenames.Contains(f.CanonicalName) &&
                    f.CanonicalName.StartsWith(folderName, StringComparison.OrdinalIgnoreCase)).ToList();

                filenames.UnionWith(pf.Select(f => f.CanonicalName));

                projectFiles.AddRange(pf);
            }

            foreach(string fileName in files)
            {
                var pf = allFiles.Where(f => !filenames.Contains(f.CanonicalName) &&
                    f.CanonicalName.Equals(fileName, StringComparison.OrdinalIgnoreCase)).ToList();

                filenames.UnionWith(pf.Select(f => f.CanonicalName));

                projectFiles.AddRange(pf);
            }

            return projectFiles;
        }

        /// <summary>
        /// Process all project hierarchy nodes recursively returning information about the files in them
        /// </summary>
        /// <param name="hierarchy">The starting hierarchy node</param>
        /// <param name="itemId">The item ID</param>
        /// <param name="projectFiles">The list to which project file information is added</param>
        private static void ProcessHierarchyNodeRecursively(IVsHierarchy hierarchy, uint itemId,
          IList<SpellCheckFileInfo> projectFiles)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            IVsHierarchy nestedHierarchy;

            // First, guess if the node is actually the root of another hierarchy (a project, for example)
            Guid nestedHierarchyGuid = typeof(IVsHierarchy).GUID;
            int result = hierarchy.GetNestedHierarchy(itemId, ref nestedHierarchyGuid, out IntPtr nestedHierarchyValue,
                out uint nestedItemIdValue);

            if(result == VSConstants.S_OK && nestedHierarchyValue != IntPtr.Zero && nestedItemIdValue == VSConstants.VSITEMID_ROOT)
            {
                // Get the new hierarchy
                nestedHierarchy = System.Runtime.InteropServices.Marshal.GetObjectForIUnknown(
                    nestedHierarchyValue) as IVsHierarchy;
                System.Runtime.InteropServices.Marshal.Release(nestedHierarchyValue);

                if(nestedHierarchy != null)
                    ProcessHierarchyNodeRecursively(nestedHierarchy, VSConstants.VSITEMID_ROOT, projectFiles);
            }
            else
            {
                // The node is not the root of another hierarchy, it is a regular node
                var projectFile = DetermineProjectFileInformation(hierarchy, itemId);

                if(projectFile != IgnoredHierarchyItem)
                {
                    if(projectFile != null)
                        projectFiles.Add(projectFile);

                    result = hierarchy.GetProperty(itemId, (int)__VSHPROPID.VSHPROPID_FirstVisibleChild,
                        out object value);

                    while(result == VSConstants.S_OK && value != null && value is int nodeId)
                    {
                        uint visibleChildNode = (uint)nodeId;

                        if(visibleChildNode == VSConstants.VSITEMID_NIL)
                            break;

                        ProcessHierarchyNodeRecursively(hierarchy, visibleChildNode, projectFiles);

                        result = hierarchy.GetProperty(visibleChildNode, (int)__VSHPROPID.VSHPROPID_NextVisibleSibling,
                            out value);
                    }
                }
            }
        }

        /// <summary>
        /// This is used to determine project and file information for a hierarchy node
        /// </summary>
        /// <param name="hierarchy">The hierarchy node to examine</param>
        /// <param name="itemId">The item ID</param>
        /// <remarks>This filters out the root solution node, project nodes, folder nodes, and any other
        /// unrecognized nodes.</remarks>
        private static SpellCheckFileInfo DetermineProjectFileInformation(IVsHierarchy hierarchy, uint itemId)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            if(hierarchy is IVsProject project)
            {
                int result = project.GetMkDocument(VSConstants.VSITEMID_ROOT, out string projectName);

                // If there is no project name, it's probably a solution item
                if(result != VSConstants.S_OK)
                    projectName = "Solution Items";
                else
                    if(projectName.Length > 1 && projectName[projectName.Length - 1] == '\\')
                        projectName += Path.GetFileName(projectName.Substring(0, projectName.Length - 1));

                result = hierarchy.GetProperty(itemId, (int)__VSHPROPID.VSHPROPID_Name, out object value);

                if(result == VSConstants.S_OK && value != null)
                {
                    string name = value.ToString();

                    // Certain project folders in C++ projects return a GUID for their name.  These should be
                    // ignored (References, External Dependencies, etc.).
                    if(name.Length != 0 && name[0] == '{' && Guid.TryParse(name, out _))
                        return IgnoredHierarchyItem;

                    result = hierarchy.GetCanonicalName(itemId, out string canonicalName);

                    if(result == VSConstants.S_OK && !String.IsNullOrWhiteSpace(canonicalName) &&
                      canonicalName.IndexOfAny(Path.GetInvalidPathChars()) == -1 &&
                      Path.IsPathRooted(canonicalName) && !canonicalName.EndsWith("\\", StringComparison.Ordinal) &&
                      !canonicalName.Equals(projectName, StringComparison.OrdinalIgnoreCase))
                    {
                        result = hierarchy.GetProperty(itemId, (int)__VSHPROPID4.VSHPROPID_BuildAction, out value);


                        bool isGlobalConfigItem = (result == VSConstants.S_OK && value != null &&
                            ((string)value).Equals("GlobalAnalyzerConfigFiles", StringComparison.OrdinalIgnoreCase));
                        bool isEditorConfigItem = (result == VSConstants.S_OK && value != null &&
                            ((string)value).Equals("EditorConfgFiles ", StringComparison.OrdinalIgnoreCase));
                        bool isCodeAnalysisDictionary = (result == VSConstants.S_OK && value != null &&
                            ((string)value).Equals("CodeAnalysisDictionary", StringComparison.OrdinalIgnoreCase));

                        return new SpellCheckFileInfo
                        {
                            ProjectFile = projectName,
                            Filename = name,
                            CanonicalName = canonicalName,
                            IsGlobalConfigItem = isGlobalConfigItem,
                            IsEditorConfigItem = isEditorConfigItem,
                            IsCodeAnalysisDictionary = isCodeAnalysisDictionary
                        };
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// This is used to generate the configuration for the instance
        /// </summary>
        /// <param name="additionalGlobalConfigFiles">An optional enumerable list of additional global
        /// configuration files.</param>
        /// <param name="additionalEditorConfigFiles">An optional enumerable list of additional editor
        /// configuration files.</param>
        /// <param name="codeAnalysisFiles">An optional enumerable list of code analysis dictionaries.</param>
        /// <returns>The configuration to use or null if the file should not be spell checked (disabled or not a
        /// type of file that can be spell checked such as a binary file).</returns>
        public SpellCheckerConfiguration GenerateConfiguration(IEnumerable<string> additionalGlobalConfigFiles,
          IEnumerable<string> additionalEditorConfigFiles, IEnumerable<string> codeAnalysisFiles)
        {
            SpellCheckerConfiguration config = null;

            try
            {
                config = SpellCheckerConfiguration.CreateSpellCheckerConfigurationFor(this.CanonicalName,
                    additionalGlobalConfigFiles, additionalEditorConfigFiles);

                if(config.IncludeInProjectSpellCheck && !IsBinaryFile(this.CanonicalName))
                {
                    // Merge any code analysis dictionary settings
                    if(codeAnalysisFiles != null)
                    {
                        foreach(string cad in codeAnalysisFiles)
                        {
                            if(File.Exists(cad))
                                config.ImportCodeAnalysisDictionary(cad);
                        }
                    }

                    // If wanted, set the language based on the resource filename
                    if(config.DetermineResourceFileLanguageFromName &&
                      Path.GetExtension(this.Filename).Equals(".resx", StringComparison.OrdinalIgnoreCase))
                    {
                        // Localized resource files are expected to have filenames in the format
                        // BaseName.Language.resx (i.e. LocalizedForm.de-DE.resx).
                        string ext = Path.GetExtension(Path.GetFileNameWithoutExtension(this.Filename));

                        if(ext.Length > 1)
                        {
                            ext = ext.Substring(1);

                            if(SpellCheckerDictionary.AvailableDictionaries(
                              config.AdditionalDictionaryFolders).TryGetValue(ext, out SpellCheckerDictionary match))
                            {
                                // Clear any existing dictionary languages and use just the one that matches the
                                // file's language.
                                config.DictionaryLanguages.Clear();
                                config.DictionaryLanguages.Add(match.Culture);
                            }
                        }
                    }

                    this.ConfigurationFiles = config.LoadedConfigurationFiles;
                    this.IgnoredWordsFiles = config.IgnoredWordsFiles;
                }
                else
                    config = null;
            }
            catch(Exception ex)
            {
                // Ignore errors, we just won't load the configurations after the point of failure
                System.Diagnostics.Debug.WriteLine(ex);
            }

            return config;
        }

        /// <summary>
        /// This is used to determine whether or not the given file is a binary file
        /// </summary>
        /// <param name="filename">The file to check</param>
        /// <remarks>Since we cannot create an exhaustive list of file types that we cannot spell check, take a
        /// peek at the first 5120 bytes.  If it looks like a binary file, ignore it.  Quick and dirty but mostly
        /// effective.</remarks>
        public static bool IsBinaryFile(string filename)
        {
            bool result = true;

            try
            {
                // If it's not there, ignore it
                if(File.Exists(filename))
                {
                    using StreamReader sr = new(filename, true);
                    var fileChars = new char[5120];

                    // Note the length as it may be less than the maximum
                    int length = sr.Read(fileChars, 0, fileChars.Length);

                    result = fileChars.Take(length).Any(c => c < 32 && !validChars.Contains(c));
                }
            }
            catch(Exception ex)
            {
                // Ignore errors, we'll treat it as binary so that it isn't spell checked
                System.Diagnostics.Debug.WriteLine(ex);
            }

            return result;
        }
        #endregion
    }
}
