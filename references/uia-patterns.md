# UIA patterns → what an element can do

An element's *ControlType* says what it is; its *patterns* say what you can do to it. The CLI prints them with `--props patterns`:

```
Button "Seven" [automationId=num7Button] patterns=Invoke,LegacyIAccessible,ScrollItem
```

If a command fails with *"Element does not support XPattern"*, check `--props patterns` first — you targeted the wrong element (or the wrong one of several matches).

| Pattern | Capability | `flaui` command | FlaUI |
|---|---|---|---|
| `Invoke` | press/activate | `click` | `.Invoke()` / `.Patterns.Invoke.Pattern.Invoke()` |
| `Value` | read/write a value | `set-value`, `get-property --prop value` | `.Patterns.Value.Pattern.SetValue(...)` / `.Value` |
| `Text` | read document text | `get-property --prop text` | `.Patterns.Text.Pattern.DocumentRange.GetText(-1)` |
| `Toggle` | on/off | `toggle` | `.Patterns.Toggle.Pattern.Toggle()` |
| `ExpandCollapse` | expand/collapse | `expand`, `collapse` | `.Expand()`, `.Collapse()` |
| `Selection` | container of selectable items | — | `.Patterns.Selection.Pattern` |
| `SelectionItem` | selectable item | `select` | `.Patterns.SelectionItem.Pattern.Select()` |
| `Scroll` | scroll container | `scroll` | `.Patterns.Scroll.Pattern.Scroll(...)` |
| `ScrollItem` | scroll item into view | — | `.Patterns.ScrollItem.Pattern.ScrollIntoView()` |
| `RangeValue` | numeric range | — | `.Patterns.RangeValue.Pattern.Value = n` |
| `Window` | window state/close | `maximize`/`minimize`/`restore`/`close` | `.SetWindowVisualState(...)`, `.Close()` |
| `Grid` / `GridItem` | grid navigation | — | item `.GridItem` |
| `Table` / `TableItem` | table navigation | — | item `.TableItem` |
| `Transform` | move/resize | — | `.Patterns.Transform.Pattern` |
| `LegacyIAccessible` | MSAA bridge | — | used by WinForms/legacy controls |

## Action selection in `flaui`

`click` uses `Invoke` when available, else a real mouse click at the element's center (`--mouse` forces the mouse). Use the pattern path when possible — it is focus-independent and more reliable in headless/RDP sessions.

`set-value` uses `Value` first, then the `TextBox` wrapper. `select` uses the `ComboBox` wrapper, then `SelectionItem`.

## Patterns that imply timing

`ExpandCollapse`, `Selection`, and `Scroll` often change the tree lazily (a menu opens, a list virtualizes). After such an action, **re-`dump`** — the children may not exist until the state change settles. In tests, wrap the follow-up find in `Retry.WhileNull` (see `flaui-csharp.md`).
