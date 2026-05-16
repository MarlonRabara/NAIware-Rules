# Migration from NAIware.RuleEditor.IntelliSenseService

## Existing behavior

The current editor service:

- resolves context type using `AssemblyTypeDiscoveryService`
- builds dot-notation property path strings
- caches path strings
- returns simple prefix-based string suggestions
- includes generic keywords/operators

## New behavior

`NAIware.RuleIntelligence`:

- builds a typed schema from `ObjectTreeHydrator`
- keeps `PropertyInfo`, `Type`, collection flags, and paths
- resolves partial paths
- suggests type-aware operators
- suggests values based on expected type
- suggests logical connectors only when appropriate
- returns rich completion items instead of strings

## Recommended UI adapter

If the UI still needs strings temporarily:

```csharp
IReadOnlyList<string> labels = response.Items
    .Select(x => x.Label)
    .ToList();
```

Longer-term, bind the dropdown to:

```text
Label
Detail
Kind
Documentation
InsertText
```
