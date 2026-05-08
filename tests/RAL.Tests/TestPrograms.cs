namespace RAL.Tests;

/*
 * Reusable RAL source-program strings used across all test classes.
 *
 * IMPORTANT — reserved keywords in RAL that cannot be used as identifiers:
 *   move, to, cancel, if, then, else, Resource, Number, Bool, String,
 *   Category, Reservation, DateTime, Duration, category, is, a, template,
 *   or, and, seq, not, true, false, reschedule, use, check, reserve, where,
 *   from, for, w, week, weeks, d, day, days, h, hour, hours, m, minute,
 *   minutes, recurring, strict, flexible, every, until
 *
 * All variable names below are chosen to avoid these keywords.
 */
static class TestPrograms
{
    // ── Valid programs ──────────────────────────────────────────────────────

    public const string ValidArithmetic =
        "Number x = 2 / 5 * (2 + 2);";

    public const string ValidNumberDecl =
        "Number x = 42;";

    public const string ValidBoolLiteral =
        "Bool b = true;";

    public const string ValidStringDecl =
        """String s = "hello";""";

    public const string ValidCategory =
        "category Room;";

    public const string ValidCategoryHierarchy =
        "category Room;\ncategory DoubleRoom is a Room;";

    public const string ValidResourceDecl =
        "category Room;\nRoom myRoom {}";

    public const string ValidResourceWithProperty =
        "category Room;\nRoom myRoom { Number beds = 2; }";

    public const string ValidResourceTemplate =
        """
        category Room;
        Room myRoom {}
        template booking(Number qty, String label) {
          Number count = qty;
        }
        """;

    public const string ValidNestedArithmetic =
        "Number x = 2 + 3 * 4;";

    public const string ValidParenArithmetic =
        "Number x = (2 + 3) * 4;";

    public const string ValidBoolExpr =
        "Bool b = true and false;";

    public const string ValidShadowing =
        "Number x = 5;\ntemplate t(Number x) {\n  Number y = x;\n}";

    public const string ValidIfStmt =
        "Bool cond = true;\nif (cond) then { Bool p = false; } else { Bool q = true; }";

    public const string ValidMultipleDecls =
        "Number x = 1;\nNumber y = 2;\nNumber z = 3;";

    // ── Unary-operator programs ──────────────────────────────────────────────

    // "not(false)" should produce UnaryOperation(NOT, BoolV(false)) and evaluate to BoolVal(true).
    public const string NotFalse =
        "Bool result = not(false);";

    // "not(true)" should produce UnaryOperation(NOT, BoolV(true)) and evaluate to BoolVal(false).
    public const string NotTrue =
        "Bool result = not(true);";

    // "not(5)" should be rejected by the typechecker because NOT requires Bool.
    public const string NotOnNumber =
        "Number n = not(5);";

    // "Number x = -5;" should produce UnaryOperation(NEG, NumberV(5)) and evaluate to NumberVal(-5).
    public const string UnaryMinusFive =
        "Number x = -5;";

    // ── DateTime / Duration programs ──────────────────────────────────────────
    //
    // DateTime literal format (from grammar):
    //   dateLit = digit digit '/' digit digit '-' digit digit digit digit
    //   timeLit = digit digit ':' digit digit
    //   Full example:  15/03-2026        (date only)
    //                  15/03-2026 14:00  (date + time)
    //
    // Duration literal format (from grammar):
    //   number unit, optionally combined e.g. 1 week 2 days 3 hours 30 minutes
    //   Units: w/week/weeks  d/day/days  h/hour/hours  m/minute/minutes

    // "DateTime dt = 15/03-2026;" — plain date literal, no time component.
    public const string ValidDateTimeDeclaration =
        "DateTime dt = 15/03-2026;";

    // "Duration dur = 2 days;" — simple day-based duration.
    public const string ValidDurationDeclaration =
        "Duration dur = 2 days;";

    // dt + dur → DateTime; all three variables in scope for the third assignment.
    public const string ValidDateTimePlusDuration =
        "DateTime startDate = 15/03-2026;\nDuration period = 2 days;\nDateTime endDate = startDate + period;";

