namespace Platform
{
    using Particular;
    using System;

    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Platform";
            try
            {
                PlatformLauncher.Launch(showPlatformToolConsoleOutput: true, servicePulseDefaultRoute: "/monitoring");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.ReadLine();
            }
        }
    }
}