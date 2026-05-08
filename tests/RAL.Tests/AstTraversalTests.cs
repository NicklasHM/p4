using RAL.AST;

namespace RAL.Tests;

/*
 * Test-only AST traversal utility.
 * This visitor exists solely to support assertions in the test suite.
 * It never touches production AST classes.
 */
file static class AstVisitor
{
    // Count occurrences of each concrete Stmt/Exp type in the tree.
    public static Dictionary<Type, int> CountNodes(Stmt root)
    {
        var counts = new Dictionary<Type, int>();
        WalkStmt(root, counts);
        return counts;
    }

    private static void WalkStmt(Stmt? stmt, Dictionary<Type, int> c)
    {
        if (stmt is null) return;
        Increment(c, stmt.GetType());

        switch (stmt)
        {
            case Composite s:
                WalkStmt(s.Stmt1, c);
                WalkStmt(s.Stmt2, c);
                break;
            case VarDecl:
                break;
            case ResourceDecl rd:
                if (rd.PropertyList is not null)
                    foreach (Stmt p in rd.PropertyList) WalkStmt(p, c);
                break;
            case CategoryDecl:
                break;
            case TemplateDecl td:
                WalkStmt(td.TemplateBody, c);
                break;
            case If i:
                WalkExp(i.Condition, c);
                WalkStmt(i.ThenBody, c);
                WalkStmt(i.ElseBody, c);
                break;
            case ExpStmt es:
                WalkExp(es.Expression, c);
                break;
            case TemplateCall tc:
                if (tc.ArgList is not null)
                    foreach (Exp a in tc.ArgList) WalkExp(a, c);
                break;
            case Cancel cn:
                WalkExp(cn.Reservation, c);
                break;
            case Move:
            case Availability:
            case Skip:
                break;
        }
    }

    private static void WalkExp(Exp? exp, Dictionary<Type, int> c)
    {
        if (exp is null) return;
        Increment(c, exp.GetType());

        switch (exp)
        {
            case BinaryOperation b:
                WalkExp(b.LeftExpression, c);
                WalkExp(b.RightExpression, c);
                break;
            case UnaryOperation u:
                WalkExp(u.Expression, c);
                break;
            case Assignment a:
                WalkExp(a.Value, c);
                break;
            case Reschedule r:
                WalkExp(r.Reservation, c);
                break;
            case Reserve:
            case Reference:
            case NumberV:
            case BoolV:
            case StringV:
            case DateTimeV:
            case DurationV:
                break;
        }
    }

    // Returns true if any Reference node in the tree has a non-null PropertyId.
    public static bool FindAnyReferenceWithPropertyId(Stmt root)
    {
        return SearchStmtForPropertyRef(root);
    }

    private static bool SearchStmtForPropertyRef(Stmt? stmt)
    {
        if (stmt is null) return false;
        return stmt switch
        {
            Composite c   => SearchStmtForPropertyRef(c.Stmt1) || SearchStmtForPropertyRef(c.Stmt2),
            ExpStmt es    => SearchExpForPropertyRef(es.Expression),
            If i          => SearchExpForPropertyRef(i.Condition)
                             || SearchStmtForPropertyRef(i.ThenBody)
                             || SearchStmtForPropertyRef(i.ElseBody),
            TemplateDecl td => SearchStmtForPropertyRef(td.TemplateBody),
            ResourceDecl rd => rd.PropertyList?.Any(p => SearchStmtForPropertyRef(p)) ?? false,
            Cancel cn     => SearchExpForPropertyRef(cn.Reservation),
            _             => false
        };
    }

    private static bool SearchExpForPropertyRef(Exp? exp)
    {
        if (exp is null) return false;
        return exp switch
        {
            Reference r when r.PropertyId != null => true,
            BinaryOperation b => SearchExpForPropertyRef(b.LeftExpression) || SearchExpForPropertyRef(b.RightExpression),
            UnaryOperation u  => SearchExpForPropertyRef(u.Expression),
            Assignment a      => SearchExpForPropertyRef(a.Value),
            _                 => false
        };
    }

    // Returns true if any ResourceDecl in the tree has a non-null, non-empty PropertyList.
    public static bool FindResourceDeclWithProperties(Stmt root)
    {
        return SearchStmtForResourceWithProps(root);
    }

    private static bool SearchStmtForResourceWithProps(Stmt? stmt)
    {
        if (stmt is null) return false;
        return stmt switch
        {
            ResourceDecl rd when rd.PropertyList is { Count: > 0 } => true,
            Composite c => SearchStmtForResourceWithProps(c.Stmt1) || SearchStmtForResourceWithProps(c.Stmt2),
            _           => false
        };
    }

    // Returns the where-clause Condition expression from the first Reserve or Availability
    // query found in the tree, or null if none exists or the clause is absent.
    // Note: AstVisitor.WalkStmt does not walk into Reserve/Availability query data,
    // so CountNodes cannot be used for this purpose — use this helper instead.
    public static Exp? FindFirstQueryCondition(Stmt root)
        => SearchForQueryCondition(root);

    private static Exp? SearchForQueryCondition(Stmt? stmt)
    {
        if (stmt is null) return null;
        return stmt switch
        {
            Availability av => av.Query.Condition,
            Composite c     => SearchForQueryCondition(c.Stmt1) ?? SearchForQueryCondition(c.Stmt2),
            ExpStmt es      => FindQueryConditionInExp(es.Expression),
            TemplateDecl td => SearchForQueryCondition(td.TemplateBody),
            _               => null
        };
    }

    private static Exp? FindQueryConditionInExp(Exp? exp)
    {
        if (exp is null) return null;
        return exp switch
        {
            Reserve r    => r.Query.Condition,
            Assignment a => FindQueryConditionInExp(a.Value),
            _            => null
        };
    }

    // Returns the first TemplateCall node found in the statement tree, or null.
    public static TemplateCall? FindFirstTemplateCall(Stmt root)
        => SearchForTemplateCall(root);

    private static TemplateCall? SearchForTemplateCall(Stmt? stmt)
    {
        if (stmt is null) return null;
        return stmt switch
        {
            TemplateCall tc => tc,
            Composite c     => SearchForTemplateCall(c.Stmt1) ?? SearchForTemplateCall(c.Stmt2),
            TemplateDecl td => SearchForTemplateCall(td.TemplateBody),
            _               => null
        };
    }

    private static void Increment(Dictionary<Type, int> c, Type t)
    {
        c.TryGetValue(t, out int n);
        c[t] = n + 1;
    }
}

