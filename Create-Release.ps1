param (
    [string]$config = $(throw "config (Release/Dev) is required")
 )

Function GetMSBuildExe {
 	[CmdletBinding()]
 	$DotNetVersion = "4.0"
 	$RegKey = "HKLM:\software\Microsoft\MSBuild\ToolsVersions\$DotNetVersion"
 	$RegProperty = "MSBuildToolsPath"
 	$MSBuildExe = Join-Path -Path (Get-ItemProperty $RegKey).$RegProperty -ChildPath "msbuild.exe"
 	Return $MSBuildExe
 }

&(GetMSBuildExe) Espera.Android\Espera.Android.csproj /p:Configuration=$config /t:Clean
&(GetMSBuildExe) Espera.Android\Espera.Android.csproj /p:Configuration=$config /t:PackageForAndroid

& 'C:\Program Files (x86)\Java\jdk1.7.0_67\bin\jarsigner.exe' -verbose -sigalg SHA1withRSA -digestalg SHA1  -keystore ./Espera.Mobile.keystore -signedjar ./Espera.Android/bin/$config/com.flagbug.esperamobile-signed.apk ./Espera.Android/bin/$config/com.flagbug.esperamobile.apk Espera

mkdir -Force ./Release
& 'C:\Program Files (x86)\Android\android-sdk\build-tools\20.0.0\zipalign.exe' -f -v 4 ./Espera.Android/bin/$config/com.flagbug.esperamobile-signed.apk ./Release/Espera.Android.apk
