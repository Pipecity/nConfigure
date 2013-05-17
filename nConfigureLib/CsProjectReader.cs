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

using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;
using System.Xml.XPath;
using System;

namespace nConfigureLib
{
	/// <summary>
	/// CsProjectReader will read visual studio 2008 csproj files
	/// </summary>
    public static class CsProjectReader
	{
        static Logger log = new Logger(typeof(CsProject));
		public static CsProject ReadProject(string fullAbsoluteFileName)
		{
			
            try
			{
                var project = new CsProject(fullAbsoluteFileName);
                var doc = new XmlDocument();
                using (var reader = new XmlTextReader(fullAbsoluteFileName))
                {
                    doc.Load(reader);
                }

				project.Guid = ExtractGuid(doc);
				project.AssemblyName = ExtractAssemblyName(doc);
				project.ProjectType = ExtractProjectType(doc);
				project.OutputDebug = ExtractOutputPathHelper(
					ExtractOutputDirDebug(doc,project.AbsolutProjectDir ),
					project.AssemblyName,
					Path.GetDirectoryName(project.FullAbsoluteFileName),
					project.ProjectType);

				project.OutputRelease = ExtractOutputPathHelper(
					ExtractOutputDirRelease(doc, project.AbsolutProjectDir),
					project.AssemblyName,
					Path.GetDirectoryName(project.FullAbsoluteFileName),
					project.ProjectType);
				
				project.References.AddRange(ExtractDllReferences(doc, project.AbsolutProjectDir));
				project.References.AddRange(ExtractProjectReferences(doc));

                //if (project.OutputDebug != project.OutputRelease)
                //{
                //    log.Warning("The debug output and release output are not equal.");
                //}
                return project;
			}
			catch (Exception e)
			{
                throw new Exception("Unable to read project " + fullAbsoluteFileName + " due to: " + e.Message.ToString(), e);
			}   
		}

        private static Guid ExtractGuid(XmlDocument doc)
		{
			XmlNamespaceManager nsmgr = GetANamespaceManager(doc);
			try
			{
				string guidText = doc.SelectSingleNode("/a:Project/a:PropertyGroup/a:ProjectGuid", nsmgr).InnerText;
				Guid guid = new Guid(guidText);
				return guid;
			}
			catch (Exception e)
			{
				throw new Exception("Unable to find GUID in csproj file." , e);
			}	
		}

        private static string ExtractAssemblyName(XmlDocument doc)
		{
			XmlNamespaceManager nsmgr = GetANamespaceManager(doc);
			try
			{
				return doc.SelectSingleNode("/a:Project/a:PropertyGroup/a:AssemblyName", nsmgr).InnerText;
			}
			catch (Exception e)
			{
				throw new Exception("Unable to find AssemblyName in csproj file." ,e);
			}
		}

        private static string ExtractOutputDirRelease(XmlDocument doc, string projectAbsoluteDir)
		{
			XmlNamespaceManager nsmgr = GetANamespaceManager(doc); 

			try
			{
				string query = "/a:Project/a:PropertyGroup[@Condition=\" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' \"]/a:OutputPath";
				XmlNode node = doc.SelectSingleNode(query, nsmgr);
                
                string dir = node.InnerText;
                if (!Path.IsPathRooted(dir))
                    dir = Path.Combine(projectAbsoluteDir, dir);

                dir = System.IO.Path.GetFullPath(dir);

				return dir;
			}
			catch (Exception e)
			{
				throw new Exception("Unable to find release output path", e);
			}
		}

        private static string ExtractOutputDirDebug(XmlDocument doc, string projectAbsoluteDir)
		{
			XmlNamespaceManager nsmgr = GetANamespaceManager(doc);
			try
			{
				string query = "/a:Project/a:PropertyGroup[@Condition=\" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' \"]/a:OutputPath";
				XmlNode node = doc.SelectSingleNode(query, nsmgr);
                string dir = node.InnerText;
                if (!Path.IsPathRooted(dir))
                    dir = Path.Combine(projectAbsoluteDir, dir);
                
				dir = System.IO.Path.GetFullPath(dir);

				return dir;
			}
			catch (Exception e)
			{
				throw new Exception("Unable to find debug output path", e);
			}
		}

