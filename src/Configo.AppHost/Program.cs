var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithUserName(builder.AddParameter("DatabaseUser"))
    .WithPassword(builder.AddParameter("DatabasePassword"))
    .WithContainerName("configo_db")
    .WithDataVolume("configo_db_data")
    .WithLifetime(ContainerLifetime.Persistent)
    .AddDatabase("Configo");

builder.AddProject<Projects.Configo_Server>("server")
    .WithReference(postgres)
    .WithEnvironment("CONFIGO_PROVIDER", "Postgres");

builder.Build().Run();
