#requires -PSEdition Core

Push-Location 
try
{
    $ProjectPath = (Join-Path $PSScriptRoot "../..")

    cd $ProjectPath

    $MigrationName = Read-Host "Migration Name"

    dotnet tool restore
    dotnet dotnet-ef migrations add "$MigrationName" --project "../Configo.Migrations.SqlServer" --verbose --context ConfigoDbContext -- --provider SqlServer
    dotnet dotnet-ef migrations add "$MigrationName" --project "../Configo.Migrations.NpgSql" --verbose --context ConfigoDbContext -- --provider Postgres
}
finally {
    Pop-Location
}

