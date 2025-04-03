using Particular.Aspire.ServicePlatform;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.ClientUI>("clientui");
builder.AddProject<Projects.Sales>("sales");
builder.AddProject<Projects.Billing>("billing");
builder.AddProject<Projects.Shipping>("shipping");

builder.AddParticularServicePlatform();

builder.Build().Run();
