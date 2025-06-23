using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Terminal.Gui;
using Window = Terminal.Gui.Window;

namespace MonitoringDemo;

sealed partial class ProcessWindow : Window
{
    private const string Letters = "abcdefghijklmnopqrstuvwxyz";

    private readonly string name;
    private readonly bool singleInstance;
    private readonly int basePort;
    private readonly DemoLauncher launcher;
    private readonly CancellationToken cancellationToken;

    private readonly ConcurrentDictionary<string, ObservableCollection<string>> linesPerInstance = new();
    private readonly Dictionary<Rune, char> recognizedKeys = new();
    public ListView? InstanceView { get; }
    public ListView LogView { get; }
    private ObservableCollection<string> Instances { get; } = new();

    private string?[] PrometheusPorts = new string[10];

    private Dictionary<string, Process> Processes { get; } = new();

    [GeneratedRegex(@"Press (.) to")]
    private static partial Regex PressKeyRegex();

    [GeneratedRegex(@"!BeginWidget (\w+) (\w+)")]
    private static partial Regex WidgetStartRegex();

    [GeneratedRegex(@"!EndWidget (\w+)")]
    private static partial Regex WidgetEndRegex();

    [GeneratedRegex(@"!Widget (\w+) (\w+)")]
    private static partial Regex WidgetUpdateRegex();

    public ProcessWindow(string title, string name, bool singleInstance, int basePort, DemoLauncher launcher, CancellationToken cancellationToken)
    {
        this.name = name;
        this.singleInstance = singleInstance;
        this.basePort = basePort;
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

        AddCommand(Command.DeleteAll, () =>
        {
            var instance = Instances[SelectedInstance];
            var lines = linesPerInstance.GetOrAdd(instance, _ => []);
            lines.Clear();
            LogView.SetSource(lines);
            return true;
        });
        AddCommand(Command.HotKey, () =>
        {
            var instance = Instances[SelectedInstance];
            Processes[instance].Send("?");
            return true;
        });
        AddCommand(Command.Up, () =>
        {
            ScaleOut();
            return true;
        });
        AddCommand(Command.Down, () =>
        {
            ScaleIn();
            return true;
        });

        KeyBindings.Add(Key.C.WithCtrl, Command.DeleteAll);
        KeyBindings.Add(Key.F1, Command.HotKey);

        if (!singleInstance)
        {
            KeyBindings.Add(Key.F2, Command.Up);
            KeyBindings.Add(Key.F3, Command.Down);
        }

        StartNewProcess(CancellationTokenSource.CreateLinkedTokenSource(cancellationToken));
    }

    private void ScaleIn()
    {
        var instance = Instances[SelectedInstance];
        DoScaleIn(instance);
    }

    private void ScaleInLast()
    {
        var instance = Instances.LastOrDefault();
        if (instance == null)
        {
            return;
        }
        DoScaleIn(instance);
    }

    private void DoScaleIn(string instance)
    {
        Debug.WriteLine($"Stopping instance {instance}");

        Processes.Remove(instance, out var process);
        process!.Dispose();
        Instances.Remove(instance);
        linesPerInstance.TryRemove(instance, out _);

        FreePort(instance);
    }

    private void ScaleOut()
    {
        Debug.WriteLine($"Starting new instance.");

        var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        StartNewProcess(cancellationTokenSource);
    }

    private void ScaleTo(int value)
    {
        var numberOfInstances = (value / 2) + 1; //Value is 0-9. Make it min one instance, max 5 instances
        Debug.WriteLine($"Scaling to {numberOfInstances}.");
        while (Instances.Count < numberOfInstances)
        {
            ScaleOut();
        }
        while (Instances.Count > numberOfInstances)
        {
            ScaleInLast();
        }
    }

    int SelectedInstance => Math.Max(InstanceView?.SelectedItem ?? 0, 0);

    private void InstanceView_SelectedItemChanged(object? sender, ListViewItemEventArgs args)
    {
        SelectInstance(Instances[args.Item]);
    }

