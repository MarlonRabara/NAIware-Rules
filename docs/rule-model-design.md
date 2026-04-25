# NAIware Rules — Models & Runtime Extension Design

> **Terminology and versioning rule:** This project uses **Library** as the root term for a persisted set of rules. Do not use "catalog" for the product/domain naming. Versioning belongs to the **RulesLibrary** as a whole. Individual rule expressions are not versioned and do not maintain expression-level revision history. A context owns categories, categories may contain deeply nested subcategories, and rule expressions are attached at category leaf nodes.

## 1. Existing Framework Analysis

### Current Architecture Summary

The existing rules engine is a **runtime-only, in-memory expression evaluator**. There is no
persistent model, no versioning, and no structured result model. The key entities are:

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
| Versioning | None | New concept — **library-level** |
| Rule Processor | `Rules.Engine.Execute()` | New higher-level API that wraps engine |

## 2. Target Domain Model

### Design Principles

1. **Library entities are POCOs** — no dependency on the parsing engine. They define *what* to evaluate.
2. **Runtime entities are separate** — they carry evaluation state and results. They are produced by the processor.
3. **The existing engine remains untouched** — `Rules.Engine`, `RuleTree`, `ExpressionNode<R>`, etc. continue to do parsing and evaluation. The new library/processor layer sits above them.
4. **Backward-compatible** — existing usage patterns (`engine.AddRule(...)`, `engine.Execute()`) continue to work.
5. **Library-level versioning** — versioning is anchored at the `RulesLibrary`, not per-expression. The entire library (contexts, categories, expressions, parameters, result definitions) is snapshotted as a single coherent unit. This dramatically reduces system complexity and avoids the coordination problems of per-expression versions.

### Versioning Strategy: Library-Level Snapshots

Rather than versioning each `RuleExpression` individually, the **entire `RulesLibrary` is the versioned artifact**. The rationale:

- **Coherence** — A rule change rarely happens in isolation. Expressions, parameters, categories, and result codes evolve together. A library snapshot captures a consistent design.
- **Reproducibility** — Replaying an historical evaluation requires only the library version; no complex cross-entity version resolution is needed.
- **Simplicity** — Consumers version one thing: the library. There is no confusion about which version of an expression applies when a category references it.
- **Operational clarity** — Releases, rollbacks, and audits operate on a single unit.

Expressions themselves remain **mutable leaf artifacts within a library version**. When a change is needed, the consumer publishes a new library version containing the updated expressions. The prior library version is preserved immutably for audit and replay.

### Entity Relationship Diagram (Text)

```
RulesLibrary  (versioned root)
  ├── Version, PreviousVersionIdentity, PublishedUtc, ChangeNote
  │
  └─── 1:N ──→ RuleContext
                  ├─── 1:N ──→ RuleCategory
                  │               └─── M:N ──→ RuleExpression  (via RuleCategoryExpression)
                  ├─── 1:N ──→ RuleExpression
                  │               └── RuleResultDefinition (1:1 embedded)
                  └─── 1:N ──→ RuleParameterDefinition
                                  └─── M:N ──→ RuleExpression (via RuleExpressionParameter)
```

### Namespace Layout

