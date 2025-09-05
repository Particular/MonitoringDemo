using System.Threading.Channels;

namespace MonitoringDemo;

sealed class ProcessHandle(Channel<string?> outputChannel, Action<string> sendAction, Action closeAction)
    : IDisposable
{
    public ChannelReader<string?> Reader { get; } = outputChannel.Reader;

    public void Send(string value)
    {
        sendAction(value);
    }

    public IAsyncEnumerable<string?> ReadAllAsync(CancellationToken cancellationToken = default) {
        return outputChannel.Reader.ReadAllAsync(cancellationToken);
    }

    public void Dispose()
    {
        outputChannel.Writer.TryComplete();
        closeAction();
    }

    public static readonly ProcessHandle Empty = new(Channel.CreateBounded<string?>(0), _ => { }, () => { });
}