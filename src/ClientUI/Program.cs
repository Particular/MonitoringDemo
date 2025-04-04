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
var simulatedWork = simulatedCustomers.Run(cancellation.Token);

var nonInteractive = args.Length > 1 && bool.TryParse(args[1], out var isInteractive) && !isInteractive;
var interactive = !nonInteractive;

Console.WriteLine("#Progress");

for (var i = 0; i < 100; i++)
{
    Console.WriteLine(i);
    await Task.Delay(50);
}

Console.WriteLine("#ProgressEnd");

UserInterface.RunLoop("Load (ClientUI)", new Dictionary<char, (string, Action)>
{
    ['c'] = ("toggle High/Low traffic mode", () => simulatedCustomers.ToggleTrafficMode()),
}, writer => simulatedCustomers.WriteState(writer), false /* TODO for now*/);

cancellation.Cancel();

await simulatedWork;

await endpointInstance.Stop();