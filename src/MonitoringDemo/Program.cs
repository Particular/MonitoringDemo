using System.Text;
using System.Threading.Channels;
using MonitoringDemo;
using Terminal.Gui;

CancellationTokenSource tokenSource = new();

var remoteControlMode = true; //TODO
//    args.Length > 0 && string.Equals(args[0], bool.TrueString, StringComparison.InvariantCultureIgnoreCase);

Application.Init();
var top = Application.Top;

using var launcher = new DemoLauncher(remoteControlMode);
//Console.WriteLine("Starting the Particular Platform");

var platformWindow = PrepareProcessWindow(top, launcher, "PlatformLauncher", tokenSource.Token);
var billingWindow = PrepareProcessWindow(top, launcher, "Billing", tokenSource.Token);
var shippingWindow = PrepareProcessWindow(top, launcher, "Shipping", tokenSource.Token);
var clientWindow = PrepareProcessWindow(top, launcher, "ClientUI", tokenSource.Token);

top.Add(new MenuBar([
    new MenuBarItem("_Platform", "", () => BringWindowToFront(top, platformWindow.Window, platformWindow.View))
    {
    },
    new MenuBarItem("_Billing", "", () => BringWindowToFront(top, billingWindow.Window, billingWindow.View))
    {
    },
    new MenuBarItem("_Shipping", "", () => BringWindowToFront(top, shippingWindow.Window, shippingWindow.View))
    {
    },
    new MenuBarItem("_ClientUI", "", () => BringWindowToFront(top, clientWindow.Window, clientWindow.View))
    {
    },
    new MenuBarItem("_Quit", "", () =>
    {
        tokenSource.Cancel();
        Application.RequestStop();
    })
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

static ProcessWindow PrepareProcessWindow(Toplevel top, DemoLauncher launcher, string name, CancellationToken token)
{
    var window = CreateProcessWindow(name);
    top.Add(window.Window);
    var platformOutput = launcher.AddProcess(name);
    PrintOutput(window, platformOutput!.Reader, top, token);

    return window;
}

static ProcessWindow CreateProcessWindow(string title)
{
    var logLines = new List<string>();
    var listView = new ListView(logLines)
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
    window.Add(listView);

    return new ProcessWindow()
    {
        LogLines = logLines,
        View = listView,
        Window = window
    };
}

static void PrintOutput(ProcessWindow window, ChannelReader<string?> outputReader, Toplevel top, CancellationToken cancellationToken)
{
    _ = Task.Run(async () =>
    {
        while (await outputReader.WaitToReadAsync(cancellationToken))
        {
            //BringWindowToFront(top, window.Window, window.View);

            while (outputReader.TryRead(out var output))
            {
                if (string.IsNullOrWhiteSpace(output))
                {
                    continue;
                }
                Application.MainLoop.Invoke(() =>
                {
                    window.LogLines.Add(output);
                    window.View.MoveEnd(); // Scroll to end
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