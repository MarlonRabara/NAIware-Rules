namespace NAIware.Rules.Rules;

/// <summary>
/// An internal grouping of rules with parent inheritance support.
/// </summary>
internal class RuleGroup : ExpressionGroup
{
    private readonly List<RuleTree> _ruleforest = [];

    private RuleGroup() : base() { }

    public RuleGroup(string name, IEngine containingEngine) : base(name, containingEngine) { }

    public RuleGroup(string name, string parentName, IEngine containingEngine)
        : base(name, containingEngine.ExpressionGroups[parentName]) { }

    public RuleGroup(string name, ExpressionGroup parentGroup) : base(name, parentGroup) { }

    /// <summary>Gets the rules belonging to this group.</summary>
    public List<RuleTree> Rules => _ruleforest;

    /// <summary>Gets all rules including inherited from parent groups.</summary>
    public List<RuleTree> GetAllRules()
    {
        List<RuleTree> rules = [.. _ruleforest];

        if (Parent is RuleGroup parentGroup)
            rules.AddRange(parentGroup.Rules);

        return rules;
    }
}
