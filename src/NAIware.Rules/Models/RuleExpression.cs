namespace NAIware.Rules.Models;

/// <summary>
/// A reusable rule expression definition with versioning and result configuration.
/// The <see cref="Identity"/> is stable across versions; <see cref="Version"/> tracks
/// the current revision. History is maintained in <see cref="Versions"/>.
/// </summary>
public class RuleExpression
{
    private readonly Guid _identity;
    private readonly List<ExpressionVersion> _versions = [];
    private readonly List<RuleExpressionParameter> _expressionParameters = [];

    /// <summary>Creates a new rule expression.</summary>
    public RuleExpression(string name, string expression, string description = "")
        : this(Guid.NewGuid(), name, expression, description)
    {
    }

    /// <summary>Creates a new rule expression with an explicit identity.</summary>
    public RuleExpression(Guid identity, string name, string expression, string description = "")
    {
        _identity = identity;
        Name = name;
        Expression = expression;
        Description = description;
        Version = 1;
        IsActive = true;

        _versions.Add(new ExpressionVersion(1, expression, "Initial version"));
    }

    /// <summary>Gets the stable logical identity of this expression (unchanged across versions).</summary>
    public Guid Identity => _identity;

    /// <summary>Gets or sets the name of the expression.</summary>
    public string Name { get; set; }

    /// <summary>Gets or sets the description of the expression.</summary>
    public string Description { get; set; }

    /// <summary>Gets or sets the current raw expression text.</summary>
    public string Expression { get; private set; }

    /// <summary>Gets the current version number.</summary>
    public int Version { get; private set; }

    /// <summary>Gets or sets whether this expression is active for evaluation.</summary>
    public bool IsActive { get; set; }

    /// <summary>Gets or sets the result definition returned when this expression matches.</summary>
    public RuleResultDefinition? ResultDefinition { get; set; }

    /// <summary>Gets the version history of this expression.</summary>
    public IReadOnlyList<ExpressionVersion> Versions => _versions;

    /// <summary>Gets the join entities linking this expression to its parameter dependencies.</summary>
    public List<RuleExpressionParameter> ExpressionParameters => _expressionParameters;

    /// <summary>
    /// Updates the expression text and creates a new version entry.
    /// </summary>
    /// <param name="newExpression">The updated expression text.</param>
    /// <param name="changeNote">Optional description of why the expression changed.</param>
    public void Revise(string newExpression, string? changeNote = null)
    {
        Version++;
        Expression = newExpression;
        _versions.Add(new ExpressionVersion(Version, newExpression, changeNote));
    }

    /// <summary>Configures the result definition for this expression.</summary>
    public RuleExpression WithResult(string code, string message, string? severity = null)
    {
        ResultDefinition = new RuleResultDefinition(code, message, severity);
        return this;
    }

    /// <summary>Adds a parameter dependency to this expression.</summary>
    public RuleExpressionParameter AddParameter(RuleParameterDefinition parameter)
    {
        var join = new RuleExpressionParameter(_identity, parameter.Identity, parameter);
        _expressionParameters.Add(join);
        return join;
    }
}
