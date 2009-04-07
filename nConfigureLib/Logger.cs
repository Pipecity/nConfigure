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
using System.Text;

namespace nConfigureLib
{
	/// <summary>
	/// A very simple log class.
	/// </summary>
    public class Logger
	{
		private Type m_type;
		public delegate void LogHandler(string classname, string message);
		static public event LogHandler ErrorLog;
		static public event LogHandler WarningLog;
        static public event LogHandler DebugLog;
        static public int ErrorCounter { get; private set; }
        
        public Logger(Type type)
		{
			m_type = type;
         
		}

		public void Error(string error)
		{
            ErrorCounter++;
            if (ErrorLog != null)
				ErrorLog(m_type.FullName, error);
		}

		public void Warning(string warning)
		{
			if (WarningLog != null)
				WarningLog(m_type.FullName, warning);
		}
		public void Info(string info)
		{
            if (WarningLog != null)
                WarningLog(m_type.FullName, info);			
		}
		public void Debug(string debugmessage)
		{
            if (DebugLog != null)
            {
                DebugLog(m_type.FullName, debugmessage);
            }
		}

	}
}
