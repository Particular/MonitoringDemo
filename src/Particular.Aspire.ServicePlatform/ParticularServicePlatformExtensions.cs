using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Particular.Aspire.ServicePlatform;

public static class ParticularServicePlatformExtensions
{
    public static ServicePlatform AddParticularServicePlatform(this IDistributedApplicationBuilder builder)
        => AddParticularServicePlatform(builder, new ServicePlatformOptionsBuilder().Build());

    public static ServicePlatform AddParticularServicePlatform(
        this IDistributedApplicationBuilder builder,
        ServicePlatformOptions options
    )
    {
        var ravendb = options.RavenDbSettings.BuildContainer(builder);

        var auditInstance = options.AuditInstanceSettings.BuildContainer(builder, options, ravendb);

        var errorInstance = options.ErrorInstanceSettings.BuildContainer(builder, options, ravendb, auditInstance);

        var monitoringInstance = options.MonitoringInstanceSettings.BuildContainer(builder, options);

        var servicePulse = options.ServicePulseInstanceSettings.BuildContainer(builder, options, errorInstance, monitoringInstance);

        ravendb.WithParentRelationship(servicePulse);
        auditInstance.WithParentRelationship(servicePulse);
        errorInstance.WithParentRelationship(servicePulse);
        monitoringInstance.WithParentRelationship(servicePulse);

        return new ServicePlatform
        {
            AuditInstance = auditInstance,
            ErrorInstance = errorInstance,
            MonitoringInstance = monitoringInstance,
            ServicePulse = servicePulse,
            RavenDb = ravendb
        };
    }

    internal static IResourceBuilder<ContainerResource> AddTransport(this IResourceBuilder<ContainerResource> builder, ServicePlatformTransport transport)
        => transport.AddTo(builder);

    internal static IResourceBuilder<ContainerResource> AddLicense(this IResourceBuilder<ContainerResource> builder, ServicePlatformLicense license)
        => license.AddTo(builder);

    internal static IResourceBuilder<ContainerResource> SetupAndRun(this IResourceBuilder<ContainerResource> builder)
        => builder.WithArgs("--setup-and-run");

    internal static IResourceBuilder<ContainerResource> AddRavenConnection(this IResourceBuilder<ContainerResource> builder, ServicePlatformRavenDbSettings ravenDbSettings)
        => ravenDbSettings.ConnectTo(builder);


}