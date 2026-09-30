using System.Drawing;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.UIA3;
using Flaui.Cli;
using Flaui.Core;

namespace Flaui.Commands;

public static class ActionCommands
{
    private static AutomationElement Target(UIA3Automation automation, ArgParser args)
    {
        var window = Targets.ResolveWindow(automation, args);
        var element = Targets.ResolveElement(automation, window, args);
        if (!args.Has("quiet"))
            Console.WriteLine("target: " + Describe.Line(element, Array.Empty<string>()));
        return element;
    }

    private static string TargetText(ArgParser args)
    {
        string? t = args.Get("text");
        if (t is null && args.Positional.Count > 0) t = args.Positional[0];
        if (t is null) throw new CommandException("No text given. Pass --text <value> or a positional argument.", 2);
        return t;
    }

    private static Point Center(AutomationElement el)
    {
        var r = el.BoundingRectangle;
        if (r.Width <= 0 || r.Height <= 0)
            throw new CommandException("Element has zero-size bounds (probably offscreen); cannot click.", 3);
        return new Point(r.X + r.Width / 2, r.Y + r.Height / 2);
    }

    private static void FocusIfPossible(AutomationElement el)
    {
        try { el.Focus(); } catch { }
    }

    public static int Click(UIA3Automation automation, ArgParser args)
    {
        var el = Target(automation, args);
        if (!args.Has("mouse"))
        {
            var invoke = Try(() => el.Patterns.Invoke.PatternOrDefault);
            if (invoke != null)
            {
                try
                {
                    invoke.Invoke();
                    PostAction(args);
                    return 0;
                }
                catch
                {
                    // fall back to a real mouse click below
                }
            }
        }
        Mouse.LeftClick(Center(el));
        PostAction(args);
        return 0;
    }

    public static int DoubleClick(UIA3Automation automation, ArgParser args)
    {
        var el = Target(automation, args);
        Mouse.DoubleClick(Center(el), MouseButton.Left);
        PostAction(args);
        return 0;
    }

    public static int RightClick(UIA3Automation automation, ArgParser args)
    {
        var el = Target(automation, args);
        Mouse.RightClick(Center(el));
        PostAction(args);
        return 0;
    }

    public static int SetValue(UIA3Automation automation, ArgParser args)
    {
        var el = Target(automation, args);
        string text = args.Has("clear") ? string.Empty : TargetText(args);
        FocusIfPossible(el);

        var value = Try(() => el.Patterns.Value.PatternOrDefault);
        if (value != null && !value.IsReadOnly)
        {
            value.SetValue(text);
        }
        else
        {
            var ctype = Try(() => el.ControlType);
            if (ctype is ControlType.Edit or ControlType.Document)
            {
                try { el.AsTextBox().Text = text; }
                catch (Exception ex) { throw new CommandException($"Failed to set text: {ex.Message}"); }
            }
            else
            {
                throw new CommandException("Element does not support setting a value (no writable ValuePattern and not an Edit/Document).", 2);
            }
        }
        PostAction(args);
        return 0;
    }

    public static int TypeText(UIA3Automation automation, ArgParser args)
    {
        string text = TargetText(args);
        if (Targets.Selector(args) != null)
        {
            var el = Target(automation, args);
            FocusIfPossible(el);
        }
        Keyboard.Type(text);
        PostAction(args);
        return 0;
    }

    public static int Select(UIA3Automation automation, ArgParser args)
    {
        var el = Target(automation, args);
        string? item = args.Get("item") ?? (args.Positional.Count > 0 ? args.Positional[0] : null);

        bool isCombo = Try(() => el.ControlType) == ControlType.ComboBox;
        if (isCombo)
        {
            if (item is null) throw new CommandException("ComboBox selection needs --item <name>.", 2);
            var cbItem = Try(() => el.AsComboBox().Select(item));
            if (cbItem is null) throw new CommandException($"ComboBox had no selectable item named '{item}'.", 3);
            PostAction(args);
            return 0;
        }

        var selItem = Try(() => el.Patterns.SelectionItem.PatternOrDefault);
        if (selItem != null)
        {
            selItem.Select();
            PostAction(args);
            return 0;
        }

        if (item is null)
            throw new CommandException("Target is not a selectable item; pass --item <name> to select a descendant.", 2);

        foreach (var child in Targets.AllDescendants(el))
        {
            if (string.Equals(child.Name, item, StringComparison.OrdinalIgnoreCase))
            {
                var ci = Try(() => child.Patterns.SelectionItem.PatternOrDefault);
                if (ci != null) { ci.Select(); PostAction(args); return 0; }
                var inv = Try(() => child.Patterns.Invoke.PatternOrDefault);
                if (inv != null) { inv.Invoke(); PostAction(args); return 0; }
            }
        }

        throw new CommandException($"No selectable item '{item}' under the target element.", 3);
    }

