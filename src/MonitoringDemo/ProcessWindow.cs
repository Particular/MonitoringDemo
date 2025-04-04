using System.Collections.Concurrent;
using System.ComponentModel.Design;
using System.Text.RegularExpressions;
using Terminal.Gui;
using Window = Terminal.Gui.Window;

namespace MonitoringDemo;

partial class MultiInstanceProcessWindow
{
    private const string Letters = "abcdefghijklmnopqrstuvwxyz";

    private readonly string name;
    private readonly DemoLauncher launcher;

    private readonly ConcurrentDictionary<string, List<string>> linesPerInstance = new();
    private readonly HashSet<char> recognizedKeys = new();

    private readonly IWidgetFactory[] widgets =
    [
        new ProgressBarWidgetFactory()
    ];

    public Window Window { get; }
    public ListView InstanceView { get; }
    private ListView LogView { get; }
    private List<string> Instances { get; } = new();
    private Dictionary<string, ProcessHandle> Handles { get; } = new();

    [GeneratedRegex(@"Press (\w) to")]
    private static partial Regex PressKeyRegex();

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
        var instance = Instances[InstanceView.SelectedItem];

        if (obj.KeyEvent.Key == (Key.C | Key.CtrlMask))
        {
            var lines = linesPerInstance.GetOrAdd(instance, _ => []);
            lines.Clear();
            LogView.SetSource(lines);
            obj.Handled = true;
        }
        else if (obj.KeyEvent.KeyValue == 63)
        {
            //Print help only in window that has focus
            if (Window.HasFocus)
            {
                Handles[instance].Send("?");
                obj.Handled = true;
            }
        }

        var keyChar = (char)obj.KeyEvent.KeyValue;
        if (recognizedKeys.Contains(keyChar))
        {
            //If uppercase, send to all instances. If lowercase, send to selected instance
            if (char.IsUpper(keyChar))
            {
                foreach (var handle in Handles.Values)
                {
                    handle.Send(new string(keyChar, 1));
                }
            }
            else
            {
                Handles[instance].Send(new string(keyChar, 1));
            }
            obj.Handled = true;
        }
    }

    private void InstanceView_SelectedItemChanged(ListViewItemEventArgs obj)
    {
        SelectInstance(Instances[obj.Item]);
    }

    public void StartNewProcess(CancellationToken cancellationToken = default)
    {
        //TODO: Assumption: this is executed from the main loop of the App and is therefore thread-safe
        string instanceId;
        do
        {
            instanceId = new string(Enumerable.Range(0, 4).Select(x => Letters[Random.Shared.Next(Letters.Length)]).ToArray());
        } while (Instances.Contains(instanceId));

        var processHandle = launcher.AddProcess(name, instanceId);

        Handles[instanceId] = processHandle;
        Instances.Add(instanceId);

        PrintOutput(instanceId, processHandle, cancellationToken);

        SelectInstance(instanceId);
    }

    void SelectInstance(string instance)
    {
        //TODO: Assumption: this is executed from the main loop of the App and is therefore thread-safe
        LogView.SetSource(linesPerInstance.GetOrAdd(instance, _ => []));
        LogView.MoveEnd();
    }

    void PrintOutput(string instance, ProcessHandle handle, CancellationToken cancellationToken)
    {
        var lines = linesPerInstance.GetOrAdd(instance, _ => []);

        _ = Task.Run(async () =>
        {
            IWidget? currentWidget = null;
            await foreach (var output in handle.ReadAllAsync(cancellationToken))
            {
                if (string.IsNullOrWhiteSpace(output))
                {
                    continue;
                }

                Application.MainLoop.Invoke(() =>
                {
                    if (currentWidget != null)
                    {
                        var (processed, done) = currentWidget.ProcessInput(output);
                        if (processed == null)
                        {
                            lines.RemoveAt(lines.Count - 1);
                        }
                        else
                        {
                            lines[^1] = processed;
                        }
                        if (done)
                        {
                            currentWidget = null;
                        }
                        LogView.SetNeedsDisplay(LogView.Bounds);
                    }
                    else
                    {
                        currentWidget = widgets.Select(x => x.TryRecognize(output)).FirstOrDefault();
                        if (currentWidget == null)
                        {
                            //Recognizes the help messages and binds the keys
                            var match = PressKeyRegex().Match(output);
                            if (match.Success)
                            {
                                var groupValue = match.Groups[1].Value[0];
                                recognizedKeys.Add(groupValue);
                                recognizedKeys.Add(char.ToLowerInvariant(groupValue));
                            }

                            lines.Add(output);
                        }
                    }

                    LogView.MoveEnd(); // Scroll to end
                });
            }

            // while (await handle.Reader.WaitToReadAsync(cancellationToken))
            // {
            //     while (handle.Reader.TryRead(out var output))
            //     {
            //         if (string.IsNullOrWhiteSpace(output))
            //         {
            //             continue;
            //         }
            //
            //         Application.MainLoop.Invoke(() =>
            //         {
            //             lines.Add(output);
            //             LogView.MoveEnd(); // Scroll to end
            //         });
            //     }
            // }
        });
    }
}

public interface IWidgetFactory
{
    IWidget? TryRecognize(string line);
}

public interface IWidget
{
    (string?, bool) ProcessInput(string line);
}

public class ProgressBarWidgetFactory : IWidgetFactory
{
    public IWidget? TryRecognize(string line)
    {
        if (line.StartsWith("#Progress"))
        {
            return new ProgressBarWidget();
        }

        return null;
    }
}

public class ProgressBarWidget : IWidget
{
    public (string?, bool) ProcessInput(string line)
    {
        if (line.StartsWith("#ProgressEnd"))
        {
            return (null, true);
        }
        var progressPercent = int.Parse(line);
        var barsFilled = progressPercent / 10;
        var bars = new string('\u2588', barsFilled);
        var spaces = new string(' ', 10 - barsFilled);
        return ($"[{bars}{spaces}] {progressPercent}%", false);
    }
}


