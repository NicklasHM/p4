using RAL.AST;

namespace RAL.Tests;

/*
 * Tests that verify only syntax and basic AST-shape concerns.
 * No type or runtime assertions belong here.
 */
public class ParserTests
{
    // ── Valid programs must produce zero parse errors ────────────────────────

    [Fact]
    public void ValidArithmetic_ParsesSuccessfully()
    {
        TestHelpers.ParseShouldSucceed(TestPrograms.ValidArithmetic);
    }

    [Fact]
    public void ValidBoolLiteral_ParsesSuccessfully()
    {
        TestHelpers.ParseShouldSucceed(TestPrograms.ValidBoolLiteral);
    }

    [Fact]
    public void ValidStringDecl_ParsesSuccessfully()
    {
        TestHelpers.ParseShouldSucceed(TestPrograms.ValidStringDecl);
    }

    [Fact]
    public void ValidCategory_ParsesSuccessfully()
    {
        TestHelpers.ParseShouldSucceed(TestPrograms.ValidCategory);
    }

    [Fact]
    public void ValidResourceDecl_ParsesSuccessfully()
    {
        TestHelpers.ParseShouldSucceed(TestPrograms.ValidResourceDecl);
    }

    [Fact]
    public void ValidResourceWithProperty_ParsesSuccessfully()
    {
        TestHelpers.ParseShouldSucceed(TestPrograms.ValidResourceWithProperty);
    }

    [Fact]
    public void ValidResourceAndTemplate_ParsesSuccessfully()
    {
        TestHelpers.ParseShouldSucceed(TestPrograms.ValidResourceTemplate);
    }

    [Fact]
    public void ValidNestedArithmetic_ParsesSuccessfully()
    {
        TestHelpers.ParseShouldSucceed(TestPrograms.ValidNestedArithmetic);
    }

    [Fact]
    public void ValidParenArithmetic_ParsesSuccessfully()
    {
        TestHelpers.ParseShouldSucceed(TestPrograms.ValidParenArithmetic);
    }

    [Fact]
    public void ValidIfStatement_ParsesSuccessfully()
    {
        TestHelpers.ParseShouldSucceed(TestPrograms.ValidIfStmt);
    }

    [Fact]
    public void ValidCategoryHierarchy_ParsesSuccessfully()
    {
        TestHelpers.ParseShouldSucceed(TestPrograms.ValidCategoryHierarchy);
    }

    [Fact]
    public void ValidShadowing_ParsesSuccessfully()
    {
        TestHelpers.ParseShouldSucceed(TestPrograms.ValidShadowing);
    }

    [Fact]
    public void EmptyProgram_ParsesSuccessfully()
    {
        // An empty file is a valid RAL program (zero statements).
        TestHelpers.ParseShouldSucceed("");
    }

    // ── Type-error programs must still parse successfully ───────────────────
    // (These have valid syntax; the error belongs to the typechecker.)

    [Fact]
    public void StringToNumber_IsSyntacticallyValid()
    {
        TestHelpers.ParseShouldSucceed(TestPrograms.InvalidTypeStringAssignedToNumber);
    }

    [Fact]
    public void BoolDivision_IsSyntacticallyValid()
    {
        TestHelpers.ParseShouldSucceed(TestPrograms.InvalidTypeBoolDivision);
    }

    // ── Invalid syntax must produce parse errors ─────────────────────────────

    [Fact]
    public void MissingSemicolon_IsRejectedByParser()
    {
        TestHelpers.ParseShouldFail(TestPrograms.InvalidSyntaxMissingSemicolon);
    }

    [Fact]
    public void BadDeclaration_IsRejectedByParser()
    {
        TestHelpers.ParseShouldFail(TestPrograms.InvalidSyntaxBadDecl);
    }

    [Fact]
    public void UnclosedParenthesis_IsRejectedByParser()
    {
        TestHelpers.ParseShouldFail(TestPrograms.InvalidSyntaxUnclosedParen);
    }

    // ── Root AST node shape ──────────────────────────────────────────────────

    [Fact]
    public void SingleDeclaration_RootIsComposite()
    {
        // "Number x = 42;" → VarDecl + initialiser → Composite
        Stmt root = TestHelpers.ParseShouldSucceed(TestPrograms.ValidNumberDecl);
        Assert.IsType<Composite>(root);
    }

