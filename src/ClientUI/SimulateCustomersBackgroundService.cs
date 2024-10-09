namespace ClientUI;

class SimulateCustomersBackgroundService(SimulatedCustomers customers) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken cancellationToken = default)
        => customers.Run(cancellationToken);
}
