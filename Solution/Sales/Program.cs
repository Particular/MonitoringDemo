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

            var instanceName = args.FirstOrDefault();
            var instanceId = Guid.NewGuid();

            if(string.IsNullOrEmpty(instanceName))
            {
                Console.Title = "Sales";

                instanceName = "original-instance";
                instanceId = new Guid("14A46D23-4874-497B-ABCD-8D6E2488DB25");
            }
            else
            {
                Console.Title = $"Sales - {instanceName}";
            }

            Console.WriteLine("Using instance-id {0}", instanceName);

            var endpointConfiguration = new EndpointConfiguration("Sales");
            endpointConfiguration.LimitMessageProcessingConcurrencyTo(4);


            endpointConfiguration.UsePersistence<InMemoryPersistence>();

            var transport = endpointConfiguration.UseTransport<SqlServerTransport>();
            transport.ConnectionStringName("NServiceBus/Transport");

            //endpointConfiguration.AuditProcessedMessagesTo("audit");

            endpointConfiguration.UniquelyIdentifyRunningInstance()
                .UsingCustomDisplayName(instanceName)
                .UsingCustomIdentifier(instanceId);

            var metrics = endpointConfiguration.EnableMetrics();
            metrics.SendMetricDataToServiceControl(
                "Particular.Monitoring",
                TimeSpan.FromMilliseconds(500)
            );

            var simulationEffects = new SimulationEffects();
            endpointConfiguration.RegisterComponents(cc => cc.RegisterSingleton(simulationEffects));

            var endpointInstance = await Endpoint.Start(endpointConfiguration)
                .ConfigureAwait(false);

            RunUserInterfaceLoop(simulationEffects, instanceName);

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