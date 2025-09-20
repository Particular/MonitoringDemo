namespace Shared;

class DialControl : IControl
{
    private int value;
    private readonly char inputId;
    private readonly char upKey;
    private readonly char downKey;
    private readonly string helpMessage;
    private readonly Func<string> getState;
    private readonly Action<int> setAction;

    public DialControl(char inputId, char upKey, char downKey, string helpMessage, Func<string> getState,
        Action<int> setAction)
    {
        this.inputId = inputId;
        this.upKey = upKey;
        this.downKey = downKey;
        this.helpMessage = helpMessage;
        this.getState = getState;
        this.setAction = setAction;
    }

    public bool Match(string input)
    {
        if (input[0] == upKey)
        {
            //Increase
            if (value < 9)
            {
                value++;
            }

            setAction(value);
            return true;
        }

        if (input[0] == downKey)
        {
            //Decrease
            if (value > 0)
            {
                value--;
            }

            setAction(value);
            return true;
        }

        if (input[0] == '$' && input.Length >= 3 && input[1] == inputId)
        {
            value = int.Parse(input[2].ToString());
            setAction(value);
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