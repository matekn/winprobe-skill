# Selectors: picking the stable one

`flaui` finds elements with `--by <kind> --value <v>`. Prefer the top of this list; drop down only when forced.

| Rank | `--by` | Range | Why |
|---|---|---|---|
| 1 | `automationId` | within the window | Developer-assigned, stable across renames/sessions. **Use first.** |
| 2 | `name` | within the window | UIA Name (visible label/text). Changes with copy, localization. |
| 3 | `class` | within the window | Win32 class / WPF class. Stable but often non-unique. |
| 4 | `controlType` | within the window | e.g. `Button`, `Edit`. Almost never unique — pair with `--index`. |
| 5 | `xpath` | within the window | Positional. Breaks on any structural change. Last resort. |
| — | `text` | within the window | Value/text content (searches `Value`/`Text` patterns then Name). |

Matching: exact match is tried first, then case-insensitive substring (unless `--exact`). `--index N` picks the Nth match (0-based) — use it when several elements match.

## Finding the selector

```
flaui dump --window Calculator                 # grep the tree
flaui dump --window Calculator --props class,patterns   # richer
flaui find --by name --value "Equals" --props patterns,bounds
```

`dump` line shape:

```
Button "Seven" [automationId=num7Button] enabled class="Button"
│      │        │                           │        └ optional --props
│      │        │                           └ optional flags
│      │        └ AutomationId (empty = none; fall back to Name/class)
│      └ UIA Name (the visible label)
└ UIA ControlType
```

## Rules of thumb

- **Empty `automationId`** (WinForms, WinUI, many UWP controls) → use `name`, or `class` + `--index`.
- **Name changes with language** → if the app is localized, prefer `automationId`, else test in a fixed culture.
- **Dynamic names** ("Save (2)") → match a stable substring; `--by name --value "Save"` uses substring fallback.
- **The same label appears twice** → tighten with `class`, or `--index`.
- **XPath** is useful for a quick probe (`get-property --xpath /Window[1]/...`) but should not end up in a committed test.

## For the C# test

The CLI selector maps directly to FlaUI:

```csharp
window.FindFirstDescendant(cf => cf.ByAutomationId("num7Button"));
window.FindFirstDescendant(cf => cf.ByName("Seven"));
window.FindFirstDescendant(cf => cf.ByClassName("Button"));
window.FindAllDescendants(cf => cf.ByControlType(ControlType.Button));
```

See `flaui-csharp.md` for the full find/act/assert idioms.
