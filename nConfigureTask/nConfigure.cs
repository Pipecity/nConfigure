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
using Microsoft.Build.Framework;
using nConfigureLib;
using System.Diagnostics;
using System.Threading;

namespace nConfigureTask
{
    public class nConfigure : ITask
    {
        /// <summary>msbuild stuff</summary>
        public IBuildEngine BuildEngine { get; set; }
        /// <summary>msbuild stuff</summary>
        public ITaskHost HostObject { get; set; }
        
        /// <summary>Will look for csproj in these paths</summary>
        [Required]
        public string SourcePaths { get; set; }
        /// <summary>Override CodePaths and will not look in this paths for csproj files</summary>
        public string IgnoreSourcePaths { get; set; }
        /// <summary> Will look for precompiled dll in these paths</summary>
        public string DllPaths { get; set; }
        /// <summary>The file to where the task generate the msbuild script</summary>
        [Required]
        public string Output { get; set; }
        /// <summary> Should be either Debug or Release </summary>
        public string ResolveForConfiguration { get; set; }

        public nConfigure()
        {
            Logger.ErrorLog += new Logger.LogHandler(Logger_ErrorLog);
            Logger.WarningLog += new Logger.LogHandler(Logger_WarningLog);
            Logger.DebugLog += new Logger.LogHandler(Logger_DebugLog);
        }
        /// <summary>
        /// called by msbuild
        /// </summary>
        public bool Execute()
        {
            try
            {
                DisplayGPLText();

                var build = new Build();
                build.SourcePaths.AddRange(Split(SourcePaths));
                build.AddIgnoreSourcePaths(Split(IgnoreSourcePaths));
                build.PreCompiledDllPaths.AddRange(Split(DllPaths));
                build.Scan();
                if (IsDebugConfiguration(ResolveForConfiguration))
                    build.ResolveForDebugConfiguration();
                else
                    build.ResolveForReleaseConfiguration();

                build.WriteMSBuilFile(Output);

                BuildEngine.LogMessageEvent(new BuildMessageEventArgs(
                    "Succeded to generate " + Output,
                    "",
                    "",
                    MessageImportance.High));

                if (Logger.ErrorCounter > 0)
                {
                    BuildEngine.LogMessageEvent(new BuildMessageEventArgs(
                   Logger.ErrorCounter + " Errors during nConfigure",
                   "",
                   "",
                   MessageImportance.High));
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                BuildEngine.LogMessageEvent(new BuildMessageEventArgs(
                    "Exception during nCongfigure" + e.Message,
                    "",
                    "",
                    MessageImportance.High));
                return false;
            }
        }

        private void DisplayGPLText()
        {
            BuildEngine.LogMessageEvent(new BuildMessageEventArgs(
                GPLText.Create(),
                "",
                "",
                MessageImportance.High));
            Thread.Sleep(1000);
        }

        private string[] Split(string input)
        {
            string[] output;
            if (input == null || input.Length == 0)
                output = new string[0];
            else
                output = input.Split(';');

            return output;
        }

        private bool IsDebugConfiguration(string resolveForConfiguration)
        {
            if (string.IsNullOrEmpty(resolveForConfiguration))
                return true;
            if (resolveForConfiguration.ToLower() == "debug")
                return true;
            return false;
        }

        void Logger_DebugLog(string classname, string message)
        {
            BuildEngine.LogMessageEvent(
                new BuildMessageEventArgs(message, "", classname, MessageImportance.Normal));
        }

        void Logger_WarningLog(string classname, string message)
        {
            BuildEngine.LogWarningEvent(
                new BuildWarningEventArgs("", "", "", 0, 0, 0, 0, message, "", classname));
        }

        void Logger_ErrorLog(string classname, string message)
        {
            BuildEngine.LogErrorEvent(
                new BuildErrorEventArgs("", "", "", 0, 0, 0, 0, message, "", classname));
        }
    }
}
