var builder = DistributedApplication.CreateBuilder(args);

var configoDbUser = builder.AddParameter("DatabaseUser");
var configoDbPassword = builder.AddParameter("DatabasePassword");
var configoDb = builder.AddPostgres("configo-db")
    .WithUserName(configoDbUser)
    .WithPassword(configoDbPassword)
    .WithHostPort(51300)
    .WithContainerName("configo_db")
    .WithDataVolume("configo_db_data")
    .WithLifetime(ContainerLifetime.Persistent);

var tenantId = builder.AddParameter("TenantId");
var clientId = builder.AddParameter("ClientId");
var clientSecret = builder.AddParameter("ClientSecret");

builder.AddProject<Projects.Configo_Server>("configo-server")
    .WithReference(configoDb, "Configo")
    .WithEnvironment("CONFIGO_PROVIDER", "Postgres")
    .WithEnvironment("CONFIGO_AUTHENTICATION__MICROSOFT__TENANTID", tenantId)
    .WithEnvironment("CONFIGO_AUTHENTICATION__MICROSOFT__CLIENTID", clientId)
    .WithEnvironment("CONFIGO_AUTHENTICATION__MICROSOFT__CLIENTSECRET", clientSecret);

builder.Build().Run();
