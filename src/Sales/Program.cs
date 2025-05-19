using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Messages;
using Shared;

var instancePostfix = args.FirstOrDefault();
var title = string.IsNullOrEmpty(instancePostfix) ? "Processing (Sales)" : $"Sales - {instancePostfix}";
var instanceName = string.IsNullOrEmpty(instancePostfix) ? "sales" : $"sales-{instancePostfix}";
var prometheusPortString = args.Skip(1).FirstOrDefault();

var instanceId = DeterministicGuid.Create("Sales", instanceName);

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
    .UsingCustomIdentifier(instanceId)
    .UsingCustomDisplayName(instanceName);

var metrics = endpointConfiguration.EnableMetrics();

metrics.SendMetricDataToServiceControl(
    "Particular.Monitoring",
    TimeSpan.FromMilliseconds(500)
);

var failureSimulation = new ProcessingEndpointControls();
failureSimulation.Register(endpointConfiguration);

endpointConfiguration.UsePersistence<NonDurablePersistence>();
endpointConfiguration.EnableOutbox();

var ui = new UserInterface();
failureSimulation.BindSlowProcessingDial(ui, '2', 'w');
failureSimulation.BindDatabaseFailuresDial(ui, '3', 'e');
failureSimulation.BindFailureReceivingButton(ui, 'z');
failureSimulation.BindFailureProcessingButton(ui, 'x');
failureSimulation.BindFailureDispatchingButton(ui, 'c');

if (prometheusPortString != null)
{
    endpointConfiguration.ConfigureOpenTelemetry("Sales", instanceId.ToString(), int.Parse(prometheusPortString));
}

var endpointInstance = await Endpoint.Start(endpointConfiguration);

ui.RunLoop(title);

await endpointInstance.Stop();