// ── Tests ────────────────────────────────────────────────────────────────────

public class AstTraversalTests
{
    // ── Operator-precedence structure ─────────────────────────────────────────

    [Fact]
    public void AddMul_Precedence_AddIsAboveMul()
    {
        // "Number x = 2 + 3 * 4;" should produce:
        //   BinaryOperation(ADD, NumberV(2), BinaryOperation(MUL, NumberV(3), NumberV(4)))
        // i.e. ADD wraps MUL, meaning ADD is the outer (higher) node.
        Stmt root = TestHelpers.ParseShouldSucceed(TestPrograms.ValidNestedArithmetic);
        Exp rhs = TestHelpers.ExtractFirstAssignmentRhs(root);

        var add = Assert.IsType<BinaryOperation>(rhs);
        Assert.Equal(BinaryOperator.ADD, add.Operator);

        // Right child of ADD must be MUL
        var mul = Assert.IsType<BinaryOperation>(add.RightExpression);
        Assert.Equal(BinaryOperator.MUL, mul.Operator);
    }

    [Fact]
    public void ParenthesesOverride_MulIsAboveAdd()
    {
        // "Number x = (2 + 3) * 4;" should produce:
        //   BinaryOperation(MUL, BinaryOperation(ADD, NumberV(2), NumberV(3)), NumberV(4))
        // i.e. MUL wraps ADD.
        Stmt root = TestHelpers.ParseShouldSucceed(TestPrograms.ValidParenArithmetic);
        Exp rhs = TestHelpers.ExtractFirstAssignmentRhs(root);

        var mul = Assert.IsType<BinaryOperation>(rhs);
        Assert.Equal(BinaryOperator.MUL, mul.Operator);

        // Left child of MUL must be ADD
        var add = Assert.IsType<BinaryOperation>(mul.LeftExpression);
        Assert.Equal(BinaryOperator.ADD, add.Operator);
    }

