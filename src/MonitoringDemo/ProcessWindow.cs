using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using Terminal.Gui;
using Window = Terminal.Gui.Window;

namespace MonitoringDemo;

sealed partial class ProcessWindow : Window
{
    private const string Letters = "abcdefghijklmnopqrstuvwxyz";

    private readonly string name;
    private readonly bool singleInstance;
    private readonly DemoLauncher launcher;
    private readonly CancellationToken cancellationToken;

    private readonly ConcurrentDictionary<string, ObservableCollection<string>> linesPerInstance = new();
    private readonly HashSet<char> recognizedKeys = new();
    public ListView? InstanceView { get; }
    public ListView LogView { get; }
    private ObservableCollection<string> Instances { get; } = new();
    private Dictionary<string, ProcessHandle> Handles { get; } = new();

    [GeneratedRegex(@"Press (\w) to")]
    private static partial Regex PressKeyRegex();

    [GeneratedRegex(@"!BeginWidget (\w+) (\w+)")]
    private static partial Regex WidgetStartRegex();

    [GeneratedRegex(@"!EndWidget (\w+)")]
    private static partial Regex WidgetEndRegex();

    [GeneratedRegex(@"!Widget (\w+) (\w+)")]
    private static partial Regex WidgetUpdateRegex();

    public ProcessWindow(string title, string name, bool singleInstance, DemoLauncher launcher, CancellationToken cancellationToken = default)
    {
        this.name = name;
        this.singleInstance = singleInstance;
        this.launcher = launcher;
        this.cancellationToken = cancellationToken;

        Title = title;
        X = 0;
        Y = 1;
        Width = Dim.Fill();
        Height = Dim.Fill();

        if (!singleInstance)
        {
            InstanceView = new ListView
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                Source = new ListWrapper<string>(Instances)
            };
            InstanceView.SetSource(Instances);
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

            Add(instanceViewFrame);
        }

        LogView = new ListView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            Source = new ListWrapper<string>([])
        };
        LogView.AllowsMarking = false;
        LogView.AllowsMultipleSelection = false;
        var logViewFrame = new FrameView
        {
            X = InstanceView != null ? 15 : 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            Title = "Output"
        };
        logViewFrame.Add(LogView);

        Add(logViewFrame);

        KeyDown += Window_KeyDown;

        StartNewProcess();
    }

    private void Window_KeyDown(object? sender, Key e)
    {
        var instance = Instances[SelectedInstance];

        // TODO fix?
        if (e == Key.C.WithCtrl)
        {
            var lines = linesPerInstance.GetOrAdd(instance, _ => []);
            lines.Clear();
            LogView.SetSource(lines);
            e.Handled = true;
        }
        else if (e == Key.F1) //Press F1 for help
        {
            //Print help only in window that has focus
            if (HasFocus)
            {
                Handles[instance].Send("?");
                e.Handled = true;
            }
        }
    }

    int SelectedInstance => Math.Max(InstanceView?.SelectedItem ?? 0, 0);

    private void InstanceView_SelectedItemChanged(object? sender, ListViewItemEventArgs args)
    {
        SelectInstance(Instances[args.Item]);
    }

    void StartNewProcess()
    {
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
        LogView.SetSource(linesPerInstance.GetOrAdd(instance, _ => []));
        LogView.MoveEnd();
    }

    void PrintOutput(string instance, ProcessHandle handle, CancellationToken cancellationToken)
    {
        var lines = linesPerInstance.GetOrAdd(instance, _ => []);

        _ = Task.Run(async () =>
        {
            var activeWidgets = new Dictionary<string, IWidget>();
            var activeWidgetPositions = new Dictionary<string, int>();

            await foreach (var output in handle.ReadAllAsync(cancellationToken))
            {
                if (string.IsNullOrWhiteSpace(output))
                {
                    continue;
                }

                Application.Invoke(() =>
                {
                    var startWidgetMatch = WidgetStartRegex().Match(output);
                    if (startWidgetMatch.Success)
                    {
                        var widgetName = startWidgetMatch.Groups[1].Value;
                        var widgetId = startWidgetMatch.Groups[2].Value;

                        var widget = CreateWidget(widgetName);
                        if (widget != null)
                        {
                            activeWidgets[widgetId] = widget;
                        }
                        return;
                    }
                    var endWidgetMatch = WidgetEndRegex().Match(output);
                    if (endWidgetMatch.Success)
                    {
                        var widgetId = endWidgetMatch.Groups[1].Value;
                        activeWidgets.Remove(widgetId);
                        return;
                    }

                    var updateWidgetMatch = WidgetUpdateRegex().Match(output);
                    if (updateWidgetMatch.Success)
                    {
                        var widgetId = updateWidgetMatch.Groups[1].Value;
                        var widgetData = updateWidgetMatch.Groups[2].Value;

                        var widgetLine = activeWidgets[widgetId].ProcessInput(widgetData);

                        if (!activeWidgetPositions.TryGetValue(widgetId, out var position))
                        {
                            activeWidgetPositions[widgetId] = lines.Count;
                            lines.Add(widgetLine);

                        }
                        else
                        {
                            lines[position] = widgetLine;
                        }
                        LogView.MoveEnd(); // Scroll to end
                        return;
                    }

                    var pressKeyMatch = PressKeyRegex().Match(output);
                    if (pressKeyMatch.Success)
                    {
                        var groupValue = pressKeyMatch.Groups[1].Value[0];
                        recognizedKeys.Add(groupValue);
                        recognizedKeys.Add(char.ToLowerInvariant(groupValue));
                    }

                    lines.Add(output);
                    LogView.MoveEnd(); // Scroll to end
                });
            }
        });
    }

    private static IWidget? CreateWidget(string widgetName)
    {
        return widgetName == "Progress" ? new ProgressBarWidget() : null;
    }

    public void HandleKey(Key e)
    {
        var instance = Instances[SelectedInstance];

        var keyChar = (char)e.KeyCode;
        if (recognizedKeys.Contains(keyChar))
        {
            //If uppercase, send to all instances. If lowercase, send to selected instance
            if (e.IsShift)
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

            e.Handled = true;
        }
    }
}