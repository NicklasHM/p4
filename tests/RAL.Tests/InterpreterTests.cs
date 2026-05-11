using RAL.AST;
using RAL.Interpreter;

namespace RAL.Tests;

/*
 * Tests for the Interpreter expression evaluator.
 * All tests construct AST nodes directly — the parser is not involved.
 * The Interpreter assumes the TypeChecker has already accepted the program,
 * so only well-typed inputs are used here (except the division-by-zero test).
 */
public class InterpreterTests
{
    // ── Literals ──────────────────────────────────────────────────────────────

    [Fact]
    public void NumberLiteral_EvaluatesToNumberVal()
    {
        Value result = TestHelpers.EvalExpression(new NumberV(1, 42f), new EnvV(), new EnvH());
        var nv = Assert.IsType<NumberVal>(result);
        Assert.Equal(42f, nv.Value);
    }

    [Fact]
    public void BoolLiteralTrue_EvaluatesToBoolValTrue()
    {
        Value result = TestHelpers.EvalExpression(new BoolV(1, true), new EnvV(), new EnvH());
        var bv = Assert.IsType<BoolVal>(result);
        Assert.True(bv.Value);
    }

    [Fact]
    public void BoolLiteralFalse_EvaluatesToBoolValFalse()
    {
        Value result = TestHelpers.EvalExpression(new BoolV(1, false),  new EnvV(), new EnvH());
        var bv = Assert.IsType<BoolVal>(result);
        Assert.False(bv.Value);
    }

    [Fact]
    public void StringLiteral_EvaluatesToStringVal()
    {
        Value result = TestHelpers.EvalExpression(new StringV(1, "hi"),  new EnvV(), new EnvH());
        var sv = Assert.IsType<StringVal>(result);
        Assert.Equal("hi", sv.Value);
    }

    // ── Arithmetic ────────────────────────────────────────────────────────────

    [Fact]
    public void Addition_TwoNumbers_ReturnsCorrectSum()
    {
        // 2 + 3 = 5
        var exp = new BinaryOperation(1, new NumberV(1, 2), BinaryOperator.ADD, new NumberV(1, 3));
        var result = Assert.IsType<NumberVal>(TestHelpers.EvalExpression(exp, new EnvV(), new EnvH()));
        Assert.Equal(5f, result.Value, precision: 4);
    }

    [Fact]
    public void Subtraction_TwoNumbers_ReturnsCorrectDifference()
    {
        // 10 - 4 = 6
        var exp = new BinaryOperation(1, new NumberV(1, 10), BinaryOperator.SUB, new NumberV(1, 4));
        var result = Assert.IsType<NumberVal>(TestHelpers.EvalExpression(exp, new EnvV(), new EnvH()));
        Assert.Equal(6f, result.Value, precision: 4);
    }

    [Fact]
    public void Multiplication_TwoNumbers_ReturnsCorrectProduct()
    {
        // 3 * 4 = 12
        var exp = new BinaryOperation(1, new NumberV(1, 3), BinaryOperator.MUL, new NumberV(1, 4));
        var result = Assert.IsType<NumberVal>(TestHelpers.EvalExpression(exp, new EnvV(), new EnvH()));
        Assert.Equal(12f, result.Value, precision: 4);
    }

    [Fact]
    public void Division_TwoNumbers_ReturnsCorrectQuotient()
    {
        // 10 / 4 = 2.5
        var exp = new BinaryOperation(1, new NumberV(1, 10), BinaryOperator.DIV, new NumberV(1, 4));
        var result = Assert.IsType<NumberVal>(TestHelpers.EvalExpression(exp, new EnvV(), new EnvH()));
        Assert.Equal(2.5f, result.Value, precision: 4);
    }

    [Fact]
    public void AddMulPrecedence_ViaDirectAst_EvaluatesTo14()
    {
        // Construct: 2 + (3 * 4) = 14
        // This is what the parser produces for "2 + 3 * 4" (MUL binds tighter).
        var mul = new BinaryOperation(1, new NumberV(1, 3), BinaryOperator.MUL, new NumberV(1, 4));
        var add = new BinaryOperation(1, new NumberV(1, 2), BinaryOperator.ADD, mul);
        var result = Assert.IsType<NumberVal>(TestHelpers.EvalExpression(add,  new EnvV(), new EnvH()));
        Assert.Equal(14f, result.Value, precision: 4);
    }

    // ── Comparison ────────────────────────────────────────────────────────────

