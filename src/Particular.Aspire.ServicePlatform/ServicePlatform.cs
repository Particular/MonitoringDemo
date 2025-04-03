using Aspire.Hosting.ApplicationModel;

namespace Particular.Aspire.ServicePlatform;

public record ServicePlatform
{
    public required IResourceBuilder<ContainerResource> RavenDb { get; init; }
    public required IResourceBuilder<ContainerResource> ErrorInstance { get; init; }
    public required IResourceBuilder<ContainerResource> AuditInstance { get; init; }
    public required IResourceBuilder<ContainerResource> MonitoringInstance { get; init; }
    public required IResourceBuilder<ContainerResource> ServicePulse { get; init; }
}
