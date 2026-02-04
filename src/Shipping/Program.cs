using System.Text.Json;
using Messages;
using Microsoft.Extensions.DependencyInjection;
using Shared;
using Shipping;

Console.Title = "Processing (Shipping)";

if (OperatingSystem.IsWindows())
{
    Console.SetWindowSize(65, 15);
}

LoggingUtils.ConfigureLogging("Shipping");

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

RunUserInterfaceLoop(simulationEffects);

await endpointInstance.Stop();

void RunUserInterfaceLoop(SimulationEffects state)
{
    while (true)
    {
        Console.Clear();
        Console.WriteLine("Shipping Endpoint");
        Console.WriteLine("Press D to toggle resource degradation simulation");
        Console.WriteLine("Press F to process OrderBilled events faster");
        Console.WriteLine("Press S to process OrderBilled events slower");
        Console.WriteLine("Press ESC to quit");
        Console.WriteLine();

        state.WriteState(Console.Out);

        var input = Console.ReadKey(true);

        switch (input.Key)
        {
            case ConsoleKey.D:
                state.ToggleDegradationSimulation();
                break;
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
