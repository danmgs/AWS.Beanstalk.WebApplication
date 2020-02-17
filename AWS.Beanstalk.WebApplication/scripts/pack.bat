md output
dotnet publish ..\AWS.Beanstalk.WebApplication.csproj -o .\output\site
pushd output
pushd site
"C:\Program Files\7-Zip\7z.exe" a ../site.zip *.*
popd
move .\site\.ebextensions .
copy ..\aws-windows-deployment-manifest.json .
"C:\Program Files\7-Zip\7z.exe" -tzip a bundle.zip . -x!site
pause