    [Fact]
    public void VarDeclWithoutInit_FirstChildIsVarDecl()
    {
        // "Number x = 42;" → Composite(VarDecl, ExpStmt(Assignment))
        Stmt root = TestHelpers.ParseShouldSucceed(TestPrograms.ValidNumberDecl);
        var composite = Assert.IsType<Composite>(root);
        Assert.IsType<VarDecl>(composite.Stmt1);
    }

    [Fact]
    public void VarDeclInit_SecondChildIsExpStmtWithAssignment()
    {
        Stmt root = TestHelpers.ParseShouldSucceed(TestPrograms.ValidNumberDecl);
        var composite = Assert.IsType<Composite>(root);
        var expStmt = Assert.IsType<ExpStmt>(composite.Stmt2);
        Assert.IsType<Assignment>(expStmt.Expression);
    }

    [Fact]
    public void NotKeyword_IsSyntacticallyAccepted()
    {
        // "not" is a recognised keyword; the syntax is valid and must parse without errors.
        Parser parser = TestHelpers.ParseProgram(TestPrograms.NotFalse);
        Assert.Equal(0, parser.errors.count);
    }

    [Fact]
    public void UnaryMinus_ShouldBeSyntacticallyValid()
    {
        // "Number x = -5;" is valid syntax and must parse without errors.
        Parser parser = TestHelpers.ParseProgram(TestPrograms.UnaryMinusFive);
        Assert.Equal(0, parser.errors.count);
    }

    // ── DateTime / Duration: parser accepts valid syntax ─────────────────────

    [Fact]
    public void ValidDateTimeDeclaration_ParsesSuccessfully()
    {
        TestHelpers.ParseShouldSucceed(TestPrograms.ValidDateTimeDeclaration);
    }

    [Fact]
    public void ValidDateTimeWithTime_ParsesSuccessfully()
    {
        // Date + optional time component — both parts must be recognised by the scanner.
        TestHelpers.ParseShouldSucceed("DateTime dt = 15/03-2026 14:00;");
    }

    [Fact]
    public void ValidDurationDeclaration_ParsesSuccessfully()
    {
        TestHelpers.ParseShouldSucceed(TestPrograms.ValidDurationDeclaration);
    }

    [Fact]
    public void ValidDuration_WeekUnit_ParsesSuccessfully()
    {
        TestHelpers.ParseShouldSucceed("Duration dur = 1 week;");
    }

    [Fact]
    public void ValidDuration_HourUnit_ParsesSuccessfully()
    {
        TestHelpers.ParseShouldSucceed("Duration dur = 3 hours;");
    }

    [Fact]
    public void ValidDuration_MinuteUnit_ParsesSuccessfully()
    {
        TestHelpers.ParseShouldSucceed("Duration dur = 30 minutes;");
    }

    [Fact]
    public void ValidDuration_CompoundUnit_ParsesSuccessfully()
    {
        // Combined units: 1 week 2 days 3 hours 30 minutes.
        TestHelpers.ParseShouldSucceed("Duration dur = 1 week 2 days 3 hours 30 minutes;");
    }

    [Fact]
    public void ValidDateTimePlusDuration_ParsesSuccessfully()
    {
        TestHelpers.ParseShouldSucceed(TestPrograms.ValidDateTimePlusDuration);
    }

    [Fact]
    public void ValidDateTimeComparison_ParsesSuccessfully()
    {
        TestHelpers.ParseShouldSucceed(TestPrograms.ValidDateTimeComparison);
    }

    [Fact]
    public void ValidDurationComparison_ParsesSuccessfully()
    {
        TestHelpers.ParseShouldSucceed(TestPrograms.ValidDurationComparison);
    }

    [Fact]
    public void InvalidDateTimePlusDateTime_IsSyntacticallyValid()
    {
        // Syntax is fine; the type error is caught by the typechecker, not the parser.
        TestHelpers.ParseShouldSucceed(TestPrograms.InvalidDateTimePlusDateTime);
    }

    // ── Core RAL features: parser accepts valid syntax ────────────────────────

    [Fact]
    public void ValidMoveStatement_ParsesSuccessfully()
    {
        TestHelpers.ParseShouldSucceed(TestPrograms.ValidMoveResourceToCategory);
    }

