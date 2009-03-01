@echo off
echo nConfigure detects dependecies between your .net project files
echo Copyright (C) 2008,2009  Magnus Berglund, nConfigure@gmail.com

echo This program is free software: you can redistribute it and/or modify
echo it under the terms of the GNU General Public License as published by
echo the Free Software Foundation, either version 3 of the License, or
echo (at your option) any later version.
echo This program is distributed in the hope that it will be useful,
echo but WITHOUT ANY WARRANTY; without even the implied warranty of
echo MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
echo GNU General Public License for more details.
 	
echo You should have received a copy of the GNU General Public License

call "%VS90COMNTOOLS%\VSVars32.bat"

echo ------------------------------------------------------------

echo Building nConfigure msbuild task
pause
msbuild nConfigureTask\nConfigureTask.csproj /p:Configuration=Debug /p:Platform=AnyCpu


echo ------------------------------------------------------------

echo Testing if the nConfigure is able to generate an output file based on the testdirectory. 

echo setting up testdirectories:
subst R: /D 
subst R: nConfigureTaskTest\TestDirectoryStructure\PrecompiledSubstToR

pause
msbuild nConfigureTaskTest\generate.xml

echo ------------------------------------------------------------

echo Trying to build all the projects in the testdirectory structure
pause
msbuild nConfigureTaskTest\build.xml

pause


