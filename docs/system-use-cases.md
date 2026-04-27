# Rules Engine Enhancements - System Use Cases

> **Terminology and versioning rule:** This project uses **Library** as the root term for a persisted set of rules. Do not use "catalog" for product/domain naming. Versioning belongs to the **RulesLibrary** as a whole. Individual rule expressions are not versioned and do not maintain expression-level revision history. A context owns categories, categories may contain deeply nested subcategories, and rule expressions are attached at category leaf nodes.


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


## Document Purpose

This document defines the **system use cases** for the enhanced rules engine library. It is intentionally focused on **system behavior, runtime flow, entity responsibilities, and technical interactions** rather than business-facing scenarios. The goal is to provide implementation guidance for extending the existing rules framework with richer rule library entities, library-level versioning, configurable rule groupings, result definitions, optional mismatch diagnostics, and a simplified rule processor.

## Scope

The system described here extends an existing rules engine library with the following high-level capabilities:

- A rules library model centered around **Rules Library**, **Rule Context**, **Rule Category / Grouping**, **Rule Expression**, **Rule Parameter**, and **Rule Result Definition**.
- **Library-level versioning** as the primary versioning strategy. The entire Rules Library is the versioned unit; individual expressions, categories, parameters, and result definitions are mutable leaves within a library version.
- A runtime model that separates library/design-time concerns from execution/runtime concerns.
- A simplified **Rule Processor** API that accepts:
  - an input object
  - a rule grouping/category reference
  - an optional flag to include mismatch diagnostics
- Automatic **Rule Context resolution** based on the input object.
- Match result handling and optional mismatch diagnostics for debugging and explainability.

## Architectural Principles

The enhanced design should follow these principles:

1. **Context-driven evaluation**  
   Rule execution is always anchored to a resolved Rule Context.

2. **Reusable expressions**  
   Rule Expressions are reusable artifacts that may appear in multiple categories/groupings within the same context.

3. **Context-owned parameters**  
   Parameters are defined at the Rule Context level and referenced by Rule Expressions.

4. **Library-level versioning**  
   The Rules Library is the primary versioned entity. Changes to any contained expression, category, parameter, or result definition constitute a new Rules Library version. This keeps versioning coherent and reproducible without per-expression version coordination.

5. **Separation of design-time and runtime models**  
   Library/configuration entities are distinct from runtime execution entities.

6. **Simple consumer API**  
   Callers should only need to provide the input object and the grouping/category to process.

7. **Engine-agnostic result semantics**  
   The rules engine returns result payloads, but the consuming system interprets what those payloads mean.

8. **Optional diagnostics**  
   Mismatch details are optional runtime diagnostics, not primary business outcomes.


---

# 1. System Boundary

The enhanced rules engine provides the infrastructure required to define, organize, validate, version, persist, load, and evaluate deterministic rule expressions.

The system is responsible for:

- Maintaining Rules Library structure.
- Managing Rule Contexts, Rule Categories, Rule Expressions, Rule Parameters, and Rule Result Definitions.
- Supporting library-level versioning.
- Resolving runtime contexts from input objects.
- Extracting parameters from input objects.
- Evaluating expressions.
- Returning match results and optional mismatch diagnostics.
- Supporting validation and test execution from developer tooling.
- Supporting JSON-based library serialization through a storage abstraction.

The system is not responsible for:

- Interpreting the final business meaning of returned result codes.
- Making final business decisions outside the configured rule result payload.
- Performing external side effects during rule evaluation.
- Replacing application workflow orchestration.
- Managing user authentication or authorization.
- Requiring a specific database or persistence technology.

---

# 2. Rules Library Lifecycle

A Rules Library moves through controlled lifecycle states.

## 2.1 Draft

A draft library is editable. Contexts, categories, expressions, parameters, and result definitions may be added, changed, removed, or reassigned.

Draft libraries may be validated and tested, but they should not be used for production runtime evaluation unless the host application explicitly allows draft execution.

## 2.2 Published

A published library is immutable. Once published, its contents represent a fixed snapshot of the Rules Library at a specific version.

Published libraries are valid targets for runtime execution.

## 2.3 Archived

An archived library version is retained for audit, replay, or historical analysis. Archived versions are not selected by default during normal runtime execution.

## 2.4 Deprecated

A deprecated library version remains executable but should not be selected for new integrations or new runtime configuration.

---

# 3. Rule Category Tree Rules

Rule Categories form a context-scoped hierarchy.

A Rule Context may contain one or more root categories. Each category may contain child categories, allowing deeply nested rule organization.

