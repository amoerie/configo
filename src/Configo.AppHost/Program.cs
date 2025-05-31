var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Configo_Server>("server");

builder.Build().Run();