    [Fact]
    public void ValidReserveStatement_ParsesSuccessfully()
    {
        TestHelpers.ParseShouldSucceed(TestPrograms.ValidReserveStatement);
    }

    [Fact]
    public void ValidAvailabilityQuery_ParsesSuccessfully()
    {
        TestHelpers.ParseShouldSucceed(TestPrograms.ValidAvailabilityQuery);
    }

    [Fact]
    public void ValidCancelReservation_ParsesSuccessfully()
    {
        TestHelpers.ParseShouldSucceed(TestPrograms.ValidCancelReservation);
    }

    [Fact]
    public void ValidRescheduleReservation_ParsesSuccessfully()
    {
        TestHelpers.ParseShouldSucceed(TestPrograms.ValidRescheduleReservation);
    }

    [Fact]
    public void ValidPropertyAccess_ParsesSuccessfully()
    {
        TestHelpers.ParseShouldSucceed(TestPrograms.ValidResourcePropertyAccess);
    }

    [Fact]
    public void ReserveWithForDuration_ParsesSuccessfully()
    {
        // "for Duration" alternative in the Time non-terminal.
        TestHelpers.ParseShouldSucceed(
            "category Room;\nRoom myRoom {}\nReservation res = reserve myRoom from 15/03-2026 for 2 days;");
    }

    [Fact]
    public void ReserveWithQuantityAndCategory_ParsesSuccessfully()
    {
        // "a*rc id" resource-spec form: "2 Room r".
        TestHelpers.ParseShouldSucceed(
            "category Room;\nRoom myRoom {}\nReservation res = reserve 2 Room from 15/03-2026 to 16/03-2026;");
    }

    [Fact]
    public void ReserveWithWhereClause_ParsesSuccessfully()
    {
        // Optional "where (Exp)" predicate in a query.
        TestHelpers.ParseShouldSucceed(
            "category Room;\nRoom myRoom { Number beds = 2; }\n" +
            "Reservation res = reserve 1 Room r from 15/03-2026 to 16/03-2026 where (r.beds == 2);");
    }

    [Fact]
    public void CancelNonReservation_IsSyntacticallyValid()
    {
        // Syntax is correct; the type error belongs to the typechecker.
        TestHelpers.ParseShouldSucceed(TestPrograms.InvalidCancelNonReservation);
    }

    // ── Where clauses: parser accepts valid syntax ────────────────────────────
    // All four programs are syntactically valid; semantic errors are for the typechecker.

    [Fact]
    public void ValidReserveWherePredicate_ParsesSuccessfully()
    {
        TestHelpers.ParseShouldSucceed(TestPrograms.ValidReserveWherePredicate);
    }

    [Fact]
    public void ReserveWhereNonBoolPredicate_IsSyntacticallyValid()
    {
        // Predicate type is a semantic concern; the parser must accept any expression.
        TestHelpers.ParseShouldSucceed(TestPrograms.InvalidReserveWhereNonBoolPredicate);
    }

    [Fact]
    public void ReserveWhereUnknownProperty_IsSyntacticallyValid()
    {
        // Unknown property names are semantic errors; the parser must accept the syntax.
        TestHelpers.ParseShouldSucceed(TestPrograms.InvalidReserveWhereUnknownProperty);
    }

    [Fact]
    public void ReserveWhereUnknownAlias_IsSyntacticallyValid()
    {
        // Unknown aliases are semantic errors; the parser must accept the identifier.
        TestHelpers.ParseShouldSucceed(TestPrograms.InvalidReserveWhereUnknownAlias);
    }

    // ── Template calls: parser accepts valid syntax ────────────────────────────

    [Fact]
    public void ValidTemplateCall_ParsesSuccessfully()
    {
        // "template booking(...) {} use booking(2, \"meeting\");" — must parse without errors.
        TestHelpers.ParseShouldSucceed(TestPrograms.ValidTemplateCall);
    }

    [Fact]
    public void UnknownTemplateCall_IsSyntacticallyValid()
    {
        // "use missingTemplate(2);" is syntactically correct.
        // Unknown template names are semantic/typechecker errors, not parser errors.
        TestHelpers.ParseShouldSucceed(TestPrograms.InvalidTemplateCallUnknownTemplate);
    }
}
