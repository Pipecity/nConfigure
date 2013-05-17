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

namespace nConfigureLib
{
	

    public enum ProjectType
	{
        Library,
		Application,
		Service
	}
   
	/// <summary>
	/// Represent one visual studio project file
	/// </summary>
    public class CsProject : IComparable<CsProject>
	{
        static Logger log = new Logger(typeof(CsProject));

        private List<Reference> _references = new List<Reference>();
        private string _outputPathRelease = "";
        private string _outputPathDebug = "";
        public string _projectFile; 

		public CsProject(string fullFileName)
		{
            ProjectName = Path.GetFileNameWithoutExtension(fullFileName);
            FullAbsoluteFileName = Path.GetFullPath(fullFileName).ToLower();
		}

		public string AssemblyName {get;set;}
        public Guid Guid { get; set; }
		public string ProjectName{get;private set;}
		public string FullAbsoluteFileName{get;private set;}
		public string AbsolutProjectDir
		{
			get { return Path.GetDirectoryName(FullAbsoluteFileName);}
		}
		
		public ProjectType ProjectType {get;set;}
		public List<Reference> References
		{
			get { return _references; }
		}
		
		public string OutputRelease
		{
			get {return _outputPathRelease.ToLower();}
			set { _outputPathRelease = value; }
		}

		public string OutputDebug
		{
			get {return _outputPathDebug.ToLower();}
			set { _outputPathDebug = value; }
		}

        public int CompareTo(CsProject other)
        {
            return FullAbsoluteFileName.CompareTo(other.FullAbsoluteFileName);
        }

        override public string ToString()
        {
            return ProjectName;
        }

        public bool IsResolved
        {
            get
            {
                foreach (var reference in References)
                {
                    if (!reference.IsResolved)
                        return false;
                    if (!reference.IsPreCompiled)
                    {
                        if (!reference.CsProject.IsResolved)
                            return false;
                    }
                }
                return true;
            }
        }

        public string ProjectFile
        {
            get
            {
                if (_projectFile == null)
                {
                    try
                    {
                        StreamReader streamReader = new StreamReader(FullAbsoluteFileName);
                        _projectFile = streamReader.ReadToEnd();
                        streamReader.Close();

                    }
                    catch (Exception e)
                    {
                        log.Error("Couldn't read project file :" + e.Message);
                        _projectFile = "Couldn't read project file :" + e.Message;
                    }
                }

                return _projectFile;
            }
        }
    }
}
