@ECHO OFF
CLS

CD Source

IF "%VS100COMNTOOLS%"=="" GOTO VS2012

"%WINDIR%\Microsoft.Net\Framework\v4.0.30319\msbuild.exe" "VSSpellChecker.sln" /nologo /v:m /m /t:Clean;Build "/p:Configuration=Release;Platform=Any CPU"
GOTO End

:VS2012
IF "%VS110COMNTOOLS%"=="" GOTO VS2013

"%WINDIR%\Microsoft.Net\Framework\v4.0.30319\msbuild.exe" "VSSpellChecker_2013.sln" /nologo /v:m /m /t:Clean;Build "/p:VisualStudioVersion=11.0;Configuration=Release;Platform=Any CPU"
GOTO End

:VS2013
"%WINDIR%\Microsoft.Net\Framework\v4.0.30319\msbuild.exe" "VSSpellChecker_2013.sln" /nologo /v:m /m /t:Clean;Build "/p:VisualStudioVersion=12.0;Configuration=Release;Platform=Any CPU"

:End
CD ..\
