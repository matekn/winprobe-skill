# UIA ControlType reference

`dump` prints each element's ControlType as the first token:

```
Button "Seven" [automationId=num7Button]
```

Use `--by controlType --value Button` (with `--index` or another constraint) when there is no usable AutomationId.

## Common types

| ControlType | Meaning | FlaUI wrapper | Typical patterns |
|---|---|---|---|
| `Window` | top-level / child window | `Window` | Window, Transform |
| `Pane` | generic container | — | Scroll |
| `Group` | labelled grouping | — | — |
| `Custom` | app-defined container | — | varies |
| `MenuBar` | menu strip | — | — |
| `Menu` | dropdown menu | — | ExpandCollapse |
| `MenuItem` | menu entry | `MenuItem` | Invoke, ExpandCollapse |
| `Button` | push button | `Button` | Invoke |
| `SplitButton` | button with a dropdown | `SplitButton` | Invoke, ExpandCollapse |
| `CheckBox` | checkbox | `CheckBox` | Toggle |
| `RadioButton` | radio | `RadioButton` | SelectionItem |
| `Edit` | text field | `TextBox` | Value, Text |
| `Document` | rich document / editor | — | Text, Value |
| `ComboBox` | drop-down | `ComboBox` | ExpandCollapse, Value, Selection |
| `List` | list | `ListBox` | Selection, Scroll |
| `ListItem` | list row | `ListBoxItem` | SelectionItem, ScrollItem |
| `DataGrid` / `Table` | grid | — | Grid, Table, Selection |
| `Tree` / `TreeItem` | tree | `Tree`, `TreeItem` | ExpandCollapse, Selection |
| `Tab` / `TabItem` | tabs | `Tab`, `TabItem` | Selection |
| `Text` | static label | `Label` | Text |
| `Hyperlink` | link | `Hyperlink` | Invoke |
| `Image` | image | — | — |
| `Slider` | slider | `Slider` | RangeValue |
| `ProgressBar` | progress | `ProgressBar` | RangeValue |
| `ScrollBar` | scrollbar | — | RangeValue |
| `ToolBar` | toolbar | — | — |
| `StatusBar` | status strip | — | — |
| `TitleBar` | window chrome | — | — |
| `Header` / `HeaderItem` | grid header | — | Invoke |
| `Separator` | divider | — | — |
| `Spinner` | numeric up/down | `Spinner` | RangeValue, Value |

Notes:

- **`Text` vs `Edit`**: `Text` is read-only display (use `.Name`); `Edit` is editable (use `Value`/`.Text`). WinUI/UWP "text boxes" can report as `Edit` or `Document`.
- **`Custom`** elements are common containers in WPF/WinUI and may carry the interesting AutomationId (e.g. Calculator's `NavView`).
- Names are **localized ControlType-adjacent** but the `controlType` token is always English.
