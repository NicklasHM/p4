using RAL.AST;

namespace RAL.Interpreter;

/*
Expression interpreter for arithmetic and boolean expressions.

This interpreter evaluates AST expression nodes into runtime values.
It currently supports:
- Number, boolean, and string literals
- Arithmetic operators: +, -, *, /
- Numeric comparisons: <, >, <=, >=
- Equality operators: ==, !=
- Boolean operators: and, or, not

The interpreter assumes that the type checker has already accepted the program.
Runtime checks are still used for cases such as division by zero.
*/

public class Interpreter
{
    private const float Epsilon = 0.00001f;

    Value EvalExp(Exp exp)
    {
        return exp switch
        {
            NumberV n => new NumberVal(n.Value),
            BoolV b => new BoolVal(b.Value),
            StringV s => new StringVal(s.Value),

            UnaryOperation u => EvalUnary(u),
            BinaryOperation b => EvalBinary(b),

            _ => throw new Exception($"Line {exp.LineNumber}: Unsupported expression.")
        };
    }

    private Value EvalUnary(UnaryOperation exp)
    {
        Value value = EvalExp(exp.Expression);

        return exp.Operator switch
        {
            UnaryOperator.NOT when value is BoolVal b
                => new BoolVal(!b.Value),

            _ => throw new Exception($"Line {exp.LineNumber}: Invalid unary operation.")
        };
    }

    private Value EvalBinary(BinaryOperation exp)
    {
        Value left = EvalExp(exp.LeftExpression);

        // RightExpression is nullable in the AST, but binary operations require it.
        if (exp.RightExpression == null)
            throw new Exception($"Line {exp.LineNumber}: Missing right-hand expression.");

        Value right = EvalExp(exp.RightExpression);

        return exp.Operator switch
        {
            BinaryOperator.ADD when left is NumberVal l && right is NumberVal r
                => new NumberVal(l.Value + r.Value),

            BinaryOperator.SUB when left is NumberVal l && right is NumberVal r
                => new NumberVal(l.Value - r.Value),

            BinaryOperator.MUL when left is NumberVal l && right is NumberVal r
                => new NumberVal(l.Value * r.Value),

            BinaryOperator.DIV when left is NumberVal l && right is NumberVal r
                => DivideNumbers(l, r, exp.LineNumber),

            BinaryOperator.LT when left is NumberVal l && right is NumberVal r
                => new BoolVal(l.Value < r.Value),

            BinaryOperator.GT when left is NumberVal l && right is NumberVal r
                => new BoolVal(l.Value > r.Value),

            BinaryOperator.LTEQ when left is NumberVal l && right is NumberVal r
                => new BoolVal(l.Value <= r.Value),

            BinaryOperator.GTEQ when left is NumberVal l && right is NumberVal r
                => new BoolVal(l.Value >= r.Value),

            BinaryOperator.EQ when left is NumberVal l && right is NumberVal r
                => new BoolVal(NumberEquals(l.Value, r.Value)),

            BinaryOperator.NEQ when left is NumberVal l && right is NumberVal r
                => new BoolVal(!NumberEquals(l.Value, r.Value)),

            BinaryOperator.EQ when left is BoolVal l && right is BoolVal r
                => new BoolVal(l.Value == r.Value),

            BinaryOperator.NEQ when left is BoolVal l && right is BoolVal r
                => new BoolVal(l.Value != r.Value),

            BinaryOperator.EQ when left is StringVal l && right is StringVal r
                => new BoolVal(l.Value == r.Value),

            BinaryOperator.NEQ when left is StringVal l && right is StringVal r
                => new BoolVal(l.Value != r.Value),

            BinaryOperator.AND when left is BoolVal l && right is BoolVal r
                => new BoolVal(l.Value && r.Value),

            BinaryOperator.OR when left is BoolVal l && right is BoolVal r
                => new BoolVal(l.Value || r.Value),

            _ => throw new Exception($"Line {exp.LineNumber}: Invalid binary operation.")
        };
    }

    private static NumberVal DivideNumbers(NumberVal left, NumberVal right, int lineNumber)
    {
        CheckDivisionByZero(right.Value, lineNumber);
        return new NumberVal(left.Value / right.Value);
    }

    private static bool NumberEquals(float a, float b)
    {
        // Floating point numbers should not be compared using exact equality.
        return Math.Abs(a - b) < Epsilon;
    }

    private static void CheckDivisionByZero(float value, int lineNumber)
    {
        // Because Number is represented as float, zero is also checked using epsilon.
        if (NumberEquals(value, 0f))
            throw new Exception($"Line {lineNumber}: Division by zero.");
    }
}