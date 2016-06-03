$SourcesPath = $Env:BUILD_SOURCESDIRECTORY
$FilePath = "$SourcesPath\src\External\GlobalAssemblyInfo.cs"
$Content = Get-Content $FilePath

$BuildNumber = $Env:BUILD_BUILDNUMBER
$BuildParts = $BuildNumber.Split('.')
$Major = $BuildParts[0]
$Minor = $BuildParts[1]
$Version = "$Major.$Minor"

Write-Verbose "Replacing assembly version with build number: $BuildNumber" -Verbose
$Content = $Content.Replace("""1.0.0.0""", """$BuildNumber""").Replace("""1.0""", """$Version""")

Write-Verbose "Saving GlobalAssemblyInfo.cs to $FilePath" -Verbose
$Content | Out-File -FilePath $FilePath
