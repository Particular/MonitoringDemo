using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Particular.Aspire.ServicePlatform;

public record ServicePlatformMonitoringInstanceSettings
{
    public string Name { get; init; } = "servicecontrolmonitoring";
    public string Image { get; init; } = "particular/servicecontrol-monitoring";
    public string Tag { get; init; } = "latest";
    public int BindPort { get; init; } = 33633;
    public ContainerLifetime Lifetime { get; init; } = ContainerLifetime.Persistent;
    public string ConnectionString => $"http://host.docker.internal:{BindPort}";

    internal IResourceBuilder<ContainerResource> BuildContainer(IDistributedApplicationBuilder builder, ServicePlatformOptions options)
        => builder.AddContainer(Name, Image, Tag)
                .WithLifetime(Lifetime)
                .WithHttpEndpoint(BindPort, 33633)
                .AddTransport(options.Transport)
                .AddLicense(options.License)
                .SetupAndRun();
}
