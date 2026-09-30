using System.Diagnostics;
using FlaUI.UIA3;
using Flaui.Cli;
using Flaui.Core;

namespace Flaui.Commands;

public static class SyncCommands
{
    public static int WaitFor(UIA3Automation automation, ArgParser args)
    {
        double timeoutSec = args.GetDouble("timeout", 10);
        int interval = args.GetInt("interval", 200);
        bool wantGone = args.Has("gone") || args.Has("not-exists") || args.Has("absent");
        var sel = Targets.Selector(args);

        if (sel is null && args.Get("window") is null && args.Get("window-title") is null)
            throw new CommandException("wait-for needs an element selector (--by/--value) or --window <title>.", 2);

        var deadline = DateTime.UtcNow.AddSeconds(timeoutSec);
        bool last = false;

        while (true)
        {
            last = Check(automation, args, sel);
            bool satisfied = wantGone ? !last : last;
            if (satisfied)
            {
                Console.WriteLine(wantGone ? "gone" : "found");
                return 0;
            }

            if (DateTime.UtcNow >= deadline)
            {
                throw new CommandException(
                    $"Timed out after {timeoutSec}s waiting for {(wantGone ? "element to disappear" : "element to appear")}.", 3);
            }
            Thread.Sleep(interval);
        }
    }

    private static bool Check(UIA3Automation automation, ArgParser args, Targets.SelectorSpec? sel)
    {
        try
        {
            var window = Targets.ResolveWindow(automation, args);
            if (sel is null) return true;

            var exact = Targets.MatchAll(window, sel, exactOnly: true);
            if (exact.Count > 0) return true;
            var loose = Targets.MatchAll(window, sel, exactOnly: false);
            return loose.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    public static int WaitIdle(UIA3Automation automation, ArgParser args)
    {
        int timeoutMs = (int)(args.GetDouble("timeout", 10) * 1000);
        int pid;
        if (args.Get("pid") is { } pidStr && int.TryParse(pidStr, out var p))
        {
            pid = p;
        }
        else
        {
            var window = Targets.ResolveWindow(automation, args);
            pid = Try(() => window.Properties.ProcessId.ValueOrDefault);
        }

        try
        {
            var proc = Process.GetProcessById(pid);
            bool idle = proc.WaitForInputIdle(timeoutMs);
            Console.WriteLine(idle ? $"idle pid={pid}" : $"busy pid={pid}");
            return idle ? 0 : 1;
        }
        catch (Exception ex)
        {
            throw new CommandException($"wait-idle failed for pid {pid}: {ex.Message}");
        }
    }

    private static T? Try<T>(Func<T> f)
    {
        try { return f(); } catch { return default; }
    }
}
