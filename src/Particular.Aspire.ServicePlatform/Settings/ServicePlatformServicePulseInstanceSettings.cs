using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Particular.Aspire.ServicePlatform;

public record ServicePlatformServicePulseInstanceSettings
{
    public string Name { get; init; } = "servicepulse";
    public string Image { get; init; } = "particular/servicepulse";
    public string Tag { get; init; } = "latest";
    public int BindPort { get; init; } = 9090;
    public ContainerLifetime Lifetime { get; init; } = ContainerLifetime.Persistent;

    internal IResourceBuilder<ContainerResource> BuildContainer(IDistributedApplicationBuilder builder, ServicePlatformOptions options, IResourceBuilder<ContainerResource> errorInstance, IResourceBuilder<ContainerResource> monitoringInstance)
    => builder.AddContainer(Name, Image, Tag)
            .WithLifetime(Lifetime)
            .WithHttpEndpoint(BindPort, 9090)
            .AddLicense(options.License)
            .WithEnvironment("SERVICECONTROL_URL", options.ErrorInstanceSettings.ConnectionString)
            .WithEnvironment("MONITORING_URL", options.MonitoringInstanceSettings.ConnectionString)
            .WaitFor(errorInstance)
            .WaitFor(monitoringInstance);
}
