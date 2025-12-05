using Projects;

var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL server WITHOUT data persistence (for testing)
// Aspire automatically manages the postgres superuser credentials
// TODO: Add .WithDataVolume("smartppc-postgres-aspire-data") after verifying this works
var postgres = builder.AddPostgres("smartppc-postgres")
    .WithPgAdmin();

// Add the database
var database = postgres.AddDatabase("smartppc");

// Add API project with database reference
builder.AddProject<SmartPPC_Api>("PPC-API")
    .WithReference(database);

builder.Build().Run();
