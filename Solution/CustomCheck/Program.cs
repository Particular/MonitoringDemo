using System;
using System.Threading.Tasks;
using NServiceBus;

class Program
{
    static async Task Main()
    {
        Console.Title = "CustomCheck.Monitor3rdParty";
        var endpointConfiguration = new EndpointConfiguration("CustomCheck.Monitor3rdParty");
        endpointConfiguration.AuditProcessedMessagesTo("audit");
        endpointConfiguration.SendFailedMessagesTo("error");
        endpointConfiguration.UseSerialization<JsonSerializer>();
        endpointConfiguration.EnableInstallers();
        endpointConfiguration.UsePersistence<InMemoryPersistence>();
        var transport = endpointConfiguration.UseTransport<SqlServerTransport>();
        transport.ConnectionStringName("NServiceBus/Transport");

        endpointConfiguration.Recoverability()
            .Delayed(delayed => delayed.NumberOfRetries(0));

        //endpointConfiguration.AuditProcessedMessagesTo("audit");
        var metrics = endpointConfiguration.EnableMetrics();
        metrics.SendMetricDataToServiceControl(
            "Particular.Monitoring",
            TimeSpan.FromMilliseconds(500),
            "original-instance"
        );

        endpointConfiguration.HeartbeatPlugin(
            serviceControlQueue: "Particular.ServiceControl");

        endpointConfiguration.CustomCheckPlugin("Particular.ServiceControl");

        await Endpoint.Start(endpointConfiguration).ConfigureAwait(false);

        Console.WriteLine("Press any key to exit");
        Console.ReadKey();
    }
}