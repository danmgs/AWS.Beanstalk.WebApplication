md output
dotnet publish ..\AWS.Beanstalk.WebApplication.csproj -o .\output\site
pushd output
pushd site
move .ebextensions ..\
"C:\Program Files\7-Zip\7z.exe" -tzip a ../site.zip .
popd
RMDIR "site" /S /Q
pause
