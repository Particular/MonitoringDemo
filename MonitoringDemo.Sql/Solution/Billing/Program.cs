using System;
using System.Threading.Tasks;
using Messages;
using NServiceBus;

namespace Billing
{
    class Program
    {
        static async Task Main()
        {
            Console.Title = "Billing";
            Console.SetWindowSize(65, 15);


            var endpointConfiguration = new EndpointConfiguration("Billing");
            endpointConfiguration.LimitMessageProcessingConcurrencyTo(100);

            endpointConfiguration.UsePersistence<InMemoryPersistence>();

            var transport = endpointConfiguration.UseTransport<SqlServerTransport>();
            transport.ConnectionStringName("NServiceBus/Transport");

            //endpointConfiguration.AuditProcessedMessagesTo("audit");
            var metrics = endpointConfiguration.EnableMetrics();
            metrics.SendMetricDataToServiceControl(
                "Particular.Monitoring", 
                TimeSpan.FromSeconds(1)
            );

            var routing = transport.Routing();
            routing.RegisterPublisher(
                typeof(OrderPlaced), 
                "Sales"
            );

            var simulationEffects = new SimulationEffects();
            endpointConfiguration.RegisterComponents(cc => cc.RegisterSingleton(simulationEffects));

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
                Console.WriteLine("Press N to toggle network latency simulation");
                Console.WriteLine("Press F to increase the simulated failure rate");
                Console.WriteLine("Press S to decrease the simulated failure rate");
                Console.WriteLine("Press ESC to quit");
                Console.WriteLine();

                state.WriteState(Console.Out);

                var input = Console.ReadKey(true);

                switch (input.Key)
                {
                    case ConsoleKey.N:
                        state.ToggleNetworkLatencySimulation();
                        break;
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