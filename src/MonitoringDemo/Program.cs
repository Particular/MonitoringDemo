using System.Diagnostics;
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
var clientWindow = CreateWindow("ClientUI", "ClientUI", "_ClientUI", false, cancellationToken);
var platformWindow = CreateWindow("Platform", "PlatformLauncher", "_Platform", true, cancellationToken);
var billingWindow = CreateWindow("Billing", "Billing", "_Billing", false, cancellationToken);
var shippingWindow = CreateWindow("Shipping", "Shipping", "S_hipping", false, cancellationToken);
var salesWindow = CreateWindow("Sales", "Sales", "_Sales", false, cancellationToken);

windows = [
    platformWindow,
    billingWindow,
    shippingWindow,
    clientWindow,
    salesWindow
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
    if (e.IsCtrl)
    {
        //Do not forward ctrl
        return;
    }

    if (e.IsPartOfControllerSequence(out var seq))
    {
        e.Handled = true;
        if (seq != null)
        {
            Debug.WriteLine(seq);
            if (seq[1] == '1')
            {
                //First controller is always wired to Client
                clientWindow.HandleSequence(seq.Substring(2));
            }
            else
            {
                var visibleWindow = windows.FirstOrDefault(x => x.Focused != null);
                visibleWindow?.HandleSequence(seq.Substring(2));
            }
        }
    }
    else
    {
        foreach (var processWindow in windows)
        {
            processWindow.HandleKey(e);
            if (e.Handled)
            {
                break;
            }
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
