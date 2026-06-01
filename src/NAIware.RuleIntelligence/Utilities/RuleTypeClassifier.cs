using System.Collections;
namespace NAIware.RuleIntelligence;

internal static class RuleTypeClassifier
{
    public static Type Normalize(Type type) => Nullable.GetUnderlyingType(type) ?? type;

    public static bool IsNullable(Type type) => Nullable.GetUnderlyingType(type) is not null;

    public static bool IsSimple(Type type)
    {
        type = Normalize(type);
        return type.IsPrimitive
            || type.IsEnum
            || type == typeof(string)
            || type == typeof(decimal)
            || type == typeof(DateTime)
            || type == typeof(DateTimeOffset)
            || type == typeof(DateOnly)
            || type == typeof(TimeOnly)
            || type == typeof(Guid)
            || type == typeof(TimeSpan);
    }

    public static bool IsNumeric(Type type)
    {
        type = Normalize(type);
        return type == typeof(byte)
            || type == typeof(sbyte)
            || type == typeof(short)
            || type == typeof(ushort)
            || type == typeof(int)
            || type == typeof(uint)
            || type == typeof(long)
            || type == typeof(ulong)
            || type == typeof(float)
            || type == typeof(double)
            || type == typeof(decimal);
    }

    public static bool IsDateLike(Type type)
    {
        type = Normalize(type);
        return type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(DateOnly);
    }

    public static bool IsTimeLike(Type type)
    {
        type = Normalize(type);
        return type == typeof(TimeSpan) || type == typeof(TimeOnly);
    }

    public static bool IsString(Type type) => Normalize(type) == typeof(string);

    public static bool IsBoolean(Type type) => Normalize(type) == typeof(bool);

    public static bool IsGuid(Type type) => Normalize(type) == typeof(Guid);

    public static bool IsCollection(Type type, [NotNullWhen(true)] out Type? itemType)
    {
        itemType = null;
        type = Normalize(type);

        if (type == typeof(string))
            return false;

        if (type.IsArray)
        {
            itemType = type.GetElementType();
            return itemType is not null;
        }

        var enumerableType = type.GetInterfaces()
            .Concat([type])
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        if (enumerableType is null)
            return false;

        itemType = enumerableType.GetGenericArguments()[0];
        return true;
    }

    public static IReadOnlySet<TypeCategory> GetCategories(Type type, bool isCollection = false)
    {
        var set = new HashSet<TypeCategory> { TypeCategory.Any };

        if (IsNullable(type)) set.Add(TypeCategory.Nullable);
        if (isCollection || IsCollection(type, out _)) set.Add(TypeCategory.Collection);

        var normalized = Normalize(type);
        if (IsBoolean(normalized)) set.Add(TypeCategory.Boolean);
        else if (IsString(normalized)) set.Add(TypeCategory.String);
        else if (IsNumeric(normalized)) set.Add(TypeCategory.Numeric);
        else if (IsDateLike(normalized)) set.Add(TypeCategory.Date);
        else if (IsTimeLike(normalized)) set.Add(TypeCategory.Time);
        else if (IsGuid(normalized)) set.Add(TypeCategory.Guid);
        else if (normalized.IsEnum) set.Add(TypeCategory.Enum);
        else set.Add(TypeCategory.Object);

        return set;
    }
}
