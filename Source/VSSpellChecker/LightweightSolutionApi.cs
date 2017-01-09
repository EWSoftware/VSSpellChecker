//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : LightweightSolutionApi.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 01/07/2017
// Note    : Copyright 2016-2017, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a class used to wrap the lightweight solution load API interactions in Visual Studio 2017
// and later.
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 12/10/2016  EFW  Created the code
//===============================================================================================================

using System;
using System.Collections.Generic;
using System.Linq;

#if VS2017
using System.IO;
using System.Threading.Tasks;

using EnvDTE;
using EnvDTE80;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

using Microsoft.VisualStudio.Workspace.Indexing;
using Microsoft.VisualStudio.Workspace.VSIntegration;
#endif

namespace VisualStudio.SpellChecker
{
    /// <summary>
    /// This class is used to wrap the lightweight solution load API interactions in Visual Studio 2017 and later
    /// </summary>
    internal static class LightweightSolutionApi
    {
#if VS2017
        /// <summary>
        /// This read-only property is used to see if solution loading is deferred
        /// </summary>
        /// <value>True if solution loading is deferred, false if not</value>
        internal static bool IsSolutionLoadDeferred
        {
            get
            {
                var vsSolution = Utility.GetServiceFromPackage<IVsSolution7, SVsSolution>(false);

                if(vsSolution == null)
                    return false;

                return vsSolution.IsSolutionLoadDeferred();
            }
        }
#endif
        /// <summary>
        /// This read-only property returns an enumerable list of project GUID/project name pairs
        /// for all deferred projects in the current solution.
        /// </summary>
        internal static IEnumerable<KeyValuePair<Guid, string>> DeferredProjects
        {
            get
            {
#if VS2017
                var vsSolution = Utility.GetServiceFromPackage<IVsSolution, SVsSolution>(false);

                if(vsSolution != null)
                {
                    IVsHierarchy[] hierarchy = new IVsHierarchy[1] { null };
                    Guid guid = Guid.Empty, projectGuid;
                    string projectName;

                    vsSolution.GetProjectEnum((uint)__VSENUMPROJFLAGS3.EPF_DEFERRED, ref guid,
                        out IEnumHierarchies enumerator);

                    enumerator.Reset();

                    while(enumerator.Next(1, hierarchy, out uint fetched) == VSConstants.S_OK && fetched == 1)
                    {
                        projectName = null;
                        projectGuid = Guid.Empty;

                        int hr = hierarchy[0].GetCanonicalName((uint)VSConstants.VSITEMID.Root, out projectName);

                        if(hr == VSConstants.S_OK)
                        {
                            hr = hierarchy[0].GetGuidProperty((uint)VSConstants.VSITEMID.Root,
                                (int)__VSHPROPID.VSHPROPID_ProjectIDGuid, out projectGuid);

                            if(hr == VSConstants.S_OK)
                                yield return new KeyValuePair<Guid, string>(projectGuid, projectName);
                        }
                    }
                }
#else
                return Enumerable.Empty<KeyValuePair<Guid, string>>();
#endif
            }
        }

#if VS2017
        /// <summary>
        /// This is used to get a list of all projects in a solution
        /// </summary>
        /// <returns>An enumerable list of the project names in the solution</returns>
        internal async static Task<IEnumerable<string>> AllProjectsAsync()
        {
            var wss = Utility.GetServiceFromPackage<IVsSolutionWorkspaceService, SVsSolutionWorkspaceService>(true);
            var dte = Utility.GetServiceFromPackage<DTE, DTE>(true);
            var solutionConfig = (EnvDTE80.SolutionConfiguration2)dte.Solution.SolutionBuild.ActiveConfiguration;

            var ws = wss.CurrentWorkspace;
            var indexService = (IIndexWorkspaceService)(await ws.GetServiceAsync(typeof(IIndexWorkspaceService)));

            var result = await indexService.GetFileReferencesAsync(wss.SolutionFile, context: $"{solutionConfig.Name}|{solutionConfig.PlatformName}",
                referenceTypes: (int)FileReferenceInfoType.ProjectReference);

            return result.Select(f => f.Path);
        }
#endif
        /// <summary>
        /// This returns a hash set containing a list of selected Solution Explorer items that appear to be
        /// projects.
        /// </summary>
        /// <returns>A case-insensitive hash set containing a list of selected items that appear to be
        /// project names.</returns>
        internal static HashSet<string> SelectedProjects()
        {
            HashSet<string> projectNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

#if VS2017
            var dte2 = Utility.GetServiceFromPackage<DTE2, SDTE>(false);

            if(dte2 != null)
                foreach(SelectedItem item in dte2.SelectedItems)
                    if(item.ProjectItem == null || item.ProjectItem.ContainingProject == null)
                    {
                        // If the name matches the solution name, assume it's the whole solution.
                        // This may not be the case but in lightweight mode, there's no way to tell
                        // the difference between the root solution node and a project node with the
                        // same name.
                        if(Path.GetFileNameWithoutExtension(dte2.Solution.FullName) == item.Name)
                        {
                            projectNames.Add("~~Solution~~");
                            break;
                        }

                        projectNames.Add(item.Name);
                    }
#endif
            return projectNames;
        }

        /// <summary>
        /// This is used to load deferred projects prior to spell checking them
        /// </summary>
        /// <param name="deferredProjects">In order to properly spell check projects, they need to be in a
        /// loaded state.  This allows us to find files by item type (i.e. the code analysis dictionaries.
        /// If any projects are in a deferred state, this will load them.</param>
        internal static void LoadDeferredProjects(IEnumerable<KeyValuePair<Guid, string>> deferredProjects)
        {
#if VS2017
            var projectsToLoad = deferredProjects.Select(p => p.Key).ToArray();
            int hr = -1;

            if(projectsToLoad.Length != 0)
            {
                var vsSolution = Utility.GetServiceFromPackage<IVsSolution4, SVsSolution>(false);

                if(vsSolution != null)
                    hr = vsSolution.EnsureProjectsAreLoaded((uint)projectsToLoad.Length, projectsToLoad,
                        (uint)__VSBSLFLAGS.VSBSLFLAGS_None);

                if(hr != VSConstants.S_OK)
                    Utility.ShowMessageBox(OLEMSGICON.OLEMSGICON_WARNING, "Not all projects were loaded.  " +
                        "Those that were not loaded will not be spell checked.");
            }
#else
            System.Diagnostics.Debug.WriteLine(deferredProjects.Count());
#endif
        }
    }
}
