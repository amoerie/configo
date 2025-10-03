var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .AddDatabase("Configo");

builder.AddProject<Projects.Configo_Server>("server")
    .WithReference(postgres)
    .WithEnvironment("CONFIGO_PROVIDER", "Postgres");

builder.Build().Run();
