using MonitoringDemo;
using System.Reflection.Metadata;
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

ProcessWindow[] windows = [];
windows = [
    CreateWindow("Platform", "PlatformLauncher", "_Platform", true, cancellationToken),
    CreateWindow("Billing", "Billing", "_Billing", false, cancellationToken),
    CreateWindow("Shipping", "Shipping", "S_hipping", false, cancellationToken),
    CreateWindow("ClientUI", "ClientUI", "_ClientUI", true, cancellationToken),
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
foreach (var window in windows)
{
    top.Add(window);
}

foreach (var window in windows.Skip(1))
{
    window.Visible = false;
}

Application.KeyDown += ApplicationKeyDown;

void ApplicationKeyDown(object? sender, Key e)
{
    if (!e.IsKeyCodeAtoZ)
    {
        return;
    }

    foreach (var processWindow in windows)
    {
        processWindow.HandleKey(e);
        if (e.Handled)
        {
            break;
        }
    }
}

Application.Run(top);

Application.Shutdown();
return;


static void SwitchWindow(IReadOnlyCollection<ProcessWindow> windowsToHide, View windowToShow, View focusTarget)
{
    // Hide all other windows windows
    foreach (var window in windowsToHide)
    {
        window.Visible = false;
    }

    windowToShow.Visible = true;
    focusTarget.SetFocus();
    windowToShow.SetNeedsDraw();
}

ProcessWindow CreateWindow(string title, string name, string menuItemText, bool singleInstance, CancellationToken cancellationToken)
{
    var processWindow = new ProcessWindow(title, name, singleInstance, launcher, cancellationToken);
    var windowsToHide = windows.Except([processWindow]).ToArray();

    var menuItem = new MenuBarItem(menuItemText, "",
        () => SwitchWindow(windowsToHide, processWindow, processWindow.LogView));

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