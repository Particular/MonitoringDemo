using System.Reflection;
using System.Text.Json;
using Messages;
using Microsoft.Extensions.DependencyInjection;
using Shared;
using Shipping;

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

var transport = new LearningTransport
{
    StorageDirectory = Path.Combine(Directory.GetParent(Assembly.GetExecutingAssembly().Location)!.Parent!.FullName, ".learningtransport")
};
endpointConfiguration.UseTransport(transport);

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

var failureSimulation = new ProcessingEndpointControls();
failureSimulation.Register(endpointConfiguration);

var ui = new UserInterface();
failureSimulation.BindSlowProcessingDial(ui, '8', 'i');
failureSimulation.BindDatabaseFailuresDial(ui, '9', 'o');
failureSimulation.BindFailureReceivingButton(ui, 'm');
failureSimulation.BindFailureProcessingButton(ui, ',');
failureSimulation.BindFailureDispatchingButton(ui, '.');

var endpointInstance = await Endpoint.Start(endpointConfiguration);

ui.RunLoop("Shipping");

await endpointInstance.Stop();