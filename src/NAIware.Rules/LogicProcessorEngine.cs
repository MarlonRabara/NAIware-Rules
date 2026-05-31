using NAIware.Core;

namespace NAIware.Rules;

/// <summary>
/// A logic processor engine that evaluates complex expressions mixing method calls, rules
/// (boolean comparisons/logical operators) and formulae (arithmetic) in a single pass.
/// </summary>
/// <remarks>
/// <para>
/// Evaluation is a single left-to-right scan over the tokenized expression that drives an operand
/// <see cref="System.Collections.Stack"/>. The engine is a hybrid: arithmetic sub-spans are delegated
/// to <see cref="Formulae.Engine"/>, boolean sub-spans are delegated to <see cref="Rules.Engine"/>,
/// and method calls are dispatched through the supplied <see cref="MethodMap"/>. Because the three
/// sub-languages are interleaved, the engine cannot simply parse-then-evaluate; instead it tracks the
/// active method-call frame and parenthetical nesting with a <see cref="MethodState"/> stack so it can
/// decide, at each delimiter (<c>,</c> / <c>)</c>), whether the just-completed span is a rule, a formula,
/// or a method argument, and collapse it into a single <see cref="IValue"/> on the operand stack.
/// </para>
/// <para>
/// The class is not thread-safe and is intended to be used once per expression instance.
/// </para>
/// </remarks>
public sealed class LogicProcessorEngine
{
    #region Private Classes

    /// <summary>
    /// Tracks the state of a single in-flight method call (or parenthetical grouping) while the
    /// outer expression is being scanned. One instance exists per nested call frame; frames are
    /// pushed/popped on a stack by the engine as <c>(</c> and <c>)</c> tokens are encountered.
    /// </summary>
    /// <remarks>
    /// It records parenthesis balance (to know when the call's argument list is complete and ready
    /// to execute), the number of comma-separated arguments seen so far, and whether the current
    /// span is a boolean rule (so the engine can route it to the rule engine rather than the
    /// formula engine).
    /// </remarks>
    private class MethodState
    {
        private bool _isReadyForExecution;
        private Stack<char>? _parenthesisTracker;
        private Stack<char>? _parameterTracker;
        private object? _lastParameterExpression;

        /// <summary>
        /// Gets whether this frame currently has an unbalanced open parenthesis, i.e. the scan is
        /// still inside the method's argument list / grouping.
        /// </summary>
        public bool IsTrackingParenthesis => _parenthesisTracker is not null && _parenthesisTracker.Count > 0;

        /// <summary>
        /// Determines whether this frame's method is still present on the operand stack, meaning the
        /// engine is positioned inside the body of this specific method call.
        /// </summary>
        /// <param name="expressionStack">The engine's live operand stack.</param>
        /// <returns><see langword="true"/> if this frame's method reference is on the stack.</returns>
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

        /// <summary>
        /// Records a parenthesis token from a string, updating the open/close balance for this frame.
        /// </summary>
        public void AddParenthesis(string parenthesis)
        {
            if (!string.IsNullOrEmpty(parenthesis))
                AddParenthesis(parenthesis[0]);
        }

        /// <summary>
        /// Records a parenthesis character, updating the balance. A matching <c>)</c> pops the
        /// corresponding <c>(</c>; when the balance returns to zero the frame is flagged as ready to
        /// execute via <see cref="IsReadyForExecution"/>.
        /// </summary>
        public void AddParenthesis(char parenthesis)
        {
            _parenthesisTracker ??= new Stack<char>();

            if (parenthesis == ')' && _parenthesisTracker.Peek() == '(')
                _parenthesisTracker.Pop();
            else
                _parenthesisTracker.Push(parenthesis);

            _isReadyForExecution = parenthesis == ')' && _parenthesisTracker.Count == 0;
        }

        /// <summary>
        /// Records that an argument separator (<c>,</c>) was seen, incrementing the detected argument
        /// count and remembering the last completed argument expression so the engine can recognize it.
        /// </summary>
        public void AddParameterDelimiter(char parameterDelimiter, object lastExpression)
        {
            _parameterTracker ??= new Stack<char>();
            _parameterTracker.Push(parameterDelimiter);
            _lastParameterExpression = lastExpression;
        }

        /// <summary>
        /// Determines whether the supplied operand matches the most recently completed argument,
        /// comparing both by reference/value and by string form.
        /// </summary>
        public bool IsLastExpression(object any)
        {
            if (Equals(_lastParameterExpression, any)) return true;
            return _lastParameterExpression is not null && Equals(_lastParameterExpression.ToString(), any);
        }

