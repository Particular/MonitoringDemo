using System.Reflection;
using System.Text.Json;
using Billing;
using Messages;
using Microsoft.Extensions.DependencyInjection;
using Shared;

var instancePostfix = args.FirstOrDefault();

var title = string.IsNullOrEmpty(instancePostfix) ? "Failure rate (Billing)" : $"Billing - {instancePostfix}";
var instanceName = string.IsNullOrEmpty(instancePostfix) ? "billing" : $"billing-{instancePostfix}";

var instanceId = DeterministicGuid.Create("Billing", instanceName);

var endpointConfiguration = new EndpointConfiguration("Billing");
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

endpointConfiguration.Recoverability()
    .Delayed(delayed => delayed.NumberOfRetries(0));

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

endpointConfiguration.UsePersistence<NonDurablePersistence>();
endpointConfiguration.EnableOutbox();

var failureSimulation = new FailureSimulation();
failureSimulation.Register(endpointConfiguration);

var simulationEffects = new SimulationEffects();
endpointConfiguration.RegisterComponents(cc => cc.AddSingleton(simulationEffects));

var endpointInstance = await Endpoint.Start(endpointConfiguration);

UserInterface.RunLoop(title, new Dictionary<char, (string, Action)>
{
    ['w'] = ("increase the simulated failure rate", () => simulationEffects.IncreaseFailureRate()),
    ['s'] = ("decrease the simulated failure rate", () => simulationEffects.DecreaseFailureRate())
}, writer => simulationEffects.WriteState(writer));

await endpointInstance.Stop();