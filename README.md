# flaui-skill

A lightweight, stateless Windows UI probe built on [FlaUI](https://github.com/FlaUI/FlaUI)/UIA3, plus a skill that teaches an AI agent to use it. It exists for two jobs:

1. **Debug a failing or flaky FlaUI / UIA test** — see what the UI *actually* is and why a selector broke.
2. **Author a new C# FlaUI test** — discover real elements, then write idiomatic NUnit/xUnit code.

`flaui` runs, inspects or acts, and exits. Output is plain text you grep through. Never guess selectors: **snapshot first, act, re-snapshot to verify.**

## Requirements

- **Run:** Windows (the tool drives the local desktop via UIA).
- **Build:** [.NET 8 SDK](https://dotnet.microsoft.com/download). Cross-compiling from Linux/macOS to `win-x64` works; running the result needs Windows.

## Build

The prebuilt binary is not committed (`dist/` is gitignored), so build it once:

```powershell
# Windows
./scripts/build.ps1
```

```bash
# Linux / macOS (cross-compile) or Windows (Git Bash / WSL)
./scripts/build.sh
```

Both publish a self-contained, single-file `dist/flaui.exe` (`net8.0-windows`, `win-x64`) and print its version.

## Quick start

```bash
flaui launch --path calc.exe --wait --window Calculator
flaui dump   --window Calculator --depth 4
flaui click  --window Calculator --automation-id num7Button
flaui find   --window Calculator --automation-id CalculatorResults --props text
flaui screenshot --window Calculator --out before.png
flaui close  --window Calculator
```

> `flaui` below is shorthand for `dist/flaui.exe`.

## Commands

Window targeting (most commands; default = foreground window):
`--pid N` · `--process <name>` · `--window <title substring>` · `--hwnd 0x...`

Element selector:
`--by automationId|name|text|class|controlType|xpath --value V`
Shorthands `--automation-id`, `--name`, `--class`, `--xpath`; plus `--index N`, `--exact`.

```
INSPECT
  dump         [--by/--value] [--depth N] [--props ...] [--visible-only] [--max-elements N] [--out file]
  find         --by/--value [--index N] [--props ...] [--tree]
  get-property --by/--value [--prop name | --props name,value,...]
  screenshot   [--out f.png] [--screen] [--by/--value]
  list-windows [--process name] [--window title]

ACT
  click | double-click | right-click  --by/--value [--mouse]
  set-value    --by/--value --text "..." [--clear]
  type-text    [--by/--value] --text "..."
  select       --by/--value --item "..."      # ComboBox / list items
  toggle | expand | collapse
  scroll       --by/--value [--direction up|down|pageup|pagedown|home|end] [--amount small|large]

KEYBOARD / MOUSE
  press-key | key-combo | move | drag

APP / WINDOW
  launch --path <exe> [--args "..."] [--wait] [--window <title>] [--timeout s]
  attach --pid N | --window <title>
  close | focus | maximize | minimize | restore

SYNC
  wait-for  --by/--value [--gone] [--timeout s] [--interval ms]
  wait-idle --pid N | [-w opts] [--timeout s]
```

Run `flaui` with no arguments for the full usage text.

## Element selection strategy

Prefer, in order: `automationId` (best) → `name` → `class`/`controlType` → `xpath` (last resort). After every action, re-dump or screenshot to confirm the state changed as expected.

## Repo layout

```
flaui-skill/
├── SKILL.md              # the agent-facing skill definition
├── PLAN.md               # design notes / plan
├── README.md
├── tool/flaui/           # the C# CLI (net8.0-windows, FlaUI.UIA3)
├── scripts/              # build.ps1 / build.sh -> dist/flaui.exe
├── references/           # selectors, C# idioms, control types, patterns, troubleshooting
└── evals/evals.json      # end-to-end checks (run on Windows)
```

## References

- `references/selectors.md` — AutomationId vs Name vs XPath; stability; how to find them.
- `references/flaui-csharp.md` — FlaUI C# idioms (find, act, assert, Retry/wait) for NUnit/xUnit.
- `references/control-types.md` — UIA ControlType values and what they mean.
- `references/uia-patterns.md` — patterns (Invoke, Value, Toggle, ExpandCollapse, Selection...).
- `references/troubleshooting.md` — why tests break and how to fix them.

## License

No license file is included yet; add one before relying on this in a public/commercial context.
