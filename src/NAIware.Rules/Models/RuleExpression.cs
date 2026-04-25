using System.Text.Json.Serialization;

namespace NAIware.Rules.Models;

/// <summary>
/// A reusable rule expression definition with metadata and result configuration.
/// Rule expressions are not independently versioned; their effective version is the containing <see cref="RulesLibrary.Version"/>.
/// </summary>
public class RuleExpression
{
    /// <summary>Creates a new rule expression.</summary>
    public RuleExpression()
    {
        Identity = Guid.NewGuid();
        Name = "NewRule";
        Description = string.Empty;
        Expression = string.Empty;
        IsActive = true;
        Tags = [];
    }

    /// <summary>Creates a new rule expression.</summary>
    public RuleExpression(string name, string expression, string description = "")
        : this(Guid.NewGuid(), name, expression, description)
    {
    }

    /// <summary>Creates a new rule expression with an explicit identity.</summary>
    public RuleExpression(Guid identity, string name, string expression, string description = "")
    {
        Identity = identity;
        Name = name;
        Expression = expression;
        Description = description;
        IsActive = true;
        Tags = [];
    }

    /// <summary>Gets or sets the stable logical identity of this expression.</summary>
    public Guid Identity { get; set; }

    /// <summary>Gets or sets the editor-friendly identity alias.</summary>
    public Guid Id
    {
        get => Identity;
        set => Identity = value;
    }

    /// <summary>Gets or sets the rule name.</summary>
    public string Name { get; set; }

    /// <summary>Gets or sets the rule description.</summary>
    public string Description { get; set; }

    /// <summary>Gets or sets the current raw expression text.</summary>
    public string Expression { get; set; }

    /// <summary>Compatibility member retained for older callers. Rule expressions are not independently versioned; use RulesLibrary.Version.</summary>
    [JsonIgnore]
    public int Version { get; private set; } = 0;

    /// <summary>Compatibility member retained for older callers. Rule expressions do not maintain version history.</summary>
    [JsonIgnore]
    public IReadOnlyList<ExpressionVersion> Versions => Array.Empty<ExpressionVersion>();

    /// <summary>Compatibility method retained for older callers. Updates the expression text without creating expression-level history.</summary>
    public void Revise(string newExpression, string? changeNote = null) => Expression = newExpression;

    /// <summary>Gets or sets whether this expression is active for evaluation.</summary>
    public bool IsActive { get; set; }

    /// <summary>Gets or sets the rule priority used by editors or future ordered execution.</summary>
    public int Priority { get; set; }

    /// <summary>Gets or sets free-form tags associated with the rule.</summary>
    public List<string> Tags { get; set; }

    /// <summary>Gets or sets the result definition returned when this expression matches.</summary>
    public RuleResultDefinition? ResultDefinition { get; set; }

    /// <summary>Gets or sets the join entities linking this expression to its parameter dependencies.</summary>
    public List<RuleExpressionParameter> ExpressionParameters { get; set; } = [];

    /// <summary>Gets or sets the result code proxy used by the editor.</summary>
    public string? ResultCode
    {
        get => ResultDefinition?.Code;
        set => EnsureResultDefinition().Code = value ?? string.Empty;
    }

    /// <summary>Gets or sets the result message proxy used by the editor.</summary>
    public string? ResultMessage
    {
        get => ResultDefinition?.Message;
        set => EnsureResultDefinition().Message = value ?? string.Empty;
    }

    /// <summary>Gets or sets the result severity proxy used by the editor.</summary>
    public string? Severity
    {
        get => ResultDefinition?.Severity;
        set => EnsureResultDefinition().Severity = value;
    }

    /// <summary>Gets or sets the optional result value proxy used by the editor.</summary>
    public string? OptionalValue
    {
        get => ResultDefinition?.Value;
        set => EnsureResultDefinition().Value = value;
    }

    /// <summary>Configures the result definition for this expression.</summary>
    public RuleExpression WithResult(string code, string message, string? severity = null, string? value = null)
    {
        ResultDefinition = new RuleResultDefinition(code, message, severity, value);
        return this;
    }

    /// <summary>Adds a parameter dependency to this expression.</summary>
    public RuleExpressionParameter AddParameter(RuleParameterDefinition parameter)
    {
        var join = new RuleExpressionParameter(Identity, parameter.Identity, parameter);
        ExpressionParameters.Add(join);
        return join;
    }

    private RuleResultDefinition EnsureResultDefinition()
    {
        ResultDefinition ??= new RuleResultDefinition(string.Empty, string.Empty);
        return ResultDefinition;
    }
}
