namespace Platform
{
    using System;
    using Particular;

    class Program
    {
        static void Main(string[] args)
        {
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