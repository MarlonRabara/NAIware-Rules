namespace NAIware.Rules;

/// <summary>
/// Abstract base class for expression processing engines.
/// </summary>
public abstract class EngineBase : IEngine
{
    private readonly Parameters _parameters = new();
    private readonly Dictionary<string, ExpressionGroup> _expressionGroups = new();

    /// <inheritdoc/>
    public Parameters Parameters => _parameters;

    /// <inheritdoc/>
    public Dictionary<string, ExpressionGroup> ExpressionGroups => _expressionGroups;

    /// <summary>
    /// Creates a new parameter instance and adds it to the engine.
    /// </summary>
    public void AddParameter(string name, string description, string typeFullName)
    {
        IParameter newparam = Factory.GenerateParameter(name, description, typeFullName);

        if (newparam is null)
            throw new InvalidOperationException("Unable to generate the specified parameter.");

        AddParameter(newparam);
    }

    /// <summary>
    /// Adds a parameter to the engine instance.
    /// </summary>
    public void AddParameter(IParameter parameter)
    {
        Parameters.Add(parameter.Name, parameter);
    }
}
