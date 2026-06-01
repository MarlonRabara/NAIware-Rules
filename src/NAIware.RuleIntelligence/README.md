# NAIware.RuleIntelligence

Standalone .NET 10 / latest C# rule-authoring intelligence library.

This project is intended to live outside the Rule Editor UI and outside the windows application. The UI should consume this library for all rule IntelliSense behavior.

## Why this project exists

The current editor has lightweight completion logic in `NAIware.RuleEditor.IntelliSenseService`. That is useful, but it mixes editor behavior with reflection/path discovery. This library consolidates:

- rule schema discovery
- reflected tree mapping
- path resolution
- cursor context analysis
- property/member completion
- type-aware operator suggestions
- literal/value suggestions
- `and` / `or` completion
- ranking/filtering
- future diagnostics and validation hooks

## Repository alignment

This library is designed to use the existing `NAIware.Core.Reflection.ObjectTreeHydrator`.

Expected source shape:

```text
NAIware.Core.Reflection.ObjectTreeHydrator
  -> Tree
  -> Tree.Root
  -> TreeNode.Value = ReflectedPropertyNode
  -> ReflectedPropertyNode.Name
  -> ReflectedPropertyNode.Path
  -> ReflectedPropertyNode.Type
  -> ReflectedPropertyNode.PropertyInfo
  -> ReflectedPropertyNode.IsCollection
  -> ReflectedPropertyNode.IsCollectionItem
```

The comparison and logical operators intentionally align with `NAIware.Rules`:

```text
Comparison: =, !=, <>, >, <, >=, <=
Logical: and, or, &&, ||
```

## Public API

```csharp
IRuleSchemaProvider schemaProvider = new ObjectTreeRuleSchemaProvider();
IRuleIntelliSenseService completionService = new RuleIntelliSenseService();

RuleSchema schema = schemaProvider.Build(typeof(MyRuleContext), "Context");

RuleCompletionResponse response = completionService.GetCompletions(new RuleCompletionRequest
{
    Schema = schema,
    Expression = "Property.City = ",
    CursorPosition = "Property.City = ".Length
});
```

## UI usage

The editor should call only this library:

```csharp
var response = _ruleIntelliSense.GetCompletions(new RuleCompletionRequest
{
    Schema = _schema,
    Expression = txtExpression.Text,
    CursorPosition = txtExpression.SelectionStart,
    MaxItems = 50
});

completionList.DataSource = response.Items;
```

The UI should not directly:

- reflect properties
- walk object graphs
- parse dot paths
- infer types
- choose operators
- decide whether logical connectors are valid

## Install into the existing solution

Place the project here:

```text
src/NAIware.RuleIntelligence/
```

Add it to the solution:

```powershell
dotnet sln add .\src\NAIware.RuleIntelligence\NAIware.RuleIntelligence.csproj
```

Then reference it from the Rule Editor UI:

```powershell
dotnet add .\src\NAIware.RuleEditor\NAIware.RuleEditor.csproj reference .\src\NAIware.RuleIntelligence\NAIware.RuleIntelligence.csproj
```

## Suggested migration

Replace old UI-level calls like:

```csharp
var suggestions = _intelliSenseService.GetSuggestions(context, prefix);
```

With:

```csharp
var response = _ruleIntelliSense.GetCompletions(new RuleCompletionRequest
{
    Schema = _schema,
    Expression = editorText,
    CursorPosition = cursor
});

var suggestions = response.Items;
```

## Notes

The library currently assumes the NAIware object tree uses `ObjectTreeHydrator` and `ReflectedPropertyNode`.
If the tree implementation changes, only `ObjectTreeRuleSchemaProvider` and `RuleSchemaMapper` should need changes.
