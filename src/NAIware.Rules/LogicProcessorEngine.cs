using NAIware.Core;

namespace NAIware.Rules;

/// <summary>
/// A logic processor engine that evaluates complex expressions with methods, rules, and formulae.
/// </summary>
public sealed class LogicProcessorEngine
{
    #region Private Classes

    private class MethodState
    {
        private bool _isReadyForExecution;
        private Stack<char>? _parenthesisTracker;
        private Stack<char>? _parameterTracker;
        private object? _lastParameterExpression;

        public bool IsTrackingParenthesis => _parenthesisTracker is not null && _parenthesisTracker.Count > 0;

        public bool IsInMethod(System.Collections.Stack? expressionStack)
        {
            if (expressionStack is null) return false;
            foreach (object item in expressionStack)
            {
                if (item is IMethodWrapper && ReferenceEquals(item, MethodReference))
                    return true;
            }
            return false;
        }

        public bool IsComplete =>
            _parenthesisTracker is not null && _parenthesisTracker.Count > 0 && _parenthesisTracker.Peek() == ')';

        public void AddParenthesis(string parenthesis)
        {
            if (!string.IsNullOrEmpty(parenthesis))
                AddParenthesis(parenthesis[0]);
        }

        public void AddParenthesis(char parenthesis)
        {
            _parenthesisTracker ??= new Stack<char>();

            if (parenthesis == ')' && _parenthesisTracker.Peek() == '(')
                _parenthesisTracker.Pop();
            else
                _parenthesisTracker.Push(parenthesis);

            _isReadyForExecution = parenthesis == ')' && _parenthesisTracker.Count == 0;
        }

        public void AddParameterDelimiter(char parameterDelimiter, object lastExpression)
        {
            _parameterTracker ??= new Stack<char>();
            _parameterTracker.Push(parameterDelimiter);
            _lastParameterExpression = lastExpression;
        }

        public bool IsLastExpression(object any)
        {
            if (Equals(_lastParameterExpression, any)) return true;
            return _lastParameterExpression is not null && Equals(_lastParameterExpression.ToString(), any);
        }

        public int DetectedParameterCount => _parameterTracker is null ? 0 : _parameterTracker.Count + 1;

        public bool IsReadyForExecution => _isReadyForExecution;

        public bool InRule { get; set; }

        public IMethodWrapper? MethodReference { get; set; }
    }

    #endregion

    private readonly string _expression;
    private readonly MethodMap? _methodMap;
    private readonly Parameters? _parameters;
    private List<string>? _expressionTokens;

    private LogicProcessorEngine()
    {
        _expression = string.Empty;
    }

    /// <summary>Creates a logic processor engine with an expression and parameters.</summary>
    public LogicProcessorEngine(string expression, Parameters? parameters)
        : this(expression, null, parameters) { }

    /// <summary>Creates a logic processor engine with an expression, method map, and parameters.</summary>
    public LogicProcessorEngine(string expression, MethodMap? methodMap, Parameters? parameters)
    {
        expression = Helper.ValidateExpression(expression);
        _expression = expression;
        _methodMap = methodMap;
        _parameters = parameters;
    }

