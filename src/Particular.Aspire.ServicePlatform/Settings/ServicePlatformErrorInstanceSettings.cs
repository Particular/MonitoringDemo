using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Particular.Aspire.ServicePlatform;

public record ServicePlatformErrorInstanceSettings
{
    public string Name { get; init; } = "servicecontrol";
    public string Image { get; init; } = "particular/servicecontrol";
    public string Tag { get; init; } = "latest";
    public int BindPort { get; init; } = 33333;
    public ContainerLifetime Lifetime { get; init; } = ContainerLifetime.Persistent;
    public string ConnectionString => $"http://host.docker.internal:{BindPort}";

    static string GetRemoteInstancesValue(ServicePlatformOptions options)
        => $"[{{\"api_uri\":\"{options.AuditInstanceSettings.ApiUrl}\"}}]";

    internal IResourceBuilder<ContainerResource> BuildContainer(IDistributedApplicationBuilder builder, ServicePlatformOptions options, IResourceBuilder<ContainerResource> ravendb, IResourceBuilder<ContainerResource> auditInstance)
    => builder.AddContainer(Name, Image, Tag)
        .WithLifetime(Lifetime)
        .WithHttpEndpoint(BindPort, 33333)
        .AddTransport(options.Transport)
        .AddLicense(options.License)
        .AddRavenConnection(options.RavenDbSettings)
        .SetupAndRun()
        .WithEnvironment("RemoteInstances", GetRemoteInstancesValue(options))
        .WaitFor(ravendb)
        .WaitFor(auditInstance);
}
