using System.Reflection;
using System.Text.Json;
using Messages;
using Microsoft.Extensions.DependencyInjection;
using Shared;
using Shipping;

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
    .UsingCustomIdentifier(new Guid("BB8A8BAF-4187-455E-AAD2-211CD43267CB"))
    .UsingCustomDisplayName("original-instance");

var metrics = endpointConfiguration.EnableMetrics();
metrics.SendMetricDataToServiceControl(
    "Particular.Monitoring",
    TimeSpan.FromMilliseconds(500)
);

var simulationEffects = new SimulationEffects();
endpointConfiguration.RegisterComponents(cc => cc.AddSingleton(simulationEffects));

var endpointInstance = await Endpoint.Start(endpointConfiguration);

var nonInteractive = args.Length > 1 && args[1] == bool.FalseString;
var interactive = !nonInteractive;

UserInterface.RunLoop("Processing (Shipping)", new Dictionary<char, (string, Action)>
{
    ['z'] = ("toggle resource degradation simulation", () => simulationEffects.ToggleDegradationSimulation()),
    ['q'] = ("process OrderBilled events faster", () => simulationEffects.ProcessMessagesFaster()),
    ['a'] = ("process OrderBilled events slower", () => simulationEffects.ProcessMessagesSlower())
}, writer => simulationEffects.WriteState(writer), false /* TODO for now*/);

await endpointInstance.Stop();