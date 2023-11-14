#requires -PSEdition Core

$projectNames = ("Configo.Client")
[xml]$props = Get-Content (Resolve-Path (Join-Path $PSScriptRoot "Directory.Build.props"))
$version = $props.Project.PropertyGroup.Version

foreach($projectName in $projectNames) 
{
    $projectPath = Resolve-Path (Join-Path $PSScriptRoot "./$projectName/")
    $csProjPath = Resolve-Path (Join-Path $projectPath "$projectName.csproj")
    
    Write-Host "Packing version $version"
    
    dotnet pack $csprojPath --configuration Release -p:ContinuousIntegrationBuild=true
    
    $nupkgFile = Resolve-Path (Join-Path "$projectPath/bin/Release" "$projectName.$version.nupkg")
    
    Write-Host "Publishing NuGet package file"
    
    nuget push $nupkgFile -skipduplicate -source nuget
}
