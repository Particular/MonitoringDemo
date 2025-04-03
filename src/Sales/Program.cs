using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Messages;
using Microsoft.Extensions.DependencyInjection;
using Sales;
using Shared;

var instanceName = args.FirstOrDefault();

var instanceNumber = args.FirstOrDefault();
string title;

if (string.IsNullOrEmpty(instanceNumber))
{
    title = "Processing (Sales)";

    instanceNumber = "original-instance";
}
else
{
    title = $"Sales - {instanceNumber}";
}

var instanceId = DeterministicGuid.Create("Sales", instanceNumber);

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
    StorageDirectory = Path.Combine(Directory.GetParent(Assembly.GetExecutingAssembly().Location)!.Parent!.FullName, ".learningtransport")
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

var simulationEffects = new SimulationEffects();
endpointConfiguration.RegisterComponents(cc => cc.AddSingleton(simulationEffects));

var endpointInstance = await Endpoint.Start(endpointConfiguration);

var nonInteractive = args.Length > 1 && bool.TryParse(args[1], out var isInteractive) && !isInteractive;
var interactive = !nonInteractive;

UserInterface.RunLoop(title, new Dictionary<char, (string, Action)>
{
    ['r'] = ("process messages faster", () => simulationEffects.ProcessMessagesFaster()),
    ['f'] = ("process messages slower", () => simulationEffects.ProcessMessagesSlower())
}, writer => simulationEffects.WriteState(writer), false /* TODO for now*/);

await endpointInstance.Stop();

static class DeterministicGuid
{
    public static Guid Create(params object[] data)
    {
        // use MD5 hash to get a 16-byte hash of the string
        using var provider = MD5.Create();
        var inputBytes = Encoding.Default.GetBytes(string.Concat(data));
        var hashBytes = provider.ComputeHash(inputBytes);
        // generate a guid from the hash:
        return new Guid(hashBytes);
    }
}