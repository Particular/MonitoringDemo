using System;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus;
using Shared;

namespace Sales
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.SetWindowSize(65, 15);

            LoggingUtils.ConfigureLogging("Sales");

            var instanceIdentifier = args.FirstOrDefault();
            if(string.IsNullOrEmpty(instanceIdentifier))
            {
                Console.Title = "Sales";
                instanceIdentifier = "original-instance";
            }
            else
            {
                Console.Title = $"Sales - {instanceIdentifier}";
            }

            Console.WriteLine("Using instance-id {0}", instanceIdentifier);

            var endpointConfiguration = new EndpointConfiguration("Sales");
            endpointConfiguration.LimitMessageProcessingConcurrencyTo(4);


            endpointConfiguration.UsePersistence<InMemoryPersistence>();

            var transport = endpointConfiguration.UseTransport<SqlServerTransport>();
            transport.ConnectionStringName("NServiceBus/Transport");

            //endpointConfiguration.AuditProcessedMessagesTo("audit");

            var metrics = endpointConfiguration.EnableMetrics();
            metrics.SendMetricDataToServiceControl(
                "Particular.Monitoring",
                TimeSpan.FromMilliseconds(500),
                instanceIdentifier
            );

            var simulationEffects = new SimulationEffects();
            endpointConfiguration.RegisterComponents(cc => cc.RegisterSingleton(simulationEffects));

            var endpointInstance = await Endpoint.Start(endpointConfiguration)
                .ConfigureAwait(false);

            RunUserInterfaceLoop(simulationEffects, instanceIdentifier);

            await endpointInstance.Stop()
                .ConfigureAwait(false);
        }

        static void RunUserInterfaceLoop(SimulationEffects state, string instanceName)
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine($"Sales Endpoint - {instanceName}");
                Console.WriteLine("Press F to process messages faster");
                Console.WriteLine("Press S to process messages slower");

                Console.WriteLine("Press ESC to quit");
                Console.WriteLine();

                state.WriteState(Console.Out);

                var input = Console.ReadKey(true);

                switch (input.Key)
                {
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
    }
}