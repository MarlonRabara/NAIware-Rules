# NAIware Rules — Models & Runtime Extension Design

> **Terminology and versioning rule:** This project uses **Library** as the root term for a persisted set of rules. Do not use "catalog" for the product/domain naming. Versioning belongs to the **RulesLibrary** as a whole. Individual rule expressions are not versioned and do not maintain expression-level revision history. A context owns categories, categories may contain deeply nested subcategories, and rule expressions are attached at category leaf nodes.


## Cross-Document Consistency Contract

The three specification files describe one connected system and must remain aligned:

| File | Responsibility |
|---|---|
| `rule-model-design.md` | Canonical entity model, runtime contracts, lifecycle states, validation model, persistence abstractions, and design tradeoffs. |
| `system-use-cases.md` | System behavior and runtime flows that operate on the canonical model entities. |
| `windows-ui.md` | Developer-facing Windows tooling that creates, edits, validates, tests, saves, loads, and publishes the same canonical model. |

Shared consistency rules:

1. **Library is the root term**. Do not introduce catalog/catalogue terminology.
2. **RulesLibrary is the versioned aggregate**. Individual Rule Expressions do not have independent versions.
3. **RulesLibrary.State is canonical** for lifecycle behavior. `IsPublished`, when present, is a derived convenience flag and must not conflict with `State`.
4. **RuleContext.QualifiedTypeName is the canonical persisted type binding**. Tooling should populate it from reflected .NET type metadata, preferably using the assembly-qualified CLR type name when available. Runtime context resolution may compare both assembly-qualified name and full type name to support loaded objects.
5. **Rule Categories may be deeply nested**, but Rule Expressions attach only to leaf categories. Non-leaf execution is rejected by default unless descendant leaf execution is explicitly enabled through category execution mode.
6. **Rule Parameters are owned by Rule Contexts** and referenced by Rule Expressions. The Windows UI parameter grid, validation flows, and runtime parameter extraction all operate on `RuleParameterDefinition`.
7. **Rule Result Definition is the match payload** returned when a Rule Expression evaluates true. The consuming application interprets the meaning of the result payload.
8. **Validation findings are not runtime mismatches**. Validation errors/warnings/information are design-time or pre-execution findings; mismatches are valid false expression outcomes; runtime failures are returned as structured `RuleEvaluationError` records.
9. **Windows UI actions must map to model changes and/or system use cases**. The UI does not introduce separate domain concepts that are not represented in the model or use-case flow.


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
                  │               ├─── self-referencing hierarchy through ParentCategoryIdentity
                  │               └─── M:N ──→ RuleExpression at category leaf nodes only (via RuleCategoryExpression)

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
    RuleEvaluationError.cs
    RuleEvaluationWarning.cs
    RuleExecutionMode.cs
    RuleCategoryExecutionMode.cs
    RuleEvaluationStatus.cs

NAIware.Rules.Processing/      — High-level processor
    IRuleProcessor.cs
    RuleProcessor.cs
    IRuleContextResolver.cs
    ReflectionRuleContextResolver.cs

NAIware.Rules.Validation/      — Library and expression validation
    IRuleLibraryValidator.cs
    RuleLibraryValidator.cs
    RuleValidationResult.cs
    RuleValidationFinding.cs

NAIware.Rules.Persistence/     — Storage abstraction and serialization
    IRuleLibraryRepository.cs
    JsonRuleLibrarySerializer.cs
    FileRuleLibraryRepository.cs

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
| `IsPublished` | `bool` | Derived convenience flag — true when `State` is not `Draft`; `State` remains canonical |
| `State` | `RulesLibraryState` | New — Draft, Published, Deprecated, Archived |
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
| `QualifiedTypeName` | `string` | New — canonical persisted .NET type binding; prefer `Type.AssemblyQualifiedName` when available, with `Type.FullName` comparison fallback for runtime resolution |
| `SourceAssemblyPath` | `string?` | New — design-time source DLL path used by tooling for reflection, IntelliSense, and validation; not required for runtime object matching when the type is already loaded |
| `Categories` | `List<RuleCategory>` | New |
| `Expressions` | `List<RuleExpression>` | New |
| `ParameterDefinitions` | `List<RuleParameterDefinition>` | New |

