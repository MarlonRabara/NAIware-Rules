using NAIware.Core;
using NAIware.Core.Text;

namespace NAIware.Rules;

/// <summary>
/// A factory class for creating parameters, values, operators, and expressions.
/// </summary>
public class Factory
{
    /// <summary>Generates a parameter by type full name.</summary>
    public static IParameter GenerateParameter(string name, string description, string typeFullName)
    {
        Type paramtype = Type.GetType(typeFullName)!;
        return GenerateParameter(name, description, paramtype);
    }

    /// <summary>Generates a parameter by type.</summary>
    public static IParameter GenerateParameter(string name, string description, Type parameterType)
    {
        IParameter? parameter = null;

        if (parameterType == typeof(int))
            parameter = new GenericParameter<int>(name, description);
        if (parameterType == typeof(double))
            parameter = new GenericParameter<double>(name, description);
        if (parameterType == typeof(string))
            parameter = new GenericParameter<string>(name, description);
        if (parameterType == typeof(bool))
            parameter = new GenericParameter<bool>(name, description);
        if (parameterType == typeof(DateTime))
            parameter = new GenericParameter<DateTime>(name, description);

        return parameter!;
    }

    /// <summary>Gets a value instance from a type name and string representation.</summary>
    public static IValue GetValue(string type, string? valueString)
    {
        IValue? value = null;

        try
        {
            if (valueString?.ToLower() is null)
            {
                if (type.Contains("System.Int32"))
                    value = new GenericValue<int?>(null);
                else if (type.Contains("System.Double"))
                    value = new GenericValue<double?>(null);
                else if (type == "System.String")
                    value = new GenericValue<string>(null!);
                else if (type.Contains("System.Boolean"))
                    value = new GenericValue<bool?>(null);
                else if (type.Contains("System.DateTime"))
                    value = new GenericValue<DateTime?>(null);
                else
                    value = new GenericValue<decimal?>(null);
            }
            else
            {
                switch (type)
                {
                    case "System.Int32":
                        value = new GenericValue<int>(Convert.ToInt32(valueString));
                        break;
                    case "System.Decimal":
                        decimal decval = decimal.Parse(
                            valueString,
                            System.Globalization.NumberStyles.AllowCurrencySymbol |
                            System.Globalization.NumberStyles.AllowThousands |
                            System.Globalization.NumberStyles.AllowDecimalPoint |
                            System.Globalization.NumberStyles.AllowLeadingSign);
                        value = new GenericValue<decimal>(decval);
                        break;
                    case "System.Double":
                        value = new GenericValue<double>(Convert.ToDouble(valueString));
                        break;
                    case "System.String":
                        if (!string.IsNullOrEmpty(valueString) &&
                            valueString.Length > 1 &&
                            valueString[0] == '"' &&
                            valueString[^1] == '"')
                        {
                            valueString = valueString.Length == 2
                                ? string.Empty
                                : valueString[1..^1];
                        }
                        value = new GenericValue<string>(valueString);
                        break;
                    case "System.Boolean":
                        value = new GenericValue<bool>(Convert.ToBoolean(valueString));
                        break;
                    case "System.DateTime":
                        if (value is null)
                        {
                            if (valueString.EndsWith('#'))
                                valueString = valueString[1..^1];
                            value = new GenericValue<DateTime>(Convert.ToDateTime(valueString));
                        }
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Invalid operation processing {type} in {valueString}.", ex);
        }

        return value!;
    }

    /// <summary>Gets a simple expression by auto-detecting the operator type.</summary>
    public static IExpression<R>? GetSimpleExpression<R>(IEngine engine, string leftHandSide, string expressionOp, string rightHandSide)
    {
        IOperator? op = GetOperator(expressionOp);

        if (op is ComparisonOperator compOp)
            return GetSimpleExpression<ComparisonOperator, R>(engine, leftHandSide, compOp, rightHandSide);

        if (op is LogicalOperator logOp)
            return GetSimpleExpression<LogicalOperator, R>(engine, leftHandSide, logOp, rightHandSide);

        return null;
    }

    /// <summary>Gets an operator instance from the specified string.</summary>
    public static IOperator? GetOperator(string expressionOp)
    {
        if (string.IsNullOrEmpty(expressionOp)) return null;

        return expressionOp.ToLower() switch
        {
            "!=" or "<>" or ">" or "<" or ">=" or "<=" or "=" => new ComparisonOperator(expressionOp),
            "and" or "or" or "&&" or "||" => new LogicalOperator(expressionOp),
            _ => null
        };
    }

    /// <summary>Gets an expression from arbitrary left/right operands and a typed operator.</summary>
    public static IExpression<R>? GetExpression<OP, R>(IEngine engine, object leftHandSide, OP expOp, object rightHandSide)
        where OP : IOperator
    {
        if (leftHandSide is string lStr && rightHandSide is string rStr)
            return GetSimpleExpression<OP, R>(engine, lStr, expOp, rStr);

        IValue? val;
        IExpression<R>? expression = null;

        if (leftHandSide is string ls && rightHandSide is ISimpleExpression<R>)
        {
            val = StringToValue(engine, ls);
            expression = CreateExpressionByType<OP, R>(val, expOp, rightHandSide);
        }
        else if (leftHandSide is ISimpleExpression<R> && rightHandSide is string rs)
        {
            val = StringToValue(engine, rs);
            expression = CreateExpressionByType<OP, R>(leftHandSide, expOp, val);
        }
        else if (leftHandSide is IComplexExpression<R> && rightHandSide is string rs2)
        {
            return GetExpression<OP, R>(engine, leftHandSide, expOp,
                GetExpression<MathOperator, decimal>(engine, rs2, new MathOperator("*"), "1")!);
        }
        else if (leftHandSide is string ls2 && rightHandSide is IComplexExpression<R>)
        {
            return GetExpression<OP, R>(engine,
                GetExpression<MathOperator, decimal>(engine, ls2, new MathOperator("*"), "1")!, expOp, rightHandSide);
        }
        else if (leftHandSide is ISimpleExpression<R> simpleLhs && rightHandSide is ISimpleExpression<R> simpleRhs)
        {
            expression = new ComplexExpression<OP, R>(simpleLhs, expOp, simpleRhs);
        }
        else if (leftHandSide is IComplexExpression<R> complexLhs && rightHandSide is ISimpleExpression<R> simpleRhs2)
        {
            expression = new ComplexExpression<OP, R>(complexLhs, expOp, simpleRhs2);
        }
        else if (leftHandSide is ISimpleExpression<R> simpleLhs2 && rightHandSide is IComplexExpression<R> complexRhs)
        {
            expression = new ComplexExpression<OP, R>(simpleLhs2, expOp, complexRhs);
        }
        else if (leftHandSide is IComplexExpression<R> complexLhs2 && rightHandSide is IComplexExpression<R> complexRhs2)
        {
            expression = new ComplexExpression<OP, R>(complexLhs2, expOp, complexRhs2);
        }

        return expression;
    }

    private static IExpression<R>? CreateExpressionByType<OP, R>(object? leftOperand, OP expOp, object? rightOperand)
        where OP : IOperator
    {
        IValue? val = leftOperand as IValue ?? rightOperand as IValue;
        if (val is null) return null;

        string? typeName = val is IValue valTyped
            ? Helper.ExtractType(valTyped.Type).FullName
            : null;

        // Resolve operands: prefer IValue cast, fall back to ISimpleExpression<R> as IValue.
        IValue lhs = (leftOperand as IValue ?? leftOperand as ISimpleExpression<R> as IValue)!;
        IValue rhs = (rightOperand as IValue ?? rightOperand as ISimpleExpression<R> as IValue)!;

        // Handle null/nullable fallback
        if (typeName is null && val is not null && TypeHelper.IsNullable(val.Type))
        {
            return (IExpression<R>)new SimpleExpression<decimal?, OP, R>(lhs, expOp, rhs);
        }

        return typeName switch
        {
            "System.Int32" => (IExpression<R>)new SimpleExpression<int, OP, R>(lhs, expOp, rhs),
            "System.Double" => (IExpression<R>)new SimpleExpression<double, OP, R>(lhs, expOp, rhs),
            "System.Decimal" => (IExpression<R>)new SimpleExpression<decimal, OP, R>(lhs, expOp, rhs),
            "System.String" => (IExpression<R>)new SimpleExpression<string, OP, R>(lhs, expOp, rhs),
            "System.Boolean" => (IExpression<R>)new SimpleExpression<bool, OP, R>(lhs, expOp, rhs),
            "System.DateTime" => (IExpression<R>)new SimpleExpression<DateTime, OP, R>(lhs, expOp, rhs),
            _ => null
        };
    }

    private static IValue? StringToValue(IEngine engine, string operandString)
    {
        IParameter? parameter = null;
        IValue? val = null;

        if (engine?.Parameters is not null && engine.Parameters.ContainsKey(operandString))
            parameter = engine.Parameters[operandString];

        val = parameter;

        Type? operandType = val?.Type;

        if (val is null && operandType is not null)
            val = GetValue(operandType.FullName!, operandString);

        if (val is null && !string.Equals(operandString?.ToLower(), "null"))
            val = GetValue(typeof(decimal).FullName!, operandString);

        if (val is null && string.Equals(operandString?.ToLower(), "null"))
            val = GetValue(typeof(decimal?).FullName!, operandString);

        return val;
    }

    /// <summary>Gets a simple expression with typed operator from string operands.</summary>
    public static IExpression<R>? GetSimpleExpression<OP, R>(IEngine engine, string leftHandSide, OP expOp, string rightHandSide)
        where OP : IOperator
    {
        IExpression<R>? simplerule = null;
        IParameter? lhparam = null;
        IParameter? rhparam = null;
        bool isExpressionEnumerated;

        if (engine.Parameters.ContainsKey(leftHandSide))
            lhparam = engine.Parameters[leftHandSide];
        if (engine.Parameters.ContainsKey(rightHandSide))
            rhparam = engine.Parameters[rightHandSide];

        IValue? rvalue = rhparam;
        IValue? lvalue = lhparam;
        Type ruletype = rvalue is null
            ? (lvalue is null ? null! : Helper.ExtractType(lvalue.Type))
            : Helper.ExtractType(rvalue.Type);

        if (ruletype is null)
            ruletype = typeof(decimal);

        isExpressionEnumerated = (rhparam is not null && rhparam.IsEnumerated) || (lhparam is not null && lhparam.IsEnumerated);

        if (isExpressionEnumerated)
        {
            if (rhparam is not null)
                lvalue = rhparam.GetEnumeratedValue(leftHandSide);
            if (lhparam is not null)
                rvalue = lhparam.GetEnumeratedValue(rightHandSide);
        }

        if (StringHelper.IsValidVariable(rightHandSide) && rhparam is null && rvalue is null)
        {
            rvalue = GetValue("System.Decimal", "0");
        }
        else
        {
            rvalue ??= GetValue(ruletype.FullName!, rightHandSide);
        }

        if (StringHelper.IsValidVariable(leftHandSide) && lhparam is null && lvalue is null)
        {
            lvalue = GetValue("System.Decimal", "0");
        }
        else
        {
            lvalue ??= GetValue(ruletype.FullName!, leftHandSide);
        }

        switch (ruletype.FullName)
        {
            case "System.Int32":
                simplerule = (IExpression<R>)new SimpleExpression<int, OP, R>(lvalue!, expOp, rvalue!);
                break;
            case "System.Double":
                simplerule = (IExpression<R>)new SimpleExpression<double, OP, R>(lvalue!, expOp, rvalue!);
                break;
            case "System.Decimal":
                simplerule = (IExpression<R>)new SimpleExpression<decimal, OP, R>(lvalue!, expOp, rvalue!);
                break;
            case "System.String":
                simplerule = (IExpression<R>)new SimpleExpression<string, OP, R>(lvalue!, expOp, rvalue!);
                break;
            case "System.Boolean":
                simplerule = (IExpression<R>)new SimpleExpression<bool, OP, R>(lvalue!, expOp, rvalue!);
                break;
            case "System.DateTime":
                simplerule = (IExpression<R>)new SimpleExpression<DateTime, OP, R>(lvalue!, expOp, rvalue!);
                break;
        }

        if (simplerule is null && isExpressionEnumerated)
        {
            simplerule = (IExpression<R>)new SimpleExpression<int, OP, R>(
                new GenericValue<int>(Convert.ToInt32(lvalue!.Value)),
                expOp,
                new GenericValue<int>(Convert.ToInt32(rvalue!.Value)));
        }

        return simplerule;
    }

    /// <summary>Creates a complex expression from two typed expressions.</summary>
    public static IExpression<R> GetComplexExpression<OP, R>(IExpression<R> leftHandExpression, OP expressionOperator, IExpression<R> rightHandExpression)
        where OP : IOperator
    {
        return new ComplexExpression<OP, R>(leftHandExpression, expressionOperator, rightHandExpression);
    }
}
