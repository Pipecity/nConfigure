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
	public class CsProjectReferenceResolver
	{
        public enum Configuration
        {
            Debug, Release
        }

        static Logger log = new Logger(typeof(CsProjectReferenceResolver));
		public static void Resolve(List<CsProject> projects, List<string> staticDlls, Configuration configuration)
		{
            //creates two indexes so it's possible to search below
            Dictionary<string, CsProject> dllPathIndex = CreateOutputIndex(projects, configuration);
            Dictionary<Guid, CsProject> guidIndex = CreateGuidIndex(projects);

			foreach (CsProject csProject in projects)
			{
				foreach (Reference reference in csProject.References)
				{
					switch (reference.ReferenceType)
					{
						case Reference.Type.Dll:
							UpdateReference(staticDlls, dllPathIndex, csProject, reference);
							break;
						case Reference.Type.Project:
							UpdateReference(guidIndex, csProject, reference);
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
        private static Dictionary<Guid, CsProject> CreateGuidIndex(List<CsProject> projects)
        {
            Dictionary<Guid, CsProject> guidIndex = new Dictionary<Guid, CsProject>();
            foreach (CsProject csProject in projects)
            {
                if (guidIndex.ContainsKey(csProject.Guid))
                {
                    log.Error(
                        "Project " + csProject.FullAbsoluteFileName +
                        " and project " + guidIndex[csProject.Guid].FullAbsoluteFileName +
                        " has the same guid");
                }
                else
                {
                    guidIndex.Add(csProject.Guid, csProject);
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
        private static Dictionary<string, CsProject> CreateOutputIndex(
            List<CsProject> projects, Configuration configuration)
        {
            Dictionary<string, CsProject> dllPathIndex = new Dictionary<string, CsProject>();
            foreach (CsProject csProject in projects)
            {
                string output;
                switch (configuration)
                {
                    case Configuration.Debug:
                        output = csProject.OutputDebug;
                        break;
                    case Configuration.Release:
                        output = csProject.OutputRelease;
                        break;
                    default:
                        Debug.Fail("Unknown Configuration. Will try configuration Debug");
                        output = csProject.OutputDebug;
                        break;
                }
                
                if (dllPathIndex.ContainsKey(output))
                {
                    log.Error(
                        "Project " + csProject.FullAbsoluteFileName + " with " + (configuration ==Configuration.Debug ? "Debug output=" : "Release output=") +
                        output + " has the same output as project " + dllPathIndex[output].AbsolutProjectDir);
                }
                else
                {
                    dllPathIndex.Add(csProject.OutputDebug, csProject);
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
            Dictionary<Guid, CsProject> guidIndex, 
            CsProject csProject, 
            Reference reference)
		{
			if (guidIndex.ContainsKey(reference.ReferencedProjectGuid))
			{
				reference.CsProject = guidIndex[reference.ReferencedProjectGuid];
			}
			else
			{
				string msg =
					"Couldn't find project " + reference.Name +
					" that are referenced in project " + csProject.ProjectName;
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
            Dictionary<string, CsProject> dllPathIndex, 
            CsProject csProject, 
            Reference reference)
		{
			if (dllPathIndex.ContainsKey(reference.AbsolutDllPath))
			{
				reference.CsProject = dllPathIndex[reference.AbsolutDllPath];
			}
			else if (staticDlls.Contains(reference.AbsolutDllPath))
			{
				//Don't know if I have to do anything here. Maybe mark in in the project 
                reference.IsPreCompiled = true;
			}
			else
			{
				string msg =
					"Couldn't find the referenced dll:" + reference.AbsolutDllPath +
					" that are referenced in project " + csProject.FullAbsoluteFileName +
					" or a project that builds this dll.";
				log.Error(msg);
			}
		}
	}
}