#### RuleCategory
| Property | Type | Source |
|---|---|---|
| `Identity` | `Guid` | From `Identification` |
| `Name` | `string` | From `Identification` — analogous to `ExpressionGroup.Name` |
| `Description` | `string` | From `IParameter.Description` |
| `ParentCategoryIdentity` | `Guid?` | New — supports deeply nested category hierarchy |
| `ChildCategories` | `List<RuleCategory>` | New — child category collection |
| `IsLeaf` | `bool` | New — true when the category has no child categories |
| `CategoryExpressions` | `List<RuleCategoryExpression>` | New — M:N join; expressions should be attached only to leaf categories |


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
| `Severity` | `string?` | New — optional application-defined result hint; the engine is agnostic about interpretation |

`RuleResultDefinition.Severity` is part of the match payload. It is separate from validation severity, which belongs to validation findings.

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
| `CategoryName` | `string` |
| `LibraryIdentity` | `Guid?` |
| `LibraryVersion` | `int?` |
| `IncludeDiagnostics` | `bool` |
| `IncludeInactiveRules` | `bool` |
| `ExecutionMode` | `RuleExecutionMode` |
| `CategoryExecutionMode` | `RuleCategoryExecutionMode` |


#### RuleEvaluationResult
| Property | Type |
|---|---|
| `LibraryName` | `string` |
| `LibraryVersion` | `int` |
| `SnapshotIdentity` | `Guid` |
| `ContextName` | `string` |
| `CategoryName` | `string?` |
| `Succeeded` | `bool` |
| `Status` | `RuleEvaluationStatus` |
| `ExecutionMode` | `RuleExecutionMode` |
| `CategoryExecutionMode` | `RuleCategoryExecutionMode` |
| `Matches` | `List<RuleExpressionResult>` |
| `Mismatches` | `List<RuleExpressionResult>` |
| `Errors` | `List<RuleEvaluationError>` |
| `Warnings` | `List<RuleEvaluationWarning>` |
| `EvaluatedUtc` | `DateTimeOffset` |


> Results include `LibraryName`, `LibraryVersion`, and `SnapshotIdentity` so consumers can reconcile outcomes to the exact library snapshot that produced them.

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

#### RuleEvaluationError
| Property | Type |
|---|---|
| `Code` | `string` |
| `Message` | `string` |
| `ContextName` | `string?` |
| `CategoryName` | `string?` |
| `ExpressionIdentity` | `Guid?` |
| `Severity` | `string` |

#### RuleEvaluationWarning
| Property | Type |
|---|---|
| `Code` | `string` |
| `Message` | `string` |
| `ContextName` | `string?` |
| `CategoryName` | `string?` |
| `ExpressionIdentity` | `Guid?` |

#### RuleExecutionMode
| Value | Meaning |
|---|---|
| `Strict` | Fails the request if any required artifact cannot be resolved or evaluated. |
| `Lenient` | Evaluates valid expressions and records errors for invalid expressions. |
| `DiagnosticOnly` | Runs validation/evaluation for troubleshooting without treating matches as authoritative business outcomes. |

`RuleExecutionMode` controls how runtime errors are handled. It does not control whether a non-leaf category is executable; that behavior is controlled by `RuleCategoryExecutionMode`.

#### RuleCategoryExecutionMode
| Value | Meaning |
|---|---|
| `LeafOnly` | Default behavior. The requested category must be a leaf category; non-leaf categories return `RULE_CATEGORY_NOT_EXECUTABLE`. |
| `IncludeDescendantLeaves` | Allows a non-leaf category request to evaluate all descendant leaf categories in deterministic path/ordinal order. |