    // dt - dur → DateTime; subtraction of a duration from a datetime.
    public const string ValidDateTimeMinusDuration =
        "DateTime startDate = 17/03-2026;\nDuration period = 2 days;\nDateTime endDate = startDate - period;";

    // Type error: ADD(DateTimeT, DateTimeT) is not a valid operation.
    public const string InvalidDateTimePlusDateTime =
        "DateTime begin = 15/03-2026;\nDateTime finish = 16/03-2026;\nDateTime sum = begin + finish;";

    // Type error: SUB only accepts (DateTimeT, DurationT), not (DateTimeT, DateTimeT).
    public const string InvalidDateTimeMinusDateTime =
        "DateTime begin = 16/03-2026;\nDateTime finish = 15/03-2026;\nDateTime diff = begin - finish;";

    // Type error: ADD(DurationT, DateTimeT) — operand order matters.
    public const string InvalidDurationPlusDateTime =
        "Duration period = 2 days;\nDateTime begin = 15/03-2026;\nDateTime result = period + begin;";

    // Relational comparison between two DateTimes → Bool.
    public const string ValidDateTimeComparison =
        "DateTime begin = 15/03-2026;\nDateTime finish = 16/03-2026;\nBool isBefore = begin < finish;";

    // Relational comparison between two Durations → Bool.
    public const string ValidDurationComparison =
        "Duration brief = 1 hour;\nDuration extended = 2 hours;\nBool isBriefer = brief < extended;";

    // Type error: DateTime literal assigned to a Number variable.
    public const string InvalidDateTimeAssignedToNumber =
        "Number n = 15/03-2026;";

    // Type error: Duration literal assigned to a DateTime variable.
    public const string InvalidDurationAssignedToDateTime =
        "DateTime dt = 2 days;";

    // ── Core RAL feature programs ─────────────────────────────────────────────

    // Move a resource to a declared subcategory.
    public const string ValidMoveResourceToCategory =
        "category Room;\ncategory Suite is a Room;\nRoom myRoom {}\nmove myRoom to Suite;";

    // Semantic error: resource identifier not declared → typechecker throws.
    public const string InvalidMoveUnknownResource =
        "category Room;\nmove ghost to Room;";

    // Semantic error: target category not declared → typechecker throws.
    public const string InvalidMoveUnknownCategory =
        "category Room;\nRoom myRoom {}\nmove myRoom to Unknown;";

    // Semantic error: move applied to a non-resource variable → added to errors list.
    public const string InvalidMoveNonResource =
        "Number n = 5;\ncategory Room;\nmove n to Room;";

    // Reserve a single named resource for a date-to-date interval.
    // "reserve myRoom from 15/03-2026 to 16/03-2026" is a Reserve expression.
    public const string ValidReserveStatement =
        "category Room;\nRoom myRoom {}\nReservation res = reserve myRoom from 15/03-2026 to 16/03-2026;";

    // Availability check for a single named resource.
    // "check" is the keyword for the Availability statement.
    public const string ValidAvailabilityQuery =
        "category Room;\nRoom myRoom {}\ncheck myRoom from 15/03-2026 to 16/03-2026;";

    // Cancel a known Reservation variable.
    public const string ValidCancelReservation =
        "category Room;\nRoom myRoom {}\nReservation res = reserve myRoom from 15/03-2026 to 16/03-2026;\ncancel res;";

    // Reschedule an existing reservation to a new interval.
    // "reschedule" is a Primary expression returning ReservationT.
    public const string ValidRescheduleReservation =
        "category Room;\nRoom myRoom {}\nReservation res = reserve myRoom from 15/03-2026 to 16/03-2026;\n" +
        "Reservation rescheduled = reschedule res from 20/03-2026 to 21/03-2026;";

    // Type error: cancel applied to a non-Reservation expression → error in errors list.
    public const string InvalidCancelNonReservation =
        "Number n = 5;\ncancel n;";

    // Semantic error: reserve references an undeclared resource → typechecker throws.
    public const string InvalidReserveUnknownResource =
        "category Room;\nReservation res = reserve ghost from 15/03-2026 to 16/03-2026;";

    // Semantic error: check references an undeclared resource → typechecker throws.
    public const string InvalidAvailabilityUnknownResource =
        "category Room;\ncheck ghost from 15/03-2026 to 16/03-2026;";

