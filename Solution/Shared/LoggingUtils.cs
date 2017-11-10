using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NServiceBus.Logging;
using NServiceBus.Serilog;
using Serilog;

namespace Shared
{
    public static class LoggingUtils
    {
        public static void ConfigureLogging(string endpointName)
        {
            var logsFolder = GetLogLocation();
            if (logsFolder == null)
                return;

            var logPath = Path.Combine(logsFolder, $"{endpointName}.txt");

            Log.Logger = new LoggerConfiguration()
                .WriteTo.File(logPath)
                .CreateLogger();

            LogManager.Use<SerilogFactory>();
        }

        private static string GetLogLocation()
        {
            var assemblyPath = new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath;
            var assemblyFolder = Path.GetDirectoryName(assemblyPath);

            if (string.IsNullOrEmpty(assemblyFolder))
                return null;

            var workingDir = new DirectoryInfo(assemblyFolder);
            var logLocation = FindLogFolder(workingDir);
            return (logLocation ?? workingDir).FullName;
        }

        private static DirectoryInfo FindLogFolder(DirectoryInfo currentDir)
        {
            if (currentDir == null)
                return null;

            var logsFolders = currentDir.GetDirectories("logs", SearchOption.TopDirectoryOnly);

            return logsFolders.FirstOrDefault() ?? FindLogFolder(currentDir.Parent);
        }
    }
}
