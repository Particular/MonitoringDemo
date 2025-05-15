using Particular;
using System.Reflection;

Console.Title = "PlatformLauncher";

var rootFolder = Path.Combine(Directory.GetParent(Assembly.GetExecutingAssembly().Location)!.Parent!.FullName);

try
{
    await PlatformLauncher.Launch(showPlatformToolConsoleOutput: false, servicePulseDefaultRoute: "/monitoring", rootFolder);
}
catch (Exception e)
{
    Console.WriteLine(e);
    Console.ReadLine();
}
