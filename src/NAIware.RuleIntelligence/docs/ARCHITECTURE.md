# Architecture

## Dependency direction

```text
Win App / NAIware.RuleEditor UI
    ↓
NAIware.RuleIntelligence
    ↓
NAIware.Core.Reflection
    ↓
NAIware.Rules
```

`NAIware.RuleIntelligence` should never reference the Win application or WinForms UI.

## Main flow

```text
Editor text + cursor
    ↓
RuleCompletionRequest
    ↓
RuleCompletionContextParser
    ↓
RulePathResolver
    ↓
RuleOperatorProvider / RuleValueSuggestionProvider
    ↓
RuleCompletionResponse
    ↓
UI dropdown
```

## Schema flow

```text
Root context Type
    ↓
ObjectTreeHydrator.Create(...)
    ↓
Tree of ReflectedPropertyNode
    ↓
RuleSchemaMapper
    ↓
RuleSchema / RuleCompletionNode index
```

## Why map to RuleCompletionNode?

The existing reflected node is excellent as a reflection source. The intelligence layer still benefits from its own model because completions need editor-specific concepts:

- display type
- completion kind
- ranking
- rootless path aliases
- synthetic `Count` nodes
- parent/child navigation
- future diagnostics and validation metadata
