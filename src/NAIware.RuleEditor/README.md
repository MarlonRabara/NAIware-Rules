# NAIware Rule Editor (Windows Forms)

A developer-focused Windows Forms application for authoring, validating, and testing
rule libraries for the NAIware deterministic rules engine.

Target framework: **.NET 10** (`net10.0-windows`) · Windows Forms · C# 12

## Project Name & Solution Entry

The spec text mentions `NAIware.RuleEditor.WinForms` as the canonical folder and project
name, but the existing `NAIware-Rules.slnx` already registers this project as
`src/NAIware.RuleEditor/NAIware.RuleEditor.csproj`. To preserve the solution file and
avoid an unnecessary folder rename, the project name is kept as **`NAIware.RuleEditor`**.
The root namespace is `NAIware.RuleEditor`. If the folder/project rename is required for
downstream tooling, update `NAIware-Rules.slnx` and rename the folder; no code changes
are needed.

## Project Reference

The project references `NAIware.Rules` via a direct `ProjectReference`:

```xml
<ItemGroup>
  <ProjectReference Include="..\NAIware.Rules\NAIware.Rules.csproj" />
</ItemGroup>
```

The editor consumes the engine's domain model directly:

- `NAIware.Rules.Models.RulesLibrary`
- `NAIware.Rules.Models.RuleContext`
- `NAIware.Rules.Models.RuleCategory`
- `NAIware.Rules.Models.RuleExpression`
- `NAIware.Rules.Models.RuleResultDefinition`
- `NAIware.Rules.Processing.RuleProcessor`
- `NAIware.Rules.Runtime.RuleEvaluationRequest`
- `NAIware.Rules.Runtime.RuleEvaluationResult`

These types are **not** duplicated in the UI project. The editor persists a small set of
UI-facing `*Document` DTOs (see `EditorModels.cs`) so the on-disk JSON format remains
stable as the engine evolves. `CatalogMapper` bridges the UI DTOs to the engine model at
test-run time.

## File / Class Layout

| File | Role |
|---|---|
| `Program.cs` | Application entry point |
| `MainForm.cs` | Main editor window — menu, toolbar, tree, tabbed editor, error list, status bar |
| `EditorModels.cs` | UI-facing DTOs (`RuleLibraryDocument`, `RuleContextDocument`, `RuleCategoryDocument`, `RuleExpressionDocument`, `ValidationIssue`, `ReflectedTypeInfo`) |
| `RuleLibrarySerializer.cs` | JSON load/save |
| `AssemblyTypeDiscoveryService.cs` | Reflects public concrete types from a DLL; resolves context types |
| `ContextTypePickerDialog.cs` | Filtered list modal for choosing a context type |
| `IntelliSenseService.cs` | Reflected property-path cache shared with validation; supplies auto-complete suggestions |
| `RuleValidationService.cs` | Compiler-style validation (property paths, parentheses, type mismatch, result definition completeness) |
| `RuleTestService.cs` | Maps UI DTOs → engine library, creates `RuleProcessor`, evaluates against a hydrated input |
| `CatalogMapper.cs` | Maps `RuleLibraryDocument` ↔ `NAIware.Rules.Models.RulesLibrary` |
| `TestDataDialog.cs` | Prompts the user for a JSON/XML file and hydrates it into the selected context type |
| `TestResultDialog.cs` | Shows matches, mismatches, and diagnostics from a `RuleEvaluationResult` |

## Main Window Layout

The main window matches the mockup's structure:

- **Menu bar** — `File`, `Library`, `Test`, `Help` with standard keyboard shortcuts
  (Ctrl+N, Ctrl+O, Ctrl+S, Ctrl+Shift+S, Ctrl+D, Ctrl+R, F5, F6).
- **Toolbar** — quick access to all primary commands with tooltips.
- **Left panel** — rule library tree: `Library → Context → Categories → Subcategories → Rules`.
  Right-click yields context-aware add/delete actions. Nodes carry colored glyph icons
  (L/C/G/R) rendered programmatically so no binary assets are required.
