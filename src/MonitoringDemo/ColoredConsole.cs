namespace MonitoringDemo
{
    using System;

    static class ColoredConsole
    {
        public static IDisposable Use(ConsoleColor color)
        {
            var previousColor = Console.ForegroundColor;
            Console.ForegroundColor = color;

            return new Restorer(previousColor);
        }

        class Restorer(ConsoleColor previousColor) : IDisposable
        {
            public void Dispose()
            {
                Console.ForegroundColor = previousColor;
            }
        }
    }
}