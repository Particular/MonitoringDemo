using System.Reflection;
using System.Text.Json;
using Billing;
using Messages;
using Microsoft.Extensions.DependencyInjection;
using Shared;

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
    StorageDirectory = Path.Combine(Directory.GetParent(Assembly.GetExecutingAssembly().Location)!.Parent!.FullName, ".learningtransport")
};
endpointConfiguration.UseTransport(transport);

endpointConfiguration.Recoverability()
    .Delayed(delayed => delayed.NumberOfRetries(0));

endpointConfiguration.AuditProcessedMessagesTo("audit");
endpointConfiguration.SendHeartbeatTo("Particular.ServiceControl");

endpointConfiguration.UniquelyIdentifyRunningInstance()
    .UsingCustomIdentifier(new Guid("1C62248E-2681-45A4-B44D-5CF93584BAD6"))
    .UsingCustomDisplayName("original-instance");

var metrics = endpointConfiguration.EnableMetrics();
metrics.SendMetricDataToServiceControl(
    "Particular.Monitoring",
    TimeSpan.FromMilliseconds(500)
);

var simulationEffects = new SimulationEffects();
endpointConfiguration.RegisterComponents(cc => cc.AddSingleton(simulationEffects));

var endpointInstance = await Endpoint.Start(endpointConfiguration);

var nonInteractive = args.Length > 1 && bool.TryParse(args[1], out var isInteractive) && !isInteractive;
var interactive = !nonInteractive;


UserInterface.RunLoop("Failure rate (Billing)", new Dictionary<char, (string, Action)>
{
    ['w'] = ("increase the simulated failure rate", () => simulationEffects.IncreaseFailureRate()),
    ['s'] = ("decrease the simulated failure rate", () => simulationEffects.DecreaseFailureRate())
}, writer => simulationEffects.WriteState(writer), false /* TODO for now*/);

await endpointInstance.Stop();