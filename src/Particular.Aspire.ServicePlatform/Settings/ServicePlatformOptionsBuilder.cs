namespace Particular.Aspire.ServicePlatform;

public class ServicePlatformOptionsBuilder
{
    ServicePlatformLicense? license;
    ServicePlatformTransport? transport;
    ServicePlatformRavenDbSettings? ravenDb;
    ServicePlatformErrorInstanceSettings? errorInstance;
    ServicePlatformAuditInstanceSettings? auditInstance;
    ServicePlatformMonitoringInstanceSettings? monitoringInstance;
    ServicePlatformServicePulseInstanceSettings? servicePulse;

    public ServicePlatformOptionsBuilder WithDefaultLicense()
    {
        license = new ServicePlatformDefaultLicense();
        return this;
    }

    public ServicePlatformOptionsBuilder WithLicense(string licenseText)
    {
        license = new ServicePlatformTextLicense(licenseText);
        return this;
    }

    public ServicePlatformOptionsBuilder WithLicenseFromFile(string licensePath)
    {
        license = new ServicePlatformFileLicense(licensePath);
        return this;
    }

    internal ServicePlatformOptionsBuilder WithLicense(ServicePlatformLicense license)
    {
        this.license = license;
        return this;
    }

    public ServicePlatformOptionsBuilder WithLearningTransport(string? path = null)
    {
        transport = new ServicePlatformLearningTransport(path);
        return this;
    }

    public ServicePlatformOptionsBuilder WithTransport(ServicePlatformTransport transport)
    {
        this.transport = transport;
        return this;
    }

    public ServicePlatformOptionsBuilder WithRavenDb(ServicePlatformRavenDbSettings ravenDb)
    {
        this.ravenDb = ravenDb;
        return this;
    }

    public ServicePlatformOptionsBuilder WithErrorInstance(ServicePlatformErrorInstanceSettings errorInstance)
    {
        this.errorInstance = errorInstance;
        return this;
    }

    public ServicePlatformOptionsBuilder WithAuditInstance(ServicePlatformAuditInstanceSettings auditInstance)
    {
        this.auditInstance = auditInstance;
        return this;
    }

    public ServicePlatformOptionsBuilder WithMonitoringInstance(ServicePlatformMonitoringInstanceSettings monitoringInstance)
    {
        this.monitoringInstance = monitoringInstance;
        return this;
    }

    public ServicePlatformOptionsBuilder WithServicePulse(ServicePlatformServicePulseInstanceSettings servicePulse)
    {
        this.servicePulse = servicePulse;
        return this;
    }

    public ServicePlatformOptions Build()
        => new()
        {
            License = license ?? new ServicePlatformDefaultLicense(),
            Transport = transport ?? new ServicePlatformLearningTransport(),
            RavenDbSettings = ravenDb ?? new(),
            ErrorInstanceSettings = errorInstance ?? new(),
            AuditInstanceSettings = auditInstance ?? new(),
            MonitoringInstanceSettings = monitoringInstance ?? new(),
            ServicePulseInstanceSettings = servicePulse ?? new(),
        };
}