Rule Expressions are attached only to category leaf nodes.

A leaf category is a category that does not contain child categories.

This rule prevents ambiguity between parent categories used for organization and child categories used for execution.

When the Rule Processor receives a category reference:

1. The processor resolves the target category within the resolved Rule Context.
2. If the category is a leaf category, the processor evaluates expressions directly attached to that category.
3. If the category is not a leaf category, the processor follows `RuleCategoryExecutionMode`:
   - `LeafOnly`: reject the request with `RULE_CATEGORY_NOT_EXECUTABLE`.
   - `IncludeDescendantLeaves`: evaluate all descendant leaf categories in deterministic path/ordinal order.
4. The default behavior is `LeafOnly` so broad subtree execution cannot happen accidentally.

---

# 4. Validation Severity Rules

Validation findings should be categorized by severity.

## 4.1 Error

An error prevents the library from being published or safely executed.

Examples:

- Context type cannot be resolved.
- Expression syntax is invalid.
- Referenced property path does not exist.
- Expression references an undefined parameter.
- Operator is incompatible with operand types.
- Required result definition is missing.
- Duplicate identity exists within the same library version.
- A Rule Expression is attached to a non-leaf category.

## 4.2 Warning

A warning does not prevent execution but may indicate a design issue.

Examples:

- Rule Expression is not assigned to any category.
- Rule Parameter is defined but not referenced by any expression.
- Category exists without child categories or expressions.
- Expression is inactive but still assigned to categories.
- Result severity is empty when the consuming system expects severity to be populated.
- Description is missing.

## 4.3 Information

An informational message provides helpful design-time feedback.

Examples:

- Library validated successfully.
- Context reflection completed successfully.
- Test object hydrated successfully.
- No mismatches were produced because diagnostics were disabled.

---

# 5. Deterministic Evaluation Requirement

Rule evaluation must be deterministic.

Given the same:

- Rules Library version.
- Input object.
- Category reference.
- Execution options.

The Rule Processor should produce the same evaluation result.

Rule Expressions should not perform external side effects. Expressions should not directly call databases, web services, file systems, queues, or other external dependencies.

If method support is added in a future revision, registered methods must be deterministic or explicitly marked as non-deterministic and excluded from production execution unless allowed by the host application.

---

# 6. Library Repository Contract

The rules framework should define persistence abstractions without requiring a specific database or storage technology.

The default library format should support JSON serialization.

The repository layer may be implemented using:

- Local JSON files.
- Database storage.
- Cloud object storage.
- Embedded application resources.
- Custom host application storage.

## 6.1 Recommended Repository Operations

| Operation | Purpose |
|---|---|
| `LoadLibrary(identity)` | Loads the latest available library version. |
| `LoadLibraryVersion(identity, version)` | Loads a specific version. |
| `SaveDraft(library)` | Saves an editable draft. |
| `Publish(library, changeNote)` | Creates an immutable version snapshot. |
| `ListLibraries()` | Returns available libraries. |
| `ListVersions(libraryIdentity)` | Returns version history. |
| `ArchiveVersion(snapshotIdentity)` | Marks a version as archived. |

---

# 7. System Actors / Technical Participants

## 7.1 Consuming Application
A host application or service that invokes the rules engine library.

## 7.2 Rule Processor
The main runtime service responsible for context resolution, grouping resolution, parameter extraction, expression evaluation, and result assembly.

## 7.3 Rule Context Resolver
A service or component that determines the correct Rule Context from the input object type or classifier.

## 7.4 Parameter Extraction Component
A service or component that extracts parameter values from the input object using Rule Context parameter definitions.

## 7.5 Expression Evaluator
A service or component that evaluates a Rule Expression against the extracted parameter set.

## 7.6 Library / Repository Layer
The mechanism responsible for retrieving contexts, categories/groupings, expressions, parameters, versions, and result definitions.

## 7.7 Diagnostic Generator
An optional component that constructs mismatch diagnostics when diagnostic mode is enabled.

## 7.8 Validation Service
A service or component that validates library structure, context type bindings, parameter paths, expression syntax, operator compatibility, category leaf-node rules, and missing result definitions.

## 7.9 Type Reflection / Metadata Provider
A service or component that loads selected assemblies, reflects public concrete classes, discovers property paths, and provides metadata for IntelliSense, validation, and parameter generation.

## 7.10 Library Serializer / Repository
A storage boundary that saves, loads, publishes, lists, and archives Rules Library versions without forcing a specific persistence technology.


---

