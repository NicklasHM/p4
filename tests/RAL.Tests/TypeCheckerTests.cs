using RAL.AST;
using RAL.TC;

namespace RAL.Tests;

/*
 * Tests for the TypeChecker.
 * Every program tested here must parse correctly;
 * only semantic/type rules are under scrutiny.
 */
public class TypeCheckerTests
{
    // ── Positive: programs that must typecheck without errors ────────────────

    [Fact]
    public void NumberArithmetic_IsAccepted()
    {
        TestHelpers.TypeCheckShouldSucceed(TestPrograms.ValidArithmetic);
    }

    [Fact]
    public void BoolLiteral_IsAccepted()
    {
        TestHelpers.TypeCheckShouldSucceed(TestPrograms.ValidBoolLiteral);
    }

    [Fact]
    public void StringDecl_IsAccepted()
    {
        TestHelpers.TypeCheckShouldSucceed(TestPrograms.ValidStringDecl);
    }

    [Fact]
    public void MultipleNumberDecls_AreAccepted()
    {
        TestHelpers.TypeCheckShouldSucceed(TestPrograms.ValidMultipleDecls);
    }

    [Fact]
    public void BoolExpression_IsAccepted()
    {
        TestHelpers.TypeCheckShouldSucceed(TestPrograms.ValidBoolExpr);
    }

    [Fact]
    public void ResourceAndTemplate_AreAccepted()
    {
        TestHelpers.TypeCheckShouldSucceed(TestPrograms.ValidResourceTemplate);
    }

    [Fact]
    public void ResourceWithProperty_IsAccepted()
    {
        TestHelpers.TypeCheckShouldSucceed(TestPrograms.ValidResourceWithProperty);
    }

    [Fact]
    public void CategoryHierarchy_IsAccepted()
    {
        TestHelpers.TypeCheckShouldSucceed(TestPrograms.ValidCategoryHierarchy);
    }

    [Fact]
    public void IfStatement_WithBoolCondition_IsAccepted()
    {
        TestHelpers.TypeCheckShouldSucceed(TestPrograms.ValidIfStmt);
    }

    [Fact]
    public void Shadowing_TemplateParamShadowsOuterVar_IsAccepted()
    {
        // A template parameter named "x" should be allowed even when an outer
        // scope already has a variable named "x".
        TestHelpers.TypeCheckShouldSucceed(TestPrograms.ValidShadowing);
    }

    [Fact]
    public void TemplateBody_ReadsOuterScopeVariable_IsAccepted()
    {
        // A template body must be able to read variables declared in an
        // enclosing scope. The body initialises a local Number from an outer
        // Number, so the type system has to resolve "outer" through the
        // template-body env into the enclosing env.
        const string src =
            "Number outer = 5;\ntemplate t() { Number inner = outer; }";
        TestHelpers.TypeCheckShouldSucceed(src);
    }

    // ── Positive: direct AST construction ────────────────────────────────────

    [Fact]
    public void UnaryNot_OnBool_IsAccepted()
    {
        // UnaryOperation(NOT, BoolV(false)) → BoolT — no type error.
        var node = new ExpStmt(1, new UnaryOperation(1, UnaryOperator.NOT, new BoolV(1, false)));
        TypeChecker tc = TestHelpers.RunTypeChecker(node);
        Assert.Empty(tc.errors);
    }

    [Fact]
    public void BinaryAdd_TwoNumbers_IsAccepted()
    {
        var node = new ExpStmt(1,
            new BinaryOperation(1, new NumberV(1, 2), BinaryOperator.ADD, new NumberV(1, 3)));
        TypeChecker tc = TestHelpers.RunTypeChecker(node);
        Assert.Empty(tc.errors);
    }

    [Fact]
    public void BinaryEq_TwoBools_IsAccepted()
    {
        var node = new ExpStmt(1,
            new BinaryOperation(1, new BoolV(1, true), BinaryOperator.EQ, new BoolV(1, false)));
        TypeChecker tc = TestHelpers.RunTypeChecker(node);
        Assert.Empty(tc.errors);
    }

