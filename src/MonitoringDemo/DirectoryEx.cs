﻿namespace MonitoringDemo
{
    using System.IO;

    static class DirectoryEx
    {
        public static void Delete(string directoryPath)
        {
            ForceDeleteDirectory(directoryPath);
        }

        // necessary because ravendb creates some folders read-only
        static void ForceDeleteDirectory(string path)
        {
            var directory = new DirectoryInfo(path) {Attributes = FileAttributes.Normal};

            foreach (var info in directory.GetFileSystemInfos("*", SearchOption.AllDirectories))
            {
                info.Attributes = FileAttributes.Normal;
            }

            directory.Delete(true);
        }
    }
}