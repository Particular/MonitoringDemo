namespace MonitoringDemo;

public class ProgressBarWidget : IWidget
{
    public string ProcessInput(string line)
    {
        var progressPercent = int.Parse(line);
        var barsFilled = progressPercent / 10;
        var bars = new string('\u2588', barsFilled);
        var spaces = new string(' ', 10 - barsFilled);
        return $"[{bars}{spaces}] {progressPercent}%";
    }
}