# 8. Core Entity Model (High Level)

## 8.1 Design-Time / Library Entities

### Rules Library
Top-level container for rule-related definitions.

### Rule Context
Represents the domain/classifier/type against which rules are evaluated.

### Rule Category / Rule Grouping
A context-scoped grouping mechanism used to organize reusable Rule Expressions into execution sets.

### Rule Expression
A reusable evaluatable formula/condition.

### Rule Parameter
A context-owned parameter definition representing a resolvable/extractable value from the input model.

### Rule Result Definition
A configured payload returned when a Rule Expression evaluates true.

### Rules Library Version
An immutable snapshot of a Rules Library at a specific version. Library-level versioning is the primary versioning model — the entire library (contexts, categories, expressions, parameters, and result definitions) is captured as one coherent, auditable unit.

## 8.2 Runtime / Execution Entities

### Rule Evaluation Request
A runtime request describing the object to evaluate, the grouping/category to process, and runtime options such as mismatch diagnostics.

### Rule Evaluation Result
The aggregate runtime result of processing a request.

### Rule Expression Evaluation Result
The runtime outcome of evaluating a specific Rule Expression. The effective version of the expression is inherited from the Rules Library version used for evaluation.

### Rule Mismatch Diagnostic
An optional runtime diagnostic artifact describing why a Rule Expression did not match.

---

# 9. Entity Relationships (Conceptual)

## 9.1 Rules Library Relationships
- A **Rules Library** contains many **Rule Contexts**.
- A **Rules Library** has a stable logical identity and a current version number.
- A **Rules Library** preserves prior **Rules Library Versions** as immutable snapshots for audit and replay.

## 9.2 Rule Context Relationships
- A **Rule Context** belongs to one **Rules Library**.
- A **Rule Context** contains many **Rule Categories / Groupings**.
- A **Rule Context** contains many **Rule Expressions**.
- A **Rule Context** contains many **Rule Parameters**.

## 9.3 Rule Category / Grouping Relationships
- A **Rule Category / Grouping** belongs to one **Rule Context**.
- A **Rule Category / Grouping** may have a parent category.
- A **Rule Category / Grouping** may have many child categories.
- A **Rule Category / Grouping** becomes executable when it is a leaf category.
- Rule Expressions should be attached only to leaf categories.
- A leaf **Rule Category / Grouping** has a many-to-many relationship with **Rule Expressions**.


## 9.4 Rule Expression Relationships
- A **Rule Expression** belongs to one **Rule Context**.
- A **Rule Expression** may appear in multiple **Rule Categories / Groupings**.
- A **Rule Expression** references many **Rule Parameters**.
- A **Rule Expression** may have one associated **Rule Result Definition**.
- A **Rule Expression** does **not** carry its own version; its effective version is the containing **Rules Library Version**.

## 9.5 Rule Parameter Relationships
- A **Rule Parameter** belongs to one **Rule Context**.
- A **Rule Parameter** may be referenced by many **Rule Expressions**.

## 9.6 Runtime Relationships
- A **Rule Evaluation Request** produces one **Rule Evaluation Result**.
- A **Rule Evaluation Result** references the **Rules Library Version** that produced it.
- A **Rule Evaluation Result** contains many **Rule Expression Evaluation Results**.
- A **Rule Expression Evaluation Result** references one **Rule Expression** (resolved within the active library version).
- A **Rule Expression Evaluation Result** may include one **Rule Result Definition** when the rule matches.
- A **Rule Expression Evaluation Result** may include one **Rule Mismatch Diagnostic** when the rule does not match and diagnostics are enabled.

---

# 10. System Use Cases

## SUC-01: Register and Maintain Rules Library Structure

### Goal
Allow the system to define and maintain the top-level rules library structure.

### Primary Actor
System administrator, developer, or internal configuration service.

### Preconditions
- The rules engine framework is available.
- The persistence/library layer is available if the model is persisted.

### Trigger
A new rules library structure is created or an existing one is updated.

### Main Flow
1. The system creates or loads a Rules Library.
2. The system associates one or more Rule Contexts with the Rules Library.
3. The system persists or registers the Rules Library and its contexts.
4. The system makes the library available for future rule authoring and runtime evaluation.

### Postconditions
- A valid Rules Library structure exists.
- The Rules Library can be used as the root container for contexts and related artifacts.

---

## SUC-02: Define a Rule Context

### Goal
Allow the system to define a Rule Context that anchors rules to a specific domain object type or classifier.

### Primary Actor
Developer, rule configuration service, or internal administration tooling.

