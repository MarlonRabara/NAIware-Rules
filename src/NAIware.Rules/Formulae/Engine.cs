namespace NAIware.Rules.Formulae;

/// <summary>
/// The processing engine for formulae, supporting parsing, grouping, and execution.
/// </summary>
public class Engine : EngineBase
{
    private readonly List<FormulaTree> _formulaforest = [];

    /// <summary>Creates a new formulae engine instance.</summary>
    public Engine(params string[] formulaGroups)
    {
        AddGroups(formulaGroups);
    }

    /// <summary>Gets the formulae from the engine.</summary>
    internal List<FormulaTree> Formulas => _formulaforest;

    /// <summary>Gets formulae for the specified group name.</summary>
    public List<FormulaTree>? GetFormulae(string? groupName)
    {
        if (string.IsNullOrEmpty(groupName)) return Formulas;
        if (ExpressionGroups.ContainsKey(groupName))
            return ((FormulaGroup)ExpressionGroups[groupName]).GetAllFormulae();
        return null;
    }

    /// <summary>Adds a formula from an expression string.</summary>
    public void AddFormula(string expression, params string[] memberOfFormulaGroups)
    {
        AddFormula(expression, Guid.NewGuid(), string.Empty, memberOfFormulaGroups);
    }

    /// <summary>Adds a named formula from an expression string.</summary>
    public void AddFormula(string expression, string name, params string[] memberOfFormulaGroups)
    {
        AddFormula(expression, Guid.NewGuid(), name, memberOfFormulaGroups);
    }

    /// <summary>Adds a formula with explicit GUID and name.</summary>
    public void AddFormula(string expression, Guid guid, string name, params string[] memberOfFormulaGroups)
    {
        FormulaTree? formulatree = Parse(expression);
        if (formulatree is null) return;

        formulatree.Identification = new Identification(guid, name);
        _formulaforest.Add(formulatree);

        if (memberOfFormulaGroups is { Length: > 0 })
        {
            for (int i = 0; i < memberOfFormulaGroups.Length; i++)
            {
                if (ExpressionGroups.ContainsKey(memberOfFormulaGroups[i]))
                    ((FormulaGroup)ExpressionGroups[memberOfFormulaGroups[i]]).Formulae.Add(formulatree);
            }
        }
    }

    /// <summary>Adds a formula group.</summary>
    public void AddGroup(string formulaGroupName)
    {
        ExpressionGroups.Add(formulaGroupName, new FormulaGroup(formulaGroupName, this));
    }

    /// <summary>Adds a formula group with a parent.</summary>
    public void AddGroup(string formulaGroupName, string formulaGroupParentName)
    {
        ExpressionGroups.Add(formulaGroupName, new FormulaGroup(formulaGroupName, formulaGroupParentName, this));
    }

    /// <summary>Adds multiple formula groups.</summary>
    public void AddGroups(params string[] formulaGroups)
    {
        if (formulaGroups is not null)
        {
            for (int i = 0; i < formulaGroups.Length; i++)
            {
                if (!ExpressionGroups.ContainsKey(formulaGroups[i]))
                    ExpressionGroups.Add(formulaGroups[i], new FormulaGroup(formulaGroups[i], this));
            }
        }
    }

    /// <summary>Gets all group names.</summary>
    public string[] GetGroupNames()
    {
        string[] groupnames = new string[ExpressionGroups.Keys.Count];
        ExpressionGroups.Keys.CopyTo(groupnames, 0);
        return groupnames;
    }

    /// <summary>Executes the engine (currently returns null — not yet implemented).</summary>
    public List<Identification>? Execute() => Execute(null, null);

    /// <summary>Executes against a specific group.</summary>
    public List<Identification>? Execute(string? targetGroup) => Execute(targetGroup, null);

    /// <summary>Executes against a specific group with parameters (not yet implemented).</summary>
    public List<Identification>? Execute(string? targetGroup, Parameters? parameters)
    {
        // TODO: Formula execution not yet implemented in original codebase
        return null;
    }

    /// <summary>Gets a formula tree by name.</summary>
    public FormulaTree? GetFormulaByName(string name)
    {
        for (int i = 0; i < _formulaforest.Count; i++)
        {
            if (_formulaforest[i].Identification.Name == name)
                return _formulaforest[i];
        }
        return null;
    }

    /// <summary>Parses an expression string into a formula tree.</summary>
    public FormulaTree? Parse(string expression)
    {
        expression = Helper.ValidateExpression(expression);
        return Parse(Helper.GetTokens(expression));
    }

