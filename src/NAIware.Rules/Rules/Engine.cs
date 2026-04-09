namespace NAIware.Rules.Rules;

/// <summary>
/// The processing engine for rules, supporting parsing, grouping, and execution.
/// </summary>
public class Engine : EngineBase
{
    private readonly List<RuleTree> _ruleforest = [];

    /// <summary>Creates a new rules engine instance.</summary>
    public Engine(params string[] ruleGroups)
    {
        AddGroups(ruleGroups);
    }

    /// <summary>Gets the rules from the engine.</summary>
    internal List<RuleTree> Rules => _ruleforest;

    /// <summary>Gets rules for the specified group name.</summary>
    public List<RuleTree>? GetRules(string? groupName)
    {
        if (string.IsNullOrEmpty(groupName)) return Rules;
        if (ExpressionGroups.ContainsKey(groupName))
            return ((RuleGroup)ExpressionGroups[groupName]).GetAllRules();
        return null;
    }

    /// <summary>Adds a rule from an expression string.</summary>
    public void AddRule(string expression, params string[] memberOfRuleGroups)
    {
        AddRule(expression, Guid.NewGuid(), string.Empty, memberOfRuleGroups);
    }

    /// <summary>Adds a named rule from an expression string.</summary>
    public void AddRule(string expression, string name, params string[] memberOfRuleGroups)
    {
        AddRule(expression, Guid.NewGuid(), name, memberOfRuleGroups);
    }

    /// <summary>Adds a rule with explicit GUID and name.</summary>
    public void AddRule(string expression, Guid guid, string name, params string[] memberOfRuleGroups)
    {
        RuleTree? ruletree = Parse(expression);
        if (ruletree is null) return;

        ruletree.Identification = new Identification(guid, name);
        _ruleforest.Add(ruletree);

        if (memberOfRuleGroups is { Length: > 0 })
        {
            for (int i = 0; i < memberOfRuleGroups.Length; i++)
            {
                if (ExpressionGroups.ContainsKey(memberOfRuleGroups[i]))
                    ((RuleGroup)ExpressionGroups[memberOfRuleGroups[i]]).Rules.Add(ruletree);
            }
        }
    }

    /// <summary>Adds a rule group.</summary>
    public void AddGroup(string ruleGroupName)
    {
        ExpressionGroups.Add(ruleGroupName, new RuleGroup(ruleGroupName, this));
    }

    /// <summary>Adds a rule group with a parent.</summary>
    public void AddGroup(string ruleGroupName, string ruleGroupParentName)
    {
        ExpressionGroups.Add(ruleGroupName, new RuleGroup(ruleGroupName, ruleGroupParentName, this));
    }

