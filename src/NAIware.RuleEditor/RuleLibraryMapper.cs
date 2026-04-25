namespace NAIware.RuleEditor;

/// <summary>
/// Pass-through helper retained for compatibility with earlier editor code.
/// The WinForms editor now consumes <see cref="RulesLibrary"/> directly rather than mapping from separate POCOs.
/// </summary>
public static class RuleLibraryMapper
{
    /// <summary>Returns the supplied library model unchanged.</summary>
    public static RulesLibrary ToDomain(RulesLibrary library)
    {
        ArgumentNullException.ThrowIfNull(library);
        return library;
    }
}
