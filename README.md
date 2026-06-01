# NAIware Rules

> **Terminology and versioning rule:** This project uses **Library** as the root term for a persisted set of rules. Do not use "catalog" for the product/domain naming. Versioning belongs to the **RulesLibrary** as a whole. Individual rule expressions are not versioned and do not maintain expression-level revision history. A context owns categories, categories may contain deeply nested subcategories, and rule expressions are attached at category leaf nodes.

**NAIware Rules** (Not-AI Ware Rules) is a deterministic rules engine and decisioning library for .NET. It provides a lightweight, expression-based framework for evaluating business rules and mathematical formulae at runtime without relying on AI or machine-learning techniques.

## High-Level Architecture

The solution is organized into two libraries and their corresponding test projects:

```
NAIware-Rules/
├── src/
│   ├── NAIware.Core/          # Foundational utilities and shared components
│   └── NAIware.Rules/         # Rules engine, formula engine, logic processor, library, and rule processor
│       ├── Library/           # Design-time domain model (RulesLibrary, RuleContext, RuleExpression, etc.)
│       ├── Processing/        # High-level rule processor with context resolution
│       ├── Runtime/           # Evaluation request/result models and diagnostics
│       ├── Rules/             # Rules engine and rule trees
│       └── Formulae/          # Formulae engine and formula trees
│   └── NAIware.RuleService/   # ASP.NET Core Web API for evaluating serialized models against a library
├── tests/
│   ├── NAIware.Core.Tests/    # BDD tests for core utilities
│   └── NAIware.Rules.Tests/   # BDD tests for rules, formulae, mortgage processing, and rule processor
├── docs/
│   └── rule-model-design.md      # Library & runtime extension design document
├── NAIware-Rules.slnx         # Solution file (SDK-style XML)
└── README.md
```

### NAIware.Core

A shared library of foundational utilities used throughout the solution:

| Namespace | Purpose |
|---|---|
| `Collections` | Tree structures (`TreeNode`, `BinaryTreeNode`, `TreeCollection`), hashed collections, sorted collections, hierarchies |
| `Math` | `Fraction` class with arithmetic, `MathHelper` (GCF, LCM, rounding) |
| `Reflection` | Reflection helpers, property/field/member tables, assembly information |
| `Text` | `StringHelper` (formatting, validation, character analysis), `CharacterClass` enum |
| `Security.Cryptography` | Symmetric encryption services |
| `IO` | I/O helpers |
| Root | `TypeHelper`, `ByteHelper`, `BinaryLargeNumber` (obsolete stub) |

### NAIware.Rules

The core decisioning library providing three processing engines and a library-driven rule processor:

| Component | Purpose |
|---|---|
| **Rules Engine** (`Rules.Engine`) | Parses and evaluates boolean rule expressions (e.g., `Age > 18 and Status = "Active"`) |
| **Formulae Engine** (`Formulae.Engine`) | Parses and evaluates mathematical formula expressions (e.g., `Rate * Amount + Fee`) |
| **Logic Processor Engine** (`LogicProcessorEngine`) | Evaluates complex expressions combining rules, formulae, and method calls |
| **Rule Processor** (`Processing.RuleProcessor`) | High-level library-driven processor with automatic context resolution, structured results, versioning, and optional mismatch diagnostics |

#### Key Concepts

- **Parameters** — Named, typed values injected into expressions at runtime. Created from POCOs via `ParameterFactory` using reflection. The factory automatically handles simple-type properties, nested complex objects (dot-notation), and collections (indexed dot-notation).
- **Expressions** — Parsed token trees that evaluate to a result. `SimpleExpression<V,OP,R>` handles atomic comparisons/math; `ComplexExpression<OP,R>` composes them.
- **Operators** — `ComparisonOperator` (`=`, `!=`, `<>`, `>`, `<`, `>=`, `<=`), `LogicalOperator` (`and`, `or`), `MathOperator` (`+`, `-`, `*`, `/`).
- **Method Map** — Allows registration of custom method wrappers (`IMethodWrapper`) that can be invoked within logic processor expressions.
- **Expression Groups** — Logical groupings of rules or formulae with parent-child inheritance.
- **Identification** — Each rule/formula tree carries a `Guid` and `Name` for tracking which rules fire.

