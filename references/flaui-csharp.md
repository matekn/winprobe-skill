# FlaUI C# idioms (NUnit / xUnit)

The tests you author use **FlaUI** directly. This is the translation table from `flaui` CLI probes to test code.

## Test skeleton (NUnit)

```csharp
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Tools;
using FlaUI.UIA3;
using NUnit.Framework;

[TestFixture]
public class CalculatorTests
{
    private Application _app = null!;
    private UIA3Automation _automation = null!;
    private Window _window = null!;

    [SetUp]
    public void SetUp()
    {
        _automation = new UIA3Automation();
        _app = Application.Launch("calc.exe");
        _window = _app.GetMainWindow(_automation);       // waits for a main window
    }

    [TearDown]
    public void TearDown()
    {
        _automation.Dispose();
        _app.Close();
    }
}
```

xUnit swaps `[SetUp]`/`[TearDown]` for a constructor / `IDisposable.Dispose`.

## Find

```csharp
// AutomationId (preferred)
var seven = _window.FindFirstDescendant(cf => cf.ByAutomationId("num7Button")).AsButton();

// Name
var equals = _window.FindFirstDescendant(cf => cf.ByName("Equals")).AsButton();

// With a timeout / retry — use whenever the element may not exist yet
var result = Retry.WhileNull(
    () => _window.FindFirstDescendant(cf => cf.ByAutomationId("CalculatorResults")),
    timeout: TimeSpan.FromSeconds(5)).Result;

var text = result!.Name;   // "Display is 15"
```

Wrappers: `.AsButton()`, `.AsTextBox()`, `.AsComboBox()`, `.AsCheckBox()`, `.AsWindow()`, `.AsLabel()`, `.AsMenuItem()`, `.AsListBoxItem()`, `.AsTab()`, `.AsTreeItem()`. Elements also have a generic `element.Patterns.<X>`.

## Act

```csharp
seven.Invoke();                       // InvokePattern — no mouse needed
textBox.Text = "hello";               // TextBox wrapper
combo.Select("Option A");
checkBox.IsChecked = true;
window.SetForeground();               // focus the window
Keyboard.Type("typed text");          // FlaUI.Core.Input.Keyboard
Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_S); // Ctrl+S
```

Add `using FlaUI.Core.Input;` for `Keyboard`/`Mouse`, and `FlaUI.Core.WindowsAPI` for `VirtualKeyShort`.

## Wait / retry (the fix for most flakiness)

```csharp
using FlaUI.Core.Tools;

// Poll until non-null, then assert
var button = Retry.WhileNull(() => window.FindFirstDescendant(cf => cf.ByName("Save")),
                             TimeSpan.FromSeconds(5)).Result;
Assert.That(button, Is.Not.Null);

// Poll until a condition is true
Assert.That(Retry.WhileFalse(() => detail.Text == "Done", TimeSpan.FromSeconds(5)).Result, Is.True);

// Built-in element waits
button.WaitUntilEnabled();
button.WaitUntilClickable();
element.WaitUntilClickable(TimeSpan.FromSeconds(3));
```

`Retry.WhileNull` / `WhileFalse` / `While` are the UIA equivalent of the CLI's `wait-for`. **Never** `Thread.Sleep` in a test — it is the classic source of flakiness.

## Assert

```csharp
Assert.That(result.Name, Does.Contain("15"));
Assert.That(button.IsEnabled, Is.True);
Assert.That(textBox.Text, Is.EqualTo("hello"));
```

## Mapping from `flaui` CLI

| CLI | C# |
|---|---|
| `find --by automationId --value X` | `window.FindFirstDescendant(cf => cf.ByAutomationId("X"))` |
| `find --by name --value X` | `cf.ByName("X")` |
| `click --by ... ` | `.AsButton().Invoke()` (or `.Click()`) |
| `set-value --text ...` | `.AsTextBox().Text = ...` |
| `toggle` | `.AsCheckBox().IsChecked = ...` / `.Patterns.Toggle.Pattern.Toggle()` |
| `expand` / `collapse` | `.Patterns.ExpandCollapse.Pattern.Expand()/.Collapse()` |
| `press-key` / `key-combo` | `Keyboard.Type(...)` / `Keyboard.TypeSimultaneously(...)` |
| `wait-for` | `Retry.WhileNull(...)` / `.WaitUntilClickable()` |
| `screenshot` | `FlaUI.Core.Capturing.Capture.Element(element).ToFile(path)` |

**Workflow:** discover the element with `flaui`, confirm it is stable across a re-dump, then translate to `FindFirstDescendant` + `Retry`. Run the same selector in the test.
