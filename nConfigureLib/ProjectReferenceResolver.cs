// nConfigure detects dependecies between your .net project files
// Copyright (C) 2008,2009  Magnus Berglund, nConfigure@gmail.com

// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace nConfigureLib
{
	internal class ProjectReferenceResolver
	{
        static Logger log = new Logger(typeof(ProjectReferenceResolver));
		internal static void Resolve(List<Project> projects, List<string> staticDlls, ConfigurationType configuration)
		{
            //creates two indexes so it's possible to search below
            Dictionary<string, Project> dllPathIndex = CreateOutputIndex(projects, configuration);
            Dictionary<Guid, Project> guidIndex = CreateGuidIndex(projects);

			foreach (Project project in projects)
			{
				foreach (Reference reference in project.References)
				{
					switch (reference.ReferenceType)
					{
						case Reference.Type.Dll:
							UpdateReference(staticDlls, dllPathIndex, project, reference);
							break;
						case Reference.Type.Project:
							UpdateReference(guidIndex, project, reference);
							break;
						default:
							System.Diagnostics.Debug.Fail("Unknown referencetype");
							break;
					}
				}
			}
		}

        /// <summary>
        /// Creates an search index based on the project guids.
        /// </summary>                  
        /// <param name="projects"></param>
        /// <returns></returns>
        private static Dictionary<Guid, Project> CreateGuidIndex(List<Project> projects)
        {
            Dictionary<Guid, Project> guidIndex = new Dictionary<Guid, Project>();
            foreach (Project project in projects)
            {
                if (guidIndex.ContainsKey(project.Guid))
                {
                    log.Error(
                        "Project " + project.FullAbsoluteFileName +
                        " and project " + guidIndex[project.Guid].FullAbsoluteFileName +
                        " has the same guid");
                }
                else
                {
                    guidIndex.Add(project.Guid, project);
                }
            }
            return guidIndex;
        }

        /// <summary>
        /// Create a search index based on the project names
        /// </summary>
        /// <param name="projects"></param>
        /// <param name="debug"></param>
        /// <returns></returns>
        private static Dictionary<string, Project> CreateOutputIndex(
            List<Project> projects, ConfigurationType configuration)
        {
            Dictionary<string, Project> dllPathIndex = new Dictionary<string, Project>();
            foreach (Project project in projects)
            {
                string output;
                switch (configuration)
                {
                    case ConfigurationType.Debug:
                        output = project.OutputDebug;
                        break;
                    case ConfigurationType.Release:
                        output = project.OutputRelease;
                        break;
                    default:
                        Debug.Fail("Unknown Configuration. Will try configuration Debug");
                        output = project.OutputDebug;
                        break;
                }
                
                if (dllPathIndex.ContainsKey(output))
                {
                    log.Error(
                        "Project " + project.FullAbsoluteFileName + " with " + (configuration ==ConfigurationType.Debug ? "Debug output=" : "Release output=") +
                        output + " has the same output as project " + dllPathIndex[output].AbsolutProjectDir);
                }
                else
                {
                    dllPathIndex.Add(project.OutputDebug, project);
                }
            }
            return dllPathIndex;
        }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="guidIndex"></param>
		/// <param name="csProject"></param>
		/// <param name="reference"></param>
		private static void UpdateReference(
            Dictionary<Guid, Project> guidIndex, 
            Project project, 
            Reference reference)
		{
			if (guidIndex.ContainsKey(reference.ReferencedProjectGuid))
			{
				reference.Project = guidIndex[reference.ReferencedProjectGuid];
			}
			else
			{
				string msg =
					"Couldn't find project " + reference.Name +
					" that are referenced in project " + project.ProjectName;
				log.Error(msg);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="staticDlls">all ready build dll that has no projectfile </param>
		/// <param name="dllPathIndex">a dictionary with dll debugbuild path at key</param>
		/// <param name="csProject">the project that we are trying to resolve references for</param>
		/// <param name="reference">the current reference</param>
		private static void UpdateReference(
            List<string> staticDlls, 
            Dictionary<string, Project> dllPathIndex, 
            Project project, 
            Reference reference)
		{

			if (dllPathIndex.ContainsKey(reference.AbsolutDllPath))
			{
				reference.Project = dllPathIndex[reference.AbsolutDllPath];
			}
			else if (staticDlls.Contains(reference.AbsolutDllPath))
			{
				//Don't know if I have to do anything here. Maybe mark it in the project 
                reference.IsPreCompiled = true;
			}
			else
			{
				string msg =
					"Couldn't find the referenced dll:" + reference.AbsolutDllPath +
					" that are referenced in project " + project.FullAbsoluteFileName +
					" or a project that builds this dll.";
				log.Error(msg);
			}
		}
	}
}
