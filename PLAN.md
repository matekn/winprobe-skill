---

# `flaui` Skill — Plan

A lightweight Windows UI automation tool for LLMs, built on FlaUI, for **debugging FlaUI test failures** and **authoring C# (NUnit/xUnit) FlaUI tests**.

## Goal

A small self-contained C# CLI (`flaui.exe`) that dumps a Windows app's UI state as **plain-text snapshots** the LLM greps through, plus action commands for probing. The skill teaches the LLM to use it for two jobs:

1. **Debugging FlaUI test failures** — capture what the UI *actually* looks like, find why a selector broke.
2. **Authoring C# FlaUI tests** — discover elements/workflows, then write/edit NUnit/xUnit FlaUI code (guidance only, no framework generation).

## Design decisions (from requirements)

- Build the wrapper tool **from scratch**.
- **Snapshot-first, file-based communication**: plain text output the LLM searches through.
- **Stateless** — no server, no daemon, no live handles.
- Existing tests are **C# (NUnit/xUnit) + FlaUI**; newly created tests match this.
- "Create tests" output is **not a strict artifact** — the LLM gets help to discover elements and write/edit test code itself.
- Snapshot format: **plain text only**.

## Architecture

- **Stateless CLI**: each invocation runs and exits.
- C# / .NET 8 (`net8.0-windows`), FlaUI.UIA3 (latest stable), published self-contained single-file `win-x64`.
- Snapshot-first: outputs are files/stdout; actions re-specify targets from the snapshot by stable selector.

## Plain-text snapshot format (the core)

Indented tree, one element per line — greppable and human-readable:

```
Window "Untitled - Notepad" [automationId=][hwnd=0x1234]
├─ MenuBar "Application" [automationId=MenuBar]
├─ Document "Text Editor" [automationId=15] ControlType=Document
│  └─ Edit "" [automationId=15] enabled
├─ TitleBar "" [automationId=TitleBar]
│  ├─ Button "Minimize" [automationId=Minimize]
│  ├─ Button "Maximize" [automationId=Maximize]
│  └─ Button "Close" [automationId=Close]
└─ StatusBar ""
```

Flags control depth and which properties to print (`--depth`, `--props name,automationId,text`). Actions select by `--by automationId|name|text|xpath`.

## CLI command surface

- **Inspect**: `dump` (text tree), `screenshot` (`--element|--window|--screen`), `find` (single element info), `get-property`
- **Act**: `click`, `double-click`, `right-click`, `set-value`, `type-text`, `select`, `toggle`, `expand`/`collapse`, `scroll`
- **Keyboard/mouse**: `press-key`, `key-combo`, `hotkey`, `move`, `drag`
- **App/window**: `launch`, `attach` (by pid/name), `close`, `focus`, `maximize`/`minimize`/`restore`, `list-windows`
- **Sync**: `wait-for` (element/condition), `wait-idle`

## Element selection strategy

- Prefer `AutomationId` (most stable) → `Name` → text/XPath (last resort).
- After every action, re-dump/screenshot to verify state before proceeding.
- Everything stays stateless and deterministic.

## SKILL.md design (progressive disclosure)

- **Frontmatter description**: pushy, triggers on "debug this FlaUI test", "why did this test fail", "write a UI test for X", "find the AutomationId of...", "automate this Windows app".
- **Body** = the discover → probe → verify loop:
  1. `dump` + `screenshot` to see real state
  2. grep the dump for the target; pick the most stable selector (AutomationId > Name > text/xpath)
  3. act with `--by …`, then re-dump/screenshot to verify
  4. For failures: locate the divergence from what the test *expected* (missing/renamed/id changed/timing) and suggest the fix
  5. For authoring: translate discovered elements into FlaUI C# idioms

## references/

- `selectors.md` — AutomationId vs Name vs XPath; when each breaks; how to find stable IDs
- `flaui-csharp.md` — FlaUI C# snippets (find/act/assert, `Retry`, waits) for writing NUnit/xUnit tests
- `control-types.md` + `uia-patterns.md` — AutomationType enum and patterns
- `troubleshooting.md` — common failure causes (WinForms vs UIA3, timing, stale handles, culture/localization) and fixes

## Repo layout

```
flaui-skill/
├── SKILL.md
├── tool/flaui/            # C# CLI (net8.0-windows, FlaUI.UIA3)
│   ├── Program.cs
│   └── Commands/…
├── scripts/build.sh       # dotnet publish -r win-x64 --self-contained -p:PublishSingleFile=true
├── references/            # the 5 md files above
└── evals/evals.json
```

## Milestones

1. Scaffold CLI + `dump`/`screenshot`/`find` (inspection path) — the foundation
2. Add action + keyboard/mouse + window + sync commands
3. `build.sh`; cross-compile `win-x64` self-contained exe
4. Write SKILL.md + references
5. Evals (Notepad/Calculator cases) run on Windows; iterate

## Notes / loose ends

- **Dev on Linux, run on Windows**: code + cross-compile fine on Linux; end-to-end testing needs a Windows box/VM.
- Everything stays stateless and deterministic — no hidden framework, just a probing tool + guidance.

---
