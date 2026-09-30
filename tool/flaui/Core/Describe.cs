using System.Text;
using FlaUI.Core.AutomationElements;

namespace Flaui.Core;

public static class Describe
{
    private static readonly string[] SupportedProps =
        { "class", "enabled", "offscreen", "framework", "value", "text", "bounds", "hwnd", "patterns" };

    public static string[] NormalizeProps(string[] requested, bool includeBounds)
    {
        var list = new List<string>();
        foreach (var raw in requested)
        {
            string p = raw.Trim().ToLowerInvariant();
            if (p is "all" or "*")
                list.AddRange(SupportedProps);
            else if (p is "rect" or "bounds" or "rectangle")
                list.Add("bounds");
            else if (p is "aid" or "automationid" or "name" or "controltype")
                continue;
            else if (SupportedProps.Contains(p))
                list.Add(p);
            else
                throw new Cli.CommandException($"Unknown property '{raw}'. Supported: {string.Join(", ", SupportedProps)}.", 2);
        }

        if (includeBounds && !list.Contains("bounds")) list.Add("bounds");
        return list.Distinct().ToArray();
    }

    public static string Line(AutomationElement el, string[] props, bool includeHwnd = false)
    {
        string name = Escape(Try(() => el.Name) ?? string.Empty);
        string aid = Try(() => el.Properties.AutomationId.ValueOrDefault) ?? string.Empty;
        string ctl = Try(() => el.ControlType.ToString()) ?? "Unknown";

        var sb = new StringBuilder();
        sb.Append(ctl);
        sb.Append(" \"").Append(name).Append('"');
        sb.Append(" [automationId=").Append(aid).Append(']');

        foreach (var prop in props)
        {
            AppendProp(sb, el, prop);
        }

        if (includeHwnd && !props.Contains("hwnd"))
        {
            long hwnd = Try(() => el.Properties.NativeWindowHandle.ValueOrDefault.ToInt64());
            if (hwnd != 0) sb.Append(" hwnd=0x").Append(hwnd.ToString("X"));
        }

        return sb.ToString();
    }

    private static void AppendProp(StringBuilder sb, AutomationElement el, string prop)
    {
        switch (prop)
        {
            case "class":
                string? cls = Try(() => el.ClassName);
                if (!string.IsNullOrEmpty(cls)) sb.Append(" class=\"").Append(Escape(cls!)).Append('"');
                break;

            case "enabled":
                bool en = Try(() => el.IsEnabled);
                sb.Append(en ? " enabled" : " disabled");
                break;

            case "offscreen":
                if (Try(() => el.IsOffscreen)) sb.Append(" offscreen");
                break;

            case "framework":
                string? fw = Try(() => el.FrameworkType.ToString());
                if (!string.IsNullOrEmpty(fw)) sb.Append(" framework=").Append(fw);
                break;

            case "value":
                string? val = Try(() => el.Patterns.Value.PatternOrDefault?.Value);
                if (val != null) sb.Append(" value=\"").Append(Escape(val)).Append('"');
                break;

            case "text":
                string txt = Targets.GetText(el);
                if (!string.IsNullOrEmpty(txt))
                {
                    txt = txt.Replace("\r", " ").Replace("\n", " ");
                    if (txt.Length > 80) txt = txt[..80] + "…";
                    sb.Append(" text=\"").Append(Escape(txt)).Append('"');
                }
                break;

            case "bounds":
                var r = Try(() => el.BoundingRectangle);
                if (r.Width > 0 || r.Height > 0)
                    sb.Append(" rect=").Append(r.X).Append(',').Append(r.Y).Append(',').Append(r.Width).Append(',').Append(r.Height);
                break;

            case "hwnd":
                long h = Try(() => el.Properties.NativeWindowHandle.ValueOrDefault.ToInt64());
                if (h != 0) sb.Append(" hwnd=0x").Append(h.ToString("X"));
                break;

            case "patterns":
                var names = PatternNames(el);
                if (names.Length > 0) sb.Append(" patterns=").Append(string.Join(",", names));
                break;
        }
    }

    public sealed class DumpOptions
    {
        public int MaxDepth { get; init; } = -1;
        public bool IncludeOffscreen { get; init; } = true;
        public int MaxElements { get; init; } = 5000;
        public string[] Props { get; init; } = Array.Empty<string>();
    }

    public static string Tree(AutomationElement root, DumpOptions opts)
    {
        var sb = new StringBuilder();
        int count = 0;
        bool overflow = false;

        void Rec(AutomationElement el, string prefix, bool isLast, int depth, bool isRoot)
        {
            if (overflow) return;
            if (count >= opts.MaxElements) { overflow = true; return; }
            count++;

            if (!isRoot)
            {
                sb.Append(prefix);
                sb.Append(isLast ? "└─ " : "├─ ");
            }

            sb.Append(Line(el, opts.Props, includeHwnd: isRoot));
            sb.Append('\n');

            if (opts.MaxDepth >= 0 && depth >= opts.MaxDepth) return;

            var children = Targets.SafeChildren(el);
            if (!opts.IncludeOffscreen)
                children = children.Where(c => !Try(() => c.IsOffscreen)).ToArray();

            string childPrefix = isRoot ? string.Empty : prefix + (isLast ? "   " : "│  ");
            for (int i = 0; i < children.Length; i++)
            {
                Rec(children[i], childPrefix, i == children.Length - 1, depth + 1, false);
            }
        }

        Rec(root, string.Empty, true, 0, true);

        if (overflow)
            sb.Append("… output truncated at ").Append(opts.MaxElements).Append(" elements (raise --max-elements)\n");

        return sb.ToString();
    }

    public static string[] PatternNames(AutomationElement el)
    {
        var list = new List<string>();
        void C(string n, Func<bool> f) { try { if (f()) list.Add(n); } catch { } }

        C("Invoke", () => el.Patterns.Invoke.IsSupported);
        C("Value", () => el.Patterns.Value.IsSupported);
        C("Toggle", () => el.Patterns.Toggle.IsSupported);
        C("Selection", () => el.Patterns.Selection.IsSupported);
        C("SelectionItem", () => el.Patterns.SelectionItem.IsSupported);
        C("ExpandCollapse", () => el.Patterns.ExpandCollapse.IsSupported);
        C("Scroll", () => el.Patterns.Scroll.IsSupported);
        C("ScrollItem", () => el.Patterns.ScrollItem.IsSupported);
        C("Text", () => el.Patterns.Text.IsSupported);
        C("Window", () => el.Patterns.Window.IsSupported);
        C("Grid", () => el.Patterns.Grid.IsSupported);
        C("GridItem", () => el.Patterns.GridItem.IsSupported);
        C("Table", () => el.Patterns.Table.IsSupported);
        C("TableItem", () => el.Patterns.TableItem.IsSupported);
        C("RangeValue", () => el.Patterns.RangeValue.IsSupported);
        C("Transform", () => el.Patterns.Transform.IsSupported);
        C("LegacyIAccessible", () => el.Patterns.LegacyIAccessible.IsSupported);

        return list.OrderBy(x => x).ToArray();
    }

    public static string Escape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static T? Try<T>(Func<T> f)
    {
        try { return f(); } catch { return default; }
    }
}