#### Library & Processing Model

The models layer (`NAIware.Rules.Models`) provides a design-time domain model for defining rules declaratively, while the processing layer (`NAIware.Rules.Processing`) evaluates them at runtime:

- **Rules Library** — Top-level library container holding rule contexts.
- **Rule Context** — Domain classifier (e.g., `LoanApplication`) with a `QualifiedTypeName` that enables automatic resolution from the input object's type.
- **Rule Category** — Named grouping of expressions within a context (many-to-many with expressions).
- **Rule Expression** — A versioned, reusable rule definition with an optional `RuleResultDefinition` (code + message + optional severity).
- **library Version** — Immutable snapshot of an expression at a given version, providing audit history.
- **Rule Parameter Definition** — Declares a parameter the context expects, with optional property path for extraction.
- **Rule Processor** — Takes an input object and optional category, auto-resolves the context, extracts parameters via `ParameterFactory`, evaluates expressions using the existing `Rules.Engine`, and returns structured `RuleEvaluationResult` with matches, mismatches, and optional diagnostics.

## Key Technologies

| Technology | Version |
|---|---|
| .NET | 10.0 |
| C# Language | 12 |
| xUnit | 2.9.3 |
| Reqnroll (BDD) | 3.0.0 |
| FluentAssertions | 8.3.0 |
| System.Runtime.Caching | 9.0.4 |
| System.Configuration.ConfigurationManager | 9.0.4 |

## Setup & Local Development

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Visual Studio 2026+ or any IDE with .NET 10 support

### Clone & Build

```bash
git clone https://github.com/MarlonRabara/NAIware-Rules.git
cd NAIware-Rules
dotnet restore
dotnet build
```

### Configuration

The `ParameterFactory` supports optional caching via the `NAIware.Rules.ParameterCachingEnabled` app setting:

```xml
<appSettings>
  <add key="NAIware.Rules.ParameterCachingEnabled" value="true" />
</appSettings>
```

When enabled, reflected parameter sets are cached in `MemoryCache` for 20 ms to optimize repeated evaluations against the same object.

## Build & Run

```bash
# Build the solution
dotnet build

# Build with no incremental (clean build)
dotnet build --no-incremental
```

The solution targets `net10.0` with nullable reference types enabled and XML documentation generation turned on. The build should produce **zero warnings**.

## Test Instructions

Tests are written using **Reqnroll** (BDD/Gherkin) with **xUnit** and **FluentAssertions**.

```bash
# Run all tests
dotnet test

# Run only Core tests
dotnet test tests/NAIware.Core.Tests

# Run only Rules tests
dotnet test tests/NAIware.Rules.Tests
```

### Test Coverage

| Project | Scenarios |
|---|---|
| NAIware.Core.Tests | Fraction arithmetic, MathHelper (GCF/LCM/RoundUp), StringHelper, TypeHelper, BinaryTreeNode, Comparer |
| NAIware.Rules.Tests | Rule parsing/evaluation, formula parsing/evaluation, complex expressions, parameter injection, mortgage processing (complex-type extraction) |

## Usage Examples

### Rules Engine

```csharp
var engine = new NAIware.Rules.Rules.Engine();
engine.AddParameter(new GenericParameter<int>("Age", "Customer age", 25));
engine.AddParameter(new GenericParameter<string>("Status", "Account status", "Active"));

engine.AddRule("Age > 18 and Status = \"Active\"", "EligibilityRule");

List<Identification> results = engine.Execute();
// results contains the Identification of rules that evaluated to true
```

### Formulae Engine

