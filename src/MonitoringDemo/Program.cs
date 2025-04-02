using System.Text;
using System.Threading.Channels;
using MonitoringDemo;
using Terminal.Gui;

CancellationTokenSource tokenSource = new();

var remoteControlMode =
    args.Length > 0 && string.Equals(args[0], bool.TrueString, StringComparison.InvariantCultureIgnoreCase);

Application.Init();
var top = Application.Top;

using var launcher = new DemoLauncher(remoteControlMode);
//Console.WriteLine("Starting the Particular Platform");

var (platformWindow, platformTextView, platformLogLines) = CreateProcessWindow("Platform");
top.Add(platformWindow);
var platformOutput = launcher.Platform();
PrintOutput(platformTextView, platformLogLines, platformOutput!.Reader, tokenSource.Token);

var (billingWindow, billingTextView, billingLogLines) = CreateProcessWindow("Billing");
top.Add(billingWindow);
var billingOutput = launcher.Billing();
PrintOutput(billingTextView, billingLogLines, billingOutput!.Reader, tokenSource.Token);

top.Add(new MenuBar([
    new MenuBarItem("_Windows", [
        new MenuItem("_Platform", "", () => BringWindowToFront(top, platformWindow, platformTextView), shortcut: Key.F1),
        new MenuItem("_Billing", "", () => BringWindowToFront(top, billingWindow, billingTextView),  shortcut: Key.F2),
        new MenuItem("_Quit", "", () => {
            tokenSource.Cancel();
            Application.RequestStop();
        })
    ])
]));

// using (ColoredConsole.Use(ConsoleColor.Yellow))
// {
//     Console.WriteLine(
//         "Once ServiceControl has finished starting a browser window will pop up showing the ServicePulse monitoring tab");
// }
//
// Console.WriteLine("Starting Demo Solution");

Application.Run();

Application.Shutdown();

if (!tokenSource.IsCancellationRequested)
{
    // Console.WriteLine("Starting Billing endpoint.");
    // launcher.Billing();

    // Console.WriteLine("Starting Sales endpoint.");
    // launcher.ScaleOutSales();

    // Console.WriteLine("Starting Shipping endpoint.");
    // launcher.Shipping();

    // Console.WriteLine("Starting ClientUI endpoint.");
    // launcher.ClientUI();

    //ScaleSalesEndpointIfRequired(launcher, syncEvent);


}


static void BringWindowToFront(Toplevel top, View window, View focusTarget)
{
    top.BringSubviewToFront(window);
    focusTarget.SetFocus(); // Focus a control *within* the window
    window.SetNeedsDisplay();
}

static (Window Window, ListView View, IList<string> LogLines) CreateProcessWindow(string title)
{
    var logLines = new List<string>();
    var textView = new ListView(logLines)
    {
        X = 0,
        Y = 0,
        Width = Dim.Fill(),
        Height = Dim.Fill(),
    };

    var window = new Window(title)
    {
        X = 0,
        Y = 1,
        Width = Dim.Fill(),
        Height = Dim.Fill()
    };
    window.Add(textView);
    return (window, textView, logLines);
}

static void PrintOutput(ListView listView, IList<string> logLines, ChannelReader<string?> outputReader, CancellationToken cancellationToken)
{
    _ = Task.Run(async () =>
    {
        while (await outputReader.WaitToReadAsync(cancellationToken))
        {
            while (outputReader.TryRead(out var output))
            {
                if (string.IsNullOrWhiteSpace(output))
                {
                    continue;
                }
                Application.MainLoop.Invoke(() =>
                {
                    logLines.Add(output);
                    listView.MoveEnd(); // Scroll to end
                });
            }
        }
    });
}

// void ScaleSalesEndpointIfRequired(DemoLauncher launcher, TaskCompletionSource<bool> syncEvent)
// {
//     _ = Task.Run(() =>
//     {
//         try
//         {
//             Console.WriteLine();
//             Console.WriteLine("Press [up arrow] to scale out the Sales service or [down arrow] to scale in");
//             Console.WriteLine("Press Ctrl+C stop Particular Monitoring Demo.");
//             Console.WriteLine();
//
//             while (!tokenSource.IsCancellationRequested)
//             {
//                 var input = Console.ReadKey(true);
//                 switch (input.Key)
//                 {
//                     case ConsoleKey.LeftArrow:
//                         launcher.ScaleInSales();
//                         break;
//                     case ConsoleKey.RightArrow:
//                         launcher.ScaleOutSales();
//                         break;
//                     default:
//                         launcher.Send(new string(input.KeyChar, 1));
//                         break;
//                 }
//             }
//         }
//         catch (OperationCanceledException)
//         {
//             // ignore
//         }
//         catch (Exception e)
//         {
//             // surface any other exception
//             syncEvent.TrySetException(e);
//         }
//     });
// }