- **Right panel** — tabbed editor that switches based on the selected tree node:
  - Rule: `Expression`, `Metadata` (name, description, enabled, priority, tags), `Result Definition` (code, message, severity, optional value).
  - Context: name, qualified type name (read-only), description, source assembly path.
  - Category: name, description.
  - Library: name, description.
- **Bottom panel** — Visual Studio-style error list with columns: icon, `Severity`,
  `Description`, `Context`, `Category`, `Rule`, `Expression Id`. Double-click an error to
  navigate to the offending rule and open the Expression tab.
- **Status bar** — shows the last operation plus an error/warning count summary.

## Features Implemented

| Feature | Status |
|---|---|
| New / Open / Save / Save As (JSON) | ✓ With dirty-state tracking and unsaved-changes prompt |
| Add Context From DLL | ✓ Reflects public concrete classes; stores qualified type name + assembly path |
| Tree view with library / contexts / categories / subcategories / rules | ✓ Full CRUD including nested subcategories |
| Raw expression editor (AND/OR, parentheses, dot notation, indexed collections) | ✓ Monospaced font, tab-safe editing |
| Basic IntelliSense (property paths, keywords, operators) | ✓ Triggered at ≥2 chars, positioned near caret, Enter/Tab to accept, Esc to dismiss |
| Shared reflection metadata between validation and IntelliSense | ✓ `IntelliSenseService.GetMetadata()` is used by both |
| Rule metadata editor (Name, Description, Enabled, Tags, Priority) | ✓ |
| Result definition editor (Code, Message, Severity, Optional Value) | ✓ |
| Compiler-style validation | ✓ Parentheses balance, unknown property paths, type-mismatch comparisons, missing result definitions |
| Visual Studio-style error list with double-click navigation | ✓ Icon column + colored severity + sortable |
| Test module with JSON/XML hydration and real rule processor execution | ✓ Uses `NAIware.Rules.Processing.RuleProcessor` with diagnostics |

## Intentionally Stubbed / Deferred

- **No manual property-value entry UI for tests.** As specified, tests require a JSON or
  XML file; there is no form-based data entry.
- **No plain-English translation** of rule expressions.
- **No visual rule-builder.**
- **Registered-method auto-complete.** The IntelliSense service is shaped to accept a
  method catalog in a future revision; only property paths, keywords, and operators are
  suggested today.
- **Syntax highlighting and inline error underlines** are out of scope for the first
  version (called out as future enhancements in the spec).
- **Subcategory hierarchy in the engine model.** Nested UI subcategories map onto real
  engine subcategories via `RuleCategory.AddSubcategory`. Selecting a parent category
  at runtime evaluates every descendant's active expressions.
- **Library-level versioning UI.** The library document carries a `Version` field and a
  `SavedUtc` timestamp, but the editor does not yet expose a Draft → Publish lifecycle.
  Publishing is a follow-up once engine-side `Publish()` semantics land.

## Keyboard Shortcuts

| Shortcut | Action |
|---|---|
| Ctrl+N | New Library |
| Ctrl+O | Open Library |
| Ctrl+S | Save Library |
| Ctrl+Shift+S | Save Library As |
| Ctrl+D | Add Context From DLL |
| Ctrl+R | Add Rule |
| Delete | Delete selected tree node |
| F5 | Test Rules |
| F6 | Validate Library |

## Running

From Visual Studio: set `NAIware.RuleEditor` as the startup project and press F5.

From CLI:

```powershell
dotnet run --project src/NAIware.RuleEditor/NAIware.RuleEditor.csproj
```

## Architecture Notes

- **Strict UI / service / model separation.** Services hold no UI references and can be
  unit-tested in isolation. The `MainForm` is the only class that touches both services
  and controls.
- **Nullable reference types enabled** across the project.
- **Zero build warnings** (CS1591 "missing XML comment" is suppressed for top-level
  form-building private methods; all public types carry XML docs).
- **No external NuGet dependencies** beyond the .NET runtime. All icons are rendered
  programmatically to keep the project binary-asset-free.
