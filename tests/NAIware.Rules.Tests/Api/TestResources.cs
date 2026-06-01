namespace NAIware.Rules.Tests.Api;

/// <summary>
/// Resolves the absolute path to the shared <c>tests/resources</c> directory regardless of the
/// current working directory by walking up from the test assembly's base directory until the
/// folder is found.
/// </summary>
internal static class TestResources
{
    private static readonly Lazy<string> ResourcesDirectory = new(LocateResourcesDirectory);

    /// <summary>Gets the absolute path to the <c>tests/resources</c> directory.</summary>
    public static string Directory => ResourcesDirectory.Value;

    /// <summary>Gets the absolute path to a file within <c>tests/resources</c>.</summary>
    /// <param name="fileName">The file name (or relative path) under the resources directory.</param>
    /// <returns>The absolute path to the requested resource.</returns>
    public static string Path(string fileName)
    {
        string full = System.IO.Path.Combine(Directory, fileName);
        if (!File.Exists(full))
            throw new FileNotFoundException($"Test resource '{fileName}' was not found.", full);
        return full;
    }

    private static string LocateResourcesDirectory()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            string candidate = System.IO.Path.Combine(current.FullName, "tests", "resources");
            if (System.IO.Directory.Exists(candidate))
                return candidate;

            current = current.Parent;
        }

        throw new DirectoryNotFoundException(
            "Unable to locate the 'tests/resources' directory by walking up from " + AppContext.BaseDirectory);
    }
}