    /// <summary>Evaluates the expression and returns a result of type T.</summary>
    public T Evaluate<T>(params object[] parameterSourceObjects)
    {
        _expressionTokens ??= Helper.GetTokens(_expression);

        System.Collections.Stack expressionStack = new();
        List<string>? ruleOrFormulaTokens;
        System.Collections.ArrayList methodparameters;
        IMethodWrapper? methodToExecute;
        bool isSimplified = false;
        Stack<MethodState> methodStateTracker = new();
        MethodState? methodState = null;

        for (int tokenIndex = 0; tokenIndex < _expressionTokens.Count; tokenIndex++)
        {
            string token = _expressionTokens[tokenIndex];

            if (_methodMap is not null && _methodMap.ContainsKey(token))
            {
                expressionStack.Push(_methodMap[token]);
                if (methodState is not null)
                    methodStateTracker.Push(methodState);
                methodState = new MethodState();
                methodState.MethodReference = expressionStack.Peek() as IMethodWrapper;
                continue;
            }

            // Rule check
            switch (token.ToLower())
            {
                case "==" or "=" or "||" or "&&" or "or" or "and" or "!=" or "<>" or "<=" or ">=" or "<" or ">":
                    if (!methodState!.InRule)
                        methodState.InRule = true;
                    break;
            }

            // Parenthesis tracking for methods
            if (token is "(" or ")" && methodState is not null)
                methodState.AddParenthesis(token);

            // Evaluate interim rules/formulae
            bool isContainedInAMethodCallOrParentheticalGrouping = token == ")" && (IsInMethodConstruct(methodState) || methodState is null);
            bool isMethodReadyToExecute = methodState is not null && methodState.IsReadyForExecution;

            if ((token == "," || isContainedInAMethodCallOrParentheticalGrouping || isMethodReadyToExecute) &&
                expressionStack.Peek() is string)
            {
                ruleOrFormulaTokens = [];

                while (expressionStack.Peek() is string &&
                       !(ruleOrFormulaTokens.Count > 0 &&
                         string.Equals("-", ruleOrFormulaTokens[0]) &&
                         IsRuleOrFormulaComplete(ruleOrFormulaTokens, tokenIndex, _expressionTokens)))
                {
                    string poppedExpressionItem = (expressionStack.Pop() as string)!;
                    if (poppedExpressionItem is "(" or ",")
                    {
                        if (expressionStack.Count > 0 &&
                            expressionStack.Peek() is IMethodWrapper &&
                            poppedExpressionItem == "(")
                            expressionStack.Push(poppedExpressionItem);
                        break;
                    }
                    else
                    {
                        ruleOrFormulaTokens.Insert(0, poppedExpressionItem);
                    }
                }

                if (methodState is not null && methodState.InRule)
                {
                    Rules.Engine rulesEngine = new();
                    rulesEngine.Parameters.Add(_parameters);
                    Rules.RuleTree rules = rulesEngine.Parse(ruleOrFormulaTokens)!;
                    expressionStack.Push(new GenericValue<bool>(rules.Evaluate()));
                    methodState.InRule = false;
                }
                else
                {
                    decimal? formulaVal = 0;

                    if (ruleOrFormulaTokens is { Count: > 0 })
                    {
                        if (ruleOrFormulaTokens.Count == 1 && ruleOrFormulaTokens[0].ToLower() == "null")
                        {
                            expressionStack.Push(new GenericValue<decimal?>(null));
                        }
                        else
                        {
                            formulaVal = EvaluateFormula(ruleOrFormulaTokens);

                            if (token == "," ||
                                (token == ")" && methodState is not null && methodState.IsReadyForExecution) ||
                                (token == ")" && methodState is null))
                                expressionStack.Push(new GenericValue<decimal?>(formulaVal));
                            else
                                expressionStack.Push(formulaVal.ToString());
                        }
                    }
                }

                if (!isSimplified) isSimplified = true;

                if (token == "," && methodState is not null && methodState.IsInMethod(expressionStack))
                    methodState.AddParameterDelimiter(',', expressionStack.Peek());

                if ((methodState is not null && !methodState.IsReadyForExecution) ||
                    (methodState is null && token == ")"))
                    continue;
            }

            // Method invocation
            if (methodState is not null && methodState.IsReadyForExecution)
            {
                methodparameters = new System.Collections.ArrayList();
                while (expressionStack.Peek() is not IMethodWrapper)
                {
                    var expressionStackItem = expressionStack.Pop();
                    if (expressionStackItem is string str && str == "(")
                        break;

                    if (expressionStackItem is not IValue)
                    {
                        if (decimal.TryParse(expressionStackItem as string, out decimal expressionVal))
                            expressionStackItem = new GenericValue<decimal>(expressionVal);
                    }

                    methodparameters.Insert(0, ((IValue)expressionStackItem).Value);
                }

                methodToExecute = expressionStack.Pop() as IMethodWrapper;
                var methodResult = methodToExecute!.ExecuteMethod(methodparameters.ToArray());
                if (expressionStack.Count > 0 && Helper.IsMathOperator(expressionStack.Peek() as string))
                    expressionStack.Push(methodResult?.ToString());
                else
                    expressionStack.Push(new GenericValue<object>(methodResult!));

                methodState = methodStateTracker.Count > 0 ? methodStateTracker.Pop() : null;
                continue;
            }

            if (Helper.IsMathOperator(token) && expressionStack.Peek() is IValue nextExpressionStackValue)
            {
                if (nextExpressionStackValue.Type != typeof(bool))
                    expressionStack.Push(((IValue)expressionStack.Pop()).Value.ToString());
            }

            if (expressionStack.Count == 0 || !(expressionStack.Peek() is IValue && token == ","))
                expressionStack.Push(token);
        }

        // Final evaluation if not yet simplified
        if (!isSimplified || expressionStack.Count > 1)
        {
            ruleOrFormulaTokens = [];
            while (expressionStack.Count > 0)
            {
                object expressionItem = expressionStack.Pop();
                string? formulaItem;
                if (expressionItem is string s)
                    formulaItem = s;
                else if (expressionItem is null)
                    formulaItem = "null";
                else
                    formulaItem = ((IValue)expressionItem).Value.ToString();

                ruleOrFormulaTokens.Insert(0, formulaItem!);
            }
            expressionStack.Push(new GenericValue<decimal?>(EvaluateFormula(ruleOrFormulaTokens)));
        }

        return TypeHelper.Convert<T>(((IValue)expressionStack.Pop()).Value)!;
    }

    private bool IsRuleOrFormulaComplete(List<string> ruleOrFormulaTokens, int tokenIndex, List<string> allTokens)
    {
        if (ruleOrFormulaTokens is null or { Count: 0 } || allTokens is null or { Count: 0 })
            return false;

        int lastCommaPosition = FindLastIndexOf(tokenIndex, ",", allTokens);
        return string.Equals(allTokens[lastCommaPosition + 1], ruleOrFormulaTokens[0]);
    }

    private static int FindLastIndexOf(int start, string match, List<string> collection)
    {
        int foundPosition = start;
        while (foundPosition >= 0)
        {
            if (string.Equals(collection[foundPosition], match)) break;
            foundPosition--;
        }
        return foundPosition;
    }

    private static bool IsInMethodConstruct(MethodState? methodState) =>
        methodState is not null && methodState.IsTrackingParenthesis;

    private decimal? EvaluateFormula(List<string> formulaTokens)
    {
        Formulae.Engine formulaEngine = new();
        formulaEngine.Parameters.Add(_parameters);
        Formulae.FormulaTree formulae = formulaEngine.Parse(formulaTokens)!;
        return formulae.Evaluate();
    }
}
