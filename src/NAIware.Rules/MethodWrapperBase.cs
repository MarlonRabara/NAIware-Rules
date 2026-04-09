namespace NAIware.Rules;

/// <summary>
/// An interface for wrapping method invocation in the logic processor.
/// </summary>
public interface IMethodWrapper
{
    /// <summary>Executes the wrapped method with the given parameters.</summary>
    object ExecuteMethod(params object[] parameters);
}

/// <summary>
/// A base class for logic method wrappers with parameter validation.
/// </summary>
public abstract class MethodWrapperBase : IMethodWrapper
{
    /// <summary>Initializes a new method wrapper base.</summary>
    protected MethodWrapperBase()
    {
    }

    object IMethodWrapper.ExecuteMethod(params object[] parameters) => ExecuteMethod(parameters);

    /// <summary>Executes the method after validating parameters.</summary>
    internal object ExecuteMethod(params object[] parameters)
    {
        bool isValid = true;
        ValidateParameters(ref isValid, parameters);

        if (!isValid) throw new LogicMethodArgumentException();

        return DescendantExecute(parameters);
    }

    /// <summary>Performs the actual execution in the derived class.</summary>
    protected abstract object DescendantExecute(params object[] parameters);

    /// <summary>Validates parameters before execution.</summary>
    protected abstract void ValidateParameters(ref bool isValid, params object[] parameters);
}

/// <summary>
/// An exception thrown when a logic method has invalid parameters.
/// </summary>
public class LogicMethodArgumentException : ArgumentException
{
    /// <summary>Creates a new logic method argument exception.</summary>
    public LogicMethodArgumentException() : base("The logic delegate specified has invalid parameters.") { }
}
