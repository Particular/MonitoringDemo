namespace Shared;

public interface IControl
{
    bool Match(string input);
    void Help(TextWriter textWriter);
    void ReportState(TextWriter textWriter);
}