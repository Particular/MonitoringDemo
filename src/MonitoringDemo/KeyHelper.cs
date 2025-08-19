using System.Text;
using Terminal.Gui.Input;

namespace MonitoringDemo;

static class KeyHelper
{
    private static readonly string AllRecognizedKeys = "1234567890-=qwertyuiop[]asdfghjkl;'zxcvbnm,./";
    private static readonly Rune[] Runes = AllRecognizedKeys.Select(x => (Rune)x).ToArray();
    private static readonly Rune dollarRune = (Rune)'$';

    public static bool IsRecognized(this Key k)
    {
        var rune = k.AsRune;
        return Runes.Contains(rune);
    }

    public static bool IsPartOfControllerSequence(this Key k, out string? sequence)
    {
        var r = k.AsRune;
        if (r.Value == 0)
        {
            sequence = null;
            return false;
        }
        if (r == dollarRune)
        {
            //Begin a new sequence regardless
            currentSequence = "$";
            sequence = null;
            return true;
        }
        if (currentSequence != null)
        {
            currentSequence += r.ToString();

            if (currentSequence.Length == 4)
            {
                //Sequence is complete
                sequence = currentSequence;
                currentSequence = null;
            }
            else
            {
                sequence = null;
            }
            return true;
        }

        sequence = null;
        return false;
    }

    private static string? currentSequence;
}