```csharp
var engine = new NAIware.Rules.Formulae.Engine();
engine.AddParameter(new GenericParameter<decimal>("Rate", "Interest rate", 0.05m));
engine.AddParameter(new GenericParameter<decimal>("Amount", "Loan amount", 10000m));

engine.AddFormula("Rate * Amount", "InterestCalc");

FormulaTree? formula = engine.GetFormulaByName("InterestCalc");
decimal? result = formula?.Evaluate(); // 500.00
```

### Logic Processor (Rules + Formulae + Methods)

```csharp
var methodMap = new MethodMap();
methodMap.Add("MyMethod", myMethodWrapper);

var parameters = new Parameters();
parameters.Add("X", new GenericParameter<decimal>("X", "Input", 10m));

var processor = new LogicProcessorEngine("MyMethod(X * 2)", methodMap, parameters);
decimal result = processor.Evaluate<decimal>();
```

### Default Formula Methods

`NAIware.Rules.MethodWrappers` ships a standard library of formula functions that can be registered with the logic processor. Use `DefaultMethodWrapperRegistration` to obtain a pre-populated `MethodMap`:

```csharp
using NAIware.Rules.MethodWrappers;

MethodMap methodMap = DefaultMethodWrapperRegistration.CreateDefaultMethodMap();

var processor = new LogicProcessorEngine("MAX(ABS(-10), 5)", methodMap, new Parameters());
decimal result = processor.Evaluate<decimal>(); // 10
```

`RegisterDefaults(existingMap)` adds the same functions to a map you already own.

**Logical**

| Function | Arguments | Behavior | Example | Result |
| -------- | --------- | -------- | ------- | ------ |
| `IF` | `condition, whenTrue, whenFalse` | Returns `whenTrue` when the boolean condition is true, otherwise `whenFalse`. | `IF(1 = 1, 100, 200)` | `100` |

**Numeric**

| Function | Arguments | Behavior | Example | Result |
| -------- | --------- | -------- | ------- | ------ |
| `INT` | `value` | Converts a numeric value to a 32-bit integer (`Convert.ToInt32`). | `INT(10.7)` | `11` |
| `ROUND` | `value, decimals` | Rounds to the given decimals using banker's rounding (`Math.Round`). | `ROUND(10.567, 2)` | `10.57` |
| `ROUNDUP` | `value, decimals` | Rounds up (away from zero) to the given decimals (`MathHelper.RoundUp`). | `ROUNDUP(10.561, 2)` | `10.57` |
| `MIN` | `left, right` | Returns the smaller of two values (`Math.Min`). | `MIN(5, 10)` | `5` |
| `MAX` | `left, right` | Returns the larger of two values (`Math.Max`). | `MAX(5, 10)` | `10` |
| `ABS` | `value` | Returns the absolute value (`Math.Abs`). | `ABS(-15)` | `15` |
| `SUM` | `a, b, ...` | Returns the sum of all numeric arguments. | `SUM(1, 2, 3)` | `6` |
| `AVERAGE` | `a, b, ...` | Returns the arithmetic mean of all numeric arguments. | `AVERAGE(2, 4, 6)` | `4` |
| `POWER` | `base, exponent` | Raises `base` to `exponent` (`Math.Pow`). | `POWER(2, 3)` | `8` |
| `CEILING` | `value` | Smallest integer ≥ `value` (`Math.Ceiling`). | `CEILING(4.1)` | `5` |
| `FLOOR` | `value` | Largest integer ≤ `value` (`Math.Floor`). | `FLOOR(4.9)` | `4` |

**Text**

