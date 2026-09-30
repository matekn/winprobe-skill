using System.Text;

namespace Flaui.Cli;

public sealed class ArgParser
{
    public List<string> Positional { get; } = new();
    public Dictionary<string, string> Options { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> Flags { get; } = new(StringComparer.OrdinalIgnoreCase);

    public static ArgParser Parse(string[] args, int startIndex = 0)
    {
        var p = new ArgParser();
        for (int i = startIndex; i < args.Length; i++)
        {
            string a = args[i];
            if (a.StartsWith("--", StringComparison.Ordinal))
            {
                string key;
                string? value = null;
                int eq = a.IndexOf('=');
                if (eq >= 0)
                {
                    key = a.Substring(2, eq - 2);
                    value = a.Substring(eq + 1);
                }
                else
                {
                    key = a.Substring(2);
                    if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
                    {
                        value = args[++i];
                    }
                }

                if (value is null)
                {
                    p.Flags.Add(key);
                }
                else
                {
                    p.Options[key] = value;
                }
            }
            else
            {
                p.Positional.Add(a);
            }
        }

        return p;
    }

    public bool Has(string key) => Options.ContainsKey(key) || Flags.Contains(key);

    public string? Get(string key) => Options.TryGetValue(key, out var v) ? v : null;

    public string Get(string key, string fallback) => Options.TryGetValue(key, out var v) ? v : fallback;

    public int GetInt(string key, int fallback)
    {
        if (!Options.TryGetValue(key, out var v)) return fallback;
        return int.TryParse(v, out var n) ? n : fallback;
    }

    public double GetDouble(string key, double fallback)
    {
        if (!Options.TryGetValue(key, out var v)) return fallback;
        return double.TryParse(v, System.Globalization.CultureInfo.InvariantCulture, out var n) ? n : fallback;
    }

    public string[] GetList(string key) =>
        Options.TryGetValue(key, out var v)
            ? v.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : Array.Empty<string>();

    public string Describe()
    {
        var sb = new StringBuilder();
        if (Positional.Count > 0) sb.Append(string.Join(' ', Positional));
        foreach (var (k, v) in Options) sb.Append($" --{k}={v}");
        foreach (var f in Flags) sb.Append($" --{f}");
        return sb.ToString().Trim();
    }
}