    // ── Node counting ─────────────────────────────────────────────────────────

    [Fact]
    public void MultipleDecls_CountsVarDeclCorrectly()
    {
        // "Number x = 1; Number y = 2; Number z = 3;" → three VarDecl nodes
        Stmt root = TestHelpers.ParseShouldSucceed(TestPrograms.ValidMultipleDecls);
        var counts = AstVisitor.CountNodes(root);
        Assert.True(counts.TryGetValue(typeof(VarDecl), out int n) && n == 3,
            $"Expected 3 VarDecl nodes, got {(counts.TryGetValue(typeof(VarDecl), out int m) ? m : 0)}");
    }

    [Fact]
    public void SingleDecl_ContainsExactlyOneVarDecl()
    {
        Stmt root = TestHelpers.ParseShouldSucceed(TestPrograms.ValidNumberDecl);
        var counts = AstVisitor.CountNodes(root);
        Assert.Equal(1, counts.GetValueOrDefault(typeof(VarDecl)));
    }

    [Fact]
    public void NestedArithmetic_ContainsTwoBinaryOperations()
    {
        // "Number x = 2 + 3 * 4;" → ADD and MUL = 2 BinaryOperation nodes
        Stmt root = TestHelpers.ParseShouldSucceed(TestPrograms.ValidNestedArithmetic);
        var counts = AstVisitor.CountNodes(root);
        Assert.Equal(2, counts.GetValueOrDefault(typeof(BinaryOperation)));
    }

    [Fact]
    public void ParenArithmetic_ContainsTwoBinaryOperations()
    {
        // "(2 + 3) * 4" → ADD and MUL = 2 BinaryOperation nodes
        Stmt root = TestHelpers.ParseShouldSucceed(TestPrograms.ValidParenArithmetic);
        var counts = AstVisitor.CountNodes(root);
        Assert.Equal(2, counts.GetValueOrDefault(typeof(BinaryOperation)));
    }

    [Fact]
    public void BoolExpr_ContainsOneAndBinaryOperation()
    {
        // "Bool b = true and false;"
        Stmt root = TestHelpers.ParseShouldSucceed(TestPrograms.ValidBoolExpr);
        Exp rhs = TestHelpers.ExtractFirstAssignmentRhs(root);
        var and = Assert.IsType<BinaryOperation>(rhs);
        Assert.Equal(BinaryOperator.AND, and.Operator);
    }

    // ── Unary operators: AST shape ────────────────────────────────────────────

    [Fact]
    public void NotKeyword_ShouldProduceUnaryOperationNode()
    {
        // "Bool result = not(false);" must produce
        //   UnaryOperation(NOT, BoolV(false))
        // as the right-hand side of the assignment.
        Stmt root = TestHelpers.ParseShouldSucceed(TestPrograms.NotFalse);
        Exp rhs = TestHelpers.ExtractFirstAssignmentRhs(root);

        var unary = Assert.IsType<UnaryOperation>(rhs);
        Assert.Equal(UnaryOperator.NOT, unary.Operator);
        Assert.IsType<BoolV>(unary.Expression);
        Assert.False(((BoolV)unary.Expression).Value);
    }

