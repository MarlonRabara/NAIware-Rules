# NAIware Rules — Catalog & Runtime Extension Design

## 1. Existing Framework Analysis

### Current Architecture Summary

The existing rules engine is a **runtime-only, in-memory expression evaluator**. There is no
persistent catalog, no versioning, and no structured result model. The key entities are:

| Existing Entity | Role | Key Traits |
|---|---|---|
| `Identification` | Identity (Guid + Name) on `RuleTree`/`FormulaTree` | Immutable, `ICloneable` |
| `RuleTree` | Parsed boolean expression tree | Contains `Identification`, evaluates to `bool` |
| `FormulaTree` | Parsed decimal expression tree | Contains `Identification`, evaluates to `decimal?` |
| `ExpressionGroup` | Named grouping with parent-child hierarchy | Has `Name`, `Parent`, `Container` (IEngine) |
| `RuleGroup` | Internal specialization of `ExpressionGroup` for rules | Holds `List<RuleTree>`, supports `GetAllRules()` with inheritance |
| `Rules.Engine` | The rules processing engine | Parses expressions, manages groups, executes rules, returns `List<Identification>` |
| `Parameters` | `Dictionary<string, IParameter>` | Runtime parameter bag, cloneable |
| `ParameterFactory` | Reflection-based parameter extraction from objects | Supports simple types, nested objects, collections via dot-notation |
| `IParameter` / `GenericParameter<V>` | Named typed value in an expression | Has `Name`, `Description`, `Value`, `Type` |
| `IExpression<R>` | Core expression interface | `Evaluate()`, `LeftOperand`, `RightOperand`, `Operator` |
| `LogicProcessorEngine` | Hybrid evaluator (rules + formulae + methods) | Takes expression string, `MethodMap`, `Parameters` |

### Patterns & Conventions Observed

- **Naming**: PascalCase types, `_camelCase` private fields, no `I` prefix on abstract classes
- **Identity**: `Identification` class with `Guid Identity` + `string Name`
- **Grouping**: String-keyed `Dictionary<string, ExpressionGroup>` on `EngineBase`
- **Execution returns**: `List<Identification>` — just the IDs of rules that fired
- **No persistence**: Everything is constructed in-memory via API calls
- **Namespace layout**: Root `NAIware.Rules`, sub-namespaces `Rules`, `Formulae`

### What Already Maps to New Concepts

| New Concept | Existing Analog | Can Extend? |
|---|---|---|
| Rule Context | No direct analog (engine itself acts as implicit context) | New entity needed |
| Rule Category | `ExpressionGroup` / `RuleGroup` | Extend pattern, new entity |
| Rule Expression | `RuleTree` + expression string | Wrap `RuleTree` with metadata |
| Rule Parameter | `IParameter` / `GenericParameter<V>` | Reuse for runtime; new definition entity |
| Rule Result | `Identification` (returned on match) | New entity — Identification is too thin |
| Versioning | None | New concept entirely |
| Rule Processor | `Rules.Engine.Execute()` | New higher-level API that wraps engine |

## 2. Target Domain Model

### Design Principles

1. **Catalog entities are POCOs** — no dependency on the parsing engine. They define *what* to evaluate.
2. **Runtime entities are separate** — they carry evaluation state and results. They are produced by the processor.
3. **The existing engine remains untouched** — `Rules.Engine`, `RuleTree`, `ExpressionNode<R>`, etc. continue to do parsing and evaluation. The new catalog/processor layer sits above them.
4. **Backward-compatible** — existing usage patterns (`engine.AddRule(...)`, `engine.Execute()`) continue to work.

### Entity Relationship Diagram (Text)

```
RulesLibrary
  └─── 1:N ──→ RuleContext
                  ├─── 1:N ──→ RuleCategory
                  │               └─── M:N ──→ RuleExpression  (via RuleCategoryExpression)
                  ├─── 1:N ──→ RuleExpression
                  │               ├── RuleResultDefinition (1:1 embedded)
                  │               └── ExpressionVersion (1:N history)
                  └─── 1:N ──→ RuleParameterDefinition
                                  └─── M:N ──→ RuleExpression (via RuleExpressionParameter)
```

### Namespace Layout

```
NAIware.Rules.Catalog/         — Design-time domain model
    RulesLibrary.cs
    RuleContext.cs
    RuleCategory.cs
    RuleExpression.cs
    RuleResultDefinition.cs
    ExpressionVersion.cs
    RuleParameterDefinition.cs
    RuleCategoryExpression.cs   — M:N join entity
    RuleExpressionParameter.cs  — M:N join entity

NAIware.Rules.Runtime/         — Evaluation request/result model
    RuleEvaluationRequest.cs
    RuleEvaluationResult.cs
    RuleExpressionResult.cs
    RuleMismatchDiagnostic.cs

NAIware.Rules.Processing/      — High-level processor
    IRuleProcessor.cs
    RuleProcessor.cs
    IRuleContextResolver.cs
    ReflectionRuleContextResolver.cs
```

### Catalog Entities — Attribute Derivation

All identity patterns follow the existing `Identification` class (`Guid` + `string Name`).

#### RulesLibrary
| Property | Type | Source |
|---|---|---|
| `Identity` | `Guid` | From `Identification` pattern |
| `Name` | `string` | From `Identification` pattern |
| `Description` | `string` | From `IParameter.Description` pattern |
| `Contexts` | `List<RuleContext>` | New |
| `CreatedUtc` | `DateTimeOffset` | New — standard for catalog entities |

