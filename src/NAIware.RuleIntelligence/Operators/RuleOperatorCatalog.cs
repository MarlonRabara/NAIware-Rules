namespace NAIware.RuleIntelligence;

/// <summary>
/// Built-in NAIware operators. Comparison/logical symbols intentionally align with NAIware.Rules.
/// </summary>
public static class RuleOperatorCatalog
{
    public static readonly RuleOperatorDescriptor Equal = new()
    {
        Symbol = "=",
        Kind = RuleOperatorKind.Comparison,
        Description = "Equal to.",
        SupportedTypeCategories = new HashSet<TypeCategory> { TypeCategory.Any }
    };

    public static readonly RuleOperatorDescriptor NotEqual = new()
    {
        Symbol = "!=",
        Kind = RuleOperatorKind.Comparison,
        Description = "Not equal to.",
        SupportedTypeCategories = new HashSet<TypeCategory> { TypeCategory.Any }
    };

    public static readonly RuleOperatorDescriptor NotEqualAlternate = new()
    {
        Symbol = "<>",
        Kind = RuleOperatorKind.Comparison,
        Description = "Not equal to.",
        SupportedTypeCategories = new HashSet<TypeCategory> { TypeCategory.Any }
    };

    public static readonly RuleOperatorDescriptor GreaterThan = new()
    {
        Symbol = ">",
        Kind = RuleOperatorKind.Comparison,
        Description = "Greater than.",
        SupportedTypeCategories = new HashSet<TypeCategory> { TypeCategory.Numeric, TypeCategory.Date, TypeCategory.Time }
    };

    public static readonly RuleOperatorDescriptor LessThan = new()
    {
        Symbol = "<",
        Kind = RuleOperatorKind.Comparison,
        Description = "Less than.",
        SupportedTypeCategories = new HashSet<TypeCategory> { TypeCategory.Numeric, TypeCategory.Date, TypeCategory.Time }
    };

    public static readonly RuleOperatorDescriptor GreaterOrEqual = new()
    {
        Symbol = ">=",
        Kind = RuleOperatorKind.Comparison,
        Description = "Greater than or equal to.",
        SupportedTypeCategories = new HashSet<TypeCategory> { TypeCategory.Numeric, TypeCategory.Date, TypeCategory.Time }
    };

    public static readonly RuleOperatorDescriptor LessOrEqual = new()
    {
        Symbol = "<=",
        Kind = RuleOperatorKind.Comparison,
        Description = "Less than or equal to.",
        SupportedTypeCategories = new HashSet<TypeCategory> { TypeCategory.Numeric, TypeCategory.Date, TypeCategory.Time }
    };

    public static readonly RuleOperatorDescriptor And = new()
    {
        Symbol = "and",
        DisplayText = "and",
        Kind = RuleOperatorKind.Logical,
        Description = "Logical AND."
    };

    public static readonly RuleOperatorDescriptor Or = new()
    {
        Symbol = "or",
        DisplayText = "or",
        Kind = RuleOperatorKind.Logical,
        Description = "Logical OR."
    };

    public static readonly RuleOperatorDescriptor AndSymbol = new()
    {
        Symbol = "&&",
        Kind = RuleOperatorKind.Logical,
        Description = "Logical AND symbol."
    };

    public static readonly RuleOperatorDescriptor OrSymbol = new()
    {
        Symbol = "||",
        Kind = RuleOperatorKind.Logical,
        Description = "Logical OR symbol."
    };

    public static readonly RuleOperatorDescriptor IsNull = new()
    {
        Symbol = "= null",
        DisplayText = "is null",
        Kind = RuleOperatorKind.NullCheck,
        Description = "Checks whether the value is null.",
        SupportedTypeCategories = new HashSet<TypeCategory> { TypeCategory.Any, TypeCategory.Nullable, TypeCategory.Object, TypeCategory.String }
    };

    public static readonly RuleOperatorDescriptor IsNotNull = new()
    {
        Symbol = "!= null",
        DisplayText = "is not null",
        Kind = RuleOperatorKind.NullCheck,
        Description = "Checks whether the value is not null.",
        SupportedTypeCategories = new HashSet<TypeCategory> { TypeCategory.Any, TypeCategory.Nullable, TypeCategory.Object, TypeCategory.String }
    };

    public static IReadOnlyList<RuleOperatorDescriptor> CoreComparisons { get; } =
    [
        Equal,
        NotEqual,
        NotEqualAlternate,
        GreaterThan,
        LessThan,
        GreaterOrEqual,
        LessOrEqual
    ];

    public static IReadOnlyList<RuleOperatorDescriptor> Logical { get; } =
    [
        And,
        Or,
        AndSymbol,
        OrSymbol
    ];
}