    [Fact]
    public void UnaryMinus_ShouldProduceNegOperationNode()
    {
        // "Number x = -5;" must produce
        //   UnaryOperation(NEG, NumberV(5))
        // as the right-hand side of the assignment.
        Stmt root = TestHelpers.ParseShouldSucceed(TestPrograms.UnaryMinusFive);
        Exp rhs = TestHelpers.ExtractFirstAssignmentRhs(root);

        var unary = Assert.IsType<UnaryOperation>(rhs);
        Assert.Equal(UnaryOperator.NEG, unary.Operator);
        Assert.IsType<NumberV>(unary.Expression);
    }

    // ── Composite structure ───────────────────────────────────────────────────

    [Fact]
    public void IfStatement_ContainsIfNode()
    {
        Stmt root = TestHelpers.ParseShouldSucceed(TestPrograms.ValidIfStmt);
        var counts = AstVisitor.CountNodes(root);
        Assert.True(counts.ContainsKey(typeof(If)),
            "Expected an If node in the AST.");
    }

    [Fact]
    public void ResourceDecl_IsInAST()
    {
        Stmt root = TestHelpers.ParseShouldSucceed(TestPrograms.ValidResourceDecl);
        var counts = AstVisitor.CountNodes(root);
        Assert.True(counts.ContainsKey(typeof(ResourceDecl)),
            "Expected a ResourceDecl node in the AST.");
    }

    [Fact]
    public void TemplateDecl_IsInAST()
    {
        Stmt root = TestHelpers.ParseShouldSucceed(TestPrograms.ValidResourceTemplate);
        var counts = AstVisitor.CountNodes(root);
        Assert.True(counts.ContainsKey(typeof(TemplateDecl)),
            "Expected a TemplateDecl node in the AST.");
    }

    [Fact]
    public void CategoryDecl_IsInAST()
    {
        Stmt root = TestHelpers.ParseShouldSucceed(TestPrograms.ValidCategory);
        var counts = AstVisitor.CountNodes(root);
        Assert.True(counts.ContainsKey(typeof(CategoryDecl)),
            "Expected a CategoryDecl node in the AST.");
    }

    // ── DateTime / Duration: AST shape ───────────────────────────────────────

    [Fact]
    public void DateTimeLiteral_ParsesAsDateTimeV()
    {
        // "DateTime dt = 15/03-2026;" → RHS of assignment must be DateTimeV.
        Stmt root = TestHelpers.ParseShouldSucceed(TestPrograms.ValidDateTimeDeclaration);
        Exp rhs = TestHelpers.ExtractFirstAssignmentRhs(root);
        Assert.IsType<DateTimeV>(rhs);
    }

    [Fact]
    public void DateTimeContainsCorrectDate()
    {
        // The parsed DateTimeV must carry March 15, 2026.
        Stmt root = TestHelpers.ParseShouldSucceed(TestPrograms.ValidDateTimeDeclaration);
        Exp rhs = TestHelpers.ExtractFirstAssignmentRhs(root);
        var dt = Assert.IsType<DateTimeV>(rhs);
        Assert.Equal(new DateTime(2026, 3, 15), dt.Value.Date);
    }

    [Fact]
    public void DateTimeLiteralWithTime_ParsesAsDateTimeV()
    {
        // "DateTime dt = 15/03-2026 14:00;" — optional time component must be accepted.
        Stmt root = TestHelpers.ParseShouldSucceed("DateTime dt = 15/03-2026 14:00;");
        Exp rhs = TestHelpers.ExtractFirstAssignmentRhs(root);
        var dt = Assert.IsType<DateTimeV>(rhs);
        Assert.Equal(14, dt.Value.Hour);
        Assert.Equal(0, dt.Value.Minute);
    }

