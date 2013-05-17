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
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Collections.ObjectModel;

namespace nConfigureLib
{
	
    public class Build
	{
        public string Status { get; private set; }
        public readonly Dictionary<Guid, List<CsProject>> DuplicateGuidProject;

        static Logger log = new Logger(typeof(Build));
        public readonly List<string> SourcePaths = new List<string>();
        public readonly List<string> PreCompiledDllPaths = new List<string>();
        public int Errors { get; private set; }

        private List<CsProject> _projects;
        private List<FailedCsProject> _failCsProjects;
        private List<string> _staticDlls;

        private List<string> _ignoreSourcePaths = new List<string>();
        
        public ReadOnlyCollection<string> IgnoreSourcePaths
        {
            get { return _ignoreSourcePaths.AsReadOnly(); }
        }
        
        public void AddIgnoreSourcePaths(string[] paths)
        {
            foreach (var path in paths)
            {
                _ignoreSourcePaths.Add(path.ToLower());
            }
        }


        public ReadOnlyCollection<CsProject> Projects
        {
            get { return _projects.AsReadOnly(); }
        }

        public ReadOnlyCollection<FailedCsProject> FailedProjects
        {
            get { return _failCsProjects.AsReadOnly(); }
        }

        public ReadOnlyCollection<string> StaticDlls
        {
            get { return _staticDlls.AsReadOnly(); }
        }

        public Build()
		{
            _projects = new List<CsProject>();
            _failCsProjects = new List<FailedCsProject>();
            _staticDlls = new List<string>();
            DuplicateGuidProject = new Dictionary<Guid, List<CsProject>>();
		}

        public void ResolveForDebugConfiguration()
        {
            CsProjectReferenceResolver.Resolve(
                _projects, 
                _staticDlls, 
                CsProjectReferenceResolver.Configuration.Debug);
        }

        public void ResolveForReleaseConfiguration()
        {
            CsProjectReferenceResolver.Resolve(
                _projects, 
                _staticDlls,
                CsProjectReferenceResolver.Configuration.Debug);
        }

        public void Scan()
        {
            _projects.Clear();
            _failCsProjects.Clear();
            _staticDlls.Clear();
            DuplicateGuidProject.Clear();

            var projectPaths = new List<string>();
            //scan all directories for csproj files
            foreach (string sourcePath in SourcePaths)
            {
                projectPaths.AddRange(CsProjSearch(sourcePath));
            }
            //Scan directories for dlls
            foreach (string precompiledDllPath in PreCompiledDllPaths)
            {
                DllSearch(precompiledDllPath, _staticDlls);
            }

            foreach (var projectPath in projectPaths)
            {
                CsProject aCsProject;
                try
                {
                    aCsProject = CsProjectReader.ReadProject(projectPath);
                    _projects.Add(aCsProject);
                }
                catch (Exception e) 
                {
                    log.Error(e.Message);
                    _failCsProjects.Add(new FailedCsProject(projectPath,e.Message));
                }
            }

            CheckForDuplicateProjectGuid();
        }

        private void CheckForDuplicateProjectGuid()
        {
            var tmp = new Dictionary<Guid, List<CsProject>>();
            foreach (var project in _projects)
            {
                if (!tmp.ContainsKey(project.Guid))
                    tmp.Add(project.Guid, new List<CsProject>());
                tmp[project.Guid].Add(project);
            }

            var enumerator = tmp.GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (enumerator.Current.Value.Count > 1)
                    DuplicateGuidProject.Add(enumerator.Current.Key, enumerator.Current.Value);
            }
        }

        public void WriteMSBuilFile(string outputFilePath)
        {
            WriteMsBuildFile(_projects, outputFilePath);
        }

        private void WriteMsBuildFile(List<CsProject> projects, string filename)
		{
            Status = "Writing MsBuild file : " + filename;
            MsbuildBuilder target = new MsbuildBuilder();
			XmlWriterSettings settings = new XmlWriterSettings();
			settings.Indent = true;
            settings.NewLineOnAttributes = true;
            settings.NewLineChars = Environment.NewLine;
			settings.NewLineHandling = NewLineHandling.Entitize;

			XmlWriter w = XmlWriter.Create(filename, settings);

			target.CreateBuildFile(projects, w);
			w.Flush();
			w.Close();
		}

		/// <summary>
		/// Recursive search for all csproj files and add them to projects
		/// </summary>
		/// <param name="sDir"></param>
		private List<string> CsProjSearch(string sDir)
		{
            // Skip hidden(doted) files to support clearcase
            if (sDir.ToLower().StartsWith("."))
            {
                log.Info("Ignoring path ( . = hidden )" + sDir);
                return new List<string>();
            }

		    if (_ignoreSourcePaths.Contains(sDir.ToLower()))
            {
                log.Info("Ignoring path " +sDir);
                return new List<string>();
            }

            var projectsPath = new List<string>();
            if(String.IsNullOrEmpty(sDir))
                return new List<string>();
			try
			{
                Status = "Searching for project files in " + sDir;
                foreach (string projectPath in Directory.GetFiles(sDir, "*.csproj"))
				{
                    string projectName = Path.GetFileNameWithoutExtension(projectPath);
					if (projectName.StartsWith("~"))
					    continue;
                    
                    string absolutProjectPath = Path.GetFullPath(projectPath);
                    projectsPath.Add(absolutProjectPath);
				}
				
				foreach (string subDirectory in Directory.GetDirectories(sDir))
				{
                    projectsPath.AddRange(CsProjSearch(subDirectory));
				}

                return projectsPath;
			}
			catch (System.Exception excpt)
			{
				log.Error(excpt.Message);
				throw excpt;
			}
		}

		/// <summary>
		/// Recursive search for all dll files and add them to list of dlls 
		/// </summary>
		/// <param name="sDir"></param>
		private void DllSearch(string sDir, List<string> dlls)
		{
			if (dlls == null)
				dlls = new List<string>();

			try
			{
                Status = "Searching for dlls in " + sDir;
                foreach (string fileName in Directory.GetFiles(sDir, "*.dll"))
				{
                    string absolutFilePath = Path.GetFullPath(fileName).ToLower();

                    if (dlls.Contains(absolutFilePath))
					{
						log.Error(
                            "Found a duplicate of dll " + absolutFilePath +
							" in known dll directories");
					}
					else
					{
                        dlls.Add(absolutFilePath);
					}

				}
				foreach (string d in Directory.GetDirectories(sDir))
				{
					DllSearch(d, dlls);
				}
			}
			catch (System.Exception excpt)
			{
				log.Error(excpt.Message);
				throw excpt;
			}
		}
	}
}
