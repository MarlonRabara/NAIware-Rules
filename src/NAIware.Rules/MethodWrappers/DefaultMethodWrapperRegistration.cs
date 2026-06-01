namespace NAIware.Rules.MethodWrappers;

/// <summary>
/// Provides centralized registration of the default formula method wrappers shipped with the rules engine.
/// </summary>
/// <remarks>
/// The default library exposes the following functions: <c>IF</c>, <c>INT</c>, <c>ROUNDUP</c>, <c>ROUND</c>,
/// <c>MIN</c>, <c>MAX</c>, <c>ABS</c>, <c>SUM</c>, <c>AVERAGE</c>, <c>POWER</c>, <c>CEILING</c>, <c>FLOOR</c>,
/// <c>CONCAT</c>, <c>TRIM</c>, <c>REPLACE</c>, <c>SUBSTITUTE</c>, <c>LEFT</c>, <c>RIGHT</c>, <c>MID</c>,
/// <c>UPPER</c>, <c>LOWER</c>, <c>PROPER</c>, <c>NOW</c>, <c>TODAY</c>, and <c>DATEDIFF</c>. Use
/// <see cref="CreateDefaultMethodMap"/> to obtain a ready-to-use map, or <see cref="RegisterDefaults"/> to add
/// the defaults to an existing map.
/// </remarks>
public static class DefaultMethodWrapperRegistration
{
    /// <summary>
    /// Creates a new <see cref="MethodMap"/> populated with all default formula method wrappers.
    /// </summary>
    /// <returns>A case-insensitive method map containing the default formula functions.</returns>
    public static MethodMap CreateDefaultMethodMap()
    {
        var methodMap = new MethodMap();
        RegisterDefaults(methodMap);
        return methodMap;
    }

    /// <summary>
    /// Registers the default formula method wrappers into an existing <see cref="MethodMap"/>.
    /// </summary>
    /// <param name="methodMap">The map to populate. Must not be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="methodMap"/> is <see langword="null"/>.</exception>
    public static void RegisterDefaults(MethodMap methodMap)
    {
        ArgumentNullException.ThrowIfNull(methodMap);

        // Logical
        methodMap.Add("IF", new IfMethodWrapper());

        // Numeric
        methodMap.Add("INT", new IntMethodWrapper());
        methodMap.Add("ROUNDUP", new RoundUpMethodWrapper());
        methodMap.Add("ROUND", new RoundMethodWrapper());
        methodMap.Add("MIN", new MinMethodWrapper());
        methodMap.Add("MAX", new MaxMethodWrapper());
        methodMap.Add("ABS", new AbsoluteValueMethodWrapper());
        methodMap.Add("SUM", new SumMethodWrapper());
        methodMap.Add("AVERAGE", new AverageMethodWrapper());
        methodMap.Add("POWER", new PowerMethodWrapper());
        methodMap.Add("CEILING", new CeilingMethodWrapper());
        methodMap.Add("FLOOR", new FloorMethodWrapper());

        // Text
        methodMap.Add("CONCAT", new ConcatMethodWrapper());
        methodMap.Add("TRIM", new TrimMethodWrapper());
        methodMap.Add("REPLACE", new ReplaceMethodWrapper());
        methodMap.Add("SUBSTITUTE", new SubstituteMethodWrapper());
        methodMap.Add("LEFT", new LeftMethodWrapper());
        methodMap.Add("RIGHT", new RightMethodWrapper());
        methodMap.Add("MID", new MidMethodWrapper());
        methodMap.Add("UPPER", new UpperMethodWrapper());
        methodMap.Add("LOWER", new LowerMethodWrapper());
        methodMap.Add("PROPER", new ProperMethodWrapper());

        // Date / time
        methodMap.Add("NOW", new NowMethodWrapper());
        methodMap.Add("TODAY", new TodayMethodWrapper());
        methodMap.Add("DATEDIFF", new DateDiffMethodWrapper());
    }
}