#### RuleEvaluationStatus
| Value | Meaning |
|---|---|
| `Completed` | Evaluation completed successfully. |
| `Failed` | Evaluation could not complete. |
| `PartiallyCompleted` | Evaluation completed for valid expressions but recorded one or more errors. |



## 3. Lifecycle, Validation, and Persistence Model

### RulesLibraryState

| State | Meaning |
|---|---|
| `Draft` | Editable working version. |
| `Published` | Immutable version available for runtime execution. |
| `Deprecated` | Still executable but discouraged for new runtime usage. |
| `Archived` | Retained for audit/replay but not selected for normal runtime execution. |

A library is mutable only while in Draft state. Publishing creates an immutable snapshot and marks that snapshot as Published.

### Category Leaf-Node Rule

Rule Categories may be deeply nested. Rule Expressions should be attached only to category leaf nodes.

A non-leaf category should be treated as an organizational node. Runtime execution against a non-leaf category should fail by default with `RULE_CATEGORY_NOT_EXECUTABLE` when `RuleCategoryExecutionMode = LeafOnly`.

If `RuleCategoryExecutionMode = IncludeDescendantLeaves`, the processor may evaluate all descendant leaf categories in deterministic path/ordinal order. This behavior must be explicit so a caller does not accidentally execute a broad subtree of rules.

### Validation Model

Validation findings should use compiler-style severities.

| Severity | Meaning |
|---|---|
| `Error` | Prevents publishing or safe execution. |
| `Warning` | Indicates a design concern but does not block execution. |
| `Information` | Provides helpful validation or tooling feedback. |

Recommended validation entities:

| Entity | Purpose |
|---|---|
| `RuleValidationResult` | Aggregate result of validating a library, context, category, or expression. |
| `RuleValidationFinding` | Individual validation message with severity, code, message, and target metadata. |
| `IRuleLibraryValidator` | Service contract for validating library structure and expressions. |

### Runtime Error Model

A false expression result is a valid mismatch, not an error.

A runtime error occurs when the processor cannot complete evaluation because of configuration, context resolution, parameter extraction, expression parsing, expression evaluation, or result-definition problems.

Recommended stable error codes:

| Code | Meaning |
|---|---|
| `RULE_CONTEXT_NOT_FOUND` | No matching context exists for the input object. |
| `RULE_CONTEXT_AMBIGUOUS` | More than one context matches the input object. |
| `RULE_CATEGORY_NOT_FOUND` | Requested category does not exist. |
| `RULE_CATEGORY_NOT_EXECUTABLE` | Requested category is not a leaf/executable category. |
| `RULE_PARAMETER_NOT_FOUND` | Expression references a missing parameter. |
| `RULE_PARAMETER_EXTRACTION_FAILED` | Parameter value could not be extracted from the input object. |
| `RULE_EXPRESSION_PARSE_FAILED` | Expression syntax could not be parsed. |
| `RULE_EXPRESSION_EVALUATION_FAILED` | Expression failed during evaluation. |
| `RULE_RESULT_DEFINITION_MISSING` | Matching expression has no result definition when one is required. |
| `RULE_LIBRARY_NOT_PUBLISHED` | Runtime attempted to execute a non-published library. |

### Library Repository Contract

The model should remain persistence-agnostic. The framework may provide JSON and file-based repository helpers, but the POCO model should not require Entity Framework, a relational database, or a specific storage provider.

Recommended repository operations:

| Operation | Purpose |
|---|---|
| `LoadLibrary(identity)` | Loads the latest available library version. |
| `LoadLibraryVersion(identity, version)` | Loads a specific version. |
| `SaveDraft(library)` | Saves an editable draft. |
| `Publish(library, changeNote)` | Creates an immutable version snapshot. |
| `ListLibraries()` | Returns available libraries. |
| `ListVersions(libraryIdentity)` | Returns version history. |
| `ArchiveVersion(snapshotIdentity)` | Marks a version as archived. |

