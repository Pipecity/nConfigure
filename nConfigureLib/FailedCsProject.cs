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
using System.IO;

namespace nConfigureLib
{
    /// <summary>
    /// Represent a csproject file that nConfigure was unable to read or parse
    /// </summary>
    public class FailedCsProject
    {
        public string _projectFile;

        public FailedCsProject(string filePath, string errorText)
        {
            ProjectName = Path.GetFileNameWithoutExtension(filePath);
            FilePath = filePath;
            ErrorText = errorText;
        }

        public readonly string ProjectName;
        public readonly string FilePath;
        public readonly string ErrorText;

        public string ProjectFile
        {
            get
            {
                if (_projectFile == null)
                {
                    try
                    {
                        StreamReader streamReader = new StreamReader(FilePath);
                        _projectFile = streamReader.ReadToEnd();
                        streamReader.Close();

                    }
                    catch(Exception e)
                    {
                        _projectFile = "Couldn't read project file :" + e.Message;
                    }
                }

                return _projectFile;
            }
        }
    }
}
