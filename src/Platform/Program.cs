using Particular;

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
