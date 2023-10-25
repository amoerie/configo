#requires -PSEdition Core

Push-Location 
try
{
    $ProjectPath = (Join-Path $PSScriptRoot "../..")

    cd $ProjectPath

    $MigrationName = Read-Host "Migration Name"

    dotnet tool restore
    dotnet dotnet-ef migrations add "$MigrationName" --output-dir "Database/Migrations" --verbose --context ConfigoDbContext
}
finally {
    Pop-Location
}

