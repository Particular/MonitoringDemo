using System.Text;
using Terminal.Gui;

namespace MonitoringDemo;

static class KeyHelper
{
    private static readonly string AllRecognizedKeys = "1234567890-=qwertyuiop[]asdfghjkl;'zxcvbnm,./";
    private static readonly Rune[] Runes = AllRecognizedKeys.Select(x => (Rune)x).ToArray();

    public static bool IsRecognized(this Key k)
    {
        var rune = k.AsRune;
        return Runes.Contains(rune);
    }
}