### Deterministic Evaluation

Given the same Rules Library version, input object, category reference, category execution mode, and execution options, the Rule Processor should produce the same evaluation result.

Rule Expressions should not perform external side effects such as database calls, web service calls, file writes, queue operations, or mutable global state changes.

If registered methods are added in a future revision, production execution should allow only deterministic methods by default.

## 4. Design Decisions & Tradeoffs

1. **Library POCOs, not database entities** — The existing framework has zero persistence. Adding EF Core or database coupling would be out of scope. The library is an in-memory model that could be serialized to JSON/XML by the consumer.

2. **Library-level versioning instead of expression-level versioning** — The `RulesLibrary` is the versioned unit; individual `RuleExpression` entries are mutable leaves within it. A change to any expression, category, parameter, or result definition constitutes a new library version. **Rationale**: expression-level versioning created high coordination cost (which expression version does a category point at? how do expressions of different versions interact within a single execution?) for little practical benefit. Library snapshots are coherent, auditable, and trivially reproducible.

3. **`RulesLibrary.Identity` is stable across versions; `SnapshotIdentity` is unique per version** — This mirrors the standard pattern for versioned aggregates: logical identity vs. snapshot identity. A consumer resolving "the current version of library X" uses `Identity`; an audit or replay uses `SnapshotIdentity`.

4. **`QualifiedTypeName` on `RuleContext`** — This is the key to auto-resolution. Tooling should populate it from reflected .NET type metadata, preferably using `Type.AssemblyQualifiedName` when available. Runtime resolution may compare `inputObject.GetType().AssemblyQualifiedName` and `inputObject.GetType().FullName` against registered contexts to support loaded objects and serialized library metadata.

5. **The existing `Rules.Engine` is used internally** — `RuleProcessor` creates a `Rules.Engine`, adds parameters via `ParameterFactory`, adds rules from the library, executes, and maps results. No engine changes needed.

6. **M:N join entities are explicit classes** — Rather than hidden `List<List<>>` nesting, the joins are first-class to support ordinal/ordering and future metadata.

7. **`RuleResultDefinition` is embedded, not a separate entity** — Each expression owns its result definition. This keeps the model simple and avoids a separate result definition.

8. **Mismatch diagnostics are opt-in** — When `IncludeDiagnostics` is false, no diagnostic objects are created. When true, the processor captures the parameter values that were used and the raw expression text.

9. **Draft → Published lifecycle** — `RulesLibrary.State` is the canonical lifecycle field. A library is mutable only while `State = Draft`; `IsPublished` is a derived convenience flag. Calling `Publish()` seals the version, records a `LibraryVersion` snapshot in history, and increments the version counter when the next revision begins. Expressions, categories, and parameters should only be modified on draft libraries. Published, deprecated, and archived library versions are immutable from the consumer's perspective.

10. **Category leaf-node execution** — Categories may be deeply nested, but expressions should attach only to leaf categories. Non-leaf categories are organizational by default and should not be executable unless descendant execution is explicitly enabled.

11. **Structured errors separate from mismatches** — A rule that evaluates false is a valid mismatch. Context resolution failures, missing parameters, parse failures, and evaluation exceptions are runtime errors and should be returned separately from mismatches.


### Tradeoffs Considered

| Option | Pros | Cons | Decision |
|---|---|---|---|
| Library-level versioning (chosen) | Simple, coherent, easy reproducibility, no cross-entity version resolution | Small changes still bump the whole library | **Chosen** — complexity win dominates |
| Expression-level versioning (rejected) | Fine-grained history, minimal churn per change | High coordination cost, version-resolution ambiguity, harder audit | Rejected |
| Both (hybrid) | Theoretically most flexible | Maximum complexity, confusing for consumers, two overlapping audit trails | Rejected |