    public static int Toggle(UIA3Automation automation, ArgParser args)
    {
        var el = Target(automation, args);
        var toggle = Try(() => el.Patterns.Toggle.PatternOrDefault)
            ?? throw new CommandException("Element does not support TogglePattern.", 2);
        toggle.Toggle();
        PostAction(args);
        return 0;
    }

    public static int Expand(UIA3Automation automation, ArgParser args)
    {
        var el = Target(automation, args);
        var ec = Try(() => el.Patterns.ExpandCollapse.PatternOrDefault)
            ?? throw new CommandException("Element does not support ExpandCollapsePattern.", 2);
        ec.Expand();
        PostAction(args);
        return 0;
    }

    public static int Collapse(UIA3Automation automation, ArgParser args)
    {
        var el = Target(automation, args);
        var ec = Try(() => el.Patterns.ExpandCollapse.PatternOrDefault)
            ?? throw new CommandException("Element does not support ExpandCollapsePattern.", 2);
        ec.Collapse();
        PostAction(args);
        return 0;
    }

    public static int Scroll(UIA3Automation automation, ArgParser args)
    {
        var el = Target(automation, args);
        string dir = (args.Get("direction") ?? args.Get("dir") ?? "down").ToLowerInvariant();
        string amountStr = (args.Get("amount") ?? "small").ToLowerInvariant();
        bool noAmount = amountStr is "no" or "none";
        bool largeStep = amountStr is "large" or "page" or "pageup" or "pagedown";
        ScrollAmount amt(bool increment) =>
            noAmount ? ScrollAmount.NoAmount :
            largeStep ? (increment ? ScrollAmount.LargeIncrement : ScrollAmount.LargeDecrement) :
                        (increment ? ScrollAmount.SmallIncrement : ScrollAmount.SmallDecrement);

        var scroll = Try(() => el.Patterns.Scroll.PatternOrDefault)
            ?? throw new CommandException("Element does not support ScrollPattern.", 2);

        if (dir is "up" or "down" or "pageup" or "pagedown" or "home" or "end" && !scroll.VerticallyScrollable)
        {
            Console.WriteLine("not vertically scrollable");
            return 0;
        }
        if (dir is "left" or "right" && !scroll.HorizontallyScrollable)
        {
            Console.WriteLine("not horizontally scrollable");
            return 0;
        }

        try
        {
        switch (dir)
        {
            case "up": scroll.Scroll(ScrollAmount.NoAmount, amt(false)); break;
            case "down": scroll.Scroll(ScrollAmount.NoAmount, amt(true)); break;
            case "left": scroll.Scroll(amt(false), ScrollAmount.NoAmount); break;
            case "right": scroll.Scroll(amt(true), ScrollAmount.NoAmount); break;
            case "pageup": scroll.Scroll(ScrollAmount.NoAmount, ScrollAmount.LargeDecrement); break;
            case "pagedown": scroll.Scroll(ScrollAmount.NoAmount, ScrollAmount.LargeIncrement); break;
            case "home": scroll.SetScrollPercent(-1, 0); break;
            case "end": scroll.SetScrollPercent(-1, 100); break;
            default: throw new CommandException($"Unknown --direction '{dir}'. Use up|down|left|right|pageup|pagedown|home|end.", 2);
        }
        }
        catch (CommandException) { throw; }
        catch (Exception ex)
        {
            throw new CommandException($"Scroll failed: {ex.Message}");
        }
        PostAction(args);
        return 0;
    }

    private static void PostAction(ArgParser args)
    {
        int delay = args.GetInt("delay", 0);
        if (delay > 0) Thread.Sleep(delay);
    }

    private static T? Try<T>(Func<T> f)
    {
        try { return f(); } catch { return default; }
    }
}
