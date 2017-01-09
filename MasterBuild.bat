@ECHO OFF

SETLOCAL

CD Source

ECHO *
ECHO * VS2013/VS2015 package
ECHO *

REM Use the earliest version of MSBuild available
IF EXIST "%ProgramFiles(x86)%\MSBuild\14.0" SET "MSBUILD=%ProgramFiles(x86)%\MSBuild\12.0\bin\MSBuild.exe"
IF EXIST "%ProgramFiles(x86)%\MSBuild\12.0" SET "MSBUILD=%ProgramFiles(x86)%\MSBuild\12.0\bin\MSBuild.exe"

..\Source\.nuget\NuGet restore "VSSpellChecker2013.sln"

"%MSBUILD%" "VSSpellChecker2013.sln" /nologo /v:m /m /t:Clean;Build "/p:Configuration=Release;Platform=Any CPU"

IF ERRORLEVEL 1 GOTO End

ECHO *
ECHO * VS2017 and later package
ECHO *

REM Use MSBuild from whatever edition of Visual Studio is installed
IF EXIST "%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Community\MSBuild\15.0" SET "MSBUILD=%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Community\MSBuild\15.0\bin\MSBuild.exe"
IF EXIST "%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Developer\MSBuild\15.0" SET "MSBUILD=%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Developer\MSBuild\15.0\bin\MSBuild.exe"
IF EXIST "%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0" SET "MSBUILD=%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\bin\MSBuild.exe"

IF NOT EXIST "%MSBUILD%" GOTO End

..\Source\.nuget\NuGet restore "VSSpellChecker2017AndLater.sln"

"%MSBUILD%" "VSSpellChecker2017AndLater.sln" /nologo /v:m /m /t:Clean;Build "/p:Configuration=Release;Platform=Any CPU"

IF ERRORLEVEL 1 GOTO End

:NuGet

CD ..\NuGet

..\Source\.nuget\NuGet Pack VSSpellChecker.nuspec -NoDefaultExcludes -NoPackageAnalysis -OutputDirectory ..\Deployment

CD ..\

:End

ENDLOCAL
