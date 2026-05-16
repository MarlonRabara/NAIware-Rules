namespace NAIware.RuleIntelligence;

internal static class TypeNameFormatter
{
    public static string Format(Type type)
    {
        var nullable = Nullable.GetUnderlyingType(type);
        if (nullable is not null)
            return $"{Format(nullable)}?";

        if (!type.IsGenericType)
            return type.Name;

        var name = type.Name;
        var tick = name.IndexOf('`', StringComparison.Ordinal);
        if (tick >= 0) name = name[..tick];

        return $"{name}<{string.Join(", ", type.GetGenericArguments().Select(Format))}>";
    }
}
