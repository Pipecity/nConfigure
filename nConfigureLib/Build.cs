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
using Microsoft.Build.Framework;

namespace nConfigureLib
{

    public class Build
	{
        public string Status
        {
            get { return string.Empty; }
            private set
            {
                    log.Debug(value);
            }
        }
        public readonly Dictionary<Guid, List<Project>> DuplicateGuidProject;

        static Logger log = new Logger(typeof(Build));
        public readonly List<string> SourcePaths = new List<string>();
        public readonly List<string> PreCompiledDllPaths = new List<string>();
        public int Errors { get; private set; }

        private List<Project> _projects;
        private List<FailedProject> _failProjects;
        private List<string> _staticDlls;
        private LanguageType _language = LanguageType.CS;

        private List<string> _ignoreSourcePaths = new List<string>();
        
        public ReadOnlyCollection<string> IgnoreSourcePaths
        {
            get { return _ignoreSourcePaths.AsReadOnly(); }
        }
        
        public void AddIgnoreSourcePaths(string[] paths)
        {
            foreach (var path in paths)
            {
                var absolutePath = Path.GetFullPath(path);
                log.Debug("absolute=" + absolutePath.ToLower());
                _ignoreSourcePaths.Add(absolutePath.ToLower());
            }
        }

        public void SetLanguageType(string language)
        {
            if (language == null)
                return;
            if (language.ToLower() == "vb")
                _language = LanguageType.VB;
            else
                _language = LanguageType.CS;
        }

        public ReadOnlyCollection<Project> Projects
        {
            get { return _projects.AsReadOnly(); }
        }

        public ReadOnlyCollection<FailedProject> FailedProjects
        {
            get { return _failProjects.AsReadOnly(); }
        }

        public ReadOnlyCollection<string> StaticDlls
        {
            get { return _staticDlls.AsReadOnly(); }
        }

        public Build()
		{
            _projects = new List<Project>();
            _failProjects = new List<FailedProject>();
            _staticDlls = new List<string>();
            DuplicateGuidProject = new Dictionary<Guid, List<Project>>();
		}

        public void ResolveForDebugConfiguration()
        {
            ProjectReferenceResolver.Resolve(
                _projects, 
                _staticDlls, 
                ConfigurationType.Debug);
        }

        public void ResolveForReleaseConfiguration()
        {
            ProjectReferenceResolver.Resolve(
                _projects, 
                _staticDlls,
                ConfigurationType.Debug);
        }

        public void Scan()
        {
            _projects.Clear();
            _failProjects.Clear();
            _staticDlls.Clear();
            DuplicateGuidProject.Clear();

            var projectPaths = new List<string>();
            //scan all directories for csproj files
            foreach (string sourcePath in SourcePaths)
            {
                var absolutePath = Path.GetFullPath(sourcePath);
                projectPaths.AddRange(ProjSearch(absolutePath));
            }
            //Scan directories for dlls
            foreach (string precompiledDllPath in PreCompiledDllPaths)
            {
                var absolutePath = Path.GetFullPath(precompiledDllPath);
                DllSearch(absolutePath, _staticDlls);
            }

            foreach (var projectPath in projectPaths)
            {
                Project aProject;
                try
                {
                    aProject = ProjectReader.ReadProject(projectPath);
                    _projects.Add(aProject);
                }
                catch (Exception e) 
                {
                    log.Error(e.Message);
                    _failProjects.Add(new FailedProject(projectPath,e.Message));
                }
            }

            CheckForDuplicateProjectGuid();
        }

        private void CheckForDuplicateProjectGuid()
        {
            var tmp = new Dictionary<Guid, List<Project>>();
            foreach (var project in _projects)
            {
                if (!tmp.ContainsKey(project.Guid))
                    tmp.Add(project.Guid, new List<Project>());
                tmp[project.Guid].Add(project);
            }

            var enumerator = tmp.GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (enumerator.Current.Value.Count > 1)
                    DuplicateGuidProject.Add(enumerator.Current.Key, enumerator.Current.Value);
            }
        }

        /// <summary>
        /// Recursive search for all csproj files and add them to projects
        /// </summary>
        /// <param name="sDir"></param>
        private List<string> ProjSearch(string sDir)
        {
            // Skip hidden(doted) files to support clearcase
            if (Path.GetFileName(sDir).ToLower().StartsWith("."))
            {
                log.Info("Ignoring path ( . = hidden )" + sDir);
                return new List<string>();
            }

            if (_ignoreSourcePaths.Contains(sDir.ToLower()))
            {
                log.Info("Ignoring path " + sDir);
                return new List<string>();
            }

            var projectsPath = new List<string>();
            if (String.IsNullOrEmpty(sDir))
                return new List<string>();
            try
            {
                var searchPattern = GetSearchPatternByLanguage();
                Status = "Searching for project files in " + sDir;
                foreach (string projectPath in Directory.GetFiles(sDir, searchPattern))
                {
                    string projectName = Path.GetFileNameWithoutExtension(projectPath);
                    if (projectName.StartsWith("~"))
                        continue;

                    string absolutProjectPath = Path.GetFullPath(projectPath);
                    projectsPath.Add(absolutProjectPath);
                }

                foreach (string subDirectory in Directory.GetDirectories(sDir))
                {
                    log.Debug("Dir=" + subDirectory);
                    projectsPath.AddRange(ProjSearch(subDirectory));
                }

                return projectsPath;
            }
            catch (System.Exception excpt)
            {
                log.Error(excpt.Message);
                throw excpt;
            }
        }

        private static string TryConvertToAbsolutPath(string csProjectPath, string path)
        {
            if (!Path.IsPathRooted(path))
            {
                //this is a relative path. convert it to absolut path so we could compare it easier
                path = Path.GetFullPath(Path.Combine(csProjectPath, path));
            }
            return path;
        }


        private string GetSearchPatternByLanguage()
        {
            if (_language == LanguageType.VB)
                return "*.vbproj";
            return "*.csproj";
        }

        public void WriteMSBuildFile(string outputFilePath)
        {
            WriteMsBuildFile(_projects, outputFilePath);
        }

        private void WriteMsBuildFile(List<Project> projects, string filename)
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
                        log.Debug("Dll=" + absolutFilePath);
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