| Function | Arguments | Behavior | Example | Result |
| -------- | --------- | -------- | ------- | ------ |
| `CONCAT` | `a, b, ...` | Joins the string form of all arguments. | `CONCAT("Hello", " ", "World")` | `Hello World` |
| `TRIM` | `text` | Removes leading and trailing whitespace. | `TRIM("  x  ")` | `x` |
| `REPLACE` | `text, start, length, replacement` | Replaces `length` chars from 1-based `start` with `replacement`. | `REPLACE("abcdef", 2, 3, "XY")` | `aXYef` |
| `SUBSTITUTE` | `text, oldText, newText` | Replaces every occurrence of `oldText` (ordinal, case-sensitive). | `SUBSTITUTE("a-b-c", "-", "+")` | `a+b+c` |
| `LEFT` | `text, count` | Returns the leftmost `count` characters. | `LEFT("abcdef", 3)` | `abc` |
| `RIGHT` | `text, count` | Returns the rightmost `count` characters. | `RIGHT("abcdef", 3)` | `def` |
| `MID` | `text, start, length` | Returns `length` chars from 1-based `start`. | `MID("abcdef", 2, 3)` | `bcd` |
| `UPPER` | `text` | Converts to upper case. | `UPPER("abc")` | `ABC` |
| `LOWER` | `text` | Converts to lower case. | `LOWER("ABC")` | `abc` |
| `PROPER` | `text` | Converts to title case. | `PROPER("hello WORLD")` | `Hello World` |

**Date / time**

| Function | Arguments | Behavior | Example | Result |
| -------- | --------- | -------- | ------- | ------ |
| `NOW` | _(none)_ | Returns the current local date and time. | `NOW()` | _current `DateTime`_ |
| `TODAY` | _(none)_ | Returns the current local date at midnight. | `TODAY()` | _current date_ |
| `DATEDIFF` | `unit, startDate, endDate` | Whole-unit difference `endDate - startDate`. Units: `year`, `month`, `day`, `hour`, `minute`, `second` (with common aliases). | `DATEDIFF("day", "2024-01-01", "2024-01-31")` | `30` |

> `NOW` and `TODAY` are non-deterministic. For deterministic rule evaluation, prefer injecting the current date/time as a parameter.

Function names are case-insensitive and can be nested, e.g. `MIN(ROUND(10.567, 2), 20)`.

### Mortgage Processing (Complex-Type Extraction)

This example demonstrates how `ParameterFactory` automatically extracts parameters from nested domain objects — including complex child objects and collections — so that rule expressions can reference them with dot-notation.

#### Domain Model

```csharp
public class LoanApplication
{
    public List<Borrower> Borrowers { get; set; } = [];
    public Property Property { get; set; } = new();
    public int BorrowerCount => Borrowers.Count;
}

public class Borrower
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; }
    public int Age  // Computed property
    {
        get
        {
            DateTime today = DateTime.Today;
            int age = today.Year - BirthDate.Year;
            if (BirthDate.Date > today.AddYears(-age)) age--;
            return age;
        }
    }
}

public class Property
{
    public string StreetAddress { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Zip { get; set; } = string.Empty;
}
```

#### Parameter Extraction

`ParameterFactory.CreateParameters` reflects the entire object graph into flat, dot-notated parameters:

```csharp
var app = new LoanApplication
{
    Property = new Property
    {
        StreetAddress = "456 Oak Ave", City = "Miami", State = "FL", Zip = "33101"
    },
    Borrowers =
    [
        new Borrower { FirstName = "John", LastName = "Smith", BirthDate = new DateTime(1955, 3, 15) },
        new Borrower { FirstName = "Jane", LastName = "Smith", BirthDate = new DateTime(1958, 7, 22) }
    ]
};

var factory = new ParameterFactory();
Parameters? prms = factory.CreateParameters(app);
```

This produces the following parameters automatically:

| Parameter Key | Value |
|---|---|
| `BorrowerCount` | `2` |
| `Property.StreetAddress` | `"456 Oak Ave"` |
| `Property.City` | `"Miami"` |
| `Property.State` | `"FL"` |
| `Property.Zip` | `"33101"` |
| `Borrowers.Count` | `2` |
| `Borrowers.0.FirstName` | `"John"` |
| `Borrowers.0.LastName` | `"Smith"` |
| `Borrowers.0.Age` | `70` |
| `Borrowers.1.FirstName` | `"Jane"` |
| `Borrowers.1.LastName` | `"Smith"` |
| `Borrowers.1.Age` | `66` |

