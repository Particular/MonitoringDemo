using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Particular.Aspire.ServicePlatform;

class ServicePlatformLearningTransport(string? storagePath = null) : ServicePlatformTransport
{
    public override IResourceBuilder<ContainerResource> AddTo(IResourceBuilder<ContainerResource> builder)
        => builder.WithEnvironment("TransportType", "LearningTransport")
                  .WithEnvironment("ConnectionString", "/var/lib/nservicebus/transport")
                  .WithBindMount(storagePath ?? FindStoragePath(), "/var/lib/nservicebus/transport");


    const string DefaultLearningTransportDirectory = ".learningtransport";
    static string FindStoragePath()
    {
        var directory = AppDomain.CurrentDomain.BaseDirectory;

        while (true)
        {
            // Finding a solution file takes precedence
            if (Directory.EnumerateFiles(directory).Any(file => file.EndsWith(".sln")))
            {
                return Path.Combine(directory, DefaultLearningTransportDirectory);
            }

            // When no solution file was found try to find a learning transport directory
            var learningTransportDirectory = Path.Combine(directory, DefaultLearningTransportDirectory);
            if (Directory.Exists(learningTransportDirectory))
            {
                return learningTransportDirectory;
            }

            var parent = Directory.GetParent(directory) ?? throw new Exception($"Unable to determine the storage directory path for the learning transport due to the absence of a solution file. Either create a '{DefaultLearningTransportDirectory}' directory in one of this project’s parent directories, or specify the path explicitly using the 'EndpointConfiguration.UseTransport<LearningTransport>().StorageDirectory()' API.");

            directory = parent.FullName;
        }
    }
}