    [Fact]
    public void Equality_TwoEqualNumbers_ReturnsTrue()
    {
        var exp = new BinaryOperation(1, new NumberV(1, 2), BinaryOperator.EQ, new NumberV(1, 2));
        var result = Assert.IsType<BoolVal>(TestHelpers.EvalExpression(exp, new EnvV(), new EnvH()));
        Assert.True(result.Value);
    }

    [Fact]
    public void Equality_TwoDifferentNumbers_ReturnsFalse()
    {
        var exp = new BinaryOperation(1, new NumberV(1, 2), BinaryOperator.EQ, new NumberV(1, 3));
        var result = Assert.IsType<BoolVal>(TestHelpers.EvalExpression(exp, new EnvV(), new EnvH()));
        Assert.False(result.Value);
    }

    [Fact]
    public void LessThan_LeftSmaller_ReturnsTrue()
    {
        var exp = new BinaryOperation(1, new NumberV(1, 1), BinaryOperator.LT, new NumberV(1, 5));
        var result = Assert.IsType<BoolVal>(TestHelpers.EvalExpression(exp, new EnvV(), new EnvH()));
        Assert.True(result.Value);
    }

    [Fact]
    public void GreaterThan_LeftLarger_ReturnsTrue()
    {
        var exp = new BinaryOperation(1, new NumberV(1, 10), BinaryOperator.GT, new NumberV(1, 5));
        var result = Assert.IsType<BoolVal>(TestHelpers.EvalExpression(exp, new EnvV(), new EnvH()));
        Assert.True(result.Value);
    }

    // ── Boolean operators ─────────────────────────────────────────────────────

    [Fact]
    public void Not_False_ReturnsTrue()
    {
        var exp = new UnaryOperation(1, UnaryOperator.NOT, new BoolV(1, false));
        var result = Assert.IsType<BoolVal>(TestHelpers.EvalExpression(exp, new EnvV(), new EnvH()));
        Assert.True(result.Value);
    }

    [Fact]
    public void Not_True_ReturnsFalse()
    {
        var exp = new UnaryOperation(1, UnaryOperator.NOT, new BoolV(1, true));
        var result = Assert.IsType<BoolVal>(TestHelpers.EvalExpression(exp, new EnvV(), new EnvH()));
        Assert.False(result.Value);
    }

    [Fact]
    public void And_TrueAndTrue_ReturnsTrue()
    {
        var exp = new BinaryOperation(1, new BoolV(1, true), BinaryOperator.AND, new BoolV(1, true));
        var result = Assert.IsType<BoolVal>(TestHelpers.EvalExpression(exp, new EnvV(), new EnvH()));
        Assert.True(result.Value);
    }

    [Fact]
    public void And_TrueAndFalse_ReturnsFalse()
    {
        var exp = new BinaryOperation(1, new BoolV(1, true), BinaryOperator.AND, new BoolV(1, false));
        var result = Assert.IsType<BoolVal>(TestHelpers.EvalExpression(exp, new EnvV(), new EnvH()));
        Assert.False(result.Value);
    }

    [Fact]
    public void Or_FalseOrTrue_ReturnsTrue()
    {
        var exp = new BinaryOperation(1, new BoolV(1, false), BinaryOperator.OR, new BoolV(1, true));
        var result = Assert.IsType<BoolVal>(TestHelpers.EvalExpression(exp, new EnvV(), new EnvH()));
        Assert.True(result.Value);
    }

    [Fact]
    public void StringEquality_SameStrings_ReturnsTrue()
    {
        var exp = new BinaryOperation(1, new StringV(1, "hi"), BinaryOperator.EQ, new StringV(1, "hi"));
        var result = Assert.IsType<BoolVal>(TestHelpers.EvalExpression(exp, new EnvV(), new EnvH()));
        Assert.True(result.Value);
    }

    [Fact]
    public void StringEquality_DifferentStrings_ReturnsFalse()
    {
        var exp = new BinaryOperation(1, new StringV(1, "hi"), BinaryOperator.EQ, new StringV(1, "bye"));
        var result = Assert.IsType<BoolVal>(TestHelpers.EvalExpression(exp, new EnvV(), new EnvH()));
        Assert.False(result.Value);
    }

    // ── Unary negation ────────────────────────────────────────────────────────

