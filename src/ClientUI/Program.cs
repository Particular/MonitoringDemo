using System.Text.Json;
using ClientUI;
using Messages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared;

Console.Title = "Load (ClientUI)";

if (OperatingSystem.IsWindows())
{
    Console.SetWindowSize(65, 15);
}

LoggingUtils.ConfigureLogging("ClientUI");

var endpointConfiguration = new EndpointConfiguration("ClientUI");

var serializer = endpointConfiguration.UseSerialization<SystemJsonSerializer>();
serializer.Options(new JsonSerializerOptions
{
    TypeInfoResolverChain =
        {
            MessagesSerializationContext.Default
        }
});

var transport = endpointConfiguration.UseTransport<LearningTransport>();

endpointConfiguration.AuditProcessedMessagesTo("audit");
endpointConfiguration.SendHeartbeatTo("Particular.ServiceControl");

endpointConfiguration.UniquelyIdentifyRunningInstance()
    .UsingCustomIdentifier(new Guid("EA3E7D1B-8171-4098-B160-1FEA975CCB2C"))
    .UsingCustomDisplayName("original-instance");

var metrics = endpointConfiguration.EnableMetrics();
metrics.SendMetricDataToServiceControl(
    "Particular.Monitoring",
    TimeSpan.FromMilliseconds(500)
);

var routing = transport.Routing();
routing.RouteToEndpoint(typeof(PlaceOrder), "Sales");

var builder = Host.CreateApplicationBuilder();
builder.Services.AddNServiceBusEndpoint(endpointConfiguration);
var host = builder.Build();
await host.StartAsync();

var messageSession = host.Services.GetRequiredService<IMessageSession>();

var simulatedCustomers = new SimulatedCustomers(messageSession);
var cancellation = new CancellationTokenSource();
var simulatedWork = simulatedCustomers.Run(cancellation.Token);

RunUserInterfaceLoop(simulatedCustomers);

cancellation.Cancel();

await simulatedWork;

await host.StopAsync();

void RunUserInterfaceLoop(SimulatedCustomers simulatedCustomers)
{
    while (true)
    {
        Console.Clear();
        Console.WriteLine("Simulating customers placing orders on a website");
        Console.WriteLine("Press T to toggle High/Low traffic mode");
        Console.WriteLine("Press ESC to quit");
        Console.WriteLine();

        simulatedCustomers.WriteState(Console.Out);

        var input = Console.ReadKey(true);

        switch (input.Key)
        {
            case ConsoleKey.T:
                simulatedCustomers.ToggleTrafficMode();
                break;
            case ConsoleKey.Escape:
                return;
        }
    }
}