    // ── Negative: programs that must produce at least one type error ──────────

    [Fact]
    public void StringAssignedToNumber_IsRejected()
    {
        TestHelpers.TypeCheckShouldReportError(
            TestPrograms.InvalidTypeStringAssignedToNumber, "number", "string");
    }

    [Fact]
    public void BoolDivision_IsRejected()
    {
        TestHelpers.TypeCheckShouldReportError(
            TestPrograms.InvalidTypeBoolDivision, "bool");
    }

    [Fact]
    public void IfStatement_WithNonBoolCondition_IsRejected()
    {
        TestHelpers.TypeCheckShouldReportError(
            TestPrograms.InvalidTypeIfNonBoolCondition, "bool", "condition");
    }

    // ── Negative: direct AST construction ────────────────────────────────────

    [Fact]
    public void UnaryNot_OnNumber_IsRejected()
    {
        // UnaryOperation(NOT, NumberV(5)) → typechecker must add an error.
        var node = new ExpStmt(1, new UnaryOperation(1, UnaryOperator.NOT, new NumberV(1, 5)));
        TypeChecker tc = TestHelpers.RunTypeChecker(node);
        Assert.NotEmpty(tc.errors);
        Assert.Contains(tc.errors, e =>
            e.Contains("number", StringComparison.OrdinalIgnoreCase) ||
            e.Contains("bool",   StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void BinaryDiv_BoolOperands_IsRejected()
    {
        // BinaryOperation(DIV, BoolV, BoolV) → type error.
        var node = new ExpStmt(1,
            new BinaryOperation(1, new BoolV(1, true), BinaryOperator.DIV, new BoolV(1, false)));
        TypeChecker tc = TestHelpers.RunTypeChecker(node);
        Assert.NotEmpty(tc.errors);
    }

    [Fact]
    public void BinaryAdd_StringAndNumber_IsRejected()
    {
        // BinaryOperation(ADD, StringV, NumberV) → type error.
        var node = new ExpStmt(1,
            new BinaryOperation(1, new StringV(1, "hello"), BinaryOperator.ADD, new NumberV(1, 3)));
        TypeChecker tc = TestHelpers.RunTypeChecker(node);
        Assert.NotEmpty(tc.errors);
    }

    // ── Unary NEG: TypeChecker accepts/rejects correctly (direct AST) ─────────

    [Fact]
    public void UnaryNeg_OnNumber_IsAccepted_DirectAst()
    {
        // UnaryOperation(NEG, NumberV(5)) → NumberT — no type error.
        var node = new ExpStmt(1, new UnaryOperation(1, UnaryOperator.NEG, new NumberV(1, 5)));
        TypeChecker tc = TestHelpers.RunTypeChecker(node);
        Assert.Empty(tc.errors);
    }

    [Fact]
    public void UnaryNeg_OnBool_IsRejected_DirectAst()
    {
        // UnaryOperation(NEG, BoolV(true)) → type error (NEG requires Number).
        var node = new ExpStmt(1, new UnaryOperation(1, UnaryOperator.NEG, new BoolV(1, true)));
        TypeChecker tc = TestHelpers.RunTypeChecker(node);
        Assert.NotEmpty(tc.errors);
        Assert.Contains(tc.errors, e =>
            e.Contains("number", StringComparison.OrdinalIgnoreCase) ||
            e.Contains("bool",   StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void NotOnNumber_Pipeline_ShouldBeRejectedByTypechecker()
    {
        // "Number n = not(5);" should:
        //   1. Parse successfully (syntax is valid).
        //   2. Produce UnaryOperation(NOT, NumberV(5)) in the AST.
        //   3. Be rejected by the typechecker because NOT requires Bool.
        TestHelpers.TypeCheckShouldReportError(TestPrograms.NotOnNumber, "bool", "not");
    }

    // ── Semantic errors: undeclared and duplicate identifiers ─────────────────
    // Intended behavior: these must be reported through tc.errors, not exceptions.
    // If the typechecker currently throws for any of these, the test will FAIL —
    // that failure is the signal that the typechecker needs to be fixed.

    [Fact]
    public void DuplicateVariable_SameScope_IsRejectedWithSemanticError()
    {
        // Binding the same identifier twice in the same scope must add an error
        // to tc.errors. Intended behavior: errors.Add, not a thrown exception.
        TestHelpers.TypeCheckShouldReportError(TestPrograms.InvalidTypeDuplicateVar);
    }

    [Fact]
    public void UndeclaredVariable_IsRejectedWithSemanticError()
    {
        // Looking up an identifier that was never declared must add an error
        // to tc.errors. Intended behavior: errors.Add, not a thrown exception.
        TestHelpers.TypeCheckShouldReportError(TestPrograms.InvalidTypeUndeclaredVar);
    }

    // ── Positive: DateTime / Duration — source programs ──────────────────────

    [Fact]
    public void DateTimeDeclaration_IsAccepted()
    {
        TestHelpers.TypeCheckShouldSucceed(TestPrograms.ValidDateTimeDeclaration);
    }

    [Fact]
    public void DurationDeclaration_IsAccepted()
    {
        TestHelpers.TypeCheckShouldSucceed(TestPrograms.ValidDurationDeclaration);
    }

    [Fact]
    public void DateTimePlusDuration_IsAccepted()
    {
        TestHelpers.TypeCheckShouldSucceed(TestPrograms.ValidDateTimePlusDuration);
    }

    [Fact]
    public void DateTimeMinusDuration_IsAccepted()
    {
        TestHelpers.TypeCheckShouldSucceed(TestPrograms.ValidDateTimeMinusDuration);
    }

    [Fact]
    public void DateTimeComparison_LessThan_IsAccepted()
    {
        TestHelpers.TypeCheckShouldSucceed(TestPrograms.ValidDateTimeComparison);
    }

    [Fact]
    public void DurationComparison_LessThan_IsAccepted()
    {
        TestHelpers.TypeCheckShouldSucceed(TestPrograms.ValidDurationComparison);
    }

    // ── Positive: DateTime / Duration — direct AST ────────────────────────────

    [Fact]
    public void DateTimeV_HasDateTimeT()
    {
        var node = new ExpStmt(1, new DateTimeV(1, new DateTime(2026, 3, 15)));
        TypeChecker tc = TestHelpers.RunTypeChecker(node);
        Assert.Empty(tc.errors);
    }

    [Fact]
    public void DurationV_HasDurationT()
    {
        var node = new ExpStmt(1, new DurationV(1, TimeSpan.FromDays(2)));
        TypeChecker tc = TestHelpers.RunTypeChecker(node);
        Assert.Empty(tc.errors);
    }

    [Fact]
    public void BinaryAdd_DateTimePlusDuration_IsAccepted()
    {
        // ADD(DateTimeT, DurationT) → DateTimeT — no type error.
        var node = new ExpStmt(1, new BinaryOperation(1,
            new DateTimeV(1, new DateTime(2026, 3, 15)),
            BinaryOperator.ADD,
            new DurationV(1, TimeSpan.FromDays(2))));
        TypeChecker tc = TestHelpers.RunTypeChecker(node);
        Assert.Empty(tc.errors);
    }

    [Fact]
    public void BinaryEq_TwoDateTimes_IsAccepted()
    {
        // EQ(DateTimeT, DateTimeT) → BoolT — no type error.
        var node = new ExpStmt(1, new BinaryOperation(1,
            new DateTimeV(1, new DateTime(2026, 3, 15)),
            BinaryOperator.EQ,
            new DateTimeV(1, new DateTime(2026, 3, 16))));
        TypeChecker tc = TestHelpers.RunTypeChecker(node);
        Assert.Empty(tc.errors);
    }

    [Fact]
    public void BinaryLt_TwoDateTimes_IsAccepted()
    {
        // LT(DateTimeT, DateTimeT) → BoolT — no type error.
        var node = new ExpStmt(1, new BinaryOperation(1,
            new DateTimeV(1, new DateTime(2026, 3, 15)),
            BinaryOperator.LT,
            new DateTimeV(1, new DateTime(2026, 3, 16))));
        TypeChecker tc = TestHelpers.RunTypeChecker(node);
        Assert.Empty(tc.errors);
    }

    [Fact]
    public void BinaryEq_TwoDurations_IsAccepted()
    {
        // EQ(DurationT, DurationT) → BoolT — no type error.
        var node = new ExpStmt(1, new BinaryOperation(1,
            new DurationV(1, TimeSpan.FromHours(1)),
            BinaryOperator.EQ,
            new DurationV(1, TimeSpan.FromHours(2))));
        TypeChecker tc = TestHelpers.RunTypeChecker(node);
        Assert.Empty(tc.errors);
    }

    [Fact]
    public void BinaryLt_TwoDurations_IsAccepted()
    {
        // LT(DurationT, DurationT) → BoolT — no type error.
        var node = new ExpStmt(1, new BinaryOperation(1,
            new DurationV(1, TimeSpan.FromHours(1)),
            BinaryOperator.LT,
            new DurationV(1, TimeSpan.FromHours(2))));
        TypeChecker tc = TestHelpers.RunTypeChecker(node);
        Assert.Empty(tc.errors);
    }

    // ── Negative: DateTime / Duration — must produce type errors ─────────────

    [Fact]
    public void DateTimePlusDateTime_IsRejected()
    {
        TestHelpers.TypeCheckShouldReportError(
            TestPrograms.InvalidDateTimePlusDateTime, "datetime");
    }

    [Fact]
    public void DateTimeMinusDateTime_IsRejected()
    {
        TestHelpers.TypeCheckShouldReportError(
            TestPrograms.InvalidDateTimeMinusDateTime, "datetime");
    }

    [Fact]
    public void DurationPlusDateTime_IsRejected()
    {
        TestHelpers.TypeCheckShouldReportError(
            TestPrograms.InvalidDurationPlusDateTime, "duration", "datetime");
    }

    [Fact]
    public void DateTimeAssignedToNumber_IsRejected()
    {
        TestHelpers.TypeCheckShouldReportError(
            TestPrograms.InvalidDateTimeAssignedToNumber, "number", "datetime");
    }

    [Fact]
    public void DurationAssignedToDateTime_IsRejected()
    {
        TestHelpers.TypeCheckShouldReportError(
            TestPrograms.InvalidDurationAssignedToDateTime, "duration", "datetime");
    }

    [Fact]
    public void BinaryAdd_DateTimePlusDateTime_IsRejected_DirectAst()
    {
        var node = new ExpStmt(1, new BinaryOperation(1,
            new DateTimeV(1, new DateTime(2026, 3, 15)),
            BinaryOperator.ADD,
            new DateTimeV(1, new DateTime(2026, 3, 16))));
        TypeChecker tc = TestHelpers.RunTypeChecker(node);
        Assert.NotEmpty(tc.errors);
    }

    [Fact]
    public void BinaryAdd_DurationPlusDateTime_IsRejected_DirectAst()
    {
        var node = new ExpStmt(1, new BinaryOperation(1,
            new DurationV(1, TimeSpan.FromDays(1)),
            BinaryOperator.ADD,
            new DateTimeV(1, new DateTime(2026, 3, 15))));
        TypeChecker tc = TestHelpers.RunTypeChecker(node);
        Assert.NotEmpty(tc.errors);
    }

    [Fact]
    public void BinaryLt_DateTimeAndNumber_IsRejected_DirectAst()
    {
        // DateTime compared with Number → type error.
        var node = new ExpStmt(1, new BinaryOperation(1,
            new DateTimeV(1, new DateTime(2026, 3, 15)),
            BinaryOperator.LT,
            new NumberV(1, 5)));
        TypeChecker tc = TestHelpers.RunTypeChecker(node);
        Assert.NotEmpty(tc.errors);
    }

    [Fact]
    public void BinaryEq_DurationAndString_IsRejected_DirectAst()
    {
        // Duration compared with String → type error.
        var node = new ExpStmt(1, new BinaryOperation(1,
            new DurationV(1, TimeSpan.FromDays(1)),
            BinaryOperator.EQ,
            new StringV(1, "hello")));
        TypeChecker tc = TestHelpers.RunTypeChecker(node);
        Assert.NotEmpty(tc.errors);
    }

    // ── Positive: core RAL semantics ─────────────────────────────────────────

    [Fact]
    public void ResourceDeclWithProperties_IsAccepted()
    {
        TestHelpers.TypeCheckShouldSucceed(TestPrograms.ValidResourceWithProperty);
    }

    [Fact]
    public void CategoryInheritance_IsAccepted()
    {
        TestHelpers.TypeCheckShouldSucceed(TestPrograms.ValidCategoryHierarchy);
    }

    [Fact]
    public void MoveResource_ToValidCategory_IsAccepted()
    {
        TestHelpers.TypeCheckShouldSucceed(TestPrograms.ValidMoveResourceToCategory);
    }

    [Fact]
    public void ReserveKnownResource_IsAccepted()
    {
        TestHelpers.TypeCheckShouldSucceed(TestPrograms.ValidReserveStatement);
    }

    [Fact]
    public void AvailabilityKnownResource_IsAccepted()
    {
        TestHelpers.TypeCheckShouldSucceed(TestPrograms.ValidAvailabilityQuery);
    }

    [Fact]
    public void CancelReservation_IsAccepted()
    {
        TestHelpers.TypeCheckShouldSucceed(TestPrograms.ValidCancelReservation);
    }

    [Fact]
    public void RescheduleReservation_IsAccepted()
    {
        TestHelpers.TypeCheckShouldSucceed(TestPrograms.ValidRescheduleReservation);
    }

    [Fact]
    public void PropertyAccess_KnownField_IsAccepted()
    {
        TestHelpers.TypeCheckShouldSucceed(TestPrograms.ValidResourcePropertyAccess);
    }

    // ── Negative: core RAL semantics ─────────────────────────────────────────

    [Fact]
    public void CancelNonReservation_IsRejected()
    {
        // "cancel n;" where n is Number → type error (expected Reservation).
        TestHelpers.TypeCheckShouldReportError(
            TestPrograms.InvalidCancelNonReservation, "reservation", "number");
    }

    [Fact]
    public void MoveNonResource_IsRejected()
    {
        // "move n to Room;" where n is Number → error added to tc.errors.
        TestHelpers.TypeCheckShouldReportError(
            TestPrograms.InvalidMoveNonResource, "resource");
    }

    [Fact]
    public void MoveUnknownResource_IsRejectedWithSemanticError()
    {
        // Moving an undeclared resource must add an error to tc.errors.
        // Intended behavior: errors.Add, not a thrown exception.
        TestHelpers.TypeCheckShouldReportError(TestPrograms.InvalidMoveUnknownResource);
    }

    [Fact]
    public void MoveToUnknownCategory_IsRejectedWithSemanticError()
    {
        // Moving to an undeclared category must add an error to tc.errors.
        // Intended behavior: errors.Add, not a thrown exception.
        TestHelpers.TypeCheckShouldReportError(TestPrograms.InvalidMoveUnknownCategory);
    }

    [Fact]
    public void ReserveUnknownResource_IsRejectedWithSemanticError()
    {
        // Reserving an undeclared resource must add an error to tc.errors.
        // Intended behavior: errors.Add, not a thrown exception.
        TestHelpers.TypeCheckShouldReportError(TestPrograms.InvalidReserveUnknownResource);
    }

    [Fact]
    public void AvailabilityUnknownResource_IsRejectedWithSemanticError()
    {
        // Checking availability of an undeclared resource must add an error to tc.errors.
        // Intended behavior: errors.Add, not a thrown exception.
        TestHelpers.TypeCheckShouldReportError(TestPrograms.InvalidAvailabilityUnknownResource);
    }

    [Fact]
    public void PropertyAccess_UnknownField_IsRejectedWithSemanticError()
    {
        // Accessing a field that does not exist on a declared resource must add an
        // error to tc.errors. Intended behavior: errors.Add, not a thrown exception.
        TestHelpers.TypeCheckShouldReportError(TestPrograms.InvalidResourcePropertyAccess);
    }

    [Fact]
    public void DuplicateResourceField_IsRejectedWithSemanticError()
    {
        // Two fields with the same name in one resource must add an error to tc.errors.
        // Intended behavior: errors.Add, not a thrown exception.
        const string src = "category Room;\nRoom myRoom { Number beds = 2; Number beds = 4; }";
        TestHelpers.TypeCheckShouldReportError(src);
    }

    [Fact]
    public void DuplicateCategory_IsRejected()
    {
        // Declaring the same category twice.
        // Intended: typechecker must add an error to tc.errors (current impl
        // throws via EnvH.EstablishRelation on the duplicate Add).
        const string src = "category Room;\ncategory Room;";
        TestHelpers.TypeCheckShouldReportError(src, "Room", "already");
    }

    // ── Where-clause semantics ───────────────────────────────────────────────

    [Fact]
    public void ValidReserveWithWherePredicate_IsAccepted()
    {
        // Alias r in scope, r.beds is a known Number field, r.beds == 2 is Bool.
        TestHelpers.TypeCheckShouldSucceed(TestPrograms.ValidReserveWherePredicate);
    }

    [Fact]
    public void ReserveWhereNonBoolPredicate_IsRejectedWithSemanticError()
    {
        // Predicate "r.beds + 2" has type Number; where clause requires Bool.
        // ConditionIsWellTyped must add an error to tc.errors.
        TestHelpers.TypeCheckShouldReportError(TestPrograms.InvalidReserveWhereNonBoolPredicate,
            "bool", "condition");
    }

    [Fact]
    public void ReserveWhereUnknownProperty_IsRejectedWithSemanticError()
    {
        // 'floors' is not a declared property of any Room resource.
        // Intended: typechecker must add an error to tc.errors.
        // Current implementation silently returns NumberT for unknown fields — test will FAIL.
        TestHelpers.TypeCheckShouldReportError(TestPrograms.InvalidReserveWhereUnknownProperty);
    }

    [Fact]
    public void ReserveWhereUnknownAlias_IsRejectedWithSemanticError()
    {
        // Alias 'x' was never introduced in the resource spec.
        // Intended: typechecker must add an error to tc.errors.
        // Current implementation throws via envV.Lookup — test will FAIL.
        TestHelpers.TypeCheckShouldReportError(TestPrograms.InvalidReserveWhereUnknownAlias);
    }

    // ── Template call semantics ───────────────────────────────────────────────

    [Fact]
    public void ValidTemplateCall_IsAccepted()
    {
        // Template declared with (Number, String) parameters, called with matching arguments.
        TestHelpers.TypeCheckShouldSucceed(TestPrograms.ValidTemplateCall);
    }

    [Fact]
    public void TemplateCall_WrongArgType_IsRejectedWithSemanticError()
    {
        // Template expects Number but receives String → tc.errors must be non-empty.
        TestHelpers.TypeCheckShouldReportError(TestPrograms.InvalidTemplateCallWrongArgType,
            "Argument", "booking");
    }

    [Fact]
    public void TemplateCall_WrongArgCount_IsRejectedWithSemanticError()
    {
        // Template expects 2 arguments but call supplies 1 → tc.errors must be non-empty.
        TestHelpers.TypeCheckShouldReportError(TestPrograms.InvalidTemplateCallWrongArgCount,
            "booking", "arguments");
    }

    [Fact]
    public void TemplateCall_UnknownTemplate_IsRejectedWithSemanticError()
    {
        // Calling an undeclared template must add an error to tc.errors.
        // Intended behavior: errors.Add, not a thrown exception.
        // If the current implementation throws, this test will FAIL —
        // that failure is the signal that envT.Lookup must be fixed.
        TestHelpers.TypeCheckShouldReportError(TestPrograms.InvalidTemplateCallUnknownTemplate);
    }
}
