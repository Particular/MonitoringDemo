﻿using System.Text.Json;
using Billing;
using Messages;
using Microsoft.Extensions.DependencyInjection;
using Shared;

LoggingUtils.ConfigureLogging("Billing");

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

endpointConfiguration.UseTransport<LearningTransport>();

endpointConfiguration.Recoverability()
    .Delayed(delayed => delayed.NumberOfRetries(0));

endpointConfiguration.AuditProcessedMessagesTo("audit");
endpointConfiguration.SendHeartbeatTo("Particular.ServiceControl");

var instanceId = "1C62248E-2681-45A4-B44D-5CF93584BAD6";
endpointConfiguration.UniquelyIdentifyRunningInstance()
    .UsingCustomIdentifier(new Guid(instanceId))
    .UsingCustomDisplayName("original-instance");

endpointConfiguration.ConfigureOpenTelemetry("Sales", instanceId, 9120);

var metrics = endpointConfiguration.EnableMetrics();
metrics.SendMetricDataToServiceControl(
    "Particular.Monitoring",
    TimeSpan.FromMilliseconds(500)
);

var simulationEffects = new SimulationEffects();
endpointConfiguration.RegisterComponents(cc => cc.AddSingleton(simulationEffects));
endpointConfiguration.Recoverability().OnConsecutiveFailures(5, new RateLimitSettings(TimeSpan.FromSeconds(5)));

var endpointInstance = await Endpoint.Start(endpointConfiguration);

var nonInteractive = args.Length > 1 && args[1] == bool.FalseString;
var interactive = !nonInteractive;

UserInterface.RunLoop("Failure rate (Billing)", new Dictionary<char, (string, Action)>
{
    ['w'] = ("increase the simulated failure rate", () => simulationEffects.IncreaseFailureRate()),
    ['s'] = ("decrease the simulated failure rate", () => simulationEffects.DecreaseFailureRate())
}, writer => simulationEffects.WriteState(writer), interactive);

await endpointInstance.Stop();