    /// <summary>Parses tokens into a formula tree.</summary>
    public FormulaTree? Parse(List<string> tokens)
    {
        System.Collections.Stack expstack = new();
        System.Collections.Stack operationstack = new();
        MathOperator? mathop;
        int currentParenNestingLevel = 0;
        Stack<int> parenthesisOpNesting = new();

        try
        {
            do
            {
                // Single parameter token → multiply by 1 to create expression
                if (tokens.Count > 0 && Parameters!.ContainsKey(tokens[0]))
                {
                    expstack.Push(Factory.GetExpression<MathOperator, decimal>(this, tokens[0], new MathOperator("*"), "1")!);
                    tokens.RemoveAt(0);
                    if (tokens.Count == 0)
                        break;
                    else
                        continue;
                }

                // Refactor simple formula
                if (operationstack.Count > 0 && operationstack.Peek() is MathOperator &&
                    expstack.Peek() is not MathOperator &&
                    !(expstack.Peek() is string && expstack.Peek()!.ToString() == "(") &&
                    ((parenthesisOpNesting.Count > 0 && currentParenNestingLevel == parenthesisOpNesting.Peek()) || parenthesisOpNesting.Count == 0))
                {
                    object? rhs = expstack.Pop();
                    mathop = expstack.Pop() as MathOperator;

                    object? lhs;
                    if (mathop!.Text == "-" && (expstack.Count == 0 || expstack.Peek() is MathOperator))
                    {
                        lhs = "0";
                    }
                    else
                    {
                        lhs = expstack.Pop();
                    }

                    if (lhs?.ToString()?.ToLower() == "null" || rhs?.ToString()?.ToLower() == "null")
                    {
                        expstack.Push(Factory.GetExpression<MathOperator, decimal?>(this, lhs!, mathop, rhs!));
                    }
                    else
                    {
                        expstack.Push(Factory.GetExpression<MathOperator, decimal>(this, lhs!, mathop, rhs!));
                    }

                    operationstack.Pop();
                    if (parenthesisOpNesting.Count > 0)
                        parenthesisOpNesting.Pop();
                    continue;
                }

                // Refactor grouping
                if (operationstack.Count > 0 && operationstack.Peek() is string s && s == ")")
                {
                    expstack.Pop(); // discard ")"
                    object? pareninner = expstack.Pop();
                    if (pareninner is IExpression<decimal> innerformula)
                    {
                        innerformula.HasLeftParenthesis = true;
                        innerformula.HasRightParenthesis = true;
                    }
                    expstack.Pop(); // discard "("
                    expstack.Push(pareninner);
                    operationstack.Pop();
                    currentParenNestingLevel--;
                    continue;
                }

                // Process next token
                if (tokens.Count > 0)
                {
                    expstack.Push(tokens[0]);
                    tokens.RemoveAt(0);
                }

                // Analyze stack items
                switch (expstack.Peek() as string)
                {
                    case "*" or "/" or "+" or "-":
                        operationstack.Push(new MathOperator((expstack.Pop() as string)!));
                        expstack.Push(operationstack.Peek());
                        parenthesisOpNesting.Push(currentParenNestingLevel);
                        break;
                    case "and" or "or":
                        operationstack.Push(new LogicalOperator((expstack.Pop() as string)!));
                        expstack.Push(operationstack.Peek());
                        parenthesisOpNesting.Push(currentParenNestingLevel);
                        break;
                    case ")":
                        operationstack.Push(")");
                        break;
                    case "(":
                        currentParenNestingLevel++;
                        break;
                    default:
                    {
                        string? pushedStackItem = expstack.Peek() as string;
                        if (decimal.TryParse(pushedStackItem, out decimal pushedStackValue))
                        {
                            expstack.Pop();
                            expstack.Push(Factory.GetExpression<MathOperator, decimal>(this, pushedStackValue.ToString(), new MathOperator("*"), "1"));
                        }
                        else
                        {
                            if (Parameters?.Count > 0)
                            {
                                foreach (var key in Parameters.Keys)
                                {
                                    if (key.Contains(pushedStackItem!))
                                        throw new InvalidOperationException($"Invalid variable name '{pushedStackItem}': Suggest using {key} instead.");
                                }
                            }
                        }

                        if (tokens.Count == 0 && expstack.Count > 1)
                        {
                            bool hasOperators = false;
                            foreach (object expItem in expstack)
                            {
                                if (expItem is Operator)
                                {
                                    hasOperators = true;
                                    break;
                                }
                            }
                            if (!hasOperators)
                                throw new Exception("Not enough operators to evaluate this expression.");
                        }
                        break;
                    }
                }
            } while (tokens.Count > 0 || (tokens.Count == 0 && expstack.Count > 1));

            if (expstack.Count == 0) return null;
        }
        catch (Exception ex)
        {
            throw new FormatException($"The system encountered an error when attempting to parse out a formula structure from the specified expression. Error: {ex.Message}.", ex);
        }

        FormulaTree formulatree = new();
        ExpressionNode<decimal>? treeNode;

        if (expstack.Count == 1)
        {
            object? evaluatedValue = expstack.Pop();
            if (evaluatedValue is not ExpressionNode<decimal>)
            {
                if (evaluatedValue?.ToString()?.ToLower() == "null")
                    evaluatedValue = "0";

                treeNode = Factory.GetExpression<MathOperator, decimal>(this, evaluatedValue!, new MathOperator("*"), "1") as ExpressionNode<decimal>;
            }
            else
            {
                treeNode = evaluatedValue as ExpressionNode<decimal>;
            }
        }
        else if (expstack.Count == 3)
        {
            var rightHandSide = expstack.Pop() as ExpressionNode<decimal>;
            var expressionOp = expstack.Pop() as MathOperator;
            var leftHandSide = expstack.Pop() as ExpressionNode<decimal>;
            treeNode = Factory.GetComplexExpression(leftHandSide!, expressionOp!, rightHandSide!) as ExpressionNode<decimal>;
        }
        else
        {
            treeNode = null;
        }

        formulatree.Root = treeNode!;
        return formulatree;
    }
}
