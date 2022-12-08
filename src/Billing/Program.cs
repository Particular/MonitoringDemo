﻿namespace Billing
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using NServiceBus;
    using Shared;

    class Program
    {
        static async Task Main()
        {
            Console.Title = "Failure rate (Billing)";
            Console.SetWindowSize(65, 15);

            LoggingUtils.ConfigureLogging("Billing");

            var endpointConfiguration = new EndpointConfiguration("Billing");
            endpointConfiguration.LimitMessageProcessingConcurrencyTo(4);

            endpointConfiguration.UsePersistence<NonDurablePersistence>();

            endpointConfiguration.UseTransport<LearningTransport>();

            endpointConfiguration.Recoverability()
                .Delayed(delayed => delayed.NumberOfRetries(0));

            endpointConfiguration.AuditProcessedMessagesTo("audit");
            endpointConfiguration.SendHeartbeatTo("Particular.ServiceControl");

            endpointConfiguration.UniquelyIdentifyRunningInstance()
                .UsingCustomIdentifier(new Guid("1C62248E-2681-45A4-B44D-5CF93584BAD6"))
                .UsingCustomDisplayName("original-instance");

            var metrics = endpointConfiguration.EnableMetrics();
            metrics.SendMetricDataToServiceControl(
                "Particular.Monitoring",
                TimeSpan.FromMilliseconds(500)
            );

            var simulationEffects = new SimulationEffects();
            endpointConfiguration.RegisterComponents(cc => cc.AddSingleton(simulationEffects));

            var endpointInstance = await Endpoint.Start(endpointConfiguration)
                .ConfigureAwait(false);

            RunUserInterfaceLoop(simulationEffects);

            await endpointInstance.Stop()
                .ConfigureAwait(false);
        }

        static void RunUserInterfaceLoop(SimulationEffects state)
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("Billing Endpoint");
                Console.WriteLine("Press F to increase the simulated failure rate");
                Console.WriteLine("Press S to decrease the simulated failure rate");
                Console.WriteLine("Press ESC to quit");
                Console.WriteLine();

                state.WriteState(Console.Out);

                var input = Console.ReadKey(true);

                switch (input.Key)
                {
                    case ConsoleKey.F:
                        state.IncreaseFailureRate();
                        break;
                    case ConsoleKey.S:
                        state.DecreaseFailureRate();
                        break;
                    case ConsoleKey.Escape:
                        return;
                }
            }
        }
    }
}