#### Use Case 1: No Borrowers Validation

```csharp
var engine = new Engine();
engine.Parameters.Add(prms);

engine.AddRule("BorrowerCount = 0", "NoBorrowers");
List<Identification> results = engine.Execute();

if (results.Exists(r => r.Name == "NoBorrowers"))
    Console.WriteLine("Must have at least one borrower");
```

#### Use Case 2: Reverse Mortgage Eligibility

Using indexed dot-notation, check every borrower's age against the rule:

```csharp
var factory = new ParameterFactory();
Parameters? prms = factory.CreateParameters(loanApplication);

bool allPass = true;
for (int i = 0; i < loanApplication.Borrowers.Count; i++)
{
    var engine = new Engine();
    engine.Parameters.Add(prms);
    engine.AddRule($"Borrowers.{i}.Age >= 62", "ReverseMortgageEligible");

    List<Identification> results = engine.Execute();
    if (!results.Exists(r => r.Name == "ReverseMortgageEligible"))
    {
        allPass = false;
        break;
    }
}

if (loanApplication.Borrowers.Count > 0 && allPass)
    Console.WriteLine("You are eligible for a Reverse Mortgage!");
```

#### How Complex-Type Extraction Works

`ParameterFactory.AppendParameters` classifies each property and handles it accordingly:

| Property Kind | Behavior | Naming Convention |
|---|---|---|
| **Simple type** (int, string, DateTime, etc.) | Extracted directly as a `GenericParameter<T>` | `PropertyName` |
| **Complex object** (non-collection reference type) | Recursed into with dot-prefix | `Parent.PropertyName` |
| **Collection** (`IList`) | Count added + each element recursed with indexed dot-prefix | `Collection.Count`, `Collection.0.Property` |

Circular references are guarded with a `HashSet<object>` using `ReferenceEqualityComparer`.

### Rule Processor (Library-Driven Evaluation)

The `RuleProcessor` provides a high-level API that eliminates manual engine setup. Define rules in a library, pass an input object, and get structured results:

#### 1. Define the Library

```csharp
using NAIware.Rules.Models;

var library = new RulesLibrary("MortgageRules", "Mortgage processing rules");

// Register a context — QualifiedTypeName enables auto-resolution
var context = library.AddContext(
    "LoanApplication",
    typeof(LoanApplication).FullName!,
    "Rules for loan applications");

// Add expressions with result definitions
context.AddExpression("NoBorrowers", "BorrowerCount = 0", "Validates borrower presence")
    .WithResult("BORR-001", "Must have at least one borrower", severity: "Error");

context.AddExpression("ReverseMortgageAge", "Borrowers.0.Age >= 62", "Check reverse mortgage eligibility")
    .WithResult("ELIG-001", "Primary borrower qualifies for reverse mortgage");

// Group expressions into categories
var validation = context.AddCategory("Validation");
validation.AddExpression(context.Expressions[0]); // NoBorrowers

var eligibility = context.AddCategory("Eligibility");
eligibility.AddExpression(context.Expressions[1]); // ReverseMortgageAge
```

#### 2. Evaluate with Automatic Context Resolution

```csharp
using NAIware.Rules.Processing;
using NAIware.Rules.Runtime;

var processor = new RuleProcessor(library);

var request = new RuleEvaluationRequest(
    inputObject: myLoanApplication,
    categoryName: "Validation",      // optional — null evaluates all active expressions
    includeDiagnostics: true);       // optional — produces mismatch explanations

RuleEvaluationResult result = processor.Evaluate(request);

// Matches
foreach (var match in result.Matches)
{
    Console.WriteLine($"[{match.Result?.Code}] {match.Result?.Message}");
}

// Mismatch diagnostics (when enabled)
foreach (var mismatch in result.Mismatches)
{
    Console.WriteLine($"Rule '{mismatch.ExpressionName}' did not match.");
    if (mismatch.Diagnostic is not null)
    {
        Console.WriteLine($"  Expression: {mismatch.Diagnostic.Expression}");
        Console.WriteLine($"  Explanation: {mismatch.Diagnostic.Explanation}");
        foreach (var kvp in mismatch.Diagnostic.EvaluatedParameters)
            Console.WriteLine($"  {kvp.Key} = {kvp.Value}");
    }
}
```

