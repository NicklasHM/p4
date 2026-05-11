using RAL.AST;
using RAL.Interpreter;
using Interp = RAL.Interpreter.Interpreter;

namespace RAL.Tests;

/*
 * Tests for the Interpreter expression evaluator.
 * Most tests construct AST nodes directly — the parser is not involved.
 * The Interpreter assumes the TypeChecker has already accepted the program,
 * so only well-typed inputs are used here (except the division-by-zero test).
 *
 * Statement-execution tests at the bottom drive the full Parse → TypeCheck →
 * Interpret pipeline because they exercise side effects on ResourceRegistry /
 * ReservationRegistry, which only the Interpreter mutates.
 *
 * ResourceRegistry and ReservationRegistry are process-wide singletons.
 * This class implements IDisposable so the registries are cleared before
 * AND after every test (xUnit creates a fresh instance per [Fact]). That way
 * pipeline tests can reuse the same RAL source from TestPrograms.cs without
 * inventing unique category names to dodge cross-test pollution.
 */
public class InterpreterTests : IDisposable
{
    public InterpreterTests() => TestHelpers.ResetRegistries();
    public void Dispose()     => TestHelpers.ResetRegistries();

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
    public void Reference_UnboundName_ThrowsException()
    {
        // Evaluating Reference("x", null) against an empty EnvV must throw —
        // the lookup has no binding to return. Reference itself IS supported
        // (see Reference_DeclaredVariable_ReturnsBoundValue); only unbound lookup fails.
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

    // ── Statement execution (intended behavior) ─────────────────────────────
    //
    // These tests drive Interpreter.EvalStmt rather than EvalExp.
    // Tests 1–7 use direct AST construction; tests 8–13 run the full
    // parse → typecheck → interpret pipeline because the source programs
    // declare categories / resources / reservations that the interpreter
    // mutates via side-effects on EnvV and the global registries.
    //
    // Pipeline tests reuse RAL source from TestPrograms.cs. The class-level
    // ResetRegistries (constructor / Dispose) clears the ResourceRegistry and
    // ReservationRegistry singletons between tests, so the same canonical
    // names ("Room", "myRoom", "res") can appear in every test without
    // cross-test pollution.
    //
    // Tests tagged [Trait("status","pending")] describe intended Reserve /
    // Cancel / Reschedule behavior and FAIL today because those interpreter
    // paths are stubbed — the failure is the signal that the missing path
    // needs implementing. Filter them out with:
    //   dotnet test --filter "status!=pending"


    [Fact]  // 1
    public void VarDecl_Number_BindsZeroDefault()
    {
        // "Number x;" must bind x → NumberVal(0) in the current scope.
        var stmt = new VarDecl(1, new NumberT(), "x");
        var envV = new EnvV();
        Interp.EvalStmt(stmt, envV, new EnvH());
        var value = Assert.IsType<NumberVal>(envV.Lookup("x"));
        Assert.Equal(0f, value.Value, precision: 4);
    }

    [Fact]  // 2
    public void VarDecl_Bool_BindsFalseDefault()
    {
        // "Bool b;" must bind b → BoolVal(false).
        var stmt = new VarDecl(1, new BoolT(), "b");
        var envV = new EnvV();
        Interp.EvalStmt(stmt, envV, new EnvH());
        var value = Assert.IsType<BoolVal>(envV.Lookup("b"));
        Assert.False(value.Value);
    }

    [Fact]  // 3
    public void Composite_ExecutesLeftThenRight()
    {
        // Left declares x; right assigns 5 to x.
        // If the order were reversed, the Assignment would throw because
        // x would not yet be bound — so a successful x == 5 confirms left-then-right.
        var stmt = new Composite(1,
            new VarDecl(1, new NumberT(), "x"),
            new ExpStmt(1, new Assignment(1,
                new Reference(1, "x", null),
                new NumberV(1, 5))));
        var envV = new EnvV();
        Interp.EvalStmt(stmt, envV, new EnvH());
        var value = Assert.IsType<NumberVal>(envV.Lookup("x"));
        Assert.Equal(5f, value.Value, precision: 4);
    }

    [Fact]  // 4
    public void Reference_DeclaredVariable_ReturnsBoundValue()
    {
        // Pre-bind x → 7 in envV; evaluating Reference("x", null) must return that value.
        var envV = new EnvV();
        envV.Bind("x", new NumberVal(7));
        var value = Assert.IsType<NumberVal>(
            TestHelpers.EvalExpression(new Reference(1, "x", null), envV, new EnvH()));
        Assert.Equal(7f, value.Value, precision: 4);
    }

    [Fact]  // 5
    public void Assignment_VariableForm_UpdatesBinding()
    {
        // Pre-bind x → 0; evaluate Assignment(x, 9); envV.Lookup("x") must be 9.
        var envV = new EnvV();
        envV.Bind("x", new NumberVal(0));
        TestHelpers.EvalExpression(
            new Assignment(1, new Reference(1, "x", null), new NumberV(1, 9)),
            envV, new EnvH());
        var value = Assert.IsType<NumberVal>(envV.Lookup("x"));
        Assert.Equal(9f, value.Value, precision: 4);
    }

    [Fact]  // 6
    public void If_TrueCondition_ExecutesThenBranch()
    {
        // Pre-bind flag → 0; if (true) then { flag = 1; } else { flag = 2; }
        // After execution flag must be 1.
        var envV = new EnvV();
        envV.Bind("flag", new NumberVal(0));
        var ifStmt = new If(1,
            new BoolV(1, true),
            new ExpStmt(1, new Assignment(1, new Reference(1, "flag", null), new NumberV(1, 1))),
            new ExpStmt(1, new Assignment(1, new Reference(1, "flag", null), new NumberV(1, 2))));
        Interp.EvalStmt(ifStmt, envV, new EnvH());
        var value = Assert.IsType<NumberVal>(envV.Lookup("flag"));
        Assert.Equal(1f, value.Value, precision: 4);
    }

    [Fact]  // 7
    public void If_FalseCondition_ExecutesElseBranch()
    {
        // Pre-bind flag → 0; if (false) then { flag = 1; } else { flag = 2; }
        // After execution flag must be 2.
        var envV = new EnvV();
        envV.Bind("flag", new NumberVal(0));
        var ifStmt = new If(1,
            new BoolV(1, false),
            new ExpStmt(1, new Assignment(1, new Reference(1, "flag", null), new NumberV(1, 1))),
            new ExpStmt(1, new Assignment(1, new Reference(1, "flag", null), new NumberV(1, 2))));
        Interp.EvalStmt(ifStmt, envV, new EnvH());
        var value = Assert.IsType<NumberVal>(envV.Lookup("flag"));
        Assert.Equal(2f, value.Value, precision: 4);
    }

    [Fact]  // 8
    public void CategoryDecl_RegistersInResourceRegistry()
    {
        // After running "category Room;" the registry must contain a Room bucket.
        // ResourceRegistry.ToString contains the category name in its header per bucket.
        Stmt root = TestHelpers.ParseShouldSucceed(TestPrograms.ValidCategory);
        TestHelpers.RunTypeChecker(root);
        Interp.EvalStmt(root, new EnvV(), new EnvH());
        Assert.Contains("Room", ResourceRegistry.Instance().ToString());
    }

    [Fact]  // 9
    public void ResourceDecl_CreatesResourceValWithPropertyMap()
    {
        // "Room myRoom { Number beds = 2; }" — envV.Lookup("myRoom") must be a
        // ResourceVal whose Properties["beds"] equals NumberVal(2).
        Stmt root = TestHelpers.ParseShouldSucceed(TestPrograms.ValidResourceWithProperty);
        TestHelpers.RunTypeChecker(root);
        var envV = new EnvV();
        Interp.EvalStmt(root, envV, new EnvH());
        var resource = Assert.IsType<ResourceVal>(envV.Lookup("myRoom"));
        Assert.Equal("Room", resource.CategoryId);
        var beds = Assert.IsType<NumberVal>(resource.Properties["beds"]);
        Assert.Equal(2f, beds.Value, precision: 4);
    }

    [Fact]  // 10
    public void Move_UpdatesRegistryBucketAndResourceCategoryId()
    {
        // After "move myRoom to Suite;" the resource's CategoryId must be "Suite"
        // and the registry must list it under Suite.
        Stmt root = TestHelpers.ParseShouldSucceed(TestPrograms.ValidMoveResourceToCategory);
        TestHelpers.RunTypeChecker(root);
        var envV = new EnvV();
        Interp.EvalStmt(root, envV, new EnvH());
        var resource = Assert.IsType<ResourceVal>(envV.Lookup("myRoom"));
        Assert.Equal("Suite", resource.CategoryId);
        Assert.Contains("Suite", ResourceRegistry.Instance().ToString());
    }

    [Fact, Trait("status", "pending")]  // 11
    public void Reserve_NamedResourceInValidInterval_ProducesNonFailedReservation()
    {
        // Intended behavior: "reserve r from … to …" yields a ReservationVal
        // whose Failed() is false (i.e. the Reservations list is non-empty).
        // Today EvalReserve is a stub — Reserve falls through to the default
        // "Unsupported expression" exception in EvalExp. Test fails informatively.
        Stmt root = TestHelpers.ParseShouldSucceed(TestPrograms.ValidReserveStatement);
        TestHelpers.RunTypeChecker(root);
        var envV = new EnvV();
        Interp.EvalStmt(root, envV, new EnvH());
        var reservation = Assert.IsType<ReservationVal>(envV.Lookup("res"));
        Assert.False(reservation.Failed());
    }

    [Fact, Trait("status", "pending")]  // 12
    public void Cancel_RemovesReservationFromRegistry()
    {
        // Intended behavior: after "cancel res;" the reservation must report
        // Failed() == true (i.e. its Reservations list is empty).
        // Today Cancel is not matched in EvalStmt's switch — it throws
        // "Unknown Statement". Test fails informatively.
        Stmt root = TestHelpers.ParseShouldSucceed(TestPrograms.ValidCancelReservation);
        TestHelpers.RunTypeChecker(root);
        var envV = new EnvV();
        Interp.EvalStmt(root, envV, new EnvH());
        var reservation = Assert.IsType<ReservationVal>(envV.Lookup("res"));
        Assert.True(reservation.Failed());
    }

    [Fact, Trait("status", "pending")]  // 13
    public void Reschedule_ReturnsReservationWithNewInterval()
    {
        // Intended behavior: "reschedule res from 20/03-2026 to 21/03-2026"
        // returns a ReservationVal whose atom carries the new start/end.
        // Today Reschedule has no EvalExp case — falls to "Unsupported expression".
        Stmt root = TestHelpers.ParseShouldSucceed(TestPrograms.ValidRescheduleReservation);
        TestHelpers.RunTypeChecker(root);
        var envV = new EnvV();
        Interp.EvalStmt(root, envV, new EnvH());
        var reservation = Assert.IsType<ReservationVal>(envV.Lookup("rescheduled"));
        Assert.False(reservation.Failed());
        var atom = Assert.Single(reservation.Reservations);
        Assert.Equal(new DateTime(2026, 3, 20), atom.Start.Value);
        Assert.Equal(new DateTime(2026, 3, 21), atom.End.Value);
    }
}
