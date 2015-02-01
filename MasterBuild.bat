@ECHO OFF
CLS

CD Source

REM Build the package using the lowest Visual Studio version available
IF NOT EXIST "%VS100COMNTOOLS%..\IDE\devenv.exe" GOTO VS2012

"%WINDIR%\Microsoft.Net\Framework\v4.0.30319\msbuild.exe" "VSSpellChecker_2010.sln" /nologo /v:m /m /t:Clean;Build "/p:Configuration=Release;Platform=Any CPU"
GOTO End

:VS2012
IF NOT EXIST "%VS110COMNTOOLS%..\IDE\devenv.exe" GOTO VS2013

"%WINDIR%\Microsoft.Net\Framework\v4.0.30319\msbuild.exe" "VSSpellChecker_2012.sln" /nologo /v:m /m /t:Clean;Build "/p:Configuration=Release;Platform=Any CPU"
GOTO End

:VS2013
"%WINDIR%\Microsoft.Net\Framework\v4.0.30319\msbuild.exe" "VSSpellChecker_2013.sln" /nologo /v:m /m /t:Clean;Build "/p:Configuration=Release;Platform=Any CPU"

:End
CD ..\
