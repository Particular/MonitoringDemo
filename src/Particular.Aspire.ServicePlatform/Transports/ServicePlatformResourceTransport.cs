using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Particular.Aspire.ServicePlatform;

class ServicePlatformResourceTransport(string transportType, IResourceBuilder<IResourceWithConnectionString> resource) : ServicePlatformTransport
{
    public override IResourceBuilder<ContainerResource> AddTo(IResourceBuilder<ContainerResource> builder)
        => builder
            .WithEnvironment("TransportType", transportType)
            .WithEnvironment(context => context.EnvironmentVariables["ConnectionString"] = new ConnectionStringReference(resource.Resource, false))
            .WaitFor(resource);

}