using System.Text;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Capturing;
using FlaUI.UIA3;
using Flaui.Cli;
using Flaui.Core;

namespace Flaui.Commands;

public static class InspectCommands
{
    public static int Dump(UIA3Automation automation, ArgParser args)
    {
        var window = Targets.ResolveWindow(automation, args);
        var props = Describe.NormalizeProps(args.GetList("props"), includeBounds: args.Has("bounds"));

        var opts = new Describe.DumpOptions
        {
            MaxDepth = args.GetInt("depth", -1),
            MaxElements = args.GetInt("max-elements", 5000),
            IncludeOffscreen = !args.Has("visible-only") && !args.Has("no-offscreen"),
            Props = props
        };

        var sb = new StringBuilder();
        int pid = Try(() => window.Properties.ProcessId.ValueOrDefault);
        long hwnd = Try(() => window.Properties.NativeWindowHandle.ValueOrDefault.ToInt64());
        sb.Append("Window pid=").Append(pid).Append(" hwnd=0x").Append(hwnd.ToString("X")).Append('\n');
        sb.Append(Describe.Tree(window, opts));

        string text = sb.ToString();
        if (args.Get("out") is { } outPath)
        {
            File.WriteAllText(outPath, text);
            Console.WriteLine(outPath);
        }
        else
        {
            Console.Write(text);
        }
        return 0;
    }

    public static int Find(UIA3Automation automation, ArgParser args)
    {
        var window = Targets.ResolveWindow(automation, args);
        var element = Targets.Selector(args) is null ? window : Targets.ResolveElement(automation, window, args);
        var props = Describe.NormalizeProps(args.GetList("props"), includeBounds: args.Has("bounds"));

        int pid = Try(() => element.Properties.ProcessId.ValueOrDefault);
        Console.WriteLine($"pid={pid} {Describe.Line(element, props, includeHwnd: true)}");

        if (args.Has("tree"))
        {
            var opts = new Describe.DumpOptions
            {
                MaxDepth = args.GetInt("depth", -1),
                MaxElements = args.GetInt("max-elements", 5000),
                IncludeOffscreen = !args.Has("visible-only"),
                Props = props
            };
            Console.Write(Describe.Tree(element, opts));
        }

        return 0;
    }

    private static readonly string[] KnownProps =
    {
        "name", "automationId", "controlType", "className", "framework", "enabled",
        "offscreen", "bounds", "hwnd", "value", "text", "xpath", "helpText", "acceleratorKey",
        "accessKey", "localizedControlType", "isPassword", "patterns"
    };

    public static int GetProperty(UIA3Automation automation, ArgParser args)
    {
        var window = Targets.ResolveWindow(automation, args);
        var element = Targets.Selector(args) is null ? window : Targets.ResolveElement(automation, window, args);
        var props = args.GetList("props");
        if (props.Length == 0 && args.Get("prop") is { } single) props = new[] { single };
        if (props.Length == 0) props = KnownProps;

        var sb = new StringBuilder();
        foreach (var p in props)
        {
            string key = p.Trim();
            string value = ReadProperty(element, key);
            sb.Append(key).Append('=').Append(value).Append('\n');
        }
        Console.Write(sb.ToString());
        return 0;
    }

    private static string ReadProperty(AutomationElement el, string key)
    {
        try
        {
            return key.ToLowerInvariant() switch
            {
                "name" => el.Name ?? string.Empty,
                "automationid" => el.Properties.AutomationId.ValueOrDefault ?? string.Empty,
                "controltype" => el.ControlType.ToString(),
                "classname" or "class" => el.ClassName ?? string.Empty,
                "framework" or "frameworktype" => el.FrameworkType.ToString(),
                "enabled" or "isenabled" => el.IsEnabled.ToString(),
                "offscreen" or "isoffscreen" => el.IsOffscreen.ToString(),
                "bounds" or "rectangle" => Rect(el),
                "hwnd" or "nativewindowhandle" => "0x" + el.Properties.NativeWindowHandle.ValueOrDefault.ToInt64().ToString("X"),
                "value" => el.Patterns.Value.PatternOrDefault?.Value ?? string.Empty,
                "text" => Targets.GetText(el),
                "xpath" => FlaUI.Core.Debug.GetXPathToElement(el) ?? string.Empty,
                "helptext" => el.Properties.HelpText.ValueOrDefault ?? string.Empty,
                "acceleratorkey" => el.Properties.AcceleratorKey.ValueOrDefault ?? string.Empty,
                "accesskey" => el.Properties.AccessKey.ValueOrDefault ?? string.Empty,
                "localizedcontroltype" => el.Properties.LocalizedControlType.ValueOrDefault ?? string.Empty,
                "ispassword" => el.Properties.IsPassword.ValueOrDefault.ToString(),
                "patterns" => string.Join(",", Describe.PatternNames(el)),
                _ => throw new CommandException($"Unknown property '{key}'.", 2)
            };
        }
        catch (CommandException) { throw; }
        catch (Exception ex) { return $"<error: {ex.Message}>"; }
    }

    private static string Rect(AutomationElement el)
    {
        var r = el.BoundingRectangle;
        return $"{r.X},{r.Y},{r.Width},{r.Height}";
    }

    public static int Screenshot(UIA3Automation automation, ArgParser args)
    {
        string outPath = args.Get("out", $"flaui-screenshot-{DateTime.Now:yyyyMMdd-HHmmss}.png");
        var settings = new CaptureSettings();
        if (args.Get("scale") is { } scaleStr && double.TryParse(scaleStr, System.Globalization.CultureInfo.InvariantCulture, out var scale))
            settings.OutputScale = scale;

        CaptureImage image;
        if (args.Has("screen"))
        {
            image = Capture.MainScreen(settings);
        }
        else if (Targets.Selector(args) != null)
        {
            var window = Targets.ResolveWindow(automation, args);
            var element = Targets.ResolveElement(automation, window, args);
            image = Capture.Element(element, settings);
        }
        else
        {
            var window = Targets.ResolveWindow(automation, args);
            image = Capture.Element(window, settings);
        }

        using (image)
        {
            string full = Path.GetFullPath(outPath);
            image.ToFile(full);
            var r = image.OriginalBounds;
            Console.WriteLine($"{full} {r.Width}x{r.Height}");
        }
        return 0;
    }

    public static int ListWindows(UIA3Automation automation, ArgParser args)
    {
        var windows = Targets.ListTopWindows(automation);
        string? processOpt = args.Get("process");
        string? titleOpt = args.Get("window") ?? args.Get("window-title");

        foreach (var w in windows)
        {
            if (processOpt != null && !w.Process.Equals(processOpt.Replace(".exe", "", StringComparison.OrdinalIgnoreCase), StringComparison.OrdinalIgnoreCase))
                continue;
            if (titleOpt != null && !w.Title.Contains(titleOpt, StringComparison.OrdinalIgnoreCase))
                continue;

            Console.WriteLine($"pid={w.Pid} hwnd=0x{w.Hwnd:X} process={w.Process} \"{w.Title}\"");
        }
        return 0;
    }

    private static T? Try<T>(Func<T> f)
    {
        try { return f(); } catch { return default; }
    }
}
