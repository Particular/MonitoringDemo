namespace Particular.Aspire.ServicePlatform;

public record ServicePlatformOptions
{
    public required ServicePlatformLicense License { get; init; }
    public required ServicePlatformTransport Transport { get; init; }
    public required ServicePlatformRavenDbSettings RavenDbSettings { get; set; }
    public required ServicePlatformErrorInstanceSettings ErrorInstanceSettings { get; init; }
    public required ServicePlatformAuditInstanceSettings AuditInstanceSettings { get; init; }
    public required ServicePlatformMonitoringInstanceSettings MonitoringInstanceSettings { get; init; }
    public required ServicePlatformServicePulseInstanceSettings ServicePulseInstanceSettings { get; init; }
}
