namespace Shared;

class ButtonControl : IControl
{
    private readonly char inputId;
    private readonly char buttonKey;
    private readonly string helpMessage;
    private readonly string? pressedMessage;
    private readonly Action pressedAction;

    public ButtonControl(char inputId, char buttonKey, string helpMessage, string? pressedMessage, Action pressedAction)
    {
        this.inputId = inputId;
        this.buttonKey = buttonKey;
        this.helpMessage = helpMessage;
        this.pressedMessage = pressedMessage;
        this.pressedAction = pressedAction;
    }

    public bool Match(string input)
    {
        if (input[0] == buttonKey)
        {
            pressedAction();
            return true;
        }

        if (input[0] == '$' && input.Length >= 2 && input[1] == inputId)
        {
            pressedAction();
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
        if (pressedMessage != null)
        {
            textWriter.WriteLine(pressedMessage);
        }
    }
}