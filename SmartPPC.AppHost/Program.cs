using Projects;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<SmartPPC_Api>("PPC-API");

builder.Build().Run();
