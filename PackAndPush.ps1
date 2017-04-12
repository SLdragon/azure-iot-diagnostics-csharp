$root = (split-path -parent $MyInvocation.MyCommand.Definition);
cd $root;

$version = [System.Reflection.Assembly]::LoadFile("$root\IoTDeviceSDKWrapper\bin\Release\IoTDeviceSDKWrapper.dll").GetName().Version
$versionStr = "{0}.{1}.{2}" -f ($version.Major, $version.Minor, $version.Build)

Write-Host "Setting .nuspec version tag to $versionStr"
$content = (Get-Content $root\Microsoft.Azure.Devices.Client.Diagnostic.nuspec) 
$content = $content -replace '\$version\$',$versionStr
$content | Out-File $root\package.nuspec

nuget pack .\package.nuspec

$packageName= "Microsoft.Azure.Devices.Client.Diagnostic.$versionStr.nupkg"

appveyor PushArtifact $packageName

Remove-Item .\package.nuspec


