using MonitoringDemo;
using Terminal.Gui;

CancellationTokenSource tokenSource = new();
var cancellationToken = tokenSource.Token;

Application.Init();
var top = Application.Top;

using var launcher = new DemoLauncher();
//Console.WriteLine("Starting the Particular Platform");

var menuBarItems = new List<MenuBarItem>();

var platformWindow = CreateWindow("Platform", "PlatformLauncher", "_Platform", cancellationToken);

var billingWindow = CreateWindow("Billing", "Billing", "_Billing", cancellationToken);

var shippingWindow = CreateWindow("Shipping", "Shipping", "S_hipping", cancellationToken);

var clientUIWindow = CreateWindow("ClientUI", "ClientUI", "_ClientUI", cancellationToken);

var salesWindow = CreateWindow("Sales", "Sales", "_Sales", cancellationToken);

//TODO: Figure out why Sales scale out causes errors
//salesWindow.StartNewProcess(tokenSource.Token);

menuBarItems.Add(
    new MenuBarItem("_Quit", "", () =>
    {
        tokenSource.Cancel();
        Application.RequestStop();
    }));

top.Add(new MenuBar(menuBarItems.ToArray()));

Application.Run();

Application.Shutdown();


static void BringWindowToFront(Toplevel top, View window, View focusTarget)
{
    top.BringSubviewToFront(window);
    focusTarget.SetFocus(); // Focus a control *within* the window
    window.SetNeedsDisplay();
}

MultiInstanceProcessWindow CreateWindow(string title, string name, string menuItemText, CancellationToken cancellationToken)
{
    var processWindow = new MultiInstanceProcessWindow(title, name, launcher);
    top.Add(processWindow.Window);
    processWindow.StartNewProcess(cancellationToken);

    var menuItem = new MenuBarItem(menuItemText, "",
        () => BringWindowToFront(top, processWindow.Window, processWindow.InstanceView));

    menuBarItems.Add(menuItem);

    return processWindow;
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