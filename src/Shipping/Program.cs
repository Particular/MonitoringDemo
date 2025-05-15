using System.Reflection;
using System.Text.Json;
using Messages;
using Microsoft.Extensions.DependencyInjection;
using Shared;
using Shipping;

var instancePostfix = args.FirstOrDefault();

var title = string.IsNullOrEmpty(instancePostfix) ? "Processing (Shipping)" : $"Shipping - {instancePostfix}";
var instanceName = string.IsNullOrEmpty(instancePostfix) ? "shipping" : $"shipping-{instancePostfix}";

var instanceId = DeterministicGuid.Create("Shipping", instanceName);

var endpointConfiguration = new EndpointConfiguration("Shipping");
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
    StorageDirectory = Path.Combine(Directory.GetParent(Assembly.GetExecutingAssembly().Location)!.Parent!.FullName, ".learningtransport")
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

var simulationEffects = new SimulationEffects();
endpointConfiguration.RegisterComponents(cc => cc.AddSingleton(simulationEffects));

var endpointInstance = await Endpoint.Start(endpointConfiguration);

UserInterface.RunLoop(title, new Dictionary<char, (string, Action)>
{
    ['z'] = ("toggle resource degradation simulation", () => simulationEffects.ToggleDegradationSimulation()),
    ['q'] = ("process OrderBilled events faster", () => simulationEffects.ProcessMessagesFaster()),
    ['a'] = ("process OrderBilled events slower", () => simulationEffects.ProcessMessagesSlower())
}, writer => simulationEffects.WriteState(writer));

await endpointInstance.Stop();