		private static string ExtractOutputPathHelper(
			string outputDir,
			string assemblyName,
			string csProjLocation, 
			ProjectType projectType)
		{
			string outputPath = TryConvertToAbsolutPath(csProjLocation, outputDir); 

			string extension;
			switch (projectType)
			{
				case ProjectType.Application:
					extension = ".exe";
					break;
				case ProjectType.Library:
					extension = ".dll";
					break;
				case ProjectType.Service:
					extension = ".exe";
					break;
				default:
					System.Diagnostics.Debug.Fail("Unknown file type");
					extension = "";
					break;
			}
			outputPath = Path.Combine(outputPath, assemblyName + extension);
            
			outputPath= outputPath.Replace("%28", "(");
			outputPath= outputPath.Replace("%29", ")");

			return outputPath;
		}

		/// <summary>
		/// Finds all project refences in a csproj xml document and adds them to a project 
		/// </summary>
		/// <param name="project"></param>
		/// <param name="doc"></param>
		/// <param name="nsmgr"></param>
		private static List<Reference> ExtractProjectReferences(XmlDocument doc)
		{
			List<Reference> result = new List<Reference>();
			XmlNamespaceManager nsmgr = GetANamespaceManager(doc);	

			//find all project references
			XmlNodeList nodes = doc.SelectNodes("/a:Project/a:ItemGroup/a:ProjectReference", nsmgr);
			foreach (XmlNode node in nodes)
			{
                try
                {
                    string name = node.SelectSingleNode("a:Name", nsmgr).InnerText;
                    string guidString = node.SelectSingleNode("a:Project", nsmgr).InnerText;
                    Guid referencedGuid = new Guid(guidString);
                    Reference reference = new Reference(name, referencedGuid);
                    result.Add(reference);
                }
                catch (IOException e)
                {
                    throw new Exception("Couldn't read project references in  " + node.ToString() + " due to "+e.Message, e);
                }
                catch (Exception e)
                {
                    throw new Exception("Couldn't read project references " + node.ToString(), e);
                }
			}
			return result;
		}

		/// <summary>
		/// Finds all dll refences in a csproj xml document and adds them to a project
		/// </summary>
		/// <param name="project"></param>
		/// <param name="nsmgr"></param>
		/// <param name="root"></param>
		private static List<Reference> ExtractDllReferences(XmlDocument doc, string csProjectPath)
		{
			List<Reference> references = new List<Reference>();
			XmlNamespaceManager nsmgr = GetANamespaceManager(doc);	
			//find all dll references
			XmlNodeList nodes = doc.SelectNodes("/a:Project/a:ItemGroup/a:Reference/a:HintPath", nsmgr);
            foreach (XmlNode node in nodes)
			{
                try
                {
                    string path = node.InnerText;
                    path = TryConvertToAbsolutPath(csProjectPath, path);

                    path = path.Replace("%28", "(");
                    path = path.Replace("%29", ")");
                    path = Path.GetFullPath(path).ToLower();

                    string name = Path.GetFileNameWithoutExtension(node.InnerText);
                    Reference reference = new Reference(name, path);
                    references.Add(reference);
                }
                catch (IOException e)
                {
                    throw new Exception("Couldn't read project references in " + node.ToString() +" due to  " + e.Message, e);
                }
                catch (Exception e)
                {
                    throw new Exception("Couldn't read dll references in " + node.ToString(),e);
                }
			}
			return references;
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


		/// <summary>
		/// find out if its a lib or project
		/// </summary>
		/// <param name="nsmgr"></param>
		/// <param name="root"></param>
		/// <returns></returns>
		private static ProjectType ExtractProjectType(XmlDocument doc)
		{
			XmlNamespaceManager nsmgr = GetANamespaceManager(doc);
			try
			{
				string outputType = doc.SelectSingleNode("/a:Project/a:PropertyGroup/a:OutputType", nsmgr).InnerText;
				switch (outputType)
				{
					case "Library":
						return ProjectType.Library;
					case "Exe":
						return ProjectType.Application;
					case "WinExe":
						return ProjectType.Service;
					default:
						throw new Exception("Unable to find out if it was an library or application(found:" + outputType+")");
				}
			}
			catch (Exception e)
			{
				throw new Exception("Unable to find out if it was an library or application",e);
			}
		}



		private static XmlNamespaceManager GetANamespaceManager(XmlDocument doc)
		{
			//Couldn'get get the default namespace to work so i use 'a' instead
			XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
			nsmgr.AddNamespace("a", "http://schemas.microsoft.com/developer/msbuild/2003");

			return nsmgr;
		}

		
	}
}
