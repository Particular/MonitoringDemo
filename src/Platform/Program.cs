namespace Platform;

using Particular;
using System;
using System.Threading.Tasks;

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