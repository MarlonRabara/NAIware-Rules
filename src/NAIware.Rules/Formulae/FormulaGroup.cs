namespace NAIware.Rules.Formulae;

/// <summary>
/// An internal grouping of formulae with parent inheritance support.
/// </summary>
internal class FormulaGroup : ExpressionGroup
{
    private readonly List<FormulaTree> _formulaforest = [];

    private FormulaGroup() : base() { }

    public FormulaGroup(string name, IEngine containingEngine) : base(name, containingEngine) { }

    public FormulaGroup(string name, string parentName, IEngine containingEngine)
        : base(name, containingEngine.ExpressionGroups[parentName]) { }

    public FormulaGroup(string name, ExpressionGroup parentGroup) : base(name, parentGroup) { }

    /// <summary>Gets the formulae belonging to this group.</summary>
    public List<FormulaTree> Formulae => _formulaforest;

    /// <summary>Gets all formulae including inherited from parent groups.</summary>
    public List<FormulaTree> GetAllFormulae()
    {
        List<FormulaTree> formulae = [.. _formulaforest];

        if (Parent is FormulaGroup parentGroup)
            formulae.AddRange(parentGroup.Formulae);

        return formulae;
    }
}