    [Fact]
    public void DurationLiteral_ParsesAsDurationV()
    {
        // "Duration dur = 2 days;" → RHS of assignment must be DurationV.
        Stmt root = TestHelpers.ParseShouldSucceed(TestPrograms.ValidDurationDeclaration);
        Exp rhs = TestHelpers.ExtractFirstAssignmentRhs(root);
        Assert.IsType<DurationV>(rhs);
    }

    [Fact]
    public void DurationContainsCorrectTimeSpan()
    {
        // "2 days" → DurationV with TimeSpan of exactly 2 days.
        Stmt root = TestHelpers.ParseShouldSucceed(TestPrograms.ValidDurationDeclaration);
        Exp rhs = TestHelpers.ExtractFirstAssignmentRhs(root);
        var dur = Assert.IsType<DurationV>(rhs);
        Assert.Equal(TimeSpan.FromDays(2), dur.Value);
    }

    [Fact]
    public void DateTimePlusDurationInline_IsBinaryAddWithDateTimeVAndDurationV()
    {
        // "DateTime dt = 15/03-2026 + 2 days;" — RHS must be ADD(DateTimeV, DurationV).
        Stmt root = TestHelpers.ParseShouldSucceed("DateTime dt = 15/03-2026 + 2 days;");
        Exp rhs = TestHelpers.ExtractFirstAssignmentRhs(root);
        var add = Assert.IsType<BinaryOperation>(rhs);
        Assert.Equal(BinaryOperator.ADD, add.Operator);
        Assert.IsType<DateTimeV>(add.LeftExpression);
        Assert.IsType<DurationV>(add.RightExpression);
    }

    [Fact]
    public void DateTimeComparison_IsBinaryLtWithTwoDateTimeV()
    {
        // "Bool result = 15/03-2026 < 16/03-2026;" — must be LT(DateTimeV, DateTimeV).
        Stmt root = TestHelpers.ParseShouldSucceed("Bool result = 15/03-2026 < 16/03-2026;");
        Exp rhs = TestHelpers.ExtractFirstAssignmentRhs(root);
        var lt = Assert.IsType<BinaryOperation>(rhs);
        Assert.Equal(BinaryOperator.LT, lt.Operator);
        Assert.IsType<DateTimeV>(lt.LeftExpression);
        Assert.IsType<DateTimeV>(lt.RightExpression);
    }

    // ── Core RAL features: AST shape ─────────────────────────────────────────

    [Fact]
    public void ReserveExpression_CreatesReserveNode()
    {
        Stmt root = TestHelpers.ParseShouldSucceed(TestPrograms.ValidReserveStatement);
        var counts = AstVisitor.CountNodes(root);
        Assert.True(counts.ContainsKey(typeof(Reserve)),
            "Expected a Reserve node in the AST.");
    }

    [Fact]
    public void AvailabilityCheck_CreatesAvailabilityNode()
    {
        Stmt root = TestHelpers.ParseShouldSucceed(TestPrograms.ValidAvailabilityQuery);
        var counts = AstVisitor.CountNodes(root);
        Assert.True(counts.ContainsKey(typeof(Availability)),
            "Expected an Availability node in the AST.");
    }

    [Fact]
    public void MoveStatement_CreatesMoveNode()
    {
        Stmt root = TestHelpers.ParseShouldSucceed(TestPrograms.ValidMoveResourceToCategory);
        var counts = AstVisitor.CountNodes(root);
        Assert.True(counts.ContainsKey(typeof(Move)),
            "Expected a Move node in the AST.");
    }

    [Fact]
    public void CancelStatement_CreatesCancelNode()
    {
        Stmt root = TestHelpers.ParseShouldSucceed(TestPrograms.ValidCancelReservation);
        var counts = AstVisitor.CountNodes(root);
        Assert.True(counts.ContainsKey(typeof(Cancel)),
            "Expected a Cancel node in the AST.");
    }

