namespace Shared;

public class ProgressBar : IDisposable
{
    private readonly string widgetId = Guid.NewGuid().ToString("N");

    public ProgressBar()
    {
        Console.WriteLine($"!BeginWidget Progress {widgetId}");
    }

    public void Update(int percent)
    {
        Console.WriteLine($"!Widget {widgetId} {percent}");
    }

    public void Dispose()
    {
        Console.WriteLine($"!EndWidget {widgetId}");
    }
}