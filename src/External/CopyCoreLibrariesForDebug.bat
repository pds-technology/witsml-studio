echo Copying Core libraries for debugging...

pushd "%~dp0..\packages\PDS.Framework.*\lib\net46\"
copy "%~dp0..\..\..\witsml\src\Framework\bin\Debug\PDS.Framework.*"
popd

pushd "%~dp0..\packages\PDS.Witsml.*\lib\net46\"
copy "%~dp0..\..\..\witsml\src\Witsml\bin\Debug\PDS.Witsml.*"
popd