    [Fact]
    public void RescheduleExpression_CreatesRescheduleNode()
    {
        Stmt root = TestHelpers.ParseShouldSucceed(TestPrograms.ValidRescheduleReservation);
        var counts = AstVisitor.CountNodes(root);
        Assert.True(counts.ContainsKey(typeof(Reschedule)),
            "Expected a Reschedule node in the AST.");
    }

    [Fact]
    public void PropertyAccess_CreatesReferenceWithPropertyId()
    {
        // "myRoom.beds" must produce Reference("myRoom", "beds") — PropertyId is non-null.
        Stmt root = TestHelpers.ParseShouldSucceed(TestPrograms.ValidResourcePropertyAccess);
        Assert.True(AstVisitor.FindAnyReferenceWithPropertyId(root),
            "Expected at least one Reference with a non-null PropertyId.");
    }

    [Fact]
    public void ResourceDeclWithProperties_HasNonEmptyPropertyList()
    {
        // "Room myRoom { Number beds = 2; }" → ResourceDecl.PropertyList must be non-empty.
        Stmt root = TestHelpers.ParseShouldSucceed(TestPrograms.ValidResourceWithProperty);
        Assert.True(AstVisitor.FindResourceDeclWithProperties(root),
            "Expected a ResourceDecl with a non-null, non-empty PropertyList.");
    }

    // ── Where clauses: AST shape ──────────────────────────────────────────────

    [Fact]
    public void ReserveWhereClause_HasNonNullCondition()
    {
        // "reserve ... where (r.beds == 2);" — QueryData.Condition must not be null.
        Stmt root = TestHelpers.ParseShouldSucceed(TestPrograms.ValidReserveWherePredicate);
        Exp? condition = AstVisitor.FindFirstQueryCondition(root);
        Assert.NotNull(condition);
    }

    [Fact]
    public void ReserveWhereClause_ConditionHasPropertyReference()
    {
        // "where (r.beds == 2)" — condition must be EQ(Reference("r","beds"), NumberV).
        Stmt root = TestHelpers.ParseShouldSucceed(TestPrograms.ValidReserveWherePredicate);
        Exp? condition = AstVisitor.FindFirstQueryCondition(root);
        Assert.NotNull(condition);
        var eq = Assert.IsType<BinaryOperation>(condition!);
        Assert.Equal(BinaryOperator.EQ, eq.Operator);
        var propertyRef = Assert.IsType<Reference>(eq.LeftExpression);
        Assert.Equal("r", propertyRef.VariableId);
        Assert.Equal("beds", propertyRef.PropertyId);
    }

    // ── Template calls: AST shape ──────────────────────────────────────────────

    [Fact]
    public void TemplateCall_CreatesTemplateCallNode()
    {
        // "use booking(...);" must produce a TemplateCall node in the AST.
        Stmt root = TestHelpers.ParseShouldSucceed(TestPrograms.ValidTemplateCall);
        var counts = AstVisitor.CountNodes(root);
        Assert.True(counts.ContainsKey(typeof(TemplateCall)),
            "Expected a TemplateCall node in the AST.");
    }

    [Fact]
    public void TemplateCall_HasCorrectTemplateId()
    {
        // "use booking(2, \"meeting\");" must produce TemplateCall with TemplateId == "booking".
        Stmt root = TestHelpers.ParseShouldSucceed(TestPrograms.ValidTemplateCall);
        TemplateCall? tc = AstVisitor.FindFirstTemplateCall(root);
        Assert.NotNull(tc);
        Assert.Equal("booking", tc!.TemplateId);
    }

    [Fact]
    public void TemplateCall_HasCorrectArgumentCount()
    {
        // "use booking(2, \"meeting\");" must produce TemplateCall with ArgList of length 2.
        Stmt root = TestHelpers.ParseShouldSucceed(TestPrograms.ValidTemplateCall);
        TemplateCall? tc = AstVisitor.FindFirstTemplateCall(root);
        Assert.NotNull(tc);
        Assert.Equal(2, tc!.ArgList?.Count);
    }
}