    void StartNewProcess(CancellationTokenSource cancellationTokenSource)
    {
        string instanceId;
        do
        {
            instanceId = new string(Enumerable.Range(0, 4).Select(x => Letters[Random.Shared.Next(Letters.Length)]).ToArray());
        } while (Instances.Contains(instanceId));

        var port = FindPort(instanceId);
        if (port == null)
        {
            //No more free ports
            return;
        }

        var process = new Process(launcher.AddProcess(name, instanceId, basePort + port.Value), cancellationTokenSource);
        Processes[instanceId] = process;
        Instances.Add(instanceId);

        PrintOutput(instanceId, process, cancellationTokenSource.Token);

        SelectInstance(instanceId);
    }

    int? FindPort(string instance)
    {
        for (var i = 0; i < PrometheusPorts.Length; i++)
        {
            if (PrometheusPorts[i] == null)
            {
                PrometheusPorts[i] = instance;
                return i;
            }
        }
        return null;
    }

    void FreePort(string instance)
    {
        for (var i = 0; i < PrometheusPorts.Length; i++)
        {
            if (PrometheusPorts[i] == instance)
            {
                PrometheusPorts[i] = null;
            }
        }
    }

    void SelectInstance(string instance)
    {
        LogView.SetSource(linesPerInstance.GetOrAdd(instance, _ => []));
        LogView.MoveEnd();
    }

    void PrintOutput(string instance, Process process, CancellationToken cancellationToken)
    {
        var lines = linesPerInstance.GetOrAdd(instance, _ => []);

        _ = Task.Run(async () =>
        {
            try
            {
                var activeWidgets = new Dictionary<string, IWidget>();
                var activeWidgetPositions = new Dictionary<string, int>();

                await foreach (var output in process.ReadAllAsync(cancellationToken))
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
                            var c = char.ToLowerInvariant(groupValue);
                            recognizedKeys[(Rune)c] = c;
                        }

                        lines.Add(output);
                        //Simple hacky way to not store all the data in the world
                        if (lines.Count > 100)
                        {
                            lines.RemoveAt(0);
                        }
                        LogView.MoveEnd(); // Scroll to end
                    });
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Ignore cancellation
            }
        });
    }

    private static IWidget? CreateWidget(string widgetName)
    {
        return widgetName == "Progress" ? new ProgressBarWidget() : null;
    }

    public void HandleSequence(string sequenceWithoutDollar)
    {
        if (sequenceWithoutDollar is ['A', _, ..])
        {
            if (!singleInstance)
            {
                //First dial is scale out
                var scaleFactor = int.Parse(sequenceWithoutDollar.Substring(1, 1));
                ScaleTo(scaleFactor);
            }
        }
        else
        {
            foreach (var handle in Processes.Values)
            {
                handle.Send($"${sequenceWithoutDollar}");
            }
        }
    }

    public void HandleKey(Key e)
    {
        var instance = Instances[SelectedInstance];
        var r = e.AsRune;
        if (!recognizedKeys.TryGetValue(r, out var c))
        {
            return;
        }

        //If uppercase, send to all instances. If lowercase, send to selected instance
        if (e.IsShift)
        {
            foreach (var handle in Processes.Values)
            {
                handle.Send(new string(c, 1));
            }
        }
        else
        {
            Processes[instance].Send(new string(c, 1));
        }

        e.Handled = true;
    }

    sealed class Process(ProcessHandle handle, CancellationTokenSource cancellationTokenSource)
        : IDisposable
    {
        public void Send(string value)
        {
            handle.Send(value);
        }

        public IAsyncEnumerable<string?> ReadAllAsync(CancellationToken cancellationToken = default)
        {
            return handle.ReadAllAsync(cancellationToken);
        }

        public void Dispose()
        {
            cancellationTokenSource.Cancel();
            handle.Dispose();
            cancellationTokenSource.Dispose();
        }
    }
}