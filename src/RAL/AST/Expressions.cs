namespace RAL.AST
{
    public abstract class Expr : AstNode { }

    // Literals
    public class NumberLiteral : Expr { public double Value { get; set; } }
    public class StringLiteral : Expr { public string Value { get; set; } }
    public class BoolLiteral   : Expr { public bool   Value { get; set; } }

    // Variables
    public class IdentExpr : Expr
    {
        public string Name  { get; set; }
        public string Field { get; set; }  // null if not a.b
    }

    public class AssignExpr : Expr
    {
        public string Name  { get; set; }
        public string Field { get; set; }  // null if not a.b
        public Expr   Value { get; set; }
    }

    // Operators
    public class BinaryExpr : Expr
    {
        public string Op    { get; set; }
        public Expr   Left  { get; set; }
        public Expr   Right { get; set; }
    }

    public class UnaryExpr : Expr
    {
        public string Op      { get; set; }  // "not"
        public Expr   Operand { get; set; }
    }

    // Time
    public class DurationExpr : Expr
    {
        public double? Weeks   { get; set; }
        public double? Days    { get; set; }
        public double? Hours   { get; set; }
        public double? Minutes { get; set; }
    }

    public class DateTimeExpr : Expr
    {
        public string       DateLit  { get; set; }
        public string       TimeLit  { get; set; }
        public string       OffsetOp { get; set; }  // "+" | "-" | null
        public DurationExpr Offset   { get; set; }  // null if no offset
    }

    public class TimeExpExpr : Expr
    {
        public DateTimeExpr From   { get; set; }
        public DateTimeExpr To     { get; set; }   // null if "for" form
        public DurationExpr ForDur { get; set; }   // null if "to" form
    }

    // Domain
    public class AvailabilityExpr : Expr
    {
        public QueryExpr Query { get; set; }
    }

    public class ReservationExpr : Expr
    {
        public QueryExpr Query { get; set; }
    }

    public class QueryExpr : Expr
    {
        public ResourceExpr   Resource   { get; set; }
        public TimeExpExpr    Time       { get; set; }
        public Expr           Where      { get; set; }      // null if absent
        public RecurrenceExpr Recurrence { get; set; }      // null if absent
    }

    public class ResourceExpr : Expr
    {
        public Expr         Quantity     { get; set; }  // null if no quantity
        public string       QuantityType { get; set; }  // category ident after qty
        public string       Name         { get; set; }
        public ResourceExpr Next         { get; set; }  // null if no "and"
    }

    public class RecurrenceExpr : Expr
    {
        public string       Kind   { get; set; }  // "strict" | "flexible"
        public DurationExpr Every  { get; set; }
        public DateTimeExpr Until  { get; set; }  // null if "for" form
        public DurationExpr ForDur { get; set; }  // null if "until" form
    }

    public class RescheduleExpr : Expr
    {
        public string      ReservationName { get; set; }
        public TimeExpExpr NewTime         { get; set; }
    }

    public class TemplateInvokeExpr : Expr
    {
        public string     Name { get; set; }
        public List<Expr> Args { get; set; }
    }
}