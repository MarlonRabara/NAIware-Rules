# docAnalysis.Models

C# model library for document analysis and validation JSON payloads.

## Target Framework

This project targets `.NET 10` with the C# language version set to `latest` in the project file:

```xml
<TargetFramework>net10.0</TargetFramework>
<LangVersion>latest</LangVersion>
```

## Root object

`DocumentAnalysisPackage` is the root object and can contain both:

- `DocAnalysisResults`
- `DocValidationSummary`

## Validation summary shape

`DocValidationSummary` now matches the supplied validation JSON:

```json
{
  "requiresReview": false,
  "threshold": 0.82,
  "issues": []
}
```

## Serialization / Deserialization

The root model includes convenience methods for round-tripping JSON:

```csharp
using docAnalysis.Models;

// Deserialize from a JSON file and hydrate the object model.
DocumentAnalysisPackage package = DocumentAnalysisPackage.FromJsonFile("document-analysis-package.json");

// Deserialize from a JSON string.
DocumentAnalysisPackage packageFromString = DocumentAnalysisPackage.FromJson(json);

// Serialize back to JSON.
string outputJson = package.ToJson();

// Serialize back to a JSON file.
package.ToJsonFile("document-analysis-package-output.json");
```

## Construction

All model classes include public parameterless constructors. Nested objects and collection properties are initialized so the model can be fully constructed manually without null-reference setup code:

```csharp
var package = new DocumentAnalysisPackage();

package.DocAnalysisResults!.Metadata!.SourceFileName = "sample.pdf";
package.DocValidationSummary!.RequiresReview = false;
package.DocValidationSummary.Threshold = 0.82m;
package.DocValidationSummary.Issues!.Add(new ValidationIssue
{
    FieldName = "borrowerName",
    Severity = "warning",
    Message = "Borrower name confidence is below threshold.",
    Confidence = 0.76m
});
```

## Notes

- The analysis model was generated from `DocAnalysisMetaData.json`.
- `DocValidationSummary` was updated to match `{ "requiresReview": false, "threshold": 0.82, "issues": [] }`.
- `Fields` uses `Dictionary<string, JsonElement>` so values can remain resilient if a provider returns strings, nulls, numbers, booleans, arrays, or nested objects.
- `JsonExtensionData` is included throughout the model so unknown provider-specific fields can be preserved during deserialization.
