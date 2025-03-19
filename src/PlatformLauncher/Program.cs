using Particular;

Console.Title = "PlatformLauncher";
try
{
    await PlatformLauncher.Launch(showPlatformToolConsoleOutput: false, servicePulseDefaultRoute: "/monitoring");
}
catch (Exception e)
{
    Console.WriteLine(e);
    Console.ReadLine();
}
