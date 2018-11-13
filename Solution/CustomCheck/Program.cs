using System;
using System.Configuration;
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
        endpointConfiguration.UseSerialization<NServiceBus.NewtonsoftSerializer>();
        endpointConfiguration.EnableInstallers();
        endpointConfiguration.UsePersistence<InMemoryPersistence>();

        var connectionString = ConfigurationManager.ConnectionStrings["NServiceBus/Transport"].ConnectionString;
        endpointConfiguration.UseTransport<SqlServerTransport>()
            .ConnectionString(connectionString);
        endpointConfiguration.Recoverability()
            .Delayed(delayed => delayed.NumberOfRetries(0));

        var metrics = endpointConfiguration.EnableMetrics();
        metrics.SendMetricDataToServiceControl(
            "Particular.Monitoring",
            TimeSpan.FromMilliseconds(500),
            "original-instance"
        );

        endpointConfiguration.SendHeartbeatTo(
            serviceControlQueue: "Particular.ServiceControl");

        endpointConfiguration.ReportCustomChecksTo("Particular.ServiceControl");

        await Endpoint.Start(endpointConfiguration).ConfigureAwait(false);

        Console.WriteLine("Press any key to exit");
        Console.ReadKey();
    }
}