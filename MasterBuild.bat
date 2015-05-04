@ECHO OFF
CLS

CD Source

REM Enforce use of VS 2010 SDK if present.  If not it tries to use the VS 2011 SDK even if its not there.
IF EXIST "%ProgramFiles(x86)%\MSBuild\Microsoft\VisualStudio\v10.0\VSSDK\Microsoft.VsSDK.targets" SET VisualStudioVersion=10.0

..\Source\.nuget\NuGet restore "VSSpellChecker.sln"

"%WINDIR%\Microsoft.Net\Framework\v4.0.30319\msbuild.exe" "VSSpellChecker.sln" /nologo /v:m /m /t:Clean;Build "/p:Configuration=Release;Platform=Any CPU"

CD ..\NuGet

..\Source\.nuget\NuGet Pack VSSpellChecker.nuspec -NoDefaultExcludes -NoPackageAnalysis -OutputDirectory ..\Deployment

CD ..\
