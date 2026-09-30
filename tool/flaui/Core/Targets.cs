using System.Diagnostics;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using Flaui.Cli;

namespace Flaui.Core;

public sealed record WindowInfo(
    AutomationElement Element,
    int Pid,
    string Process,
    string Title,
    long Hwnd,
    string ControlType);

public static class Targets
{
    public static AutomationElement Foreground(UIA3Automation automation)
    {
        IntPtr hwnd = Native.GetForegroundWindow();
        if (hwnd == IntPtr.Zero)
            throw new CommandException("No foreground window.", 3);
        return automation.FromHandle(hwnd);
    }

    public static IReadOnlyList<WindowInfo> ListTopWindows(UIA3Automation automation)
    {
        var result = new List<WindowInfo>();
        AutomationElement[] children;
        try
        {
            children = automation.GetDesktop().FindAllChildren();
        }
        catch (Exception ex)
        {
            throw new CommandException($"Failed to enumerate desktop windows: {ex.Message}");
        }

        foreach (var el in children)
        {
            try
            {
                string ctl = el.ControlType.ToString();
                if (!ctl.Equals("Window", StringComparison.OrdinalIgnoreCase) &&
                    !ctl.Equals("Pane", StringComparison.OrdinalIgnoreCase))
                    continue;

                long hwnd = el.Properties.NativeWindowHandle.ValueOrDefault.ToInt64();
                if (hwnd == 0) continue;

                int pid = el.Properties.ProcessId.ValueOrDefault;
                string title = el.Name ?? string.Empty;
                string proc = ProcessName(pid);
                result.Add(new WindowInfo(el, pid, proc, title, hwnd, ctl));
            }
            catch
            {
                // element went away mid-enumeration; skip it
            }
        }

        return result;
    }

    private static string ProcessName(int pid)
    {
        try { return Process.GetProcessById(pid).ProcessName; }
        catch { return string.Empty; }
    }

