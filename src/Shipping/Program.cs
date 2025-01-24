using System.Text.Json;
using Messages;
using Microsoft.Extensions.DependencyInjection;
using Shared;
using Shipping;

LoggingUtils.ConfigureLogging("Shipping");
var instanceId = "BB8A8BAF-4187-455E-AAD2-211CD43267CB";

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

endpointConfiguration.UseTransport<LearningTransport>();

endpointConfiguration.AuditProcessedMessagesTo("audit");
endpointConfiguration.SendHeartbeatTo("Particular.ServiceControl");

endpointConfiguration.ConfigureOpenTelemetry("Sales", instanceId, 9110);

endpointConfiguration.UniquelyIdentifyRunningInstance()
    .UsingCustomIdentifier(new Guid(instanceId))
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
}, writer => simulationEffects.WriteState(writer), interactive);

await endpointInstance.Stop();

