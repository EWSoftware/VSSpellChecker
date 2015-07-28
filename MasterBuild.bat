@ECHO OFF
CLS

CD Source

REM Use Visual Studio 2015 references if Visual Studio 2013 is not present
IF NOT EXIST "%VS120COMNTOOLS%..\IDE\devenv.exe" SET VisualStudioVersion=14.0
IF EXIST "%VS120COMNTOOLS%..\IDE\devenv.exe" SET VisualStudioVersion=12.0

..\Source\.nuget\NuGet restore "VSSpellChecker.sln"

"%ProgramFiles(x86)%\MSBuild\12.0\bin\MSBuild.exe" "VSSpellChecker.sln" /nologo /v:m /m /t:Clean;Build "/p:Configuration=Release;Platform=Any CPU"

CD ..\NuGet

..\Source\.nuget\NuGet Pack VSSpellChecker.nuspec -NoDefaultExcludes -NoPackageAnalysis -OutputDirectory ..\Deployment

CD ..\
