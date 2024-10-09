static class ParticularServicePlatformExtensions
{
    public static void AddParticularServicePlatform(this IDistributedApplicationBuilder builder)
    {
        var storagePath = FindStoragePath();
        var license = File.ReadAllText(@"C:\ProgramData\ParticularSoftware\license.xml");

        builder.AddContainer("servicecontroldb", "particular/servicecontrol-ravendb", "latest")
            .WithBindMount("AppHost-servicecontroldb-data", "/opt/RavenDB/Server/RavenData")
            .WithEndpoint(8080, 8080);

        builder.AddContainer("servicecontrol", "particular/servicecontrol")
            // Learning transport
            .WithBindMount(storagePath, "/var/lib/nservicebus/transport")
            .WithEnvironment("TransportType", "LearningTransport")
            .WithEnvironment("ConnectionString", "/var/lib/nservicebus/transport")
            // End learning transport
            .WithEnvironment("RavenDB_ConnectionString", "http://host.docker.internal:8080")
            .WithEnvironment("RemoteInstances", "[{\"api_uri\":\"http://host.docker.internal:44444/api\"}]")
            .WithEnvironment("PARTICULARSOFTWARE_LICENSE", license)
            .WithArgs("--setup-and-run")
            // See https://www.jimmybogard.com/integrating-the-particular-service-platform-with-aspire/
            //.WithContainerRuntimeArgs("-p", "33333:33333")
            .WithEndpoint(33333, 33333);

        builder.AddContainer("servicecontrolaudit", "particular/servicecontrol-audit")
            // Learning transport
            .WithBindMount(storagePath, "/var/lib/nservicebus/transport")
            .WithEnvironment("TransportType", "LearningTransport")
            .WithEnvironment("ConnectionString", "/var/lib/nservicebus/transport")
            // End learning transport
            .WithEnvironment("RavenDB_ConnectionString", "http://host.docker.internal:8080")
            .WithEnvironment("PARTICULARSOFTWARE_LICENSE", license)
            .WithArgs("--setup-and-run")
            .WithEndpoint(44444, 44444);

        builder.AddContainer("servicecontrolmonitoring", "particular/servicecontrol-monitoring")
            // Learning transport
            .WithBindMount(storagePath, "/var/lib/nservicebus/transport")
            .WithEnvironment("TransportType", "LearningTransport")
            .WithEnvironment("ConnectionString", "/var/lib/nservicebus/transport")
            // End learning transport
            .WithEnvironment("PARTICULARSOFTWARE_LICENSE", license)
            .WithArgs("--setup-and-run")
            .WithEndpoint(33633, 33633);

        builder.AddContainer("servicepulse", "particular/servicepulse")
             .WithEnvironment("SERVICECONTROL_URL", "http://host.docker.internal:33333")
             .WithEnvironment("MONITORING_URL", "http://host.docker.internal:33633")
             .WithEnvironment("PARTICULARSOFTWARE_LICENSE", license)
             .WithHttpEndpoint(9090, 9090);
    }

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