#### 3. Library-Level Versioning

Versioning is owned by the `RulesLibrary`, not by individual `RuleExpression` instances.

```csharp
Console.WriteLine(library.Version); // 1

var expr = context.Expressions[0];
expr.Expression = "BorrowerCount < 1";

// Publishing or saving this changed rule set creates a new library version/snapshot.
library.Version++;
Console.WriteLine(library.Version); // 2
```

Rule expressions are mutable leaves within a library version. They do not have expression-level version numbers, revision histories, or per-rule rollback semantics.

## Rule Service API

`src/NAIware.RuleService` is an ASP.NET Core Web API that evaluates a serialized model (JSON or XML) against a rules library and returns structured results. It enables out-of-process and automated evaluation — for example, the integration tests in `NAIware.Rules.Tests` post a sample MISMO loan document to the API and assert on the results.

### Pipeline

```
serialized model (JSON/XML)  ──▶  ModelDeserializationService  ──▶  domain model object
        +                                  (optional translator)
rules library (JSON)         ──▶  RulesLibraryLoader           ──▶  RulesLibrary
                                                                        │
                                              RuleEvaluationService ────┤  resolves & aligns context
                                                                        ▼
                                                   RuleProcessor  ──▶  EvaluateModelResponse
```

### Endpoints

| Endpoint | Purpose |
|---|---|
| `POST /api/rules/evaluate` | Accepts an `EvaluateModelRequest` and returns an `EvaluateModelResponse` (matches, mismatches with optional diagnostics, errors, and warnings). |
| `POST /api/rules/validate` | Accepts a `ValidateExpressionRequest` (model assembly/type plus a draft expression) and returns a `ValidationResponse`. Lets a formula be drafted and checked before it is saved — no rules library required. |
| `POST /api/rules/validate-library` | Accepts a `ValidateLibraryRequest` (inline `LibraryJson` or `LibraryPath`) and returns a `ValidationResponse` for every expression in the library. |
| `GET /api/rules/health` | Liveness probe. |

The evaluate request supplies the model assembly/type, the model payload (inline `Payload` or a `PayloadPath`), the rules library (inline `LibraryJson` or a `LibraryPath`), and an optional custom translator. When a translator is configured (`SerializerAssemblyPath` + `SerializerQualifiedTypeName`), the service invokes its `Deserialize(string filePath)` method via reflection — the same contract the Rule Editor uses — so MISMO-style translators (e.g. `Mortgage.Model.Translators.MISMO`) work unchanged. Otherwise it falls back to `System.Text.Json` / `XmlSerializer`.

### Validation

The validation endpoints run the same compiler-style checks the Rule Editor uses, because both hosts share `NAIware.Rules.Validation.RuleValidationService`. The service resolves the model `Type` through `AssemblyContextMetadataProvider` (an `IContextMetadataProvider` backed by the collectible assembly loader) and reports property-path, parenthesis, and operand-type issues as `ValidationIssue` records. `ValidationResponse.IsValid` is `true` when there are no `Error`-severity issues; warnings (such as a missing result definition) do not block a draft.

Model and translator assemblies are loaded on demand into collectible `AssemblyLoadContext` instances that preserve a single `Type` identity across related DLLs, mirroring the editor's `AssemblyTypeDiscoveryService`.

### Security considerations

> **The API is a developer/test harness, not a production-hardened service.** It loads arbitrary assemblies and reads arbitrary file paths supplied by the caller. Before any real-world exposure it requires, at minimum:
> - Authentication and authorization on the endpoints.
> - Allow-listing (or outright removal) of caller-supplied `*AssemblyPath`, `PayloadPath`, and `LibraryPath` values to prevent path traversal and arbitrary assembly loading.
> - Sandboxing/isolation for untrusted model and translator assemblies, since loaded code executes in-process.
> - Request size limits and timeouts to bound resource usage.

