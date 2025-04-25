using System.Reflection;
using System.Text.Json;
using ClientUI;
using Messages;
using Shared;

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
    .UsingCustomIdentifier(new Guid("EA3E7D1B-8171-4098-B160-1FEA975CCB2C"))
    .UsingCustomDisplayName("original-instance");

var metrics = endpointConfiguration.EnableMetrics();
metrics.SendMetricDataToServiceControl(
    "Particular.Monitoring",
    TimeSpan.FromMilliseconds(500)
);

routing.RouteToEndpoint(typeof(PlaceOrder), "Sales");

var endpointInstance = await Endpoint.Start(endpointConfiguration);

var simulatedCustomers = new SimulatedCustomers(endpointInstance);
var cancellation = new CancellationTokenSource();

var ui = new UserInterface();
simulatedCustomers.BindSendingRateDial(ui, '-', '[');
simulatedCustomers.BindDuplicateLikelihoodDial(ui, '=', ']');
simulatedCustomers.BindManualModeToggle(ui, ';');
simulatedCustomers.BindManualSendButton(ui, '/');

var simulatedWork = simulatedCustomers.Run(cancellation.Token);

ui.RunLoop("Client UI");

cancellation.Cancel();

await simulatedWork;

await endpointInstance.Stop();