### Preconditions
- A Rules Library exists.

### Trigger
A new context is required for a new object/domain/classifier.

### Main Flow
1. The system creates a Rule Context.
2. The system associates the Rule Context with a Rules Library.
3. The system assigns the type/classifier mapping needed for runtime context resolution by populating `RuleContext.QualifiedTypeName`.
4. When the context is created from a reflected DLL, the system prefers the assembly-qualified CLR type name and may retain `SourceAssemblyPath` for design-time reflection, IntelliSense, and validation.
5. The system makes the context available for categories, expressions, and parameters.

### Alternate Flows
- If a matching Rule Context already exists, the system may update or extend it rather than create a duplicate.

### Postconditions
- The Rule Context exists and is associated with the Rules Library.
- The Rule Context can now own categories/groupings, expressions, and parameters.

---

## SUC-03: Define Rule Categories / Groupings Within a Context

### Goal
Allow the system to organize expressions into reusable execution groupings within a Rule Context.

### Primary Actor
Developer, rule authoring service, or configuration tooling.

### Preconditions
- A Rule Context exists.

### Trigger
A new evaluation grouping is needed.

### Main Flow
1. The system creates a Rule Category / Grouping under a Rule Context or under an existing parent category.
2. The system determines whether the category is a leaf category.
3. The system associates Rule Expressions only with leaf categories.
4. If a category gains child categories, the system prevents expressions from remaining directly attached to that now non-leaf category or records a validation error.
5. The system persists or registers the category/grouping configuration.
6. Leaf categories become available as normal runtime processing targets.

### Postconditions
- A Rule Category / Grouping exists within a Rule Context.
- Leaf groupings can be used by the Rule Processor to select expressions for execution.
- Non-leaf groupings remain organizational unless the caller explicitly uses descendant leaf execution mode.

---

## SUC-04: Define Reusable Rule Expressions

### Goal
Allow the system to create reusable Rule Expressions that can be attached to one or more categories/groupings within the same context.

### Primary Actor
Developer, rule authoring service, or configuration tooling.

### Preconditions
- A Rule Context exists.

### Trigger
A new expression is needed.

### Main Flow
1. The system creates a Rule Expression under a Rule Context.
2. The system associates the expression with one or more Rule Parameters.
3. The system attaches the expression to one or more leaf Rule Categories / Groupings.
4. The system prevents attachment to non-leaf categories or records a validation error.
5. The system persists or registers the expression.

### Postconditions
- The Rule Expression exists as a reusable, context-scoped artifact.
- The Rule Expression is available for runtime evaluation.

---

## SUC-05: Define Rule Parameters at the Context Level

### Goal
Allow the system to define extractable/resolvable parameters at the Rule Context level.

### Primary Actor
Developer, authoring tool, or configuration tooling.

### Preconditions
- A Rule Context exists.

### Trigger
A new parameter is required for rule processing.

### Main Flow
1. The system creates a Rule Parameter under a Rule Context.
2. The system associates the parameter with one or more Rule Expressions.
3. The system persists or registers the parameter definition.
4. The parameter becomes available for extraction during rule evaluation.

### Postconditions
- The Rule Context owns the parameter definition.
- Rule Expressions may reference the parameter without redefining extraction behavior independently.

---

## SUC-06: Version a Rules Library

### Goal
Allow the system to create and manage revisions of a Rules Library using library-level versioning. The entire library (contexts, categories, expressions, parameters, result definitions) is snapshotted as a single coherent unit.

### Primary Actor
Developer, authoring tool, or version management service.

### Preconditions
- A Rules Library exists.

### Trigger
One or more changes to the library's contained expressions, categories, parameters, or result definitions must be promoted as a new version.

### Main Flow
1. The system confirms the library is in a mutable (draft) state.
2. The system applies the intended changes to expressions, categories, parameters, or result definitions within the draft library.
3. The system publishes the library, which seals the current state as an immutable Rules Library Version.
4. The system records the snapshot in the library's version history, including the version number, publish timestamp, and optional change note.
5. The system links the new version to the prior version for audit traceability.
6. The system makes the new library version available for runtime evaluation.

### Alternate Flows
- If historical reproducibility is required, the system ensures runtime execution references a specific Rules Library Version rather than "latest".
- If a published library must be revised, the system creates a new draft based on the most recent published version and repeats the publish flow.

### Postconditions
- A new Rules Library Version exists.
- All prior library versions remain available as immutable snapshots for audit and replay.
- The library's contents (contexts, categories, expressions, parameters, result definitions) are versioned together as a coherent unit.

