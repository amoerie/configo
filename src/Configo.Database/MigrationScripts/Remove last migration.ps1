#requires -PSEdition Core

Push-Location
try
{
    $ProjectPath = (Join-Path $PSScriptRoot "../../Configo")
    cd $ProjectPath

    dotnet tool restore
    dotnet dotnet-ef migrations remove --project "../Configo.Database.SqlServer" --verbose --context ConfigoDbContext -- --provider SqlServer
    dotnet dotnet-ef migrations remove --project "../Configo.Database.NpgSql" --verbose --context ConfigoDbContext -- --provider Postgres
}
finally {
    Pop-Location
}

