@ECHO OFF

SETLOCAL

CD Source

ECHO *
ECHO * VS2017/2019 and VS2022 and Later packages
ECHO *

REM Use MSBuild from whatever edition of Visual Studio is installed
IF EXIST "%ProgramFiles%\Microsoft Visual Studio\2022\Preview\MSBuild\Current" SET "MSBUILD=%ProgramFiles%\Microsoft Visual Studio\2022\Preview\MSBuild\Current\bin\MSBuild.exe"
IF EXIST "%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Community\MSBuild\15.0" SET "MSBUILD=%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Community\MSBuild\15.0\bin\MSBuild.exe"
IF EXIST "%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Professional\MSBuild\15.0" SET "MSBUILD=%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Professional\MSBuild\15.0\bin\MSBuild.exe"
IF EXIST "%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0" SET "MSBUILD=%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\bin\MSBuild.exe"
IF EXIST "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Community\MSBuild\Current" SET "MSBUILD=%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Community\MSBuild\Current\bin\MSBuild.exe"
IF EXIST "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Professional\MSBuild\Current" SET "MSBUILD=%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Professional\MSBuild\Current\bin\MSBuild.exe"
IF EXIST "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current" SET "MSBUILD=%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\bin\MSBuild.exe"

IF NOT EXIST "%MSBUILD%" GOTO End

"%MSBUILD%" /r /nologo /v:m /m /t:Clean;Build "/p:Configuration=Release;Platform=Any CPU" VSSpellChecker.sln

IF ERRORLEVEL 1 GOTO End

CD ..\

IF NOT "%SHFBROOT%"=="" "%MSBUILD%" /nologo /v:m /m /t:Clean;Build "/p:Configuration=Release;Platform=Any CPU" "Docs\VSSpellCheckerDocs.sln"

IF "%SHFBROOT%"=="" ECHO **** Sandcastle help file builder not installed.  Skipping help build. ****

CD NuGet

NuGet Pack VSSpellChecker.nuspec -OutputDirectory ..\Deployment

CD ..\

:End

ENDLOCAL
