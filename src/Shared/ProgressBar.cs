namespace Shared;

public class ProgressBar : IDisposable
{
    private readonly string description;
    private readonly string widgetId = Guid.NewGuid().ToString("N");

    public ProgressBar(string description)
    {
        this.description = description;
        if (Console.IsOutputRedirected)
        {
            Console.WriteLine(description);
            Console.WriteLine($"!BeginWidget Progress {widgetId}");
        }
    }

    public void Update(int percent)
    {
        if (Console.IsOutputRedirected)
        {
            Console.WriteLine($"!Widget {widgetId} {percent}");
        }
        else if (percent % 25 == 0)
        {
            Console.WriteLine($"{description}: {percent}%");
        }
    }

    public void Dispose()
    {
        if (Console.IsOutputRedirected)
        {
            Console.WriteLine($"!EndWidget {widgetId}");
        }
    }
}