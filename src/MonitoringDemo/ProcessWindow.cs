using System.Collections.Concurrent;
using System.Threading.Channels;
using System.Xml;
using Terminal.Gui;
using Window = Terminal.Gui.Window;

namespace MonitoringDemo;

class ProcessWindow
{
    public Window Window { get;}
    public ListView View { get; }
    public List<string> LogLines { get; }

    public ProcessWindow(string title)
    {
        LogLines = new List<string>();
        View = new ListView(LogLines)
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };

        Window = new Window(title)
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        Window.Add(View);
    }
}

class MultiInstanceProcessWindow
{
    private const string Letters = "abcdefghijklmnopqrstuvwxyz";

    private readonly string name;
    private readonly DemoLauncher launcher;

    private readonly ConcurrentDictionary<string, List<string>> linesPerInstance = new();

    public Window Window { get; }
    public ListView InstanceView { get; }
    public ListView LogView { get; }
    public List<string> Instances { get; } = new();
    public List<int> Processes { get; } = new();

    public MultiInstanceProcessWindow(string title, string name, DemoLauncher launcher)
    {
        this.name = name;
        this.launcher = launcher;
        InstanceView = new ListView(Instances)
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };
        InstanceView.SelectedItemChanged += InstanceView_SelectedItemChanged;

        var instanceViewFrame = new FrameView
        {
            X = 0,
            Y = 0,
            Width = 15,
            Height = Dim.Fill(),
            Title = "Instances"
        };
        instanceViewFrame.Add(InstanceView);

        LogView = new ListView(new List<string>())
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };

        var logViewFrame = new FrameView
        {
            X = 15,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            Title = "Output"
        };
        logViewFrame.Add(LogView);

        Window = new Window(title)
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        Window.Add(instanceViewFrame);
        Window.Add(logViewFrame);

        Window.KeyPress += Window_KeyPress;
    }

    private void Window_KeyPress(View.KeyEventEventArgs obj)
    {
        if (obj.KeyEvent.Key == (Key.C | Key.CtrlMask))
        {
            var instance = Instances[InstanceView.SelectedItem];
            var lines = linesPerInstance.GetOrAdd(instance, _ => []);
            lines.Clear();
            LogView.SetSource(lines);
            obj.Handled = true;
        }
    }

    private void InstanceView_SelectedItemChanged(ListViewItemEventArgs obj)
    {
        SelectInstance(Instances[obj.Item]);
    }

#pragma warning disable PS0003
    public void StartNewProcess(CancellationToken cancellationToken)
#pragma warning restore PS0003
    {
        //TODO: Assumption: this is executed from the main loop of the App and is therefore thread-safe
        string instanceId;
        do
        {
            instanceId = new string(Enumerable.Range(0, 4).Select(x => Letters[Random.Shared.Next(Letters.Length)]).ToArray());
        } while (Instances.Contains(instanceId));

        var (instanceOutput, processId) = launcher.AddProcess(name, instanceId);

        Instances.Add(instanceId);
        Processes.Add(processId);

        PrintOutput(instanceId, instanceOutput!.Reader, cancellationToken);

        SelectInstance(instanceId);
    }

    void SelectInstance(string instance)
    {
        //TODO: Assumption: this is executed from the main loop of the App and is therefore thread-safe
        LogView.SetSource(linesPerInstance.GetOrAdd(instance, _ => []));
        LogView.MoveEnd();
    }

    void PrintOutput(string instance, ChannelReader<string?> outputReader, CancellationToken cancellationToken)
    {
        var lines = linesPerInstance.GetOrAdd(instance, _ => []);

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
                        lines.Add(output);
                        LogView.MoveEnd(); // Scroll to end
                    });
                }
            }
        });
    }
}


