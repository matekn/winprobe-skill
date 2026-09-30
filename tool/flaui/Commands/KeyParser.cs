using FlaUI.Core.WindowsAPI;
using Flaui.Cli;

namespace Flaui.Commands;

public static class KeyParser
{
    private static readonly Dictionary<string, VirtualKeyShort> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["enter"] = VirtualKeyShort.RETURN,
        ["return"] = VirtualKeyShort.RETURN,
        ["esc"] = VirtualKeyShort.ESCAPE,
        ["escape"] = VirtualKeyShort.ESCAPE,
        ["tab"] = VirtualKeyShort.TAB,
        ["space"] = VirtualKeyShort.SPACE,
        ["spacebar"] = VirtualKeyShort.SPACE,
        ["backspace"] = VirtualKeyShort.BACK,
        ["back"] = VirtualKeyShort.BACK,
        ["del"] = VirtualKeyShort.DELETE,
        ["delete"] = VirtualKeyShort.DELETE,
        ["ins"] = VirtualKeyShort.INSERT,
        ["insert"] = VirtualKeyShort.INSERT,
        ["up"] = VirtualKeyShort.UP,
        ["down"] = VirtualKeyShort.DOWN,
        ["left"] = VirtualKeyShort.LEFT,
        ["right"] = VirtualKeyShort.RIGHT,
        ["home"] = VirtualKeyShort.HOME,
        ["end"] = VirtualKeyShort.END,
        ["pageup"] = VirtualKeyShort.PRIOR,
        ["pgup"] = VirtualKeyShort.PRIOR,
        ["pagedown"] = VirtualKeyShort.NEXT,
        ["pgdn"] = VirtualKeyShort.NEXT,
        ["ctrl"] = VirtualKeyShort.CONTROL,
        ["control"] = VirtualKeyShort.CONTROL,
        ["shift"] = VirtualKeyShort.SHIFT,
        ["alt"] = VirtualKeyShort.ALT,
        ["win"] = VirtualKeyShort.LWIN,
        ["windows"] = VirtualKeyShort.LWIN,
        ["plus"] = VirtualKeyShort.OEM_PLUS,
        ["minus"] = VirtualKeyShort.OEM_MINUS,
        ["comma"] = VirtualKeyShort.OEM_COMMA,
        ["period"] = VirtualKeyShort.OEM_PERIOD,
        ["dot"] = VirtualKeyShort.OEM_PERIOD,
        ["slash"] = VirtualKeyShort.OEM_2,
        ["semicolon"] = VirtualKeyShort.OEM_1,
        ["quote"] = VirtualKeyShort.OEM_7,
    };

    public static VirtualKeyShort Parse(string raw)
    {
        string s = raw.Trim();
        if (Aliases.TryGetValue(s, out var alias)) return alias;

        if (s.Length == 1)
        {
            char c = char.ToUpperInvariant(s[0]);
            if (c is >= 'A' and <= 'Z') return (VirtualKeyShort)Enum.Parse(typeof(VirtualKeyShort), "KEY_" + c);
            if (c is >= '0' and <= '9') return (VirtualKeyShort)Enum.Parse(typeof(VirtualKeyShort), "KEY_" + c);
        }

        string enumName = s.Replace("-", "_").Replace(" ", "_").ToUpperInvariant();
        if (Enum.TryParse<VirtualKeyShort>(enumName, ignoreCase: true, out var vk)) return vk;

        throw new CommandException($"Unknown key '{raw}'. Try a letter, digit, F1-F24, enter, esc, tab, space, backspace, delete, up/down/left/right, home, end, pageup, pagedown, ctrl, shift, alt, win.", 2);
    }
}
