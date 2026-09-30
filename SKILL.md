---
name: flaui
description: >-
  Debug failing FlaUI / UIA Windows UI tests and author NUnit/xUnit FlaUI tests by
  inspecting the real UI. Use whenever a Windows UI automation test fails or is flaky
  ("why did this test fail", "selector not found", "element not found", "flaky", "timing"),
  when locating UI elements or AutomationIds ("find the AutomationId of", "what is the name of the
  button"), when writing or fixing C# UI automation with FlaUI, TestStack.White, or UIA,
  or whenever you need to drive, probe, click, type into, screenshot, or dump the UI of a
  Windows desktop app (Notepad, Calculator, Explorer, WinForms, WPF, WinUI/UWP). Also use to
  capture a plain-text snapshot of any app's element tree.
---

# FlaUI Debugger & Test Author

`flaui` is a stateless Windows UI probe built on FlaUI/UIA3. Each invocation inspects or acts and exits. Output is **plain text** you grep through. It exists for two jobs:

1. **Debug a failing/flaky FlaUI (or UIA) test** — see what the UI *actually* is, find why a selector broke.
2. **Author a new C# FlaUI test** — discover real elements/selectors, then write/edit NUnit/xUnit code.

Never guess selectors or element state. **Snapshot first, then act, then re-snapshot to verify.**

## Locating the tool

- Prebuilt (if present): `dist/flaui.exe`
- Otherwise build once: run `scripts/build.ps1` (Windows) or `scripts/build.sh` (cross-compile) and use `dist/flaui.exe`.
- The examples below use `flaui` as shorthand for that path.

If the tool can't be built (no .NET SDK) and no `dist/flaui.exe` exists, say so instead of guessing.

## The loop (follow this every time)

1. **Discover.** Launch or attach, then `dump` the window to get a text element tree.
2. **Locate.** Grep the dump for the target. Pick the most stable selector:
   `automationId` (best) → `name` → `class`/`controlType` → `xpath` (last resort).
3. **Act.** Run an action with `--by/--value`, then **re-dump or screenshot** and confirm the state changed as expected.
4. **Diagnose failures** by comparing the *dump* to what the test *expected* (missing / renamed / id changed / timing / wrong window).
5. **Author**: translate the discovered elements into idiomatic FlaUI C# (see `references/flaui-csharp.md`).

## Commands

Window targeting (most commands; **default = foreground window**):
`--pid N` · `--process <name>` (e.g. `Calculator`) · `--window <title substring>` · `--hwnd 0x...`

Element selector:
`--by automationId|name|text|class|controlType|xpath --value V`
Shorthands: `--automation-id V`, `--name V`, `--class V`, `--xpath V` · `--index N` · `--exact`

```
INSPECT
  dump         [--by/--value] [--depth N] [--props class,enabled,value,text,bounds,patterns,hwnd]
               [--visible-only] [--max-elements N] [--out file]        # text tree of a window or element
  find         --by/--value [--index N] [--props ...] [--tree]          # single element (or subtree)
  get-property --by/--value [--prop name | --props name,value,...]      # omit selector for the window itself
  screenshot   [--out f.png] [--screen] [--by/--value]                  # window by default
  list-windows [--process name] [--window title]

ACT  (each prints "target: <element line>"; add --quiet to silence)
  click | double-click | right-click  --by/--value [--mouse]
  set-value    --by/--value --text "..." [--clear]
  type-text    [--by/--value] --text "..."
  select       --by/--value --item "..."          # ComboBox / list items
  toggle | expand | collapse                     # --by/--value
  scroll       --by/--value [--direction up|down|pageup|pagedown|home|end] [--amount small|large]

KEYBOARD / MOUSE
  press-key --key <k>          # enter, esc, tab, space, backspace, delete, f1..f24, a, 0, up, home...
  key-combo --key ctrl+shift+s # chords/hotkeys
  move      --x N --y N | --by/--value
  drag      --by/--value --to-by/--to-value <dest> | --to-x N --to-y N

APP / WINDOW
  launch --path <exe> [--args "..."] [--wait] [--window <title>] [--timeout s]
  attach --pid N | --window <title>
  close  [--kill] [-w opts]   |   focus | maximize | minimize | restore

SYNC
  wait-for  --by/--value [--gone] [--timeout s] [--interval ms]
  wait-idle --pid N | [-w opts] [--timeout s]
```

## Examples

```bash
flaui launch --path calc.exe --wait --window Calculator
flaui dump --window Calculator --depth 4
flaui click --window Calculator --automation-id num7Button
flaui find --window Calculator --automation-id CalculatorResults --props text
flaui screenshot --window Calculator --out before.png
flaui wait-for --window Calculator --name "Equals" --timeout 5
```

Always prefer `--automation-id` when the dump shows one. Re-dump after acting.

## Debugging a failing test — how to reason

When a FlaUI test fails, it usually asserts on a selector. Reproduce the state and compare:

- `wait-for --by ... --value ... --timeout 3` — does the element exist *at all* right now?
- `dump` the window the test targets — is it the **right window**? (store apps host under `ApplicationFrameHost`).
- If the element exists but by a different id/name → the selector changed; give the new one.
- If it only appears after a delay → timing; recommend a `WaitUntil`/`Retry` (see references).
- If it's a WinForms app, legacy selectors behave differently (see `references/troubleshooting.md`).

See `references/troubleshooting.md` for the common failure taxonomy.

## References

- `references/selectors.md` — AutomationId vs Name vs XPath; stability; how to find them.
- `references/flaui-csharp.md` — FlaUI C# idioms (find, act, assert, Retry/wait) for NUnit/xUnit.
- `references/control-types.md` — UIA ControlType values and what they mean.
- `references/uia-patterns.md` — patterns (Invoke, Value, Toggle, ExpandCollapse, Selection...) and how they map to actions.
- `references/troubleshooting.md` — why tests break and how to fix them.
