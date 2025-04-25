using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Messages;
using Shared;

var instanceName = args.FirstOrDefault();

var instanceNumber = args.FirstOrDefault();
string title;

if (string.IsNullOrEmpty(instanceNumber))
{
    title = "Sales";

    instanceNumber = "original-instance";
}
else
{
    title = $"Sales - {instanceNumber}";
}

var instanceId = DeterministicGuid.Create("Sales", instanceNumber);

var endpointConfiguration = new EndpointConfiguration("Sales");
endpointConfiguration.LimitMessageProcessingConcurrencyTo(4);

var serializer = endpointConfiguration.UseSerialization<SystemJsonSerializer>();
serializer.Options(new JsonSerializerOptions
{
    TypeInfoResolverChain =
        {
            MessagesSerializationContext.Default
        }
});

var transport = new LearningTransport
{
    StorageDirectory = Path.Combine(Directory.GetParent(Assembly.GetExecutingAssembly().Location)!.Parent!.FullName, ".learningtransport"),
    TransportTransactionMode = TransportTransactionMode.ReceiveOnly
};
endpointConfiguration.UseTransport(transport);

endpointConfiguration.AuditProcessedMessagesTo("audit");
endpointConfiguration.SendHeartbeatTo("Particular.ServiceControl");

endpointConfiguration.UniquelyIdentifyRunningInstance()
    .UsingCustomDisplayName(instanceName)
    .UsingCustomIdentifier(instanceId);

var metrics = endpointConfiguration.EnableMetrics();
metrics.SendMetricDataToServiceControl(
    "Particular.Monitoring",
    TimeSpan.FromMilliseconds(500)
);

var failureSimulation = new ProcessingEndpointControls();
failureSimulation.Register(endpointConfiguration);

endpointConfiguration.UsePersistence<NonDurablePersistence>();
endpointConfiguration.EnableOutbox();

Debugger.Launch();

var ui = new UserInterface();
failureSimulation.BindSlowProcessingDial(ui, '2', 'w');
failureSimulation.BindDatabaseFailuresDial(ui, '3', 'e');
failureSimulation.BindFailureReceivingButton(ui, 'z');
failureSimulation.BindFailureProcessingButton(ui, 'x');
failureSimulation.BindFailureDispatchingButton(ui, 'c');

var endpointInstance = await Endpoint.Start(endpointConfiguration);

ui.RunLoop(title);

await endpointInstance.Stop();

static class DeterministicGuid
{
    public static Guid Create(params object[] data)
    {
        // use MD5 hash to get a 16-byte hash of the string
        using var provider = MD5.Create();
        var inputBytes = Encoding.Default.GetBytes(string.Concat(data));
        var hashBytes = provider.ComputeHash(inputBytes);
        // generate a guid from the hash:
        return new Guid(hashBytes);
    }
}