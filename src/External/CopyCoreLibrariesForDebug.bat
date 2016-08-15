echo Copying Core libraries for debugging...

pushd "%~dp0..\packages"

for /D %%f in (PDS.Framework.20*) do (
	@if exist %%f\lib\net45 (
	    copy "%~dp0..\..\..\witsml\src\Framework\bin\Debug\PDS.Framework.*" %%f\lib\net45
	)
)

for /D %%f in (PDS.Witsml.20*) do (
	@if exist %%f\lib\net45 (
	    copy "%~dp0..\..\..\witsml\src\Witsml\bin\Debug\PDS.Witsml.*" %%f\lib\net45
	)
)

popd