        /// <summary>Gets the number of comma-separated arguments detected for this method call so far.</summary>
        public int DetectedParameterCount => _parameterTracker is null ? 0 : _parameterTracker.Count + 1;

        /// <summary>Gets whether the argument list is balanced and the method is ready to be invoked.</summary>
        public bool IsReadyForExecution => _isReadyForExecution;

        /// <summary>Gets or sets whether the current span is a boolean rule rather than an arithmetic formula.</summary>
        public bool InRule { get; set; }

        /// <summary>Gets or sets the method wrapper this frame represents.</summary>
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

    /// <summary>
    /// Evaluates the expression and converts the final result to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The desired result type. The collapsed operand value is coerced via <see cref="TypeHelper"/>.</typeparam>
    /// <param name="parameterSourceObjects">Optional source objects (reserved for parameter binding scenarios).</param>
    /// <returns>The evaluated result coerced to <typeparamref name="T"/>.</returns>
    /// <remarks>
    /// <para>
    /// The algorithm is a single left-to-right scan over the tokens that maintains an operand stack
    /// and a stack of <see cref="MethodState"/> frames. For each token it performs, in order:
    /// </para>
    /// <list type="number">
    ///   <item><description>Method recognition — a known <see cref="MethodMap"/> entry opens a new call frame.</description></item>
    ///   <item><description>Rule detection — comparison/logical operators mark the current span as a boolean rule.</description></item>
    ///   <item><description>Parenthesis tracking — keeps the active frame's open/close balance up to date.</description></item>
    ///   <item><description>Span collapse — at a <c>,</c>, a closing <c>)</c>, or when a method is ready, the
    ///     contiguous run of string tokens on the stack is popped and evaluated either as a rule
    ///     (via <see cref="Rules.Engine"/>) or a formula (via <see cref="EvaluateFormula"/>), pushing a single value.</description></item>
    ///   <item><description>Method invocation — once a frame is balanced, its arguments are popped and the method executed.</description></item>
    /// </list>
    /// <para>
    /// After the scan, any residual multi-token expression on the stack is evaluated as a final formula,
    /// and the single remaining operand is returned.
    /// </para>
    /// </remarks>
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
                    methodState.AddParameterDelimiter(',', expressionStack.Peek()!);

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

                    methodparameters.Insert(0, ((IValue)expressionStackItem!).Value);
                }

                methodToExecute = expressionStack.Pop() as IMethodWrapper;
                var methodResult = methodToExecute!.ExecuteMethod(methodparameters.ToArray()!);
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
                    expressionStack.Push(((IValue)expressionStack.Pop()!).Value.ToString());
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
                object? expressionItem = expressionStack.Pop();
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

        return TypeHelper.Convert<T>(((IValue)expressionStack.Pop()!).Value)!;
    }

    /// <summary>
    /// Determines whether the tokens accumulated so far form a complete rule/formula span by checking
    /// that they begin immediately after the most recent argument separator. Used to avoid prematurely
    /// collapsing a unary-minus span that is still being built.
    /// </summary>
    private bool IsRuleOrFormulaComplete(List<string> ruleOrFormulaTokens, int tokenIndex, List<string> allTokens)
    {
        if (ruleOrFormulaTokens is null or { Count: 0 } || allTokens is null or { Count: 0 })
            return false;

        int lastCommaPosition = FindLastIndexOf(tokenIndex, ",", allTokens);
        return string.Equals(allTokens[lastCommaPosition + 1], ruleOrFormulaTokens[0]);
    }

    /// <summary>Scans backwards from <paramref name="start"/> for the last index of <paramref name="match"/>.</summary>
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

    /// <summary>Determines whether the engine is currently inside a method's parenthetical construct.</summary>
    private static bool IsInMethodConstruct(MethodState? methodState) =>
        methodState is not null && methodState.IsTrackingParenthesis;

    /// <summary>
    /// Delegates an arithmetic token span to the formula <see cref="Formulae.Engine"/> and returns the
    /// computed decimal result, sharing this engine's parameters with the sub-engine.
    /// </summary>
    private decimal? EvaluateFormula(List<string> formulaTokens)
    {
        Formulae.Engine formulaEngine = new();
        formulaEngine.Parameters.Add(_parameters);
        Formulae.FormulaTree formulae = formulaEngine.Parse(formulaTokens)!;
        return formulae.Evaluate();
    }
}