### Known issues / concerns

- **`tests/resources/LoanEligibilityRules.json` is broken.** Its expression uses the root-context prefix `Loan.PrimaryBorrower.FirstName`, but `ParameterFactory` extracts parameters **without** a root prefix (e.g. `PrimaryBorrower.FirstName`). Under `Strict` execution mode this raises a parse error. The API integration tests use a corrected library, `tests/resources/MortgageEligibilityRules.json`, authored against the verified parameter paths. The original resource should be fixed or removed separately.
- **`NAIware.RuleEditor` does not build as part of the solution.** It references `NAIware.RuleIntelligence`, which `NAIware-Rules.slnx` excludes from the build (`Project="false"`). A full-solution build therefore fails on the editor. This is pre-existing and unrelated to the Rule Service; the service and test projects build and run independently.

## Coding Standards

- **Nullable reference types** are enabled across all projects. All public APIs should be nullable-annotated.
- **XML documentation** is generated (`GenerateDocumentationFile`). Every public/protected type and member should carry `<summary>` and relevant `<param>`, `<returns>`, `<exception>`, `<remarks>` tags.
- **Build quality**: The solution must build with **zero warnings**. Do not suppress warnings globally; fix them properly.
- **Testing**: BDD-style tests using Reqnroll/Gherkin feature files with xUnit and FluentAssertions.
- **Naming**: Follow standard .NET naming conventions (PascalCase for public members, camelCase with underscore prefix for private fields).

## Known Assumptions & Business Rules

- The rules engine supports these comparison types: `=`, `!=`, `<>`, `>`, `<`, `>=`, `<=`.
- Logical connectives are `and` / `or` (also `&&` / `||`, normalized during tokenization).
- Math operators are `+`, `-`, `*`, `/`.
- Date literals are delimited with `#` (e.g., `#2024-01-01#`).
- String literals are delimited with `"` (e.g., `"Active"`).
- The `Formulae.Engine.Execute()` method is a stub — formula execution returns `null`. Individual formula evaluation via `FormulaTree.Evaluate()` is fully functional.
- `BinaryLargeNumber` is an obsolete, incomplete stub. Use `System.Numerics.BigInteger` instead.

## Troubleshooting

| Issue | Resolution |
|---|---|
| `NU1603` warnings about Reqnroll | Ensure Reqnroll packages are pinned to `3.0.0` in test projects |
| `ParsingException` during rule/formula parsing | Check for unmatched parentheses, invalid characters, or unsupported operators in the expression |
| `InvalidOperationException` from parameter creation | Verify the type name string matches a supported type (`System.Int32`, `System.Decimal`, `System.Double`, `System.String`, `System.Boolean`, `System.DateTime`) |
| Parameter caching not working | Add `NAIware.Rules.ParameterCachingEnabled = true` to app settings |

## Recommended Next Steps

1. **Replace `System.Collections.Stack` with `Stack<object>`** in the parser methods of `Rules.Engine`, `Formulae.Engine`, and `LogicProcessorEngine` to eliminate non-generic collection usage and improve type safety.
2. **Implement `Formulae.Engine.Execute()`** — it currently returns `null` as a stub. Individual formula evaluation via `FormulaTree.Evaluate()` works, but the batch-execution entry point is not yet wired up.
3. **Remove `BinaryLargeNumber`** entirely since it is an incomplete, obsolete stub. Use `System.Numerics.BigInteger` for arbitrary-precision arithmetic instead.
4. **Evaluate FluentAssertions licensing** — the test output shows a commercial-license notice from Xceed. Determine whether the project qualifies under the community license or if an alternative assertion library (e.g., `Shouldly`) should be adopted.

## License

See the repository for license information.
