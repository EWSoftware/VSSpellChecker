using EnvDTE;
using System;
using System.Collections.Generic;
using System.Linq;

namespace VisualStudio.SpellChecker
{
    public static class SolutionExtensions
    {
        public static IEnumerable<Project> EnumerateProjects(this Solution solution)
        {
            return solution.Projects.OfType<Project>().SelectMany(EnumerateProjects);
        }

        private static IEnumerable<Project> EnumerateProjects(Project project)
        {
            switch (project.Kind)
            {
                case Constants.vsProjectKindSolutionItems:
                    foreach (ProjectItem projectItem in project.ProjectItems)
                    {
                        if (projectItem.SubProject != null)
                        {
                            foreach (var result in EnumerateProjects(projectItem.SubProject))
                            {
                                yield return result;
                            }
                        }
                    }
                    break;
                case Constants.vsProjectKindUnmodeled:
                    break;
                default:
                    if (!String.IsNullOrWhiteSpace(project.FullName))
                    {
                        yield return project;
                    }
                    break;
            }
        }
    }
}
