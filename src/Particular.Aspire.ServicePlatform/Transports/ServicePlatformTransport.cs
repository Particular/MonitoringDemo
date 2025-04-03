using Aspire.Hosting.ApplicationModel;

namespace Particular.Aspire.ServicePlatform;

public abstract class ServicePlatformTransport
{
    public abstract IResourceBuilder<ContainerResource> AddTo(IResourceBuilder<ContainerResource> builder);

    public static ServicePlatformTransport Learning(string? path = null)
        => new ServicePlatformLearningTransport(path);

    public static ServicePlatformTransport RabbitMq(IResourceBuilder<IResourceWithConnectionString> rabbitMq)
        => new ServicePlatformResourceTransport("RabbitMQ.QuorumConventionalRouting", rabbitMq);

    public static ServicePlatformTransport SqlServer(IResourceBuilder<IResourceWithConnectionString> sqlDatabase)
        => new ServicePlatformResourceTransport("SQLServer", sqlDatabase);
}
