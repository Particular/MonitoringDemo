using Particular;

class Program
{
    static async Task Main(string[] args)
    {
        Console.Title = "Platform";
        try
        {
            await PlatformLauncher.Launch(showPlatformToolConsoleOutput: false, servicePulseDefaultRoute: "/monitoring");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            Console.ReadLine();
        }
    }
}