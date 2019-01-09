namespace ClientUI
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Messages;
    using NServiceBus;

    class SimulatedCustomers
    {
        public SimulatedCustomers(IEndpointInstance endpointInstance)
        {
            this.endpointInstance = endpointInstance;
        }

        public void WriteState(TextWriter output)
        {
            var trafficMode = highTrafficMode ? "High" : "Low";
            output.WriteLine($"{trafficMode} traffic mode - sending {rate} orders / second");
        }

        public void ToggleTrafficMode()
        {
            highTrafficMode = !highTrafficMode;
            rate = highTrafficMode ? 8 : 1;
        }

        public async Task Run(CancellationToken token)
        {
            nextReset = DateTime.UtcNow.AddSeconds(1);
            currentIntervalCount = 0;

            while (!token.IsCancellationRequested)
            {
                var now = DateTime.UtcNow;
                if (now > nextReset)
                {
                    currentIntervalCount = 0;
                    nextReset = now.AddSeconds(1);
                }

                await PlaceSingleOrder()
                    .ConfigureAwait(false);
                currentIntervalCount++;

                try
                {
                    if (currentIntervalCount >= rate)
                    {
                        var delay = nextReset - DateTime.UtcNow;
                        if (delay > TimeSpan.Zero)
                        {
                            await Task.Delay(delay, token)
                                .ConfigureAwait(false);
                        }
                    }
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        Task PlaceSingleOrder()
        {
            var placeOrderCommand = new PlaceOrder
            {
                OrderId = Guid.NewGuid().ToString()
            };

            return endpointInstance.Send(placeOrderCommand);
        }

        readonly IEndpointInstance endpointInstance;
        bool highTrafficMode;

        DateTime nextReset;
        int currentIntervalCount;
        int rate = 1;
    }
}