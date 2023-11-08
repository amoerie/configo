#requires -PSEdition Core

Push-Location
try
{
    $ProjectPath = (Join-Path $PSScriptRoot "../..")
    cd $ProjectPath

    dotnet tool restore
    dotnet dotnet-ef migrations remove --project "../Configo.Migrations.SqlServer" --verbose --context ConfigoDbContext -- --provider SqlServer
    dotnet dotnet-ef migrations remove --project "../Configo.Migrations.NpgSql" --verbose --context ConfigoDbContext -- --provider Postgres
}
finally {
    Pop-Location
}

