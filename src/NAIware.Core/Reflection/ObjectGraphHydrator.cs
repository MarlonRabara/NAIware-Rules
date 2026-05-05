using System.Collections;
using System.Reflection;

namespace NAIware.Core.Reflection;

/// <summary>
/// Creates a fully initialized object graph from a .NET type.
/// Intended for rule IntelliSense, parameter discovery, and lightweight model exploration.
/// </summary>
public static class ObjectGraphHydrator
{
    private const int DefaultMaxDepth = 8;

    /// <summary>
    /// Creates a hydrated instance of the specified type.
    /// </summary>
    public static object Create(Type type, int maxDepth = DefaultMaxDepth)
    {
        ArgumentNullException.ThrowIfNull(type);

        var context = new HydrationContext(maxDepth);
        return CreateInstance(type, context, depth: 0)
            ?? throw new InvalidOperationException($"Unable to create instance of type '{type.FullName}'.");
    }

    /// <summary>
    /// Creates a hydrated instance of the specified type.
    /// </summary>
    public static T Create<T>(int maxDepth = DefaultMaxDepth) where T : class
    {
        return (T)Create(typeof(T), maxDepth);
    }

    private static object? CreateInstance(Type type, HydrationContext context, int depth)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (depth > context.MaxDepth)
        {
            return GetDefaultValue(type);
        }

        if (type == typeof(string))
        {
            return string.Empty;
        }

        if (type.IsEnum)
        {
            var values = Enum.GetValues(type);
            return values.Length > 0 ? values.GetValue(0) : Activator.CreateInstance(type);
        }

        if (IsSimpleType(type))
        {
            return GetDefaultValue(type);
        }

        if (TryCreateCollection(type, context, depth, out var collection))
        {
            return collection;
        }

        if (type.IsAbstract || type.IsInterface)
        {
            return null;
        }

        if (context.IsInCurrentPath(type))
        {
            return null;
        }

        var constructor = type.GetConstructor(Type.EmptyTypes);
        if (constructor is null)
        {
            return null;
        }

        object instance;

        try
        {
            instance = constructor.Invoke(null);
        }
        catch
        {
            return null;
        }

        context.Push(type);

        foreach (var property in GetHydratableProperties(type))
        {
            try
            {
                var value = CreateInstance(property.PropertyType, context, depth + 1);

                if (value is not null || CanAssignNull(property.PropertyType))
                {
                    property.SetValue(instance, value);
                }
            }
            catch
            {
                // Keep hydration resilient. One bad property should not prevent
                // the rest of the object graph from being initialized.
            }
        }

        context.Pop(type);

        return instance;
    }

    private static IEnumerable<PropertyInfo> GetHydratableProperties(Type type)
    {
        return type
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p =>
                p.CanRead &&
                p.CanWrite &&
                p.GetIndexParameters().Length == 0);
    }

    private static bool TryCreateCollection(
        Type type,
        HydrationContext context,
        int depth,
        out object? collection)
    {
        collection = null;

        Type? itemType = GetCollectionItemType(type);
        if (itemType is null)
        {
            return false;
        }

        Type concreteCollectionType;

        if (type.IsArray)
        {
            var item = CreateInstance(itemType, context, depth + 1);
            var array = Array.CreateInstance(itemType, 1);
            array.SetValue(item, 0);
            collection = array;
            return true;
        }

        if (type.IsInterface || type.IsAbstract)
        {
            concreteCollectionType = typeof(List<>).MakeGenericType(itemType);
        }
        else
        {
            concreteCollectionType = type;
        }

        if (!typeof(IList).IsAssignableFrom(concreteCollectionType))
        {
            return false;
        }

        var constructor = concreteCollectionType.GetConstructor(Type.EmptyTypes);
        if (constructor is null)
        {
            return false;
        }

        var list = constructor.Invoke(null) as IList;
        if (list is null)
        {
            return false;
        }

        var childItem = CreateInstance(itemType, context, depth + 1);
        if (childItem is not null)
        {
            list.Add(childItem);
        }

        collection = list;
        return true;
    }

    private static Type? GetCollectionItemType(Type type)
    {
        if (type == typeof(string))
        {
            return null;
        }

        if (type.IsArray)
        {
            return type.GetElementType();
        }

        if (type.IsGenericType &&
            type.GetGenericTypeDefinition() == typeof(List<>))
        {
            return type.GetGenericArguments()[0];
        }

        var enumerableInterface = type
            .GetInterfaces()
            .Concat([type])
            .FirstOrDefault(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        return enumerableInterface?.GetGenericArguments()[0];
    }

    private static bool IsSimpleType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        return type.IsPrimitive ||
               type == typeof(decimal) ||
               type == typeof(DateTime) ||
               type == typeof(DateTimeOffset) ||
               type == typeof(Guid) ||
               type == typeof(TimeSpan);
    }

    private static object? GetDefaultValue(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (type == typeof(string))
        {
            return string.Empty;
        }

        if (type == typeof(Guid))
        {
            return Guid.Empty;
        }

        if (type == typeof(DateTime))
        {
            return DateTime.MinValue;
        }

        if (type == typeof(DateTimeOffset))
        {
            return DateTimeOffset.MinValue;
        }

        if (type == typeof(TimeSpan))
        {
            return TimeSpan.Zero;
        }

        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }

    private static bool CanAssignNull(Type type)
    {
        return !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;
    }

    private sealed class HydrationContext
    {
        private readonly Stack<Type> _path = new();

        public HydrationContext(int maxDepth)
        {
            MaxDepth = maxDepth;
        }

        public int MaxDepth { get; }

        public bool IsInCurrentPath(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            return _path.Contains(type);
        }

        public void Push(Type type)
        {
            _path.Push(Nullable.GetUnderlyingType(type) ?? type);
        }

        public void Pop(Type type)
        {
            if (_path.Count > 0)
            {
                _path.Pop();
            }
        }
    }
}