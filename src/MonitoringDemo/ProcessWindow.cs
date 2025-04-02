using Terminal.Gui;

namespace MonitoringDemo;

class ProcessWindow
{
    public required Window Window { get; init; }
    public required ListView View { get; init; }
    public required IList<string> LogLines { get; init; }
}