    // Property access on a declared resource field that exists.
    public const string ValidResourcePropertyAccess =
        "category Room;\nRoom myRoom { Number beds = 2; }\nNumber numBeds = myRoom.beds;";

    // Semantic error: property access on a field that does not exist in the resource.
    // Correct behavior: typechecker must report an error / throw for unknown property.
    public const string InvalidResourcePropertyAccess =
        "category Room;\nRoom myRoom {}\nNumber numFloors = myRoom.floors;";

    // ── Where-clause programs ────────────────────────────────────────────────
    //
    // Syntax reminder: where (Exp) — parentheses are required.
    // The alias bound in the resource spec (e.g. "r") is in scope inside the predicate.

    // Valid: alias r in scope, r.beds is a known Number field, r.beds == 2 is Bool.
    public const string ValidReserveWherePredicate =
        "category Room;\nRoom room1 { Number beds = 2; }\n" +
        "Reservation res = reserve 1 Room r from 15/03-2026 to 16/03-2026 where (r.beds == 2);";

    // Semantic error: predicate r.beds + 2 has type Number, not Bool.
    // Intended: typechecker must add error to tc.errors.
    public const string InvalidReserveWhereNonBoolPredicate =
        "category Room;\nRoom room1 { Number beds = 2; }\n" +
        "Reservation res = reserve 1 Room r from 15/03-2026 to 16/03-2026 where (r.beds + 2);";

    // Semantic error: 'floors' is not a declared property of Room resources.
    // Intended: typechecker must add error to tc.errors.
    public const string InvalidReserveWhereUnknownProperty =
        "category Room;\nRoom room1 { Number beds = 2; }\n" +
        "Reservation res = reserve 1 Room r from 15/03-2026 to 16/03-2026 where (r.floors == 2);";

    // Semantic error: alias 'x' was never introduced in the resource spec; 'r' was.
    // Intended: typechecker must add error to tc.errors (currently throws).
    public const string InvalidReserveWhereUnknownAlias =
        "category Room;\nRoom room1 { Number beds = 2; }\n" +
        "Reservation res = reserve 1 Room r from 15/03-2026 to 16/03-2026 where (x.beds == 2);";

    // ── Template call programs ───────────────────────────────────────────────

    // Valid: declaration then call — argument types match declared parameter types.
    public const string ValidTemplateCall =
        """
        template booking(Number n, String label) {}
        use booking(2, "meeting");
        """;

    // Type error: template expects Number for first arg but receives String.
    public const string InvalidTemplateCallWrongArgType =
        """
        template booking(Number n) {}
        use booking("wrong");
        """;

    // Semantic error: template expects 2 arguments but call supplies only 1.
    public const string InvalidTemplateCallWrongArgCount =
        """
        template booking(Number n, String label) {}
        use booking(2);
        """;

    // Semantic error: 'missingTemplate' was never declared.
    // Intended behavior: typechecker must add an error to tc.errors, not throw.
    public const string InvalidTemplateCallUnknownTemplate =
        "use missingTemplate(2);";

    // ── Invalid syntax ──────────────────────────────────────────────────────

    // Missing semicolon after statement.
    public const string InvalidSyntaxMissingSemicolon =
        "Number x = 5";

    // Declaration with an operator directly after identifier — bad grammar.
    public const string InvalidSyntaxBadDecl =
        "Number cd/2;";

    // Parenthesis never closed.
    public const string InvalidSyntaxUnclosedParen =
        "Number x = (5 + 3;";

    // ── Type-error programs (valid syntax, bad types) ────────────────────────

    public const string InvalidTypeStringAssignedToNumber =
        """Number x = "hello";""";

    public const string InvalidTypeBoolDivision =
        "Number s = true / false;";

    // Both declarations bind the same name in the same scope — throws.
    public const string InvalidTypeDuplicateVar =
        "Number x = 5;\nNumber x = 10;";

    // Reference to an undeclared variable — throws.
    public const string InvalidTypeUndeclaredVar =
        "Number x = undeclared;";

    // if-statement with a non-bool condition.
    public const string InvalidTypeIfNonBoolCondition =
        "Number n = 5;\nif (n) then { Bool c = true; }";
}
