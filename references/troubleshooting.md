# Troubleshooting failing / flaky UI tests

Work the taxonomy top-down. For each, reproduce with `flaui` and confirm before recommending a fix.

## 1. Wrong window

The test searched one window but the element lives in another.

- `list-windows` — is the target there? Multiple windows with the same title?
- Store/UWP apps (Calculator, Windows 11 Notepad, Settings) host under **`ApplicationFrameHost`**; the real content can be a child `Window` with `class="Windows.UI.Core.CoreWindow"`. Attach by **window title**, not process name.
- `--pid` / `--window` disambiguate. In C#, `app.GetMainWindow(automation)` then re-find, or enumerate `automation.GetDesktop().FindAllChildren()`.

## 2. Selector changed

The element exists but the AutomationId/Name/class is different from the test.

- `dump` the window and grep for the intended label.
- If found under a different `automationId` → give the test the new one. Prefer `AutomationId` over `Name`.
- If `automationId=` is empty, pick `name`/`class` (+ `--index`).

## 3. Timing / not yet present (the #1 cause of flakiness)

The element appears only after the app/animations/loading finish.

- `wait-for --by ... --value ... --timeout 3` — does it appear if you wait?
- Fix in tests: replace `Thread.Sleep` with `Retry.WhileNull(...)` or `element.WaitUntilClickable()` (see `flaui-csharp.md`). After `expand`/menu/`select`, the children may load lazily — re-find after the action.

## 4. Stale / disconnected element

The element was found earlier, then the UI rebuilt (navigation, re-render) and the cached reference is dead ("element is no longer available").

- Fix: re-find inside the retry, don't cache an `AutomationElement` across a state change.

## 5. Offscreen / virtualized / not scrolled

`rect` is `0,0,0,0` or the element is `offscreen`.

- `scroll --by ... --value ... --direction down`, or use `ScrollItem.ScrollIntoView` in C#.
- Virtualized lists only realize visible rows — scroll first, then find.

## 6. WinForms / legacy (UIA3 vs UIA2 / MSAA)

Old WinForms apps expose fewer properties and may need `automationId` fallbacks.

- Use `--props patterns,class` and lean on `class` + `name` + `--index`.
- In C#, try `UIA2Automation` if `UIA3Automation` cannot see a control.
- Pattern support may be `LegacyIAccessible` only — `click` falls back to a mouse click automatically.

## 7. Structure changed (xpath broke)

XPath is positional. If the app added a container, every index shifts.

- Migrate the test off `--xpath` onto `automationId`/`name`.

## 8. Culture / localization

`Name` is localized ("Guardar" vs "Save"). Tests pinned to an English string break on other locales.

- Prefer `AutomationId`; otherwise set the test culture or match a language-independent attribute.

## 9. DPI / coordinates / elevation

- Mouse-based actions can miss under scaling; prefer the pattern path (`click` does this by default).
- **Elevated (admin) windows** cannot be automated by a non-elevated process — UIA returns nothing. Run the test/utility elevated, or report the UAC boundary as the cause.

## 10. Multiple matches

`--by name --value "Save"` may hit several elements; the CLI takes the first exact match, then the first substring match.

- Add `class`/`controlType`, or `--index N`. The `find` output and the action `target:` line tell you which one you got.

## Reporting a diagnosis

State: (1) the window targeted, (2) the selector as written, (3) what the dump shows (present? different id? offscreen? late?), (4) the concrete fix (new selector / wait / scroll / window), and (5) the changed C# line.
