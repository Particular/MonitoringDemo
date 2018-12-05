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
                PlatformLauncher.Launch(true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.ReadLine();
            }
        }
    }
}
