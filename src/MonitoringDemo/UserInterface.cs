namespace Shared;

public static class UserInterface
{
#pragma warning disable PS0018
    public static void RunLoop(string title, Dictionary<char, (string Message, Action Action)> controls, Action<TextWriter> reportState)
#pragma warning restore PS0018
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