using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using ClientUI;
using Messages;
using Shared;

var instancePostfix = args.FirstOrDefault();

var title = string.IsNullOrEmpty(instancePostfix) ? "ClientUI" : $"ClientUI - {instancePostfix}";
var instanceName = string.IsNullOrEmpty(instancePostfix) ? "clientui" : $"clientui-{instancePostfix}";
var instanceId = DeterministicGuid.Create("ClientUI", instanceName);
var prometheusPortString = args.Skip(1).FirstOrDefault();

var endpointConfiguration = new EndpointConfiguration("ClientUI");

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
    StorageDirectory = Path.Combine(Directory.GetParent(Assembly.GetExecutingAssembly().Location)!.Parent!.FullName, ".learningtransport")
};
var routing = endpointConfiguration.UseTransport(transport);

endpointConfiguration.AuditProcessedMessagesTo("audit");
endpointConfiguration.SendHeartbeatTo("Particular.ServiceControl");

endpointConfiguration.UniquelyIdentifyRunningInstance()
    .UsingCustomIdentifier(instanceId)
    .UsingCustomDisplayName(instanceName);

endpointConfiguration.EnableOpenTelemetry();

var metrics = endpointConfiguration.EnableMetrics();
metrics.SendMetricDataToServiceControl(
    "Particular.Monitoring",
    TimeSpan.FromMilliseconds(500)
);

routing.RouteToEndpoint(typeof(PlaceOrder), "Sales");

if (prometheusPortString != null)
{
    OpenTelemetryUtils.ConfigureOpenTelemetry("ClientUI", instanceId.ToString(), int.Parse(prometheusPortString));
}

var endpointInstance = await Endpoint.Start(endpointConfiguration);

var simulatedCustomers = new SimulatedCustomers(endpointInstance);
var cancellation = new CancellationTokenSource();

var ui = new UserInterface();
simulatedCustomers.BindSendingRateDial(ui, '-', '[');
simulatedCustomers.BindDuplicateLikelihoodDial(ui, '=', ']');
simulatedCustomers.BindManualModeToggle(ui, ';');
simulatedCustomers.BindManualSendButton(ui, '/');
simulatedCustomers.BindNoiseToggle(ui, '`');
simulatedCustomers.BindBlackFridayToggle(ui, '\'');

var simulatedWork = simulatedCustomers.Run(cancellation.Token);

ui.RunLoop(title);

cancellation.Cancel();

await simulatedWork;

await endpointInstance.Stop();