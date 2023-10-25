#requires -PSEdition Core

Push-Location
try
{
    $ProjectPath = (Join-Path $PSScriptRoot "../..")
    cd $ProjectPath

    dotnet tool restore
    dotnet dotnet-ef migrations remove --verbose --context ConfigoDbContext
}
finally {
    Pop-Location
}

