using System.Reflection;
using System.Text.Json;
using Billing;
using Messages;
using Microsoft.Extensions.DependencyInjection;
using Shared;

var instancePostfix = args.FirstOrDefault();

var title = string.IsNullOrEmpty(instancePostfix) ? "Failure rate (Billing)" : $"Billing - {instancePostfix}";
var instanceName = string.IsNullOrEmpty(instancePostfix) ? "billing" : $"billing-{instancePostfix}";
var instanceId = DeterministicGuid.Create("Billing", instanceName);
var prometheusPortString = args.Skip(1).FirstOrDefault();

var endpointControls = new ProcessingEndpointControls(() => PrepareEndpointConfiguration(instanceId, instanceName, prometheusPortString));

var ui = new UserInterface();
endpointControls.BindSlowProcessingDial(ui, '5', 't');
endpointControls.BindDatabaseFailuresDial(ui, '6', 'y');

endpointControls.BindDatabaseDownToggle(ui, 'f');
endpointControls.BindDelayedRetriesToggle(ui, 'g');
endpointControls.BindAutoThrottleToggle(ui, 'h');

endpointControls.BindFailureReceivingButton(ui, 'v');
endpointControls.BindFailureProcessingButton(ui, 'b');
endpointControls.BindFailureDispatchingButton(ui, 'n');

if (prometheusPortString != null)
{
    OpenTelemetryUtils.ConfigureOpenTelemetry("Billing", instanceId.ToString(), int.Parse(prometheusPortString));
}

endpointControls.Start();

ui.RunLoop(title);

await endpointControls.StopEndpoint();

EndpointConfiguration PrepareEndpointConfiguration(Guid guid, string s, string? prometheusPortString1)
{
    var endpointConfiguration1 = new EndpointConfiguration("Billing");
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

    endpointConfiguration1.Recoverability()
        .Delayed(delayed => delayed.NumberOfRetries(0));

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

    endpointConfiguration1.UsePersistence<NonDurablePersistence>();
    endpointConfiguration1.EnableOutbox();

    endpointConfiguration1.EnableOpenTelemetry();

    return endpointConfiguration1;
}