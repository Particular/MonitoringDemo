using Particular;

Console.Title = "PlatformLauncher";

for (int i = 0; i < 10; i++)
{
    Console.Write(".");
    await Task.Delay(100);
}

try
{
    await PlatformLauncher.Launch(showPlatformToolConsoleOutput: false, servicePulseDefaultRoute: "/monitoring");
}
catch (Exception e)
{
    Console.WriteLine(e);
    Console.ReadLine();
}