```
NAIware.Rules.Models/         — Design-time domain model
    RulesLibrary.cs            — Versioned root aggregate
    LibraryVersion.cs          — Immutable snapshot metadata
    RuleContext.cs
    RuleCategory.cs
    RuleExpression.cs          — Mutable leaf within a library version
    RuleResultDefinition.cs
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

### Library Entities — Attribute Derivation

All identity patterns follow the existing `Identification` class (`Guid` + `string Name`).

#### RulesLibrary *(the versioned aggregate root)*
| Property | Type | Source |
|---|---|---|
| `Identity` | `Guid` | Logical identity — stable across versions of the same library |
| `Name` | `string` | From `Identification` pattern |
| `Description` | `string` | From `IParameter.Description` pattern |
| `Version` | `int` | New — monotonic version counter for this library |
| `PreviousVersionIdentity` | `Guid?` | New — links to the prior snapshot's `SnapshotIdentity` (null for v1) |
| `SnapshotIdentity` | `Guid` | New — unique per version snapshot; distinct from `Identity` |
| `PublishedUtc` | `DateTimeOffset` | New — when this version was published |
| `ChangeNote` | `string?` | New — optional description of what changed in this version |
| `IsPublished` | `bool` | New — false while in draft, true once finalized |
| `Contexts` | `List<RuleContext>` | New |
| `Versions` | `List<LibraryVersion>` | New — in-memory history of prior snapshots (optional; consumer may persist separately) |

#### LibraryVersion *(audit/history record for a published library snapshot)*
| Property | Type | Source |
|---|---|---|
| `SnapshotIdentity` | `Guid` | Unique per snapshot |
| `Version` | `int` | The version number this snapshot represents |
| `PublishedUtc` | `DateTimeOffset` | When the snapshot was published |
| `ChangeNote` | `string?` | Optional description of the change |
| `LibrarySnapshot` | `RulesLibrary` | The immutable captured state of the library at that version |

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

#### RuleExpression *(mutable leaf within a library version)*
| Property | Type | Source |
|---|---|---|
| `Identity` | `Guid` | From `Identification` — logical identity, stable across library versions |
| `Name` | `string` | From `Identification` |
| `Description` | `string` | From `IParameter.Description` |
| `Expression` | `string` | The raw expression string (analogous to what `engine.AddRule(expression)` takes) |
| `IsActive` | `bool` | New — soft-disable without removal |
| `ResultDefinition` | `RuleResultDefinition?` | New — 1:1 embedded |
| `ExpressionParameters` | `List<RuleExpressionParameter>` | New — M:N join to parameters |

> **Note**: `RuleExpression` no longer carries its own version number or version history. The containing `RulesLibrary.Version` is the effective version of every expression it contains.

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
| `LibraryName` | `string` |
| `LibraryVersion` | `int` |
| `ContextName` | `string` |
| `CategoryName` | `string?` |
| `Matches` | `List<RuleExpressionResult>` |
| `Mismatches` | `List<RuleExpressionResult>` |
| `EvaluatedUtc` | `DateTimeOffset` |

> Results now include `LibraryName` + `LibraryVersion` so consumers can reconcile outcomes to the exact library snapshot that produced them.

#### RuleExpressionResult
| Property | Type |
|---|---|
| `ExpressionIdentity` | `Guid` |
| `ExpressionName` | `string` |
| `Matched` | `bool` |
| `Result` | `RuleResultDefinition?` (populated on match) |
| `Diagnostic` | `RuleMismatchDiagnostic?` (populated on mismatch when requested) |

> `LibraryVersion` is no longer a field here because expressions do not carry an individual version. The version is carried at the aggregate `RuleEvaluationResult.LibraryVersion` level.

#### RuleMismatchDiagnostic
| Property | Type |
|---|---|
| `Expression` | `string` (the raw expression) |
| `EvaluatedParameters` | `Dictionary<string, string?>` (name → actual value) |
| `Explanation` | `string?` (optional human-readable reason) |

## 3. Design Decisions & Tradeoffs

1. **Library POCOs, not database entities** — The existing framework has zero persistence. Adding EF Core or database coupling would be out of scope. The library is an in-memory model that could be serialized to JSON/XML by the consumer.

2. **Library-level versioning instead of expression-level versioning** — The `RulesLibrary` is the versioned unit; individual `RuleExpression` entries are mutable leaves within it. A change to any expression, category, parameter, or result definition constitutes a new library version. **Rationale**: expression-level versioning created high coordination cost (which expression version does a category point at? how do expressions of different versions interact within a single execution?) for little practical benefit. Library snapshots are coherent, auditable, and trivially reproducible.

3. **`RulesLibrary.Identity` is stable across versions; `SnapshotIdentity` is unique per version** — This mirrors the standard pattern for versioned aggregates: logical identity vs. snapshot identity. A consumer resolving "the current version of library X" uses `Identity`; an audit or replay uses `SnapshotIdentity`.

4. **`QualifiedTypeName` on `RuleContext`** — This is the key to auto-resolution. The `RuleProcessor` can match `inputObject.GetType().FullName` against registered contexts. This follows the `Type.FullName` pattern already used in `Factory.GetValue()` and `ParameterFactory`.

5. **The existing `Rules.Engine` is used internally** — `RuleProcessor` creates a `Rules.Engine`, adds parameters via `ParameterFactory`, adds rules from the library, executes, and maps results. No engine changes needed.

6. **M:N join entities are explicit classes** — Rather than hidden `List<List<>>` nesting, the joins are first-class to support ordinal/ordering and future metadata.

7. **`RuleResultDefinition` is embedded, not a separate entity** — Each expression owns its result definition. This keeps the model simple and avoids a separate result definition.

8. **Mismatch diagnostics are opt-in** — When `IncludeDiagnostics` is false, no diagnostic objects are created. When true, the processor captures the parameter values that were used and the raw expression text.

9. **Draft → Published lifecycle** — A library is mutable while `IsPublished = false`. Calling `Publish()` seals the version, records a `LibraryVersion` snapshot in history, and increments the version counter when the next revision begins. Expressions, categories, and parameters should only be modified on draft libraries. Published libraries are immutable from the consumer's perspective.

### Tradeoffs Considered

| Option | Pros | Cons | Decision |
|---|---|---|---|
| Library-level versioning (chosen) | Simple, coherent, easy reproducibility, no cross-entity version resolution | Small changes still bump the whole library | **Chosen** — complexity win dominates |
| Expression-level versioning (rejected) | Fine-grained history, minimal churn per change | High coordination cost, version-resolution ambiguity, harder audit | Rejected |
| Both (hybrid) | Theoretically most flexible | Maximum complexity, confusing for consumers, two overlapping audit trails | Rejected |
