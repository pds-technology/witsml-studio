$SourcesPath = $Env:BUILD_SOURCESDIRECTORY
$FilePath = "$SourcesPath\src\External\GlobalAssemblyInfo.cs"
$Content = Get-Content $FilePath

$BuildNumber = $Env:BUILD_BUILDNUMBER
$BuildParts = $BuildNumber.Split('.')
$Major = $BuildParts[0]
$Minor = $BuildParts[1]
$Version = "$Major.$Minor.0.0"

Write-Host ""
Write-Host "Replacing assembly version with build number: $BuildNumber"
$Content = $Content.Replace("1.0.0.0", $BuildNumber).Replace("""1.0""", """$Version""")

Write-Host "Saving GlobalAssemblyInfo.cs to $FilePath"
$Content | Out-File -FilePath $FilePath

Write-Host ""
Write-Host $Content.Substring($Content.IndexOf("using"))
