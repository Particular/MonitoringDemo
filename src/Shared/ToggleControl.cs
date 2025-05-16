namespace Shared;

class ToggleControl : IControl
{
    private readonly char inputId;
    private readonly char toggleKey;
    private readonly string helpMessage;
    private readonly Func<string> getState;
    private readonly Action enableAction;
    private readonly Action disableAction;
    private bool enabled;

    public ToggleControl(char inputId, char toggleKey, string helpMessage, Func<string> getState, Action enableAction,
        Action disableAction)
    {
        this.inputId = inputId;
        this.toggleKey = toggleKey;
        this.helpMessage = helpMessage;
        this.getState = getState;
        this.enableAction = enableAction;
        this.disableAction = disableAction;
    }

    public bool Match(string input)
    {
        if (input[0] == toggleKey)
        {
            enabled = !enabled;
            if (enabled)
            {
                enableAction();
            }
            else
            {
                disableAction();
            }
            return true;
        }

        if (input[0] == '~' && input.Length >= 3 && input[1] == inputId)
        {
            var value = int.Parse(input[2].ToString());
            enabled = value == 1;
            if (enabled)
            {
                enableAction();
            }
            else
            {
                disableAction();
            }
            return true;
        }

        return false;
    }

    public void Help(TextWriter textWriter)
    {
        textWriter.WriteLine(helpMessage);
    }

    public void ReportState(TextWriter textWriter)
    {
        textWriter.WriteLine(getState());
    }
}