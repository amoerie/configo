#requires -PSEdition Core

Push-Location
try
{
    $ProjectPath = (Join-Path $PSScriptRoot "../..")
    cd $ProjectPath

    dotnet tool restore
    dotnet dotnet-ef database update --verbose --context ConfigoDbContext
}
finally {
    Pop-Location
}

