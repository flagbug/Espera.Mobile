$MSBuildLocation = "C:\Program Files (x86)\MSBuild\12.0\bin"

& "$MSBuildLocation\MSBuild.exe" Espera.Android\Espera.Android.csproj /p:Configuration=Release /t:Clean
& "$MSBuildLocation\MSBuild.exe" Espera.Android\Espera.Android.csproj /p:Configuration=Release /t:PackageForAndroid

& 'C:\Program Files (x86)\Java\jdk1.6.0_39\bin\jarsigner.exe' -verbose -sigalg SHA1withRSA -digestalg SHA1  -keystore ./Espera.keystore -signedjar ./Espera.Android/bin/Release/Espera.Android-signed.apk ./Espera.Android/bin/Release/Espera.Android.apk Espera

mkdir ./Release
& 'C:\Program Files (x86)\Android\android-sdk\tools\zipalign.exe' -f -v 4 ./Espera.Android/bin/Release/Espera.Android-signed.apk ./Release/Espera.Android.apk