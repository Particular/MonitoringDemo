
using System.Diagnostics;
using System.Reflection;
using NServiceBus.Extensions.Logging;
using NServiceBus.Logging;
using Serilog;
using Serilog.Extensions.Logging;

namespace Shared;

public static class LoggingUtils
{
    public static void ConfigureLogging(string endpointName)
    {
        var logsFolder = GetLogLocation();

        if (logsFolder is null)
        {
            return;
        }

        var logPath = Path.Combine(logsFolder, $"{endpointName}.txt");

        Log.Logger = new LoggerConfiguration()
            .WriteTo.File(logPath)
            .CreateLogger();

        LogManager.UseFactory(new ExtensionsLoggerFactory(new SerilogLoggerFactory()));
    }

    static string? GetLogLocation()
    {
        var assemblyPath = new Uri(Assembly.GetExecutingAssembly().Location).LocalPath;
        var assemblyFolder = Path.GetDirectoryName(assemblyPath);

        if (string.IsNullOrEmpty(assemblyFolder))
        {
            return null;
        }

        var workingDir = new DirectoryInfo(assemblyFolder);
        var logLocation = FindLogFolder(workingDir);
        return (logLocation ?? workingDir).FullName;
    }

    static DirectoryInfo? FindLogFolder(DirectoryInfo? currentDir)
    {
        if (currentDir is null)
        {
            return null;
        }

        var logsFolders = currentDir.GetDirectories(".logs", SearchOption.TopDirectoryOnly);

        return logsFolders.FirstOrDefault() ?? FindLogFolder(currentDir.Parent);
    }
}
