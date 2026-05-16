# Known limitations

This is a strong starter implementation, but there are areas to evolve:

1. The parser is intentionally lightweight and forgiving. For complex nested expressions, a real expression parser may eventually be better.
2. String functions such as `Contains` are modeled as future operator descriptors but are not emitted by default because the current `ComparisonOperator` only accepts symbolic comparison operators.
3. Collection quantifiers such as `Any`, `All`, and `Exists` are not emitted by default until the rule engine supports them.
4. The optional dependency-injection extension is included as `.cs.txt` to avoid forcing a package reference. Rename it to `.cs` and add `Microsoft.Extensions.DependencyInjection.Abstractions` if needed.
5. Compilation assumes the project is placed beside `NAIware.Core` and `NAIware.Rules` in the existing `/src` folder.
