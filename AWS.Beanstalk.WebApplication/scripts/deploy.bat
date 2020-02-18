pushd deploy
RMDIR ".ebextensions" /S /Q
move ..\output\.ebextensions .
move ..\output\site.zip .
RMDIR "..\output" /S /Q
eb deploy
pause
