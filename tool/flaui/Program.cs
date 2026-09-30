using System.Text;
using FlaUI.UIA3;
using Flaui.Cli;
using Flaui.Commands;

namespace Flaui;

public static class Program
{
    public static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        if (args.Length == 0 || IsHelp(args[0]))
        {
            PrintUsage();
            return args.Length == 0 ? 2 : 0;
        }

        if (args[0] is "--version" or "-v" or "version")
        {
            Console.WriteLine("flaui 0.1.0");
            return 0;
        }

        string command = args[0].ToLowerInvariant();
        var parsed = ArgParser.Parse(args, 1);

        try
        {
            using var automation = new UIA3Automation();
            return Dispatch(command, automation, parsed);
        }
        catch (CommandException ex)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            return ex.ExitCode;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            return 1;
        }
    }

    private static int Dispatch(string command, UIA3Automation automation, ArgParser args) => command switch
    {
        "dump" or "inspect" or "tree" => InspectCommands.Dump(automation, args),
        "find" => InspectCommands.Find(automation, args),
        "get-property" or "get" or "prop" or "property" => InspectCommands.GetProperty(automation, args),
        "screenshot" or "shot" or "capture" => InspectCommands.Screenshot(automation, args),
        "list-windows" or "windows" or "list" => InspectCommands.ListWindows(automation, args),

        "click" => ActionCommands.Click(automation, args),
        "double-click" or "doubleclick" or "dblclick" => ActionCommands.DoubleClick(automation, args),
        "right-click" or "rightclick" or "rclick" => ActionCommands.RightClick(automation, args),
        "set-value" or "set" or "settext" or "set-text" => ActionCommands.SetValue(automation, args),
        "type-text" or "type" or "send-text" or "write" => ActionCommands.TypeText(automation, args),
        "select" => ActionCommands.Select(automation, args),
        "toggle" or "check" => ActionCommands.Toggle(automation, args),
        "expand" => ActionCommands.Expand(automation, args),
        "collapse" => ActionCommands.Collapse(automation, args),
        "scroll" => ActionCommands.Scroll(automation, args),

        "press-key" or "key" => InputCommands.PressKey(automation, args),
        "key-combo" or "combo" or "chord" or "hotkey" => InputCommands.KeyCombo(automation, args),
        "move" => InputCommands.Move(automation, args),
        "drag" => InputCommands.Drag(automation, args),

        "launch" or "start" or "run" => WindowCommands.Launch(automation, args),
        "attach" => WindowCommands.Attach(automation, args),
        "close" => WindowCommands.Close(automation, args),
        "focus" => WindowCommands.Focus(automation, args),
        "maximize" or "max" => WindowCommands.Maximize(automation, args),
        "minimize" or "min" => WindowCommands.Minimize(automation, args),
        "restore" => WindowCommands.Restore(automation, args),

        "wait-for" or "wait" => SyncCommands.WaitFor(automation, args),
        "wait-idle" or "idle" => SyncCommands.WaitIdle(automation, args),

        _ => throw new CommandException($"Unknown command '{command}'. Run 'flaui help'.", 2)
    };

    private static bool IsHelp(string s) => s is "--help" or "-h" or "help" or "/?";

    private static void PrintUsage()
    {
        Console.WriteLine(
"""
flaui - a stateless Windows UI automation probe (FlaUI / UIA3)

Window targeting (used by most commands; default = foreground window):
  --pid <n>               target the process id
  --process <name>        target by process name (e.g. Calculator)
  --window <title>        target by window title substring
  --hwnd <0x...|n>        target by raw window handle

Element selectors (actions + find/get-property/screenshot):
  --by <kind> --value <v> kind = automationId | name | text | class | controlType | xpath
  --name/--automation-id/--xpath/--class <v>   shorthand for --by/--value
  --index <n>             pick the n-th match (default 0)
  --exact                 exact match only (default: substring fallback)

INSPECT
  dump       [-e|--by/--value] [--depth N] [--props class,enabled,value,text,bounds,patterns,hwnd] [--visible-only] [--max-elements N] [--out file]
  find       --by/--value [--index N] [--props ...] [--tree] [-w window opts]
  get-property --by/--value [--prop name | --props name,value] [-w window opts]
  screenshot [--out file.png] [--screen] [-e|--by/--value] [--scale N] [-w window opts]
  list-windows [--process name] [--window title]

ACT
  click | double-click | right-click   --by/--value [--mouse] [--delay ms]
  set-value  --by/--value --text "..." [--clear]
  type-text  [--by/--value] --text "..."
  select     --by/--value --item "..." 
  toggle | expand | collapse   --by/--value
  scroll     --by/--value [--direction up|down|pageup|pagedown|home|end] [--amount small|large]

KEYBOARD / MOUSE
  press-key  --key <k>            (enter, esc, tab, space, f5, a, 0, up, ...)
  key-combo  --key ctrl+shift+s   (also: hotkey)
  move       --x N --y N | --by/--value
  drag       --by/--value --to-by/--to-value <dest> | --to-x N --to-y N

APP / WINDOW
  launch  --path <exe> [--args "..."] [--wait] [--window <title>] [--timeout s]
  attach  --pid N | --window <title>
  close   [--kill] [-w window opts]
  focus | maximize | minimize | restore   [-w window opts]

SYNC
  wait-for   --by/--value [--gone] [--timeout s] [--interval ms]
  wait-idle  --pid N | [-w window opts] [--timeout s]

Examples:
  flaui launch --path calc.exe --wait --window Calculator
  flaui dump --window Calculator --depth 4
  flaui click --window Calculator --by name --value "Seven"
  flaui screenshot --window Calculator --out calc.png
""");
    }
}
