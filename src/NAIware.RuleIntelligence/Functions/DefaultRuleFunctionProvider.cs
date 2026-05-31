using NAIware.Rules;
using NAIware.Rules.MethodWrappers;

namespace NAIware.RuleIntelligence;

/// <summary>
/// Default function provider that advertises the formula functions shipped with the rules engine.
/// </summary>
/// <remarks>
/// The set of available function names is taken directly from the runtime registration
/// (<see cref="DefaultMethodWrapperRegistration.CreateDefaultMethodMap"/>), guaranteeing IntelliSense never
/// drifts from what the engine can actually evaluate. Editor-facing metadata (signature, description, and
/// category) is supplied from a static table; any registered function without metadata still appears with a
/// generic signature so newly added wrappers are never silently hidden.
/// </remarks>
public sealed class DefaultRuleFunctionProvider : IRuleFunctionProvider
{
    private static readonly IReadOnlyDictionary<string, RuleFunctionMetadata> Metadata =
        new Dictionary<string, RuleFunctionMetadata>(StringComparer.OrdinalIgnoreCase)
        {
            // Logical
            ["IF"] = new("IF(condition, ifTrue, ifFalse)", "Returns one value when the condition is true and another when it is false.", RuleFunctionCategory.Logical),

            // Numeric
            ["INT"] = new("INT(number)", "Truncates a number to its integer component.", RuleFunctionCategory.Numeric),
            ["ROUNDUP"] = new("ROUNDUP(number, digits)", "Rounds a number away from zero to the specified number of digits.", RuleFunctionCategory.Numeric),
            ["ROUND"] = new("ROUND(number, digits)", "Rounds a number to the specified number of digits.", RuleFunctionCategory.Numeric),
            ["MIN"] = new("MIN(number1, number2, ...)", "Returns the smallest of the supplied numbers.", RuleFunctionCategory.Numeric),
            ["MAX"] = new("MAX(number1, number2, ...)", "Returns the largest of the supplied numbers.", RuleFunctionCategory.Numeric),
            ["ABS"] = new("ABS(number)", "Returns the absolute value of a number.", RuleFunctionCategory.Numeric),
            ["SUM"] = new("SUM(number1, number2, ...)", "Adds the supplied numbers together.", RuleFunctionCategory.Numeric),
            ["AVERAGE"] = new("AVERAGE(number1, number2, ...)", "Returns the arithmetic mean of the supplied numbers.", RuleFunctionCategory.Numeric),
            ["POWER"] = new("POWER(number, exponent)", "Raises a number to the specified exponent.", RuleFunctionCategory.Numeric),
            ["CEILING"] = new("CEILING(number)", "Rounds a number up to the nearest integer.", RuleFunctionCategory.Numeric),
            ["FLOOR"] = new("FLOOR(number)", "Rounds a number down to the nearest integer.", RuleFunctionCategory.Numeric),

            // Text
            ["CONCAT"] = new("CONCAT(text1, text2, ...)", "Joins the supplied text values into a single string.", RuleFunctionCategory.Text),
            ["TRIM"] = new("TRIM(text)", "Removes leading and trailing whitespace from text.", RuleFunctionCategory.Text),
            ["REPLACE"] = new("REPLACE(text, start, length, newText)", "Replaces a portion of text identified by position and length.", RuleFunctionCategory.Text),
            ["SUBSTITUTE"] = new("SUBSTITUTE(text, oldText, newText)", "Replaces all occurrences of one substring with another.", RuleFunctionCategory.Text),
            ["LEFT"] = new("LEFT(text, count)", "Returns the leftmost characters of text.", RuleFunctionCategory.Text),
            ["RIGHT"] = new("RIGHT(text, count)", "Returns the rightmost characters of text.", RuleFunctionCategory.Text),
            ["MID"] = new("MID(text, start, length)", "Returns a substring from the middle of text.", RuleFunctionCategory.Text),
            ["UPPER"] = new("UPPER(text)", "Converts text to upper case.", RuleFunctionCategory.Text),
            ["LOWER"] = new("LOWER(text)", "Converts text to lower case.", RuleFunctionCategory.Text),
            ["PROPER"] = new("PROPER(text)", "Capitalizes the first letter of each word in text.", RuleFunctionCategory.Text),

            // Date / time
            ["NOW"] = new("NOW()", "Returns the current date and time.", RuleFunctionCategory.DateTime),
            ["TODAY"] = new("TODAY()", "Returns the current date at midnight.", RuleFunctionCategory.DateTime),
            ["DATEDIFF"] = new("DATEDIFF(startDate, endDate, unit)", "Returns the difference between two dates in the specified unit.", RuleFunctionCategory.DateTime),
        };

    private readonly IReadOnlyList<RuleFunctionDescriptor> _functions;

    /// <summary>Creates a provider seeded from the default runtime method-wrapper registration.</summary>
    public DefaultRuleFunctionProvider()
        : this(DefaultMethodWrapperRegistration.CreateDefaultMethodMap())
    {
    }

    /// <summary>Creates a provider whose functions are derived from the supplied method map.</summary>
    /// <param name="methodMap">The runtime method map that is the source of truth for available functions.</param>
    public DefaultRuleFunctionProvider(MethodMap methodMap)
    {
        ArgumentNullException.ThrowIfNull(methodMap);
        _functions = BuildDescriptors(methodMap);
    }

    /// <inheritdoc />
    public IReadOnlyList<RuleFunctionDescriptor> GetFunctions() => _functions;

    private static IReadOnlyList<RuleFunctionDescriptor> BuildDescriptors(MethodMap methodMap)
    {
        var descriptors = new List<RuleFunctionDescriptor>(methodMap.Count);

        foreach (var name in methodMap.Keys)
        {
            var displayName = name.ToUpperInvariant();

            if (Metadata.TryGetValue(displayName, out var meta))
            {
                descriptors.Add(new RuleFunctionDescriptor
                {
                    Name = displayName,
                    Signature = meta.Signature,
                    Description = meta.Description,
                    Category = meta.Category
                });
            }
            else
            {
                // Unknown wrapper: still surface it so newly registered functions are never hidden.
                descriptors.Add(new RuleFunctionDescriptor
                {
                    Name = displayName,
                    Signature = $"{displayName}(...)",
                    Description = "Formula function.",
                    Category = RuleFunctionCategory.General
                });
            }
        }

        return descriptors
            .OrderBy(d => d.Category)
            .ThenBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private readonly record struct RuleFunctionMetadata(string Signature, string Description, RuleFunctionCategory Category);
}
