@ECHO OFF

SETLOCAL

CD Source

ECHO *
ECHO * VS2017 and later package
ECHO *

REM Use MSBuild from whatever edition of Visual Studio is installed
IF EXIST "%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Community\MSBuild\15.0" SET "MSBUILD=%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Community\MSBuild\15.0\bin\MSBuild.exe"
IF EXIST "%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Professional\MSBuild\15.0" SET "MSBUILD=%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Professional\MSBuild\15.0\bin\MSBuild.exe"
IF EXIST "%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0" SET "MSBUILD=%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\bin\MSBuild.exe"

IF NOT EXIST "%MSBUILD%" GOTO End

packages\NuGet.CommandLine.4.7.1\tools\NuGet.exe restore VSSpellChecker2017AndLater.sln

"%MSBUILD%" VSSpellChecker2017AndLater.sln /nologo /v:m /m /t:Clean;Build "/p:Configuration=Release;Platform=Any CPU"

IF ERRORLEVEL 1 GOTO End

CD ..\

IF NOT "%SHFBROOT%"=="" "%MSBUILD%" /nologo /v:m "Docs\VSSpellCheckerDocs.sln" /t:Clean;Build "/p:Configuration=Release;Platform=Any CPU"

IF "%SHFBROOT%"=="" ECHO **** Sandcastle help file builder not installed.  Skipping help build. ****

CD NuGet

..\Source\packages\NuGet.CommandLine.4.7.1\tools\NuGet.exe Pack VSSpellChecker.nuspec -NoDefaultExcludes -NoPackageAnalysis -OutputDirectory ..\Deployment

CD ..\

:End

ENDLOCAL
