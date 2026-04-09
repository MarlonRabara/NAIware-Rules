using System.Configuration;
using System.Runtime.Caching;
using NAIware.Core;
using NAIware.Core.Reflection;

namespace NAIware.Rules;

/// <summary>
/// A factory for creating parameters from objects using reflection, with optional caching.
/// </summary>
public class ParameterFactory
{
    /// <summary>Gets whether parameter caching is enabled via configuration.</summary>
    public static bool IsParameterCachingEnabled
    {
        get
        {
            bool.TryParse(ConfigurationManager.AppSettings["NAIware.Rules.ParameterCachingEnabled"], out bool isEnabled);
            return isEnabled;
        }
    }

    /// <summary>Flushes the parameter cache for the specified object.</summary>
    public static void FlushParameterCache(object? obj)
    {
        if (obj is null || !IsParameterCachingEnabled) return;

        string cacheKey = $"ParameterFactoryParams_{obj.GetType().FullName}_{obj.GetHashCode()}";
        var cache = MemoryCache.Default;
        cache.Remove(cacheKey);
    }

    /// <summary>Flushes the parameter cache for the specified objects.</summary>
    public static void FlushParameterCaches(object? obj, params object[]? otherObjects)
    {
        if (obj is null && (otherObjects is null or { Length: 0 })) return;

        FlushParameterCache(obj);

        if (otherObjects is { Length: > 0 })
        {
            foreach (object o in otherObjects)
                FlushParameterCache(o);
        }
    }

    /// <summary>Creates a single decimal parameter.</summary>
    public IParameter? CreateParameter(string paramName, decimal? parameterValue)
    {
        if (string.IsNullOrEmpty(paramName)) return null;
        var genericParameter = new GenericParameter<decimal?>(paramName, paramName)
        {
            Value = parameterValue
        };
        return genericParameter;
    }

    /// <summary>Creates parameters from multiple objects.</summary>
    public Parameters? CreateParameters(bool flushParameterObjectCache, params object[] objectsToCache)
    {
        if (objectsToCache is null or { Length: 0 }) return null;

        Parameters prms = new();

        foreach (object o in objectsToCache)
            prms.Add(CreateParameters(o, flushParameterObjectCache));

        return prms;
    }

    /// <summary>Creates parameters from an object using reflection.</summary>
    /// <remarks>
    /// Simple-type properties are extracted directly. Complex object properties are
    /// recursed into using dot-notation (e.g., <c>Property.City</c>). Collection
    /// properties implementing <see cref="System.Collections.IList"/> are iterated
    /// with indexed dot-notation (e.g., <c>Borrowers.0.Age</c>).
    /// </remarks>
    public Parameters? CreateParameters(object? obj, bool flushParameterObjectCache = false)
    {
        if (obj is null) return null;

        Parameters? prms = null;
        MemoryCache? cache = null;
        string? cacheKey = null;

        if (IsParameterCachingEnabled)
        {
            cacheKey = $"ParameterFactoryParams_{obj.GetType().FullName}_{obj.GetHashCode()}";
            cache = MemoryCache.Default;

            prms = cache[cacheKey] as Parameters;
            if (prms is not null)
            {
                if (flushParameterObjectCache)
                    cache.Remove(cacheKey);
                else
                    return prms;
            }
        }

        prms = new Parameters();

        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        AppendParameters(obj, prms, string.Empty, visited);

        if (IsParameterCachingEnabled && cache is not null && !string.IsNullOrEmpty(cacheKey))
            cache.Add(cacheKey, prms, DateTimeOffset.Now.AddMilliseconds(20));

        return prms;
    }

    private void AppendParameters(object obj, Parameters prms, string prefix, HashSet<object> visited)
    {
        if (!visited.Add(obj)) return;

        PropertyTable pt = ReflectionHelper.GetProperties(obj);

        foreach (string propertyName in pt.GetMemberNames())
        {
            var propertyInfo = pt[propertyName];
            if (propertyInfo is null) continue;

            string qualifiedName = string.IsNullOrEmpty(prefix)
                ? propertyName
                : $"{prefix}.{propertyName}";

            Type t = propertyInfo.PropertyType;
            Type underlyingType = Helper.ExtractType(t);

            try
            {
                object? extractedValue = propertyInfo.GetValue(obj, null);

                // Simple type → create parameter directly
                if (TypeHelper.IsSimpleType(underlyingType) || underlyingType.IsEnum)
                {
                    Type genericParameterType = typeof(GenericParameter<>).MakeGenericType(underlyingType);
                    IParameter parameter;

                    if (Helper.IsNullable(t) && extractedValue is null)
                    {
                        parameter = (IParameter)Activator.CreateInstance(genericParameterType, qualifiedName, qualifiedName, null)!;
                    }
                    else if (t != underlyingType)
                    {
                        parameter = (IParameter)Activator.CreateInstance(genericParameterType, qualifiedName, qualifiedName, Convert.ChangeType(extractedValue, underlyingType))!;
                    }
                    else
                    {
                        parameter = (IParameter)Activator.CreateInstance(genericParameterType, qualifiedName, qualifiedName, extractedValue)!;
                    }

                    if (!prms.Keys.Contains(qualifiedName))
                        prms.Add(qualifiedName, parameter);

                    continue;
                }

                // Collection type → iterate elements with indexed prefix
                if (extractedValue is System.Collections.IList list)
                {
                    // Add a count parameter for the collection itself
                    string countName = $"{qualifiedName}.Count";
                    if (!prms.Keys.Contains(countName))
                        prms.Add(countName, new GenericParameter<int>(countName, countName, list.Count));

                    for (int i = 0; i < list.Count; i++)
                    {
                        object? element = list[i];
                        if (element is null) continue;

                        string elementPrefix = $"{qualifiedName}.{i}";
                        Type elementType = element.GetType();

                        if (TypeHelper.IsSimpleType(elementType))
                        {
                            Type elParamType = typeof(GenericParameter<>).MakeGenericType(elementType);
                            var elParam = (IParameter)Activator.CreateInstance(elParamType, elementPrefix, elementPrefix, element)!;
                            if (!prms.Keys.Contains(elementPrefix))
                                prms.Add(elementPrefix, elParam);
                        }
                        else
                        {
                            AppendParameters(element, prms, elementPrefix, visited);
                        }
                    }

                    continue;
                }

                // Complex object type → recurse with dot-prefix
                if (extractedValue is not null && !underlyingType.IsValueType)
                {
                    AppendParameters(extractedValue, prms, qualifiedName, visited);
                }
            }
            catch
            {
                // Skip properties that cannot be extracted
            }
        }
    }
}
