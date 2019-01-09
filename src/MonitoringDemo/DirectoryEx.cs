namespace MonitoringDemo
{
    using System;
    using System.IO;
    using System.Threading;

    static class DirectoryEx
    {
        public static void Delete(string directoryPath)
        {
            for (var i = 0; i < 3; i++)
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
                    Thread.Sleep(2000);
                }
            }
        }

        public static void ForceDeleteReadonly(string directoryPath)
        {
            for (var i = 0; i < 3; i++)
            {
                try
                {
                    // necessary because ravendb creates some folders read-only
                    var directory = new DirectoryInfo(directoryPath) { Attributes = FileAttributes.Normal };

                    foreach (var info in directory.GetFileSystemInfos("*", SearchOption.AllDirectories))
                    {
                        if (info.Attributes != FileAttributes.Normal)
                        {
                            info.Attributes = FileAttributes.Normal;
                        }
                    }

                    directory.Delete(true);
                    return;
                }
                catch (DirectoryNotFoundException)
                {
                    return;
                }
                catch (Exception)
                {
                    // ignored
                    Thread.Sleep(2000);
                }
            }
        }
    }
}