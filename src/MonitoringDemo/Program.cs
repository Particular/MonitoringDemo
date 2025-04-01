using System.Text;
using System.Threading.Channels;
using MonitoringDemo;
using Terminal.Gui;

CancellationTokenSource tokenSource = new();
//Console.Title = "MonitoringDemo";

var remoteControlMode =
    args.Length > 0 && string.Equals(args[0], bool.TrueString, StringComparison.InvariantCultureIgnoreCase);
var syncEvent = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

// Console.CancelKeyPress += (sender, eventArgs) =>
// {
//     eventArgs.Cancel = true;
//     tokenSource.Cancel();
//     syncEvent.TrySetResult(true);
// };

Application.Init();
var top = Application.Top;

using var launcher = new DemoLauncher(remoteControlMode);
//Console.WriteLine("Starting the Particular Platform");

var (platformWindow, platformTextView) = CreateProcessWindow("Platform");
top.Add(platformWindow);
var platformOutput = launcher.Platform();
PrintOutput(platformTextView, platformOutput!.Reader, tokenSource.Token);

var (billingWindow, billingTextView) = CreateProcessWindow("Billing");
top.Add(billingWindow);
var billingOutput = launcher.Billing();
PrintOutput(billingTextView, billingOutput!.Reader, tokenSource.Token);

top.Add(new MenuBar([
    new MenuBarItem("_Windows", [
        new MenuItem("Platform", "", () => BringWindowToFront(top, platformWindow), shortcut: Key.F1),
        new MenuItem("Billing", "", () => BringWindowToFront(top, billingWindow),  shortcut: Key.F2),
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

    Application.Run();
}

// using (ColoredConsole.Use(ConsoleColor.Yellow))
// {
//     Console.WriteLine("Done, press ENTER.");
//     Console.ReadLine();
// }


static void BringWindowToFront(Toplevel top, View window)
{
    top.BringSubviewToFront(window);
    top.SetFocus();
    window.SetNeedsDisplay();
}

static (Window Window, TextView View) CreateProcessWindow(string title)
{
    var textView = new TextView
    {
        X = 0,
        Y = 0,
        Width = Dim.Fill(),
        Height = Dim.Fill(),
        ReadOnly = true
    };

    var window = new Window(title)
    {
        X = 0,
        Y = 1,
        Width = Dim.Fill(),
        Height = Dim.Fill()
    };
    window.Add(textView);
    return (window, textView);
}

static void PrintOutput(TextView textView, ChannelReader<string?> outputReader, CancellationToken cancellationToken)
{
    _ = Task.Run(async () =>
    {
        while (await outputReader.WaitToReadAsync(cancellationToken))
        {
            while (outputReader.TryRead(out var output))
            {
                if (output is null)
                {
                    continue;
                }
                var item = output;
                Application.MainLoop.Invoke(() =>
                {
                    textView.Text += item + Environment.NewLine;
                    textView.MoveEnd();
                });
                await Task.Delay(500, cancellationToken);
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