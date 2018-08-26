@ECHO OFF

SETLOCAL

CD Source

ECHO *
ECHO * VS2013/VS2015 package
ECHO *

REM Use the earliest version of MSBuild available
IF EXIST "%ProgramFiles(x86)%\MSBuild\14.0" SET "MSBUILD=%ProgramFiles(x86)%\MSBuild\14.0\bin\MSBuild.exe"
IF EXIST "%ProgramFiles(x86)%\MSBuild\12.0" SET "MSBUILD=%ProgramFiles(x86)%\MSBuild\12.0\bin\MSBuild.exe"

packages\NuGet.CommandLine.4.7.1\tools\NuGet.exe restore VSSpellChecker2013.sln

"%MSBUILD%" VSSpellChecker2013.sln /nologo /v:m /m /t:Clean;Build "/p:Configuration=Release;Platform=Any CPU"

IF ERRORLEVEL 1 GOTO End

CD ..\

:End

ENDLOCAL
