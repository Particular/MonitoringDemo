namespace MonitoringDemo
{
    using System;
    using System.IO;
    using System.Threading;

    static class DirectoryEx
    {
        public static void Delete(string directoryPath)
        {
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    Directory.Delete(directoryPath, true);
                    return;
                }
                catch (DirectoryNotFoundException)
                {
                    return;
                }
                catch (Exception)
                {
                    // ignored
                    Thread.Sleep(5000);
                }
            }
        }
    }
}