---

## SUC-07: Define Rule Result Definitions

### Goal
Allow the system to define the payload returned when a Rule Expression evaluates true.

### Primary Actor
Developer, authoring service, or configuration tooling.

### Preconditions
- A Rule Expression exists within a draft Rules Library.

### Trigger
A match result payload is needed for a rule.

### Main Flow
1. The system defines a Rule Result Definition for the expression.
2. The system associates the result definition with the corresponding expression.
3. The system persists or registers the result definition as part of the draft library's next version.

### Postconditions
- The Rule Expression has an associated Rule Result Definition.
- The rule engine may return that payload when the expression evaluates true.

### Notes
The rules engine returns the result definition, but the consuming system determines how to interpret it.

---

## SUC-08: Resolve Rule Context from an Input Object

### Goal
Allow the runtime engine to determine the correct Rule Context automatically from the object passed to the Rule Processor.

### Primary Actor
Rule Processor.

### Preconditions
- One or more Rule Context definitions exist.
- The framework contains a type/classifier mapping mechanism.

### Trigger
The Rule Processor receives an input object.

### Main Flow
1. The Rule Processor receives the input object.
2. The Rule Processor invokes the Rule Context Resolver.
3. The Rule Context Resolver determines the matching Rule Context using the object type, classifier, or registered mapping.
4. The Rule Processor proceeds using the resolved Rule Context.

### Alternate Flows
- If no matching context is found, the processor returns an error or failed evaluation response.
- If multiple contexts match ambiguously, the processor returns an error or selects according to configured precedence rules.

### Postconditions
- A Rule Context is resolved for the runtime request.

---

## SUC-09: Process an Object Against a Rule Grouping

### Goal
Allow the Rule Processor to evaluate an input object against a selected Rule Category / Grouping using the context inferred from the object.

### Primary Actor
Consuming Application.

### Preconditions
- The input object is available.
- A Rule Category / Grouping reference is provided.
- The corresponding Rule Context can be resolved from the object.

### Trigger
The consuming application invokes the Rule Processor.

### Main Flow
1. The consuming application passes an input object and a rule grouping/category reference to the Rule Processor.
2. The Rule Processor resolves the Rule Context from the input object.
3. The Rule Processor resolves the grouping/category within the Rule Context.
4. The Rule Processor applies `RuleCategoryExecutionMode`.
5. If the category is a leaf category, the Rule Processor loads Rule Expressions directly associated with that category.
6. If the category is non-leaf and category execution mode is `LeafOnly`, the processor returns `RULE_CATEGORY_NOT_EXECUTABLE`.
7. If the category is non-leaf and category execution mode is `IncludeDescendantLeaves`, the processor loads expressions from descendant leaf categories in deterministic path/ordinal order.
8. The Rule Processor invokes parameter extraction.
9. The Rule Processor evaluates each expression.
10. The Rule Processor collects matching results, valid mismatches, errors, and warnings.
11. The Rule Processor returns the aggregate Rule Evaluation Result.

### Postconditions
- The object has been processed against the selected grouping/category according to the requested category execution mode.
- All matching results have been returned.
- Valid mismatches and runtime errors remain separate in the aggregate result.

---

## SUC-10: Extract Parameter Values for Evaluation

### Goal
Allow the runtime engine to extract parameter values from the input object based on the resolved Rule Context.

### Primary Actor
Rule Processor / Parameter Extraction Component.

### Preconditions
- A Rule Context has been resolved.
- The Rule Context defines the relevant Rule Parameters.

### Trigger
The Rule Processor begins expression evaluation.

### Main Flow
1. The Rule Processor identifies the parameters needed by the expressions in scope.
2. The Parameter Extraction Component resolves those values from the input object.
3. The extracted values are assembled into a runtime parameter set / parameter bag.
4. The Rule Processor supplies the parameter set to the Expression Evaluator.

### Alternate Flows
- If a parameter cannot be extracted, the system records the failure according to the framework’s validation/error behavior.

### Postconditions
- A normalized runtime parameter set exists for expression evaluation.

---

## SUC-11: Evaluate a Rule Expression

### Goal
Allow the runtime engine to evaluate a single Rule Expression (resolved within the active Rules Library Version) against the extracted parameter set.

### Primary Actor
Expression Evaluator.

### Preconditions
- The active Rules Library Version has been resolved.
- The Rule Expression is available within that library version.
- Required parameters have been extracted.

### Trigger
The Rule Processor evaluates expressions within the selected grouping/category.

