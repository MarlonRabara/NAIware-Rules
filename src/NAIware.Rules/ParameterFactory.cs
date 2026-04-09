using System.Configuration;
using System.Runtime.Caching;
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

        PropertyTable pt = ReflectionHelper.GetProperties(obj);
        AppendParameters(obj, prms, pt);

        if (IsParameterCachingEnabled && cache is not null && !string.IsNullOrEmpty(cacheKey))
            cache.Add(cacheKey, prms, DateTimeOffset.Now.AddMilliseconds(20));

        return prms;
    }

    private void AppendParameters(object obj, Parameters prms, PropertyTable pt)
    {
        foreach (string propertyName in pt.GetMemberNames())
        {
            var propertyInfo = pt[propertyName];
            if (propertyInfo is null) continue;

            Type t = propertyInfo.PropertyType;
            Type underlyingType = Helper.ExtractType(t);
            Type genericParameterType = typeof(GenericParameter<>).MakeGenericType(underlyingType);

            try
            {
                object? extractedValue = propertyInfo.GetValue(obj, null);
                IParameter parameter;

                if (Helper.IsNullable(t) && extractedValue is null)
                {
                    extractedValue = 0;
                    parameter = (IParameter)Activator.CreateInstance(genericParameterType, propertyName, propertyName, null)!;
                }
                else if (t != underlyingType)
                {
                    parameter = (IParameter)Activator.CreateInstance(genericParameterType, propertyName, propertyName, Convert.ChangeType(extractedValue, underlyingType))!;
                }
                else
                {
                    parameter = (IParameter)Activator.CreateInstance(genericParameterType, propertyName, propertyName, extractedValue)!;
                }

                if (!prms.Keys.Contains(propertyName))
                    prms.Add(propertyName, parameter);
            }
            catch
            {
                // do nothing if we can't extract the value from the parameter
            }
        }
    }
}