#### RuleContext
| Property | Type | Source |
|---|---|---|
| `Identity` | `Guid` | From `Identification` |
| `Name` | `string` | From `Identification` |
| `Description` | `string` | From `IParameter.Description` |
| `QualifiedTypeName` | `string` | New — the `Type.FullName` of the target input object; enables auto-resolution |
| `Categories` | `List<RuleCategory>` | New |
| `Expressions` | `List<RuleExpression>` | New |
| `ParameterDefinitions` | `List<RuleParameterDefinition>` | New |

#### RuleCategory
| Property | Type | Source |
|---|---|---|
| `Identity` | `Guid` | From `Identification` |
| `Name` | `string` | From `Identification` — analogous to `ExpressionGroup.Name` |
| `Description` | `string` | From `IParameter.Description` |
| `CategoryExpressions` | `List<RuleCategoryExpression>` | New — M:N join |

#### RuleExpression
| Property | Type | Source |
|---|---|---|
| `Identity` | `Guid` | From `Identification` — logical identity, stable across versions |
| `Name` | `string` | From `Identification` |
| `Description` | `string` | From `IParameter.Description` |
| `Expression` | `string` | The raw expression string (analogous to what `engine.AddRule(expression)` takes) |
| `Version` | `int` | New — current version number |
| `IsActive` | `bool` | New — soft-disable without removal |
| `ResultDefinition` | `RuleResultDefinition?` | New — 1:1 embedded |
| `Versions` | `List<ExpressionVersion>` | New — version history |
| `ExpressionParameters` | `List<RuleExpressionParameter>` | New — M:N join to parameters |

#### ExpressionVersion
| Property | Type | Source |
|---|---|---|
| `Identity` | `Guid` | Unique per version snapshot |
| `Version` | `int` | Monotonic version counter |
| `Expression` | `string` | The expression text at this version |
| `CreatedUtc` | `DateTimeOffset` | Audit trail |
| `ChangeNote` | `string?` | Optional description of why it changed |

#### RuleResultDefinition
| Property | Type | Source |
|---|---|---|
| `Code` | `string` | New — application-defined code (e.g., "ELIG-001") |
| `Message` | `string` | New — human-readable description |
| `Severity` | `string?` | New — optional hint; the engine is agnostic about interpretation |

#### RuleParameterDefinition
| Property | Type | Source |
|---|---|---|
| `Identity` | `Guid` | From `Identification` |
| `Name` | `string` | From `IParameter.Name` |
| `Description` | `string` | From `IParameter.Description` |
| `QualifiedTypeName` | `string` | From `IParameter.Type.FullName` pattern |
| `PropertyPath` | `string?` | New — dot-notation path for `ParameterFactory` extraction |

#### Join Entities
- `RuleCategoryExpression` — `CategoryIdentity` + `ExpressionIdentity` + `Ordinal`
- `RuleExpressionParameter` — `ExpressionIdentity` + `ParameterIdentity`

### Runtime Entities

#### RuleEvaluationRequest
| Property | Type |
|---|---|
| `InputObject` | `object` |
| `CategoryName` | `string?` (null = evaluate all) |
| `IncludeDiagnostics` | `bool` |

#### RuleEvaluationResult
| Property | Type |
|---|---|
| `ContextName` | `string` |
| `CategoryName` | `string?` |
| `Matches` | `List<RuleExpressionResult>` |
| `Mismatches` | `List<RuleExpressionResult>` |
| `EvaluatedUtc` | `DateTimeOffset` |

#### RuleExpressionResult
| Property | Type |
|---|---|
| `ExpressionIdentity` | `Guid` |
| `ExpressionName` | `string` |
| `ExpressionVersion` | `int` |
| `Matched` | `bool` |
| `Result` | `RuleResultDefinition?` (populated on match) |
| `Diagnostic` | `RuleMismatchDiagnostic?` (populated on mismatch when requested) |

#### RuleMismatchDiagnostic
| Property | Type |
|---|---|
| `Expression` | `string` (the raw expression) |
| `EvaluatedParameters` | `Dictionary<string, string?>` (name → actual value) |
| `Explanation` | `string?` (optional human-readable reason) |

## 3. Design Decisions & Tradeoffs

1. **Catalog POCOs, not database entities** — The existing framework has zero persistence. Adding EF Core or database coupling would be out of scope. The catalog is an in-memory model that could be serialized to JSON/XML by the consumer.

2. **`RuleExpression.Identity` is stable across versions** — The `Guid` identifies the logical rule. `ExpressionVersion` records snapshots. This avoids duplicating context/category structure per version.

3. **`QualifiedTypeName` on `RuleContext`** — This is the key to auto-resolution. The `RuleProcessor` can match `inputObject.GetType().FullName` against registered contexts. This follows the `Type.FullName` pattern already used in `Factory.GetValue()` and `ParameterFactory`.

4. **The existing `Rules.Engine` is used internally** — `RuleProcessor` creates a `Rules.Engine`, adds parameters via `ParameterFactory`, adds rules from the catalog, executes, and maps results. No engine changes needed.

5. **M:N join entities are explicit classes** — Rather than hidden `List<List<>>` nesting, the joins are first-class to support ordinal/ordering and future metadata.

6. **`RuleResultDefinition` is embedded, not a separate entity** — Each expression owns its result definition. This keeps the model simple and avoids a separate result catalog.

7. **Mismatch diagnostics are opt-in** — When `IncludeDiagnostics` is false, no diagnostic objects are created. When true, the processor captures the parameter values that were used and the raw expression text.