    public static AutomationElement ResolveWindow(UIA3Automation automation, ArgParser args)
    {
        string? hwndOpt = args.Get("hwnd");
        string? pidOpt = args.Get("pid");
        string? processOpt = args.Get("process");
        string? titleOpt = args.Get("window") ?? args.Get("window-title");
        string? controlOpt = args.Get("window-control");

        bool anyWindowSelector = hwndOpt != null || pidOpt != null || processOpt != null || titleOpt != null || controlOpt != null;

        if (!anyWindowSelector)
        {
            return Foreground(automation);
        }

        if (hwndOpt != null)
        {
            long h = ParseHwnd(hwndOpt);
            return automation.FromHandle(new IntPtr(h));
        }

        var windows = ListTopWindows(automation);

        if (pidOpt != null)
        {
            if (!int.TryParse(pidOpt, out int pid))
                throw new CommandException($"--pid must be an integer, got '{pidOpt}'.", 2);
            windows = windows.Where(w => w.Pid == pid).ToList();
        }
        else if (processOpt != null)
        {
            string needle = processOpt.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                ? processOpt[..^4] : processOpt;
            windows = windows.Where(w => w.Process.Equals(needle, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (titleOpt != null)
        {
            windows = windows
                .Where(w => w.Title.Contains(titleOpt, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (controlOpt != null)
        {
            windows = windows
                .Where(w => w.Title.Contains(controlOpt, StringComparison.OrdinalIgnoreCase) ||
                            w.Process.Contains(controlOpt, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (windows.Count == 0)
            throw new CommandException("No matching top-level window found.", 3);

        // Prefer real top-level windows over shell surfaces (Taskbar, Program Manager, ...).
        var candidates = windows.Where(w => !IsShellSurface(w)).ToList();
        if (candidates.Count == 0) candidates = windows.ToList();

        var best =
            candidates.OrderByDescending(w => w.ControlType.Equals("Window", StringComparison.OrdinalIgnoreCase))
                      .ThenByDescending(w => w.Title.Length > 0)
                      .First();
        return best.Element;
    }

    private static bool IsShellSurface(WindowInfo w) =>
        w.Title.Equals("Taskbar", StringComparison.OrdinalIgnoreCase) ||
        w.Title.Equals("Program Manager", StringComparison.OrdinalIgnoreCase) ||
        w.Title.Equals("ToastWindow", StringComparison.OrdinalIgnoreCase);

    private static long ParseHwnd(string s)
    {
        s = s.Trim();
        try
        {
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                return Convert.ToInt64(s.Substring(2), 16);
            return long.Parse(s);
        }
        catch
        {
            throw new CommandException($"--hwnd must be a decimal or 0x-prefixed hex value, got '{s}'.", 2);
        }
    }

    public static IEnumerable<AutomationElement> AllDescendants(AutomationElement root)
    {
        var stack = new Stack<AutomationElement>();
        foreach (var c in SafeChildren(root)) stack.Push(c);
        while (stack.Count > 0)
        {
            var el = stack.Pop();
            yield return el;
            foreach (var c in SafeChildren(el)) stack.Push(c);
        }
    }

    public static AutomationElement[] SafeChildren(AutomationElement element)
    {
        try { return element.FindAllChildren(); }
        catch { return Array.Empty<AutomationElement>(); }
    }

    public static string GetText(AutomationElement el)
    {
        try
        {
            var vp = el.Patterns.Value.PatternOrDefault;
            if (vp != null)
            {
                string v = vp.Value ?? string.Empty;
                if (v.Length > 0) return v;
            }
        }
        catch { }

        try
        {
            var tp = el.Patterns.Text.PatternOrDefault;
            if (tp != null)
            {
                string t = tp.DocumentRange.GetText(-1) ?? string.Empty;
                if (t.Length > 0) return t;
            }
        }
        catch { }

        try { return el.Name ?? string.Empty; }
        catch { return string.Empty; }
    }

    public sealed record SelectorSpec(string By, string Value);

    private static readonly string[] ConvenienceKeys =
        { "automation-id", "automationid", "aid", "name", "xpath", "class", "classname", "control-type", "controltype" };

    public static SelectorSpec? Selector(ArgParser args)
    {
        string? by = args.Get("by");
        string? value = args.Get("value");

        foreach (var key in ConvenienceKeys)
        {
            if (args.Get(key) is { } v)
            {
                by = key switch
                {
                    "automation-id" or "automationid" or "aid" => "automationId",
                    "class" or "classname" => "class",
                    "control-type" or "controltype" => "controlType",
                    _ => key
                };
                value = v;
                break;
            }
        }

        if (by != null || value != null)
        {
            if (by is null) by = "automationId";
            if (value is null) throw new CommandException("--by requires --value.", 2);
            return new SelectorSpec(by, value);
        }

        return null;
    }

    public static AutomationElement ResolveElement(UIA3Automation automation, AutomationElement root, ArgParser args)
    {
        var sel = Selector(args)
            ?? throw new CommandException("No element selector given. Pass --by <automationId|name|text|class|controlType|xpath> --value <v>, or a convenience flag like --name.", 2);

        int index = args.GetInt("index", 0);
        bool exactOnly = args.Has("exact");

        if (sel.By.Equals("xpath", StringComparison.OrdinalIgnoreCase))
        {
            AutomationElement[] found;
            try { found = root.FindAllByXPath(sel.Value) ?? Array.Empty<AutomationElement>(); }
            catch (Exception ex) { throw new CommandException($"Invalid XPath '{sel.Value}': {ex.Message}", 2); }
            if (found.Length <= index)
                throw new CommandException($"No element matched xpath '{sel.Value}' (index {index}).", 3);
            return found[index];
        }

        var matches = MatchAll(root, sel, exactOnly);
        if (matches.Count <= index)
        {
            string hint = matches.Count == 0 ? "none matched" : $"only {matches.Count} matched";
            throw new CommandException($"No element matched by={sel.By} value=\"{sel.Value}\" ({hint}).", 3);
        }

        return matches[index];
    }

    public static List<AutomationElement> MatchAll(AutomationElement root, SelectorSpec sel, bool exactOnly)
    {
        var exact = new List<AutomationElement>();
        var loose = new List<AutomationElement>();

        foreach (var el in AllDescendants(root))
        {
            string? aid = Try(() => el.Properties.AutomationId.ValueOrDefault);
            string name = Try(() => el.Name) ?? string.Empty;
            string cls = Try(() => el.ClassName) ?? string.Empty;
            string ctype = Try(() => el.ControlType.ToString()) ?? string.Empty;

            bool ok, isExact = false;
            switch (sel.By.ToLowerInvariant())
            {
                case "automationid":
                    ok = Eq(aid, sel.Value);
                    isExact = ok;
                    if (!ok && !exactOnly) ok = Contains(aid, sel.Value);
                    break;
                case "name":
                    ok = Eq(name, sel.Value);
                    isExact = ok;
                    if (!ok && !exactOnly) ok = Contains(name, sel.Value);
                    break;
                case "text":
                    string text = GetText(el);
                    ok = Eq(text, sel.Value);
                    isExact = ok;
                    if (!ok && !exactOnly) ok = Contains(text, sel.Value);
                    break;
                case "class":
                    ok = Eq(cls, sel.Value);
                    isExact = ok;
                    if (!ok && !exactOnly) ok = Contains(cls, sel.Value);
                    break;
                case "controltype":
                    ok = Contains(ctype, sel.Value);
                    isExact = ok;
                    break;
                default:
                    throw new CommandException($"Unknown --by value '{sel.By}'. Use automationId|name|text|class|controlType|xpath.", 2);
            }

            if (!ok) continue;
            if (isExact) exact.Add(el); else loose.Add(el);
        }

        return exact.Concat(loose).ToList();
    }

    private static bool Eq(string? a, string b) =>
        a != null && a.Equals(b, StringComparison.OrdinalIgnoreCase);

    private static bool Contains(string? a, string b) =>
        a != null && a.Contains(b, StringComparison.OrdinalIgnoreCase);

    private static T? Try<T>(Func<T> f)
    {
        try { return f(); } catch { return default; }
    }
}
