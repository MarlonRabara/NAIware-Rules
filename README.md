# NAIware Rules

**NAIware Rules** (Not-AI Ware Rules) is a deterministic rules engine and decisioning library for .NET. It provides a lightweight, expression-based framework for evaluating business rules and mathematical formulae at runtime without relying on AI or machine-learning techniques.

## High-Level Architecture

The solution is organized into two libraries and their corresponding test projects:

```
NAIware-Rules/
├── src/
│   ├── NAIware.Core/          # Foundational utilities and shared components
│   └── NAIware.Rules/         # Rules engine, formula engine, and logic processor
├── tests/
│   ├── NAIware.Core.Tests/    # BDD tests for core utilities
│   └── NAIware.Rules.Tests/   # BDD tests for rules and formulae engines
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

The core decisioning library providing three processing engines:

| Component | Purpose |
|---|---|
| **Rules Engine** (`Rules.Engine`) | Parses and evaluates boolean rule expressions (e.g., `Age > 18 and Status = "Active"`) |
| **Formulae Engine** (`Formulae.Engine`) | Parses and evaluates mathematical formula expressions (e.g., `Rate * Amount + Fee`) |
| **Logic Processor Engine** (`LogicProcessorEngine`) | Evaluates complex expressions combining rules, formulae, and method calls |

#### Key Concepts

- **Parameters** — Named, typed values injected into expressions at runtime. Created from POCOs via `ParameterFactory` using reflection. The factory automatically handles simple-type properties, nested complex objects (dot-notation), and collections (indexed dot-notation).
- **Expressions** — Parsed token trees that evaluate to a result. `SimpleExpression<V,OP,R>` handles atomic comparisons/math; `ComplexExpression<OP,R>` composes them.
- **Operators** — `ComparisonOperator` (`=`, `!=`, `<>`, `>`, `<`, `>=`, `<=`), `LogicalOperator` (`and`, `or`), `MathOperator` (`+`, `-`, `*`, `/`).
- **Method Map** — Allows registration of custom method wrappers (`IMethodWrapper`) that can be invoked within logic processor expressions.
- **Expression Groups** — Logical groupings of rules or formulae with parent-child inheritance.
- **Identification** — Each rule/formula tree carries a `Guid` and `Name` for tracking which rules fire.

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
