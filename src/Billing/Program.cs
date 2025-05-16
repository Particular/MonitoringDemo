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
    StorageDirectory = Path.Combine(Directory.GetParent(Assembly.GetExecutingAssembly().Location)!.Parent!.FullName, ".learningtransport"),
    TransportTransactionMode = TransportTransactionMode.ReceiveOnly
};
endpointConfiguration.UseTransport(transport);

endpointConfiguration.Recoverability()
    .Delayed(delayed => delayed.NumberOfRetries(0));

endpointConfiguration.AuditProcessedMessagesTo("audit");
endpointConfiguration.SendHeartbeatTo("Particular.ServiceControl");

endpointConfiguration.UniquelyIdentifyRunningInstance()
    .UsingCustomIdentifier(instanceId)
    .UsingCustomDisplayName(instanceName);

var metrics = endpointConfiguration.EnableMetrics();
metrics.SendMetricDataToServiceControl(
    "Particular.Monitoring",
    TimeSpan.FromMilliseconds(500)
);

endpointConfiguration.UsePersistence<NonDurablePersistence>();
endpointConfiguration.EnableOutbox();

var failureSimulation = new ProcessingEndpointControls();
failureSimulation.Register(endpointConfiguration);

var ui = new UserInterface();
failureSimulation.BindSlowProcessingDial(ui, '5', 't');
failureSimulation.BindDatabaseFailuresDial(ui, '6', 'y');
failureSimulation.BindFailureReceivingButton(ui, 'v');
failureSimulation.BindFailureProcessingButton(ui, 'b');
failureSimulation.BindFailureDispatchingButton(ui, 'n');

var endpointInstance = await Endpoint.Start(endpointConfiguration);

ui.RunLoop("Billing");

await endpointInstance.Stop();