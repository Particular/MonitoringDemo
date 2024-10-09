using Messages;
using Microsoft.Extensions.Hosting;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Shared;

public static class NServiceBusExtensions
{
    public static void AddNServiceBus(this IHostApplicationBuilder builder, string endpointName, Action<EndpointConfiguration, RoutingSettings<LearningTransport>>? configure = null, string instanceName = "original-instance")
    {
        var endpointConfiguration = new EndpointConfiguration(endpointName);

        var serializer = endpointConfiguration.UseSerialization<SystemJsonSerializer>();
        serializer.Options(new JsonSerializerOptions
        {
            TypeInfoResolverChain =
            {
                MessagesSerializationContext.Default
            }
        });

        endpointConfiguration.AuditProcessedMessagesTo("audit");
        endpointConfiguration.SendHeartbeatTo("Particular.ServiceControl");
        var metrics = endpointConfiguration.EnableMetrics();
        metrics.SendMetricDataToServiceControl(
            "Particular.Monitoring",
            TimeSpan.FromMilliseconds(500)
        );

        endpointConfiguration.Recoverability()
            .Delayed(delayed => delayed.NumberOfRetries(0));

        endpointConfiguration.UniquelyIdentifyRunningInstance()
            .UsingCustomDisplayName(instanceName)
            .UsingCustomIdentifier(DeterministicGuid.Create(endpointName, instanceName));

        endpointConfiguration.EnableOpenTelemetry();

        var routing = endpointConfiguration.UseTransport(new LearningTransport());
        configure?.Invoke(endpointConfiguration, routing);

        builder.UseNServiceBus(endpointConfiguration);
    }
}

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