    [Fact]
    public void UnaryNeg_OnNumber_EvaluatesToNegativeValue()
    {
        // UnaryOperation(NEG, NumberV(5)) should evaluate to NumberVal(-5).
        var exp = new UnaryOperation(1, UnaryOperator.NEG, new NumberV(1, 5));
        var result = Assert.IsType<NumberVal>(TestHelpers.EvalExpression(exp, new EnvV(), new EnvH()));
        Assert.Equal(-5f, result.Value, precision: 4);
    }

    [Fact]
    public void UnaryNeg_OnZero_EvaluatesToZero()
    {
        var exp = new UnaryOperation(1, UnaryOperator.NEG, new NumberV(1, 0));
        var result = Assert.IsType<NumberVal>(TestHelpers.EvalExpression(exp, new EnvV(), new EnvH()));
        Assert.Equal(0f, result.Value, precision: 4);
    }

    // ── Runtime errors ────────────────────────────────────────────────────────

    [Fact]
    public void DivisionByZero_ThrowsException()
    {
        var exp = new BinaryOperation(1, new NumberV(1, 5), BinaryOperator.DIV, new NumberV(1, 0));
        var ex = Assert.Throws<Exception>(() => TestHelpers.EvalExpression(exp, new EnvV(), new EnvH()));
        Assert.Contains("zero", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UnsupportedExpression_Reference_ThrowsException()
    {
        // Reference nodes are not yet handled by the Interpreter.
        var exp = new Reference(1, "x", null);
        Assert.Throws<Exception>(() => TestHelpers.EvalExpression(exp, new EnvV(), new EnvH()));
    }

    [Fact]
    public void InvalidUnary_NotOnNumber_ThrowsException()
    {
        // NOT applied to a NumberV: wrong type at runtime.
        // The TypeChecker would reject this, but the Interpreter guards too.
        var exp = new UnaryOperation(1, UnaryOperator.NOT, new NumberV(1, 5));
        Assert.Throws<Exception>(() => TestHelpers.EvalExpression(exp, new EnvV(), new EnvH()));
    }

    // ── DateTime / Duration: literals ────────────────────────────────────────

    [Fact]
    public void DateTimeLiteral_EvaluatesToDateTimeVal()
    {
        var dt = new DateTime(2026, 3, 15, 14, 0, 0);
        Value result = TestHelpers.EvalExpression(new DateTimeV(1, dt),  new EnvV(), new EnvH());
        var val = Assert.IsType<DateTimeVal>(result);
        Assert.Equal(dt, val.Value);
    }

    [Fact]
    public void DurationLiteral_EvaluatesToDurationVal()
    {
        var span = TimeSpan.FromDays(2);
        Value result = TestHelpers.EvalExpression(new DurationV(1, span),  new EnvV(), new EnvH());
        var val = Assert.IsType<DurationVal>(result);
        Assert.Equal(span, val.Value);
    }

    // ── DateTime / Duration: arithmetic ──────────────────────────────────────

    [Fact]
    public void DateTimePlusDuration_ReturnsShiftedDateTime()
    {
        // 15/03-2026 + 2 days = 17/03-2026
        var start = new DateTime(2026, 3, 15);
        var period = TimeSpan.FromDays(2);
        var exp = new BinaryOperation(1,
            new DateTimeV(1, start),
            BinaryOperator.ADD,
            new DurationV(1, period));
        var result = Assert.IsType<DateTimeVal>(TestHelpers.EvalExpression(exp, new EnvV(), new EnvH()));
        Assert.Equal(new DateTime(2026, 3, 17), result.Value);
    }

    [Fact]
    public void DateTimeMinusDuration_ReturnsShiftedDateTime()
    {
        // 17/03-2026 - 2 days = 15/03-2026
        var start = new DateTime(2026, 3, 17);
        var period = TimeSpan.FromDays(2);
        var exp = new BinaryOperation(1,
            new DateTimeV(1, start),
            BinaryOperator.SUB,
            new DurationV(1, period));
        var result = Assert.IsType<DateTimeVal>(TestHelpers.EvalExpression(exp, new EnvV(), new EnvH()));
        Assert.Equal(new DateTime(2026, 3, 15), result.Value);
    }

    // ── DateTime / Duration: equality ────────────────────────────────────────

    [Fact]
    public void DateTimeEquality_SameDateTimes_ReturnsTrue()
    {
        var dt = new DateTime(2026, 3, 15);
        var exp = new BinaryOperation(1,
            new DateTimeV(1, dt), BinaryOperator.EQ, new DateTimeV(1, dt));
        var result = Assert.IsType<BoolVal>(TestHelpers.EvalExpression(exp, new EnvV(), new EnvH()));
        Assert.True(result.Value);
    }

    [Fact]
    public void DateTimeEquality_DifferentDateTimes_ReturnsFalse()
    {
        var exp = new BinaryOperation(1,
            new DateTimeV(1, new DateTime(2026, 3, 15)),
            BinaryOperator.EQ,
            new DateTimeV(1, new DateTime(2026, 3, 16)));
        var result = Assert.IsType<BoolVal>(TestHelpers.EvalExpression(exp, new EnvV(), new EnvH()));
        Assert.False(result.Value);
    }

    [Fact]
    public void DurationEquality_SameDurations_ReturnsTrue()
    {
        var span = TimeSpan.FromHours(2);
        var exp = new BinaryOperation(1,
            new DurationV(1, span), BinaryOperator.EQ, new DurationV(1, span));
        var result = Assert.IsType<BoolVal>(TestHelpers.EvalExpression(exp, new EnvV(), new EnvH()));
        Assert.True(result.Value);
    }

    [Fact]
    public void DurationEquality_DifferentDurations_ReturnsFalse()
    {
        var exp = new BinaryOperation(1,
            new DurationV(1, TimeSpan.FromHours(1)),
            BinaryOperator.EQ,
            new DurationV(1, TimeSpan.FromHours(2)));
        var result = Assert.IsType<BoolVal>(TestHelpers.EvalExpression(exp, new EnvV(), new EnvH()));
        Assert.False(result.Value);
    }

    // ── DateTime / Duration: ordering ────────────────────────────────────────

    [Fact]
    public void DateTimeLessThan_EarlierIsSmaller_ReturnsTrue()
    {
        var exp = new BinaryOperation(1,
            new DateTimeV(1, new DateTime(2026, 3, 15)),
            BinaryOperator.LT,
            new DateTimeV(1, new DateTime(2026, 3, 16)));
        var result = Assert.IsType<BoolVal>(TestHelpers.EvalExpression(exp, new EnvV(), new EnvH()));
        Assert.True(result.Value);
    }

    [Fact]
    public void DateTimeLessThan_LaterIsNotSmaller_ReturnsFalse()
    {
        var exp = new BinaryOperation(1,
            new DateTimeV(1, new DateTime(2026, 3, 16)),
            BinaryOperator.LT,
            new DateTimeV(1, new DateTime(2026, 3, 15)));
        var result = Assert.IsType<BoolVal>(TestHelpers.EvalExpression(exp, new EnvV(), new EnvH()));
        Assert.False(result.Value);
    }

    [Fact]
    public void DurationLessThan_ShorterIsSmaller_ReturnsTrue()
    {
        var exp = new BinaryOperation(1,
            new DurationV(1, TimeSpan.FromHours(1)),
            BinaryOperator.LT,
            new DurationV(1, TimeSpan.FromHours(2)));
        var result = Assert.IsType<BoolVal>(TestHelpers.EvalExpression(exp, new EnvV(), new EnvH()));
        Assert.True(result.Value);
    }

    [Fact]
    public void DurationGreaterThan_LongerIsLarger_ReturnsTrue()
    {
        var exp = new BinaryOperation(1,
            new DurationV(1, TimeSpan.FromHours(3)),
            BinaryOperator.GT,
            new DurationV(1, TimeSpan.FromHours(1)));
        var result = Assert.IsType<BoolVal>(TestHelpers.EvalExpression(exp, new EnvV(), new EnvH()));
        Assert.True(result.Value);
    }

    // ── DateTime / Duration: invalid operations reach the runtime guard ───────

    [Fact]
    public void DateTimePlusDateTime_ThrowsAtRuntime()
    {
        // The typechecker rejects this; if it reaches the interpreter anyway, it must throw.
        var exp = new BinaryOperation(1,
            new DateTimeV(1, new DateTime(2026, 3, 15)),
            BinaryOperator.ADD,
            new DateTimeV(1, new DateTime(2026, 3, 16)));
        Assert.Throws<Exception>(() => TestHelpers.EvalExpression(exp, new EnvV(), new EnvH()));
    }

    [Fact]
    public void DurationPlusDateTime_ThrowsAtRuntime()
    {
        var exp = new BinaryOperation(1,
            new DurationV(1, TimeSpan.FromDays(1)),
            BinaryOperator.ADD,
            new DateTimeV(1, new DateTime(2026, 3, 15)));
        Assert.Throws<Exception>(() => TestHelpers.EvalExpression(exp, new EnvV(), new EnvH()));
    }
}
