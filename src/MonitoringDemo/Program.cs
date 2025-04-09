using MonitoringDemo;
using Terminal.Gui;

CancellationTokenSource tokenSource = new();
var cancellationToken = tokenSource.Token;

Application.Init();

using var launcher = new DemoLauncher();

using var top = new Window();
top.Title = "Particular Monitoring Demo";
top.X = 0;
top.Y = 1;
top.Width = Dim.Fill();
top.Height = Dim.Fill();

var menuBarItems = new List<MenuBarItem>();

ProcessWindow[] windows = [
    CreateWindow("Platform", "PlatformLauncher", "_Platform", true, cancellationToken),
    CreateWindow("Billing", "Billing", "_Billing", false, cancellationToken),
    CreateWindow("Shipping", "Shipping", "S_hipping", false, cancellationToken),
    CreateWindow("ClientUI", "ClientUI", "_ClientUI", false, cancellationToken),
    CreateWindow("Sales", "Sales", "_Sales", false, cancellationToken)
];

menuBarItems.Add(
    new MenuBarItem("_Quit", "", () =>
    {
        tokenSource.Cancel();
        Application.RequestStop();
    }));

top.Add(new MenuBar
{
    Menus = menuBarItems.ToArray()
});

Application.Run(top);

Application.Shutdown();


static void BringWindowToFront(Toplevel top, View window, View focusTarget)
{
    top.MoveSubViewToStart(window);
    //top.BringSubviewToFront(window);
    focusTarget.SetFocus(); // Focus a control *within* the window
    //window.SetNeedsDisplay();
}

ProcessWindow CreateWindow(string title, string name, string menuItemText, bool singleInstance, CancellationToken cancellationToken)
{
    var processWindow = new ProcessWindow(title, name, singleInstance, launcher, cancellationToken);
    top.Add(processWindow.Window);

    var menuItem = new MenuBarItem(menuItemText, "",
        () => BringWindowToFront(top, processWindow.Window, processWindow.LogView));

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