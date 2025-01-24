using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Messages;
using Microsoft.Extensions.DependencyInjection;
using Sales;
using Shared;

LoggingUtils.ConfigureLogging("Sales");

var instanceNumber = args.FirstOrDefault();
var instanceNumberInt = 0;
string title;

if (string.IsNullOrEmpty(instanceNumber))
{
    title = "Processing (Sales)";

    instanceNumber = "original-instance";
}
else
{
    title = $"Sales - {instanceNumber}";
    instanceNumberInt = int.Parse(instanceNumber.Split('-')[1]);
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

endpointConfiguration.UseTransport<LearningTransport>();

endpointConfiguration.AuditProcessedMessagesTo("audit");
endpointConfiguration.SendHeartbeatTo("Particular.ServiceControl");

endpointConfiguration.UniquelyIdentifyRunningInstance()
    .UsingCustomDisplayName(instanceNumber)
    .UsingCustomIdentifier(instanceId);

endpointConfiguration.ConfigureOpenTelemetry("Sales", instanceId.ToString(), 9100 + instanceNumberInt);

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

UserInterface.RunLoop(title, new Dictionary<char, (string, Action)>
{
    ['r'] = ("process messages faster", () => simulationEffects.ProcessMessagesFaster()),
    ['f'] = ("process messages slower", () => simulationEffects.ProcessMessagesSlower())
}, writer => simulationEffects.WriteState(writer), interactive);

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