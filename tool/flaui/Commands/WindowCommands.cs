using System.Diagnostics;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;
using Flaui.Cli;
using Flaui.Core;

namespace Flaui.Commands;

public static class WindowCommands
{
    public static int Launch(UIA3Automation automation, ArgParser args)
    {
        string? path = args.Get("path") ?? (args.Positional.Count > 0 ? args.Positional[0] : null);
        if (string.IsNullOrWhiteSpace(path))
            throw new CommandException("No program to launch. Pass --path <exe> or positional argument.", 2);

        string arguments = args.Get("args", string.Empty);
        bool useShell = !args.Has("no-shell");

        var psi = new ProcessStartInfo
        {
            FileName = path,
            Arguments = arguments,
            UseShellExecute = useShell
        };

        // Snapshot existing top-level windows so --wait only accepts a *new* window.
        var before = args.Has("wait")
            ? new HashSet<long>(Targets.ListTopWindows(automation).Select(w => w.Hwnd))
            : null;

        Process proc;
        try { proc = Process.Start(psi)!; }
        catch (Exception ex) { throw new CommandException($"Failed to launch '{path}': {ex.Message}"); }

        if (proc is null)
            throw new CommandException($"Launch of '{path}' returned no process (it may be a shell alias). Try --wait and a window title.", 1);

        int timeoutSec = args.GetInt("timeout", 15);
        string? titleMatch = args.Get("window") ?? args.Get("window-title");

        if (args.Has("wait"))
        {
            var info = WaitForWindow(automation, before!, proc.Id, proc.ProcessName, titleMatch, timeoutSec);
            if (info is null)
            {
                Console.WriteLine($"pid={proc.Id} (launched; no window found within {timeoutSec}s)");
                return 1;
            }
            Console.WriteLine($"pid={info.Pid} hwnd=0x{info.Hwnd:X} process={info.Process} \"{info.Title}\"");
        }
        else
        {
            Console.WriteLine($"pid={proc.Id}");
        }

        return 0;
    }

    private static WindowInfo? WaitForWindow(UIA3Automation automation, HashSet<long> before, int pid, string procName, string? titleMatch, int timeoutSec)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSec);
        while (DateTime.UtcNow < deadline)
        {
            // Only consider windows that did not exist before the launch.
            var windows = Targets.ListTopWindows(automation)
                .Where(w => !before.Contains(w.Hwnd))
                .ToList();

            WindowInfo? match = null;

            if (titleMatch != null)
                match = windows
                    .OrderByDescending(w => w.ControlType.Equals("Window", StringComparison.OrdinalIgnoreCase))
                    .FirstOrDefault(w => w.Title.Contains(titleMatch, StringComparison.OrdinalIgnoreCase));

            match ??= windows.FirstOrDefault(w => w.Pid == pid && w.Title.Length > 0)
                      ?? (string.IsNullOrEmpty(procName) ? null : windows.FirstOrDefault(w => w.Process.Equals(procName, StringComparison.OrdinalIgnoreCase) && w.Title.Length > 0))
                      ?? (titleMatch != null ? null : windows.OrderByDescending(w => w.ControlType.Equals("Window", StringComparison.OrdinalIgnoreCase)).FirstOrDefault(w => w.Title.Length > 0));

            if (match != null) return match;
            Thread.Sleep(250);
        }
        return null;
    }

    public static int Attach(UIA3Automation automation, ArgParser args)
    {
        var window = Targets.ResolveWindow(automation, args);
        int pid = Try(() => window.Properties.ProcessId.ValueOrDefault);
        long hwnd = Try(() => window.Properties.NativeWindowHandle.ValueOrDefault.ToInt64());
        string title = Try(() => window.Name) ?? string.Empty;
        Console.WriteLine($"attached pid={pid} hwnd=0x{hwnd:X} \"{title}\"");
        return 0;
    }

    public static int Focus(UIA3Automation automation, ArgParser args)
    {
        var window = Targets.ResolveWindow(automation, args);
        long hwnd = Try(() => window.Properties.NativeWindowHandle.ValueOrDefault.ToInt64());
        if (hwnd != 0) Native.SetForegroundWindow(new IntPtr(hwnd));
        try { window.Focus(); } catch { }
        Console.WriteLine($"focused hwnd=0x{hwnd:X}");
        return 0;
    }

    public static int Close(UIA3Automation automation, ArgParser args)
    {
        var window = Targets.ResolveWindow(automation, args);
        int pid = Try(() => window.Properties.ProcessId.ValueOrDefault);

        if (args.Has("kill"))
        {
            try { Process.GetProcessById(pid).Kill(); }
            catch (Exception ex) { throw new CommandException($"Failed to kill pid {pid}: {ex.Message}"); }
            Console.WriteLine($"killed pid={pid}");
            return 0;
        }

        var winPattern = Try(() => window.Patterns.Window.PatternOrDefault);
        if (winPattern != null)
        {
            winPattern.Close();
            Console.WriteLine($"closed pid={pid}");
            return 0;
        }

        try { Process.GetProcessById(pid).CloseMainWindow(); }
        catch (Exception ex) { throw new CommandException($"Failed to close pid {pid}: {ex.Message}"); }
        Console.WriteLine($"closed pid={pid}");
        return 0;
    }

    public static int Maximize(UIA3Automation automation, ArgParser args) =>
        SetVisualState(automation, args, WindowVisualState.Maximized, "maximized");

    public static int Minimize(UIA3Automation automation, ArgParser args) =>
        SetVisualState(automation, args, WindowVisualState.Minimized, "minimized");

    public static int Restore(UIA3Automation automation, ArgParser args) =>
        SetVisualState(automation, args, WindowVisualState.Normal, "restored");

    private static int SetVisualState(UIA3Automation automation, ArgParser args, WindowVisualState state, string verb)
    {
        var window = Targets.ResolveWindow(automation, args);
        var pattern = Try(() => window.Patterns.Window.PatternOrDefault)
            ?? throw new CommandException("Window does not support the WindowPattern (cannot change visual state).", 2);
        pattern.SetWindowVisualState(state);
        Console.WriteLine($"{verb} hwnd=0x{Try(() => window.Properties.NativeWindowHandle.ValueOrDefault.ToInt64()):X}");
        return 0;
    }

    private static T? Try<T>(Func<T> f)
    {
        try { return f(); } catch { return default; }
    }
}