    /// <summary>Adds multiple rule groups.</summary>
    public void AddGroups(params string[] ruleGroups)
    {
        if (ruleGroups is not null)
        {
            for (int i = 0; i < ruleGroups.Length; i++)
            {
                if (!ExpressionGroups.ContainsKey(ruleGroups[i]))
                    ExpressionGroups.Add(ruleGroups[i], new RuleGroup(ruleGroups[i], this));
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

    /// <summary>Executes the engine and returns identifications of rules that evaluated to true.</summary>
    public List<Identification> Execute() => Execute(null, null);

    /// <summary>Executes against a specific group.</summary>
    public List<Identification> Execute(string? targetGroup) => Execute(targetGroup, null);

    /// <summary>Executes against a specific group with parameters.</summary>
    public List<Identification> Execute(string? targetGroup, Parameters? parameters)
    {
        List<Identification> idents = [];
        List<RuleTree> rules = !string.IsNullOrEmpty(targetGroup)
            ? ((RuleGroup)ExpressionGroups[targetGroup]).GetAllRules()
            : _ruleforest;

        if (rules is { Count: > 0 })
        {
            if (parameters is null)
            {
                lock (Parameters)
                {
                    for (int i = 0, j = rules.Count; i < j; i++)
                    {
                        if (rules[i].Evaluate())
                            idents.Add(rules[i].Identification);
                    }
                }
            }
            else
            {
                for (int i = 0, j = rules.Count; i < j; i++)
                {
                    if (rules[i].Evaluate(ref parameters))
                        idents.Add(rules[i].Identification);
                }
            }
        }

        return idents;
    }

    /// <summary>Gets a rule tree by name.</summary>
    public RuleTree? GetRuleByName(string name)
    {
        for (int i = 0; i < _ruleforest.Count; i++)
        {
            if (_ruleforest[i].Identification.Name == name)
                return _ruleforest[i];
        }
        return null;
    }

    /// <summary>Parses an expression string into a rule tree.</summary>
    public RuleTree? Parse(string expression) => Parse(Helper.GetTokens(expression));

    /// <summary>Parses tokens into a rule tree.</summary>
    public RuleTree? Parse(List<string> tokens)
    {
        System.Collections.Stack expstack = new();
        System.Collections.Stack operationstack = new();
        LogicalOperator? logicop;
        ComparisonOperator? comparisonop;

        try
        {
            do
            {
                // Refactor simple rule
                if (operationstack.Count > 0 && operationstack.Peek() is ComparisonOperator && expstack.Peek() is not ComparisonOperator)
                {
                    string rhs = (expstack.Pop() as string)!;
                    comparisonop = expstack.Pop() as ComparisonOperator;
                    string lhs = (expstack.Pop() as string)!;
                    expstack.Push(Factory.GetSimpleExpression<ComparisonOperator, bool>(this, lhs, comparisonop!, rhs));
                    operationstack.Pop();
                    continue;
                }

                // Refactor complex rule
                if (operationstack.Count > 0 && operationstack.Peek() is LogicalOperator && expstack.Peek() is IExpression<bool>)
                {
                    IExpression<bool> rhrule = (expstack.Pop() as IExpression<bool>)!;
                    if (expstack.Peek() is not LogicalOperator)
                    {
                        expstack.Push(rhrule);
                    }
                    else
                    {
                        logicop = expstack.Pop() as LogicalOperator;
                        IExpression<bool> lhrule = (expstack.Pop() as IExpression<bool>)!;
                        expstack.Push(Factory.GetComplexExpression(lhrule, logicop!, rhrule));
                        operationstack.Pop();
                        continue;
                    }
                }

                // Refactor grouping
                if (operationstack.Count > 0 && operationstack.Peek() is string s && s == ")")
                {
                    expstack.Pop(); // discard ")"
                    object pareninner = expstack.Pop();
                    if (pareninner is IExpression<bool> innerrule)
                    {
                        innerrule.HasLeftParenthesis = true;
                        innerrule.HasRightParenthesis = true;
                    }
                    expstack.Pop(); // discard "("
                    expstack.Push(pareninner);
                    operationstack.Pop();
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
                    case "=" or "<>" or "!=" or ">" or "<" or ">=" or "<=":
                        operationstack.Push(new ComparisonOperator((expstack.Pop() as string)!));
                        expstack.Push(operationstack.Peek());
                        break;
                    case "and" or "or":
                        operationstack.Push(new LogicalOperator((expstack.Pop() as string)!));
                        expstack.Push(operationstack.Peek());
                        break;
                    case ")":
                        operationstack.Push(")");
                        break;
                }
            } while (tokens.Count > 0 || (tokens.Count == 0 && expstack.Count > 1));

            if (expstack.Count == 0) return null;
        }
        catch (Exception ex)
        {
            if (ex is InvalidOperationException)
                throw new FormatException($"The system encountered an error when attempting to parse out a rule structure from the specified expression. {ex.Message}", ex);
            else
                throw new FormatException("The system encountered an error when attempting to parse out a rule structure from the specified expression.");
        }

        RuleTree ruletree = new();
        ruletree.Root = (expstack.Pop() as ExpressionNode<bool>)!;
        return ruletree;
    }
}
