using System.Drawing;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA3;
using Flaui.Cli;
using Flaui.Core;

namespace Flaui.Commands;

public static class InputCommands
{
    public static int PressKey(UIA3Automation automation, ArgParser args)
    {
        var keys = ParseSequence(args);
        if (keys.Count != 1)
            throw new CommandException($"press-key takes exactly one key; got {keys.Count}. Use key-combo for chords.", 2);
        Keyboard.Press(keys[0]);
        Delay(args);
        return 0;
    }

    public static int KeyCombo(UIA3Automation automation, ArgParser args)
    {
        var keys = ParseSequence(args);
        Keyboard.TypeSimultaneously(keys.ToArray());
        Delay(args);
        return 0;
    }

    private static List<VirtualKeyShort> ParseSequence(ArgParser args)
    {
        string? spec = args.Get("key") ?? args.Get("keys") ?? (args.Positional.Count > 0 ? args.Positional[0] : null);
        if (string.IsNullOrWhiteSpace(spec))
            throw new CommandException("No keys given. Pass --key <key|combo> or a positional argument.", 2);

        var keys = new List<VirtualKeyShort>();
        foreach (var part in spec.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            keys.Add(KeyParser.Parse(part));
        }
        return keys;
    }

    public static int Move(UIA3Automation automation, ArgParser args)
    {
        if (args.Get("x") is { } xs && args.Get("y") is { } ys && int.TryParse(xs, out int x) && int.TryParse(ys, out int y))
        {
            Mouse.MoveTo(new Point(x, y));
        }
        else if (Targets.Selector(args) != null)
        {
            var window = Targets.ResolveWindow(automation, args);
            var el = Targets.ResolveElement(automation, window, args);
            if (!args.Has("quiet")) Console.WriteLine("target: " + Describe.Line(el, Array.Empty<string>()));
            var r = el.BoundingRectangle;
            Mouse.MoveTo(new Point(r.X + r.Width / 2, r.Y + r.Height / 2));
        }
        else
        {
            throw new CommandException("Pass --x/--y or an element selector (--by/--value).", 2);
        }
        return 0;
    }

    public static int Drag(UIA3Automation automation, ArgParser args)
    {
        var window = Targets.ResolveWindow(automation, args);
        var from = Targets.ResolveElement(automation, window, args);
        if (!args.Has("quiet")) Console.WriteLine("from: " + Describe.Line(from, Array.Empty<string>()));
        var fr = from.BoundingRectangle;
        var start = new Point(fr.X + fr.Width / 2, fr.Y + fr.Height / 2);

        Point end;
        if (args.Get("to-x") is { } txs && args.Get("to-y") is { } tys && int.TryParse(txs, out int tx) && int.TryParse(tys, out int ty))
        {
            end = new Point(tx, ty);
        }
        else
        {
            var toArgs = new ArgParser();
            if (args.Get("to-by") is { } tb) toArgs.Options["by"] = tb;
            if (args.Get("to-value") is { } tv) toArgs.Options["value"] = tv;
            if (args.Get("to-name") is { } tn) toArgs.Options["name"] = tn;
            if (args.Get("to-automation-id") is { } ta) toArgs.Options["automation-id"] = ta;
            var to = Targets.ResolveElement(automation, window, toArgs);
            if (!args.Has("quiet")) Console.WriteLine("to: " + Describe.Line(to, Array.Empty<string>()));
            var tr = to.BoundingRectangle;
            end = new Point(tr.X + tr.Width / 2, tr.Y + tr.Height / 2);
        }

        Mouse.Drag(start, end, MouseButton.Left);
        Delay(args);
        return 0;
    }

    private static void Delay(ArgParser args)
    {
        int delay = args.GetInt("delay", 0);
        if (delay > 0) Thread.Sleep(delay);
    }
}
