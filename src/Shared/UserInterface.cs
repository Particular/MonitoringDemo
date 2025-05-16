namespace Shared;

public class UserInterface
{
    List<IControl> controls = [];

    public void BindDial(char inputId, char upKey, char downKey, string helpMessage, Func<string> getState, Action<int> action)
    {
        controls.Add(new DialControl(inputId, upKey, downKey, helpMessage, getState, action));
    }

    public void BindToggle(char inputId, char toggleKey, string helpMessage, Func<string> getState, Action enableAction, Action disableAction)
    {
        controls.Add(new ToggleControl(inputId, toggleKey, helpMessage, getState, enableAction, disableAction));
    }

    public void BindButton(char inputId, char buttonKey, string helpMessage, string? pressedMessage, Action pressedAction)
    {
        controls.Add(new ButtonControl(inputId, buttonKey, helpMessage, pressedMessage, pressedAction));
    }


#pragma warning disable PS0018
    public void RunLoop(string title)
#pragma warning restore PS0018
    {
        if (!Console.IsInputRedirected)
        {
            Console.Title = title;
        }

        PrintControls();

        while (true)
        {
            var input = ReadKeyOrLine();
            if (string.IsNullOrWhiteSpace(input))
            {
                return;
            }

            if (input == "?")
            {
                foreach (var ctrl in controls)
                {
                    ctrl.Help(Console.Out);
                }
            }

            var matchedControl = controls.FirstOrDefault(x => x.Match(input));
            if (matchedControl != null)
            {
                matchedControl.ReportState(Console.Out);
            }
        }
    }

    private static string? ReadKeyOrLine()
    {
        if (Console.IsInputRedirected)
        {
            return Console.ReadLine();
        }

        var key = Console.ReadKey(true);
        return new string(key.KeyChar, 1);
    }

    private static void PrintControls()
    {
        //foreach (var kvp in controls)
        //{
        //    Console.WriteLine($"Press {char.ToUpperInvariant(kvp.Key)} to {kvp.Value.Message}");
        //}
        Console.WriteLine("Press ? for help");
    }
}
