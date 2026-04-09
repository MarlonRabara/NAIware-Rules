using System.Text.RegularExpressions;

namespace NAIware.Rules;

/// <summary>
/// A class containing helper methods for expression validation, tokenization, and math operations.
/// </summary>
public class Helper
{
    /// <summary>
    /// Performs an individual character analysis of the expression.
    /// </summary>
    /// <param name="userDefinedExpression">The expression to validate.</param>
    /// <returns>The cleaned expression.</returns>
    public static string ValidateExpression(string userDefinedExpression)
    {
        string expression = string.IsNullOrEmpty(userDefinedExpression)
            ? userDefinedExpression
            : userDefinedExpression.Replace("\r\n", string.Empty).Replace("\r", string.Empty).Replace("\n", string.Empty);

        List<CharacterTracker> invalidCharTracker = [];
        if (string.IsNullOrEmpty(expression))
            throw new InvalidOperationException("The rule or formula expression is not specified (null or empty).");

        var parenthesisStack = new Stack<CharacterTracker>();
        var dateStack = new Stack<CharacterTracker>();

        for (int i = 0; i < expression.Length; i++)
        {
            var currentChar = new CharacterTracker(expression[i], i);
            if (!currentChar.IsValid)
            {
                invalidCharTracker.Add(currentChar);
                continue;
            }

            if (currentChar.Character is '(' or ')')
            {
                if (currentChar.Character == '(')
                {
                    parenthesisStack.Push(currentChar);
                    continue;
                }

                if (currentChar.Character == ')')
                {
                    if (parenthesisStack.Count == 0)
                    {
                        invalidCharTracker.Add(currentChar);
                        continue;
                    }
                    else
                    {
                        parenthesisStack.Pop();
                        continue;
                    }
                }
            }

            if (currentChar.Character == '#')
            {
                if (dateStack.Count == 0)
                    dateStack.Push(currentChar);
                else
                    dateStack.Pop();
                continue;
            }
        }

        while (parenthesisStack.Count > 0)
            invalidCharTracker.Add(parenthesisStack.Pop());

        while (dateStack.Count > 0)
            invalidCharTracker.Add(dateStack.Pop());

        if (invalidCharTracker.Count > 0)
            throw new ParsingException(invalidCharTracker, expression);

        return expression;
    }

    /// <summary>
    /// Tokenizes an expression and returns tokens in a list.
    /// </summary>
    /// <param name="expression">The expression to parse.</param>
    /// <returns>A list of tokens within the expression.</returns>
    public static List<string> GetTokens(string expression)
    {
        string regex = @"(\#[^#]+\#)|([\(\)])|(<>)|([<>!]=)|([=<>])|([&|]{2})|(""[^""]*"")|(and)|(or)|([/\+\-\*\,])|([a-zA-Z]+[\.\w]*)|(\$[+-]?[\d,]*\.?\d{0,2})";
        var regexobj = new Regex(regex);
        string[] temptokens = regexobj.Split(expression);

        List<string> tokens = [];
        for (int i = 0; temptokens is not null && i < temptokens.Length; i++)
        {
            if (temptokens[i] is not null && temptokens[i].Trim() != string.Empty)
            {
                if (temptokens[i] == "&&") temptokens[i] = "and";
                if (temptokens[i] == "||") temptokens[i] = "or";
                if (temptokens[i].Trim() == "\"" && i + 2 < temptokens.Length && temptokens[i + 2].Trim() == "\"")
                {
                    tokens.Add($"{temptokens[i].Trim()}{temptokens[i + 1]}{temptokens[i + 2].Trim()}");
                    i += 2;
                }
                else
                {
                    tokens.Add(temptokens[i].Trim());
                }
            }
        }

        return tokens;
    }

    /// <summary>
    /// Extracts the underlying type from a nullable type.
    /// </summary>
    public static Type ExtractType(Type valueType)
    {
        if (valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(Nullable<>))
            return Nullable.GetUnderlyingType(valueType)!;
        else
            return valueType;
    }

    /// <summary>
    /// Determines whether the type is a nullable type.
    /// </summary>
    public static bool IsNullable(Type anyType) =>
        anyType.IsGenericType && anyType.GetGenericTypeDefinition() == typeof(Nullable<>);

    /// <summary>
    /// Performs simple math based on the supplied operator.
    /// </summary>
    public static decimal? SimpleMath(char op, decimal? rightValParameter, decimal? leftValParameter)
    {
        if (rightValParameter is null || leftValParameter is null)
            return null;

        decimal rightVal = rightValParameter.Value;
        decimal leftVal = leftValParameter.Value;

        return op switch
        {
            '*' => decimal.Multiply(rightVal, leftVal),
            '/' => decimal.Divide(leftVal, rightVal),
            '+' => decimal.Add(rightVal, leftVal),
            '-' => leftVal - rightVal,
            _ => 0m
        };
    }

    /// <summary>
    /// Determines whether the string is a math operator.
    /// </summary>
    public static bool IsMathOperator(string? mathOperator)
    {
        if (string.IsNullOrEmpty(mathOperator)) return false;

        return mathOperator[0] switch
        {
            '*' or '/' or '+' or '-' => true,
            _ => false
        };
    }
}
