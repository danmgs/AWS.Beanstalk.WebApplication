md output
dotnet publish ..\AWS.Beanstalk.WebApplication.csproj -o .\output\site
pushd output
pushd site
move .ebextensions ..\
"C:\Program Files\7-Zip\7z.exe" -tzip a ../site.zip .
popd
copy ..\aws-windows-deployment-manifest.json .
"C:\Program Files\7-Zip\7z.exe" -tzip a bundle.zip . -x!site
pause
