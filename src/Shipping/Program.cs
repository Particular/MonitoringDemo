using System.Reflection;
using System.Text.Json;
using Messages;
using Shared;

var instancePostfix = args.FirstOrDefault();

var title = string.IsNullOrEmpty(instancePostfix) ? "Processing (Shipping)" : $"Shipping - {instancePostfix}";
var instanceName = string.IsNullOrEmpty(instancePostfix) ? "shipping" : $"shipping-{instancePostfix}";
var instanceId = DeterministicGuid.Create("Shipping", instanceName);
var prometheusPortString = args.Skip(1).FirstOrDefault();

var endpointControls = new ProcessingEndpointControls(() => PrepareEndpointConfiguration(instanceId, instanceName, prometheusPortString));

var ui = new UserInterface();
endpointControls.BindSlowProcessingDial(ui, '8', 'i');
endpointControls.BindDatabaseFailuresDial(ui, '9', 'o');

endpointControls.BindDatabaseDownToggle(ui, 'j');
endpointControls.BindDelayedRetriesToggle(ui, 'k');
endpointControls.BindAutoThrottleToggle(ui, 'l');

endpointControls.BindFailureReceivingButton(ui, 'm');
endpointControls.BindFailureProcessingButton(ui, ',');
endpointControls.BindFailureDispatchingButton(ui, '.');

if (prometheusPortString != null)
{
    OpenTelemetryUtils.ConfigureOpenTelemetry("Shipping", instanceId.ToString(), int.Parse(prometheusPortString));
}
endpointControls.Start();
ui.RunLoop(title);

await endpointControls.StopEndpoint();

EndpointConfiguration PrepareEndpointConfiguration(Guid guid, string s, string? prometheusPortString1)
{
    var endpointConfiguration1 = new EndpointConfiguration("Shipping");
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
        StorageDirectory = Path.Combine(Directory.GetParent(Assembly.GetExecutingAssembly().Location)!.Parent!.FullName, ".learningtransport")
    };
    endpointConfiguration1.UseTransport(transport);

    endpointConfiguration1.AuditProcessedMessagesTo("audit");
    endpointConfiguration1.SendHeartbeatTo("Particular.ServiceControl");

    endpointConfiguration1.UniquelyIdentifyRunningInstance()
        .UsingCustomIdentifier(guid)
        .UsingCustomDisplayName(s);

    var metrics = endpointConfiguration1.EnableMetrics();
    metrics.SendMetricDataToServiceControl(
        "Particular.Monitoring",
        TimeSpan.FromMilliseconds(500)
    );

    endpointConfiguration1.EnableOpenTelemetry();

    return endpointConfiguration1;
}