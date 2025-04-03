using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Particular.Aspire.ServicePlatform;

public record ServicePlatformAuditInstanceSettings
{
    public string Name { get; init; } = "servicecontrolaudit";
    public string Image { get; init; } = "particular/servicecontrol-audit";
    public string Tag { get; init; } = "latest";
    public int BindPort { get; init; } = 44444;
    public ContainerLifetime Lifetime { get; init; } = ContainerLifetime.Persistent;
    public string ApiUrl => $"http://host.docker.internal:{BindPort}/api";

    internal IResourceBuilder<ContainerResource> BuildContainer(IDistributedApplicationBuilder builder, ServicePlatformOptions options, IResourceBuilder<ContainerResource> ravendb)
        => builder.AddContainer(Name, Image, Tag)
                .WithLifetime(Lifetime)
                .WithHttpEndpoint(BindPort, 44444)
                .AddTransport(options.Transport)
                .AddLicense(options.License)
                .AddRavenConnection(options.RavenDbSettings)
                .SetupAndRun()
                .WaitFor(ravendb);
}
