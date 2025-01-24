namespace Shared;

public class UserInterface
{
    public static void RunLoop(string title, Dictionary<char, (string, Action)> controls, Action<TextWriter> reportState, bool interactive)
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

    static void RunInteractiveLoop(string title, Dictionary<char, (string, Action)> controls, Action<TextWriter> reportState)
    {
        Console.Title = title;
        Console.SetWindowSize(65, 15);

        while (true)
        {
            Console.Clear();
            foreach (var kvp in controls)
            {
                Console.WriteLine($"Press {char.ToUpperInvariant(kvp.Key)} to {kvp.Value.Item1}");
            }
            Console.WriteLine("Press ESC to quit");
            Console.WriteLine();

            reportState(Console.Out);

            var input = Console.ReadKey(true);

            if (controls.TryGetValue(char.ToLowerInvariant(input.KeyChar), out var action))
            {
                action.Item2();
            }
            else if (input.Key == ConsoleKey.Escape)
            {
                return;
            }
        }
    }

    static void RunNonInteractiveLoop(string title, Dictionary<char, (string, Action)> controls, Action<TextWriter> reportState)
    {
        Console.Title = title;

        reportState(Console.Out);

        while (true)
        {
            var input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
            {
                return;
            }

            var key = input[0];

            if (controls.TryGetValue(char.ToLowerInvariant(key), out var action))
            {
                action.Item2();
                reportState(Console.Out);
            }
        }
    }
}