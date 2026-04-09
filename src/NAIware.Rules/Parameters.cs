namespace NAIware.Rules;

/// <summary>
/// A dictionary of parameters keyed by name, with cloning support.
/// </summary>
public class Parameters : Dictionary<string, IParameter>, ICloneable
{
    /// <summary>Creates a new empty parameter collection.</summary>
    public Parameters()
    {
    }

    /// <summary>Merges parameters from another collection, skipping duplicates.</summary>
    public void Add(Parameters? parameters)
    {
        if (parameters is null) return;

        foreach (string parameterName in parameters.Keys)
        {
            if (!ContainsKey(parameterName))
            {
                Add(parameterName, parameters[parameterName]);
            }
        }
    }

    /// <inheritdoc/>
    object ICloneable.Clone() => Clone();

    /// <summary>Returns a deep clone of this parameter collection.</summary>
    public Parameters Clone()
    {
        var clone = (Parameters)MemberwiseClone();
        foreach (string key in clone.Keys)
            clone[key] = (IParameter)clone[key].Clone();
        return clone;
    }
}
