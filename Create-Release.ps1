param (
    [string]$config = $(throw "config (Release/Dev) is required")
 )

$MSBuildLocation = "C:\Program Files (x86)\MSBuild\12.0\bin"

& "$MSBuildLocation\MSBuild.exe" Espera.Android\Espera.Android.csproj /p:Configuration=$config /t:Clean
& "$MSBuildLocation\MSBuild.exe" Espera.Android\Espera.Android.csproj /p:Configuration=$config /t:PackageForAndroid

& 'C:\Program Files (x86)\Java\jdk1.6.0_39\bin\jarsigner.exe' -verbose -sigalg SHA1withRSA -digestalg SHA1  -keystore ./Espera.Mobile.keystore -signedjar ./Espera.Android/bin/$config/com.flagbug.esperamobile-signed.apk ./Espera.Android/bin/$config/com.flagbug.esperamobile.apk Espera

mkdir -Force ./Release
& 'C:\Program Files (x86)\Android\android-sdk\build-tools\20.0.0\zipalign.exe' -f -v 4 ./Espera.Android/bin/$config/com.flagbug.esperamobile-signed.apk ./Release/Espera.Android.apk
