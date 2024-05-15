using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Messages;
using Microsoft.Extensions.DependencyInjection;
using Sales;
using Shared;

class Program
{
    static async Task Main(string[] args)
    {
        Console.SetWindowSize(65, 15);

        LoggingUtils.ConfigureLogging("Sales");

        var instanceName = args.FirstOrDefault();

        if (string.IsNullOrEmpty(instanceName))
        {
            Console.Title = "Processing (Sales)";

            instanceName = "original-instance";
        }
        else
        {
            Console.Title = $"Sales - {instanceName}";
        }

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

        endpointConfiguration.UseTransport<LearningTransport>();

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

        var endpointInstance = await Endpoint.Start(endpointConfiguration)
            .ConfigureAwait(false);

        RunUserInterfaceLoop(simulationEffects, instanceName);

        await endpointInstance.Stop()
            .ConfigureAwait(false);
    }

    static void RunUserInterfaceLoop(SimulationEffects state, string instanceName)
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine($"Sales Endpoint - {instanceName}");
            Console.WriteLine("Press F to process messages faster");
            Console.WriteLine("Press S to process messages slower");

            Console.WriteLine("Press ESC to quit");
            Console.WriteLine();

            state.WriteState(Console.Out);

            var input = Console.ReadKey(true);

            switch (input.Key)
            {
                case ConsoleKey.F:
                    state.ProcessMessagesFaster();
                    break;
                case ConsoleKey.S:
                    state.ProcessMessagesSlower();
                    break;
                case ConsoleKey.Escape:
                    return;
            }
        }
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