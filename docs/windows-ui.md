# NAIware Rule Editor — Windows UI Specification

> **Terminology and versioning rule:** This project uses **Library** as the root term for a persisted set of rules. Do not use "catalog" for product/domain naming. Versioning belongs to the **RulesLibrary** as a whole. Individual rule expressions are not versioned and do not maintain expression-level revision history. A context owns categories, categories may contain deeply nested subcategories, and rule expressions are attached at category leaf nodes.

![NAIware Rule Editor Mockup](images/naiware-rule-editor-mockup.png)

## Overview

The NAIware Rule Editor is a Windows Forms application used to create, edit, validate, save, load, and test rule libraries for the NAIware deterministic rules engine.

The UI is developer-focused. It supports raw rule expressions, DLL-based context type selection, compiler-style validation, and file-based test object hydration.

## Terminology

| Term | Meaning |
|---|---|
| Library | Top-level rule library container |
| Context | A .NET type that rules are evaluated against, such as `Mortgage.Models.LoanApplication` |
| Category | Logical grouping of rule expressions |
| Rule Expression | A raw rule expression plus metadata and result definition |
| Result Definition | Code, message, severity, and optional value returned when a rule matches |

## Main Layout

The application uses a split-pane Windows Forms layout.

- Left panel: Library tree view
- Right panel: selected item editor
- Bottom panel: validation errors, warnings, and information
- Toolbar: file, context, validation, and testing commands

## Library Use Cases

### Create New Library

Creates a new empty library.

### Open Library

Loads a saved library from a JSON file.

### Save Library

Serializes the full library to JSON.

### Save Library As

Saves the current library to a selected JSON file path.

## Context Use Cases

### Add Context From DLL

The user selects a DLL. The application reflects over the DLL and displays available public concrete classes. The selected type becomes the context.

Example:

```text
Mortgage.Models.LoanApplication
```

The selected type is stored as the context qualified type name.

### Edit Context

The user can view or edit:

- Context name
- Qualified type name
- Description
- Source assembly path

## Category Use Cases

### Add Category

Creates a new category under the selected context.

### Add Subcategory

Creates a nested category under the selected category.

### Assign Rule To Category

Rules can be assigned to categories and displayed under the category tree.

## Rule Authoring

Rules are authored as raw expressions.

Examples:

```text
LoanApplication.Amount > 1000
LoanApplication.Amount > 1000 and LoanApplication.Property.State = "CA"
(LoanApplication.Amount > 1000 and LoanApplication.BorrowerCount > 0) or LoanApplication.Channel = "Retail"
```

The UI does not support translating the rule into plain English. It also does not include a visual rule builder.

## IntelliSense

The editor should support simplified IntelliSense using the selected context type.

Suggested IntelliSense sources:

- Reflected property paths
- Nested properties
- Operators
- Keywords such as `and`, `or`, `true`, `false`
- Registered method names in a future revision

The same reflection metadata should be reused by validation.

## Validation

Validation behaves like a compiler build.

The user clicks **Validate Library**, and the application checks all contexts and expressions.

Validation should detect:

- Context type not found
- Property path not found
- Invalid expression syntax
- Parentheses mismatch
- Type mismatch
- Invalid operator usage
- Missing result definitions

### Type Compatibility Examples

Invalid:

```text
LoanApplication.Amount > "ABC"
```

Reason:

```text
Cannot compare decimal to string.
```

Valid:

```text
LoanApplication.Amount > 1000
```

### Error Panel

The bottom panel should behave like a Visual Studio error list.

Recommended columns:

- Severity
- Message
- Context
- Category
- Rule
- Expression Id

Double-clicking an error should navigate to the rule.

## Test Module

The test module loads real serialized objects from file.

Supported initial formats:

- JSON
- XML

The Context view can also select a custom serialization assembly and serializer class. The selected class must expose a public `Deserialize(string filePath)` method, either static or instance-based. When configured, the editor uses that method to hydrate the selected serialized file instead of the built-in JSON/XML loader.

The user does not manually enter property values in the initial version.

Workflow:

1. User selects a context or rule.
2. User clicks **Test Rules**.
3. Application asks for a JSON or XML file.
4. Application hydrates the file into the selected context type.
5. Application runs the rules against the hydrated object.
6. Application displays matches, mismatches, and diagnostics.

## Future Enhancements

- Syntax highlighting
- Inline error underlines
- More advanced IntelliSense
- Monaco editor hosted through WebView2
- Saved test cases
- Batch test runs
- Performance metrics
