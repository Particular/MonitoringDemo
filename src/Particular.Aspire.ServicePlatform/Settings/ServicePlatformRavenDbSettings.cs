using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Particular.Aspire.ServicePlatform;

public record ServicePlatformRavenDbSettings
{
    public string BindMount { get; init; } = "AppHost-servicecontroldb-data";
    public int BindPort { get; init; } = 8080;
    public string Tag { get; init; } = "latest";
    public string Name { get; init; } = "servicecontroldb";
    public string Image { get; init; } = "particular/servicecontrol-ravendb";
    public ContainerLifetime Lifetime { get; init; } = ContainerLifetime.Persistent;
    public string ConnectionString => $"http://host.docker.internal:{BindPort}";

    internal IResourceBuilder<ContainerResource> BuildContainer(IDistributedApplicationBuilder builder)
        => builder.AddContainer(Name, Image, Tag)
            .WithLifetime(Lifetime)
            .WithBindMount(BindMount, "/var/lib/ravendb/data")
            .WithHttpEndpoint(BindPort, 8080);

    internal IResourceBuilder<ContainerResource> ConnectTo(IResourceBuilder<ContainerResource> builder)
        => builder.WithEnvironment("RavenDB_ConnectionString", ConnectionString);
}
