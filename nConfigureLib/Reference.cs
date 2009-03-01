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

namespace nConfigureLib
{
	/// <summary>
    /// Represent either a project or dll reference in a .net project
    /// </summary>
	public class Reference
	{
        public enum Type
        {
            Dll,
            Project
        }

        public Reference(string name, string absoluteDllPath )
		{
			Name = name;
			m_absolutDllPath = absoluteDllPath;
			ReferenceType = Reference.Type.Dll;
		}

		public Reference(string name, Guid referencedProjectGuid)
		{
			Name = name;
			ReferencedProjectGuid = referencedProjectGuid;
			ReferenceType = Reference.Type.Project;
		}

        public bool IsPreCompiled { get; internal set; }
        
		public string Name {get;private set;}
		public Reference.Type ReferenceType {get;private set;}
        public Guid ReferencedProjectGuid {get;private set;}
        
        private string m_absolutDllPath;
		public string AbsolutDllPath
		{
			get { return m_absolutDllPath.ToLower(); }
		}

		/// <summary>
		/// If we have resolved the reference this points to the project that
        /// builds the dependet file/project.
		/// </summary>
		public CsProject CsProject{get; internal set;}

        public bool IsResolved
        {
            get
            {
                if (IsPreCompiled)
                    return true;
                if (CsProject == null)
                    return false;
                return true;
            }
        }
	}

}
