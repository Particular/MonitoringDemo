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

        class Restorer : IDisposable
        {
            public Restorer(ConsoleColor previousColor)
            {
                this.previousColor = previousColor;
            }

            public void Dispose()
            {
                Console.ForegroundColor = previousColor;
            }

            private readonly ConsoleColor previousColor;
        }
    }
}