namespace Shared;

public static class UserInterface
{
    public static void RunLoop(string title, Dictionary<char, (string Message, Action Action)> controls, Action<TextWriter> reportState, bool interactive)
    {
        if (interactive)
        {
            RunInteractiveLoop(title, controls, reportState);
        }
        else
        {
            RunNonInteractiveLoop(title, controls, reportState);
        }
    }

    static void RunInteractiveLoop(string title, Dictionary<char, (string Message, Action Action)> controls, Action<TextWriter> reportState)
    {
        Console.Title = title;
        Console.SetWindowSize(65, 15);

        while (true)
        {
            Console.Clear();
            foreach (var kvp in controls)
            {
                Console.WriteLine($"Press {char.ToUpperInvariant(kvp.Key)} to {kvp.Value.Message}");
            }
            Console.WriteLine("Press ESC to quit");
            Console.WriteLine();

            reportState(Console.Out);

            var input = Console.ReadKey(true);

            if (controls.TryGetValue(char.ToLowerInvariant(input.KeyChar), out var control))
            {
                control.Action();
            }
            else if (input.Key == ConsoleKey.Escape)
            {
                return;
            }
        }
    }

    static void RunNonInteractiveLoop(string title, Dictionary<char, (string Message, Action Action)> controls, Action<TextWriter> reportState)
    {
        Console.Title = title;

        reportState(Console.Out);
        PrintControls(controls);

        while (true)
        {
            var input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
            {
                return;
            }

            var key = input[0];

            if (controls.TryGetValue(char.ToLowerInvariant(key), out var control))
            {
                control.Action();
                reportState(Console.Out);
            }
            else if (key == '?')
            {
                PrintControls(controls);
            }
        }
    }

    private static void PrintControls(Dictionary<char, (string Message, Action Action)> controls)
    {
        foreach (var kvp in controls)
        {
            Console.WriteLine($"Press {char.ToUpperInvariant(kvp.Key)} to {kvp.Value.Message}");
        }
        Console.WriteLine("Press ? for help");
    }
}