using System.Reflection;
using System.Text.Json;
using Messages;
using Shared;

var instancePostfix = args.FirstOrDefault();
var title = string.IsNullOrEmpty(instancePostfix) ? "Processing (Sales)" : $"Sales - {instancePostfix}";
var instanceName = string.IsNullOrEmpty(instancePostfix) ? "sales" : $"sales-{instancePostfix}";
var prometheusPortString = args.Skip(1).FirstOrDefault();

var instanceId = DeterministicGuid.Create("Sales", instanceName);

var endpointControls = new ProcessingEndpointControls(() => PrepareEndpointConfiguration(instanceId, instanceName, prometheusPortString));

var ui = new UserInterface();
endpointControls.BindSlowProcessingDial(ui, '2', 'w');
endpointControls.BindDatabaseFailuresDial(ui, '3', 'e');

endpointControls.BindDatabaseDownToggle(ui, 'a');
endpointControls.BindDelayedRetriesToggle(ui, 's');
endpointControls.BindAutoThrottleToggle(ui, 'd');

endpointControls.BindFailureReceivingButton(ui, 'z');
endpointControls.BindFailureProcessingButton(ui, 'x');
endpointControls.BindFailureDispatchingButton(ui, 'c');

if (prometheusPortString != null)
{
    OpenTelemetryUtils.ConfigureOpenTelemetry("Sales", instanceId.ToString(), int.Parse(prometheusPortString));
}

endpointControls.Start();

ui.RunLoop(title);

await endpointControls.StopEndpoint();

EndpointConfiguration PrepareEndpointConfiguration(Guid guid, string displayName, string? prometheusPortString1)
{
    var endpointConfiguration1 = new EndpointConfiguration("Sales");
    endpointConfiguration1.LimitMessageProcessingConcurrencyTo(4);

    var serializer = endpointConfiguration1.UseSerialization<SystemJsonSerializer>();
    serializer.Options(new JsonSerializerOptions
    {
        TypeInfoResolverChain =
        {
            MessagesSerializationContext.Default
        }
    });

    var transport = new LearningTransport
    {
        StorageDirectory = Path.Combine(Directory.GetParent(Assembly.GetExecutingAssembly().Location)!.Parent!.FullName, ".learningtransport"),
        TransportTransactionMode = TransportTransactionMode.ReceiveOnly
    };
    endpointConfiguration1.UseTransport(transport);

    endpointConfiguration1.AuditProcessedMessagesTo("audit");
    endpointConfiguration1.SendHeartbeatTo("Particular.ServiceControl");

    endpointConfiguration1.UniquelyIdentifyRunningInstance()
        .UsingCustomIdentifier(guid)
        .UsingCustomDisplayName(displayName);

    var metrics = endpointConfiguration1.EnableMetrics();

    metrics.SendMetricDataToServiceControl(
        "Particular.Monitoring",
        TimeSpan.FromMilliseconds(500)
    );

    endpointConfiguration1.UsePersistence<NonDurablePersistence>();
    endpointConfiguration1.EnableOutbox();

    endpointConfiguration1.EnableOpenTelemetry();

    return endpointConfiguration1;
}