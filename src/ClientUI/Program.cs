using System.Text.Json;
using ClientUI;
using Messages;
using Shared;

LoggingUtils.ConfigureLogging("ClientUI");

var endpointConfiguration = new EndpointConfiguration("ClientUI");

var serializer = endpointConfiguration.UseSerialization<SystemJsonSerializer>();
serializer.Options(new JsonSerializerOptions
{
    TypeInfoResolverChain =
        {
            MessagesSerializationContext.Default
        }
});

var transport = endpointConfiguration.UseTransport<LearningTransport>();

endpointConfiguration.AuditProcessedMessagesTo("audit");
endpointConfiguration.SendHeartbeatTo("Particular.ServiceControl");

var instanceId = "EA3E7D1B-8171-4098-B160-1FEA975CCB2C";
endpointConfiguration.UniquelyIdentifyRunningInstance()
    .UsingCustomIdentifier(new Guid(instanceId))
    .UsingCustomDisplayName("original-instance");

endpointConfiguration.ConfigureOpenTelemetry("Sales", instanceId, 9130);

var metrics = endpointConfiguration.EnableMetrics();
metrics.SendMetricDataToServiceControl(
    "Particular.Monitoring",
    TimeSpan.FromMilliseconds(500)
);

var routing = transport.Routing();
routing.RouteToEndpoint(typeof(PlaceOrder), "Sales");

var endpointInstance = await Endpoint.Start(endpointConfiguration);

var simulatedCustomers = new SimulatedCustomers(endpointInstance);
var cancellation = new CancellationTokenSource();
var simulatedWork = simulatedCustomers.Run(cancellation.Token);

var nonInteractive = args.Length > 1 && args[1] == bool.FalseString;
var interactive = !nonInteractive;

UserInterface.RunLoop("Load (ClientUI)", new Dictionary<char, (string, Action)>
{
    ['c'] = ("toggle High/Low traffic mode", () => simulatedCustomers.ToggleTrafficMode()),
}, writer => simulatedCustomers.WriteState(writer), interactive);

cancellation.Cancel();

await simulatedWork;

await endpointInstance.Stop();
