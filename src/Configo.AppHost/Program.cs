var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .AddDatabase("configodb");

builder.AddProject<Projects.Configo_Server>("server")
    .WithReference(postgres);

builder.Build().Run();
