using System.Reflection;
using System.Text.Json;
using Messages;
using Microsoft.Extensions.DependencyInjection;
using Sales;
using Shared;

var instancePostfix = args.FirstOrDefault();

var title = string.IsNullOrEmpty(instancePostfix) ? "Processing (Sales)" : $"Sales - {instancePostfix}";
var instanceName = string.IsNullOrEmpty(instancePostfix) ? "sales" : $"sales-{instancePostfix}";

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
    .UsingCustomDisplayName(instanceName)
    .UsingCustomIdentifier(instanceId);

var metrics = endpointConfiguration.EnableMetrics();
metrics.SendMetricDataToServiceControl(
    "Particular.Monitoring",
    TimeSpan.FromMilliseconds(500)
);

var failureSimulation = new FailureSimulation();
failureSimulation.Register(endpointConfiguration);

var simulationEffects = new SimulationEffects();
endpointConfiguration.RegisterComponents(cc => cc.AddSingleton(simulationEffects));

endpointConfiguration.UsePersistence<NonDurablePersistence>();
endpointConfiguration.EnableOutbox();

var endpointInstance = await Endpoint.Start(endpointConfiguration);

UserInterface.RunLoop(title, new Dictionary<char, (string, Action)>
{
    ['r'] = ("process messages faster", () => simulationEffects.ProcessMessagesFaster()),
    ['f'] = ("process messages slower", () => simulationEffects.ProcessMessagesSlower()),
    ['t'] = ("simulate failure in retrieving", () => failureSimulation.TriggerFailureReceiving()),
    ['y'] = ("simulate failure in processing", () => failureSimulation.TriggerFailureProcessing()),
    ['u'] = ("simulate failure in dispatching", () => failureSimulation.TriggerFailureDispatching())
}, writer => simulationEffects.WriteState(writer));

await endpointInstance.Stop();