### Main Flow
1. The Expression Evaluator receives the Rule Expression and parameter set.
2. The Expression Evaluator executes the expression logic.
3. The Expression Evaluator determines whether the expression matched or did not match.
4. The Expression Evaluator returns a Rule Expression Evaluation Result.

### Postconditions
- The system has a runtime result for that expression.

---

## SUC-12: Return Match Results for Successful Evaluations

### Goal
Allow the runtime engine to return the configured Rule Result Definition when an expression evaluates true.

### Primary Actor
Rule Processor / Expression Evaluator.

### Preconditions
- The expression evaluates true.
- A Rule Result Definition is associated with the expression.

### Trigger
A Rule Expression evaluates true.

### Main Flow
1. The Expression Evaluator marks the expression as matched.
2. The Rule Processor retrieves the configured Rule Result Definition.
3. The Rule Processor includes that result in the Rule Expression Evaluation Result.
4. The result is included in the aggregate Rule Evaluation Result returned to the caller.

### Postconditions
- The caller receives the configured result payload for the matched expression.

### Notes
The engine does not interpret whether the result code/message means informational, warning, critical, or something else.

---

## SUC-13: Optionally Produce Mismatch Diagnostics

### Goal
Allow the runtime engine to produce diagnostics explaining why a Rule Expression did not match when diagnostics are enabled.

### Primary Actor
Rule Processor / Diagnostic Generator.

### Preconditions
- A Rule Expression evaluates false.
- Mismatch diagnostics are enabled for the request.

### Trigger
The Rule Processor evaluates an expression that does not match while diagnostics mode is active.

### Main Flow
1. The Expression Evaluator determines that the Rule Expression evaluated false.
2. The Rule Processor confirms that mismatch diagnostics are enabled.
3. The Diagnostic Generator constructs a Rule Mismatch Diagnostic.
4. The diagnostic is attached to the Rule Expression Evaluation Result.
5. The diagnostic is included in the aggregate Rule Evaluation Result.

### Postconditions
- The caller receives an optional mismatch explanation for the failed rule.

### Notes
Mismatch diagnostics are runtime diagnostics only and are not primary business results.

---

## SUC-14: Return a Combined Evaluation Result

### Goal
Allow the Rule Processor to return a complete aggregate result for the processing request.

### Primary Actor
Rule Processor.

### Preconditions
- The requested expressions have been evaluated.

### Trigger
All expressions in scope have completed evaluation.

### Main Flow
1. The Rule Processor aggregates all Rule Expression Evaluation Results.
2. The Rule Processor includes all matching Rule Results.
3. The Rule Processor includes valid mismatches and mismatch diagnostics when diagnostics are enabled.
4. The Rule Processor includes structured errors and warnings when evaluation cannot complete cleanly.
5. The Rule Processor records the library name, library version, snapshot identity, execution mode, category execution mode, context name, and category name.
6. The Rule Processor assembles the final Rule Evaluation Result.
7. The Rule Processor returns the result to the consuming application.

### Postconditions
- The consuming application receives a complete aggregate evaluation result.

---

## SUC-15: Preserve Historical Library Versions for Traceability

### Goal
Allow the framework to support historical traceability of Rules Library changes over time through library-level snapshots.

### Primary Actor
Version management component, repository layer, or rule authoring service.

### Preconditions
- Library-level versioning is enabled.

### Trigger
A new library revision is published or a historical evaluation must be reconstructed.

### Main Flow
1. The framework preserves prior Rules Library Versions as immutable snapshots.
2. The framework tracks the active (current) library version used for runtime evaluation.
3. The framework allows runtime execution to target a specific historical library version when reproducibility is required.
4. Runtime evaluation results record the library version and snapshot identity they were produced under, enabling later replay or audit.

### Postconditions
- Rules Library history is preserved as a coherent series of immutable snapshots.
- Specific library versions can be identified and replayed for audit, compliance, or regression analysis.
- Because the entire library is versioned as a unit, historical reproducibility does not require cross-entity version resolution.

---


---

## SUC-16: Validate a Rules Library

### Goal
Allow the system to validate library structure, reflected type metadata, parameter definitions, expression syntax, category assignments, and result definitions before publishing or testing.

### Primary Actor
Developer, rule authoring tool, or validation service.

### Preconditions
- A draft Rules Library exists.

### Trigger
The user or system requests validation.

### Main Flow
1. The system validates library metadata and uniqueness rules.
2. The system validates Rule Context type bindings.
3. The system validates category tree structure and confirms expressions are attached only to leaf categories.
4. The system validates parameter definitions and property paths.
5. The system validates expression syntax and referenced parameters.
6. The system validates type compatibility and operator usage.
7. The system validates required result definitions.
8. The system returns a validation result containing errors, warnings, and information messages.

### Postconditions
- The library has a complete validation result.
- The library can be blocked from publishing if validation errors exist.

---

## SUC-17: Persist and Load a Rules Library

### Goal
Allow the system to save, load, publish, and retrieve Rules Library versions through a storage abstraction.

### Primary Actor
Authoring tool, repository layer, or consuming application.

### Preconditions
- A Rules Library exists or a persisted library is available.

### Trigger
The user or host application saves, opens, publishes, or loads a library.

### Main Flow
1. The system serializes or deserializes the Rules Library using the configured repository implementation.
2. The system preserves logical identity, snapshot identity, version number, lifecycle state, contexts, categories, expressions, parameters, and result definitions.
3. The system loads either the latest published version or a requested historical version.
4. The system returns the loaded library to the caller.

### Postconditions
- A Rules Library is persisted or restored without requiring a specific database technology.

---

## SUC-18: Test Rules Against a Hydrated Object

### Goal
Allow developer tooling to hydrate a real object from a file and execute rules against it for validation and troubleshooting.

### Primary Actor
Developer or Windows Rule Editor.

### Preconditions
- A Rule Context exists.
- A test object file is available.
- The selected context type can be resolved.

### Trigger
The user selects a test object file and chooses to test rules.

### Main Flow
1. The tool selects the Rule Context or Rule Expression to test.
2. The tool hydrates a JSON, XML, or custom-serialized file into the selected context type.
3. The tool invokes the Rule Processor with the hydrated object, selected category, `IncludeDiagnostics = true`, and the selected `RuleCategoryExecutionMode`.
4. The Rule Processor evaluates the request.
5. The tool displays matches, valid mismatches, diagnostics, errors, warnings, library version, and snapshot identity.

### Postconditions
- The developer can confirm whether the selected rules behave correctly against real serialized data.

---

## SUC-19: Execute a Specific Rules Library Version

### Goal
Allow runtime evaluation to target either the latest published library or a specific historical library version.

### Primary Actor
Consuming application, audit service, or regression test harness.

### Preconditions
- One or more published Rules Library versions exist.

### Trigger
A runtime request specifies a library version, or the system must replay historical behavior.

### Main Flow
1. The caller provides a library identity and optional version.
2. The repository resolves the requested version or latest published version.
3. The Rule Processor evaluates the request using that exact library snapshot.
4. The Rule Evaluation Result records the library name, version, and snapshot identity used for execution.

### Postconditions
- The result can be traced back to the exact Rules Library snapshot that produced it.

---

## SUC-20: Handle Runtime Evaluation Errors

### Goal
Allow the Rule Processor to return structured runtime errors without confusing valid mismatches with execution failures.

### Primary Actor
Rule Processor.

### Preconditions
- A Rule Evaluation Request has been received.

### Trigger
The processor encounters a context, category, parameter, expression, result definition, or evaluation issue.

### Main Flow
1. The processor identifies the failure condition.
2. The processor creates a `RuleEvaluationError` with a stable machine-readable code.
3. The processor records the related context, category, or expression when available.
4. The processor follows the configured execution mode:
   - Strict mode fails the request.
   - Lenient mode continues evaluating valid expressions when possible.
   - DiagnosticOnly mode records findings for troubleshooting.
5. The processor returns errors in the aggregate Rule Evaluation Result.

### Postconditions
- Runtime failures are visible to the caller.
- Valid no-match outcomes remain separate from error conditions.

### Recommended Error Codes

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

# 11. Rule Processor System Contract

## Goal
Provide a minimal, consumer-friendly execution API.

The Rule Processor is the primary runtime entry point. The consuming application should not need to manually construct engine groups, parameter bags, or expression trees.

## Required Inputs
- Input object
- Rule category/grouping reference

## Optional Inputs
- Library identity
- Library version
- Include mismatch diagnostics flag
- Include inactive rules flag, default false
- Execution mode
- Category execution mode

## Recommended API Shape

```csharp
RuleEvaluationResult Process(
    object inputObject,
    string categoryName,
    bool includeDiagnostics = false);

RuleEvaluationResult Process(
    RuleEvaluationRequest request);
```

## RuleEvaluationRequest

| Property | Type | Required | Purpose |
|---|---|---:|---|
| `InputObject` | `object` | Yes | Object being evaluated. |
| `CategoryName` | `string` | Yes | Category/grouping to evaluate. |
| `LibraryIdentity` | `Guid?` | No | Specific library to use. |
| `LibraryVersion` | `int?` | No | Specific version to use. |
| `IncludeDiagnostics` | `bool` | No | Whether to include mismatch diagnostics. |
| `IncludeInactiveRules` | `bool` | No | Whether inactive expressions are evaluated. |
| `ExecutionMode` | `RuleExecutionMode` | No | Strict, Lenient, or DiagnosticOnly. |
| `CategoryExecutionMode` | `RuleCategoryExecutionMode` | No | LeafOnly by default; IncludeDescendantLeaves when broad subtree testing/execution is intentional. |

## Expected Behavior
1. Infer the Rule Context from the input object.
2. Resolve the category/grouping inside that context.
3. Apply category tree rules using `RuleCategoryExecutionMode`.
4. Confirm the requested category is executable or intentionally expand to descendant leaf categories.
5. Load the relevant expressions.
6. Extract needed parameters.
7. Evaluate expressions.
8. Return matching results.
9. Optionally return mismatch diagnostics.
10. Return structured errors and warnings when evaluation cannot complete cleanly.

## Execution Modes

### Strict

Strict mode fails the evaluation if any required context, category, parameter, expression, or result definition cannot be resolved.

### Lenient

Lenient mode evaluates all valid expressions and records errors for invalid expressions without failing the entire request.

### DiagnosticOnly

Diagnostic-only mode validates and evaluates expressions for troubleshooting purposes but does not return business match results as authoritative outcomes.

### Default

The default mode should be Strict for production runtime execution.

## Contract Notes
- The caller should not need to provide the Rule Context explicitly.
- The processor should remain simple and focused on orchestration.
- Specialized responsibilities such as context resolution, parameter extraction, expression evaluation, validation, and mismatch generation may be delegated to internal components/services.
- Runtime results should include the Rules Library version and snapshot identity used for evaluation.
- Runtime failures should be represented as structured result errors rather than unhandled exceptions whenever practical.

---

# 12. Non-Functional Considerations

## 12.1 Extensibility
The model should support future additions such as:
- richer result payloads
- additional library lifecycle states such as draft, published, archived
- additional runtime tracing
- alternate context resolution strategies

## 12.2 Maintainability
The implementation should clearly separate:
- library/configuration entities
- runtime/execution entities
- orchestration services
- evaluation services

## 12.3 Backward Compatibility
The framework should evolve the current rules engine naturally instead of forcing an unnecessary rewrite.

## 12.4 Diagnostics and Explainability
Mismatch diagnostics should be optional to avoid overhead in normal execution paths.

## 12.5 Reproducibility
Library-level versioning should support historical traceability and future reproducibility needs. Runtime evaluation results should record the library version and snapshot identity they were produced under so a historical request can be replayed against the exact same library snapshot.

## 12.6 Determinism
Given the same library version, input object, category reference, category execution mode, and execution options, the Rule Processor should produce the same result.

## 12.7 Storage Independence
Persistence should remain abstracted behind repository and serialization contracts. The model should not require Entity Framework, a relational database, or a specific file storage provider.

## 12.8 Developer Experience
Developer tooling should support reflection, IntelliSense, compiler-style validation, file-based test hydration, and quick navigation from validation findings to the affected context, category, or expression.


---

# 13. Summary

The enhanced rules engine should support a library-driven model in which:

- a **Rules Library** contains one or more **Rule Contexts**
- each **Rule Context** owns its **Rule Categories / Groupings**, **Rule Expressions**, and **Rule Parameters**
- **Rule Expressions** are reusable across multiple groupings within the same context
- the **Rules Library** is the primary versioned artifact — the entire library is snapshotted as a coherent unit
- successful matches return configured **Rule Result Definitions**
- mismatches may optionally return **diagnostic explanations**
- a simplified **Rule Processor** accepts an object and a grouping/category, infers context automatically, applies category execution mode, and returns aggregate evaluation results stamped with the library version and snapshot identity that produced them
- validation behaves like a compiler build and separates errors, warnings, and informational findings
- persistence is abstracted behind repository and serialization contracts rather than tied to a database implementation
- runtime failures are returned as structured errors and are kept separate from valid mismatches


This structure keeps the engine simple for consumers while giving the library a strong, extensible internal architecture for future growth. Library-level versioning provides coherent, auditable, and trivially reproducible snapshots without the coordination overhead of per-expression version resolution.
