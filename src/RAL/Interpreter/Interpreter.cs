using System.Reflection.Metadata;
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

public class Interpreter {
    private const float Epsilon = 0.00001f;

    //Evaluates a statement with pattern matching on the AST nodes

    public static void EvalStmt(Stmt stmt)
    {
        switch(stmt)
        {   case Skip: break;
           
            case Composite c: HandleComposite(c); break;
           
            case If i: HandleIf(i); break;

            case VarDecl vd: HandleVarDecl(vd); break;

            case ExpStmt s: Console.WriteLine(EvalExp(s.Expression)); break;
            default: throw new Exception($"Unknown Statement:" + stmt.ToString());
        }
    }

    //Evaluates and returns an expression. Pattern matching on the AST nodes
    public static Value EvalExp(Exp exp) {
        return exp switch {
            NumberV n => new NumberVal(n.Value),
            BoolV b => new BoolVal(b.Value),
            StringV s => new StringVal(s.Value),
            DateTimeV dt => new DateTimeVal(dt.Value),
            DurationV dur => new DurationVal(dur.Value),

            UnaryOperation u => EvalUnary(u),
            BinaryOperation b => EvalBinary(b),

            _ => throw new Exception($"Line {exp.LineNumber}: Unsupported expression.")
        };
    }

    /* ________________________Statement Handlers______________________________*/
    
    static private void HandleComposite (Composite c) {
        // Evaluates left subtree first
        if(c.Stmt1 != null) EvalStmt(c.Stmt1);
        // Right subtree
        if(c.Stmt2 != null) EvalStmt(c.Stmt2);        
    }

    static private void HandleIf(If ifNode) {
        //Evaluate condition to interpreter values
        Value condition = EvalExp(ifNode.Condition);

        //Downcast and extract bool value, guarenteed by typechecking. Bodies will be skip
        if (condition.AsBool())
            EvalStmt(ifNode.ThenBody); //will be skip if empty {}
        else 
            EvalStmt(ifNode.ElseBody); //will be skip if excluded or empty {}
    }

    static private void HandleVarDecl(VarDecl vdNode) {

                
    }

    /*_____________________Expression Handlers_____________________*/

    static private Value EvalBinary(BinaryOperation exp) {
        Value left = EvalExp(exp.LeftExpression);

        Value right = EvalExp(exp.RightExpression);

        return exp.Operator switch {

            /*________________Arithmetic operations_______________*/
            
            //Numeric operands + - * /
            
            BinaryOperator.ADD when left is NumberVal l && right is NumberVal r
                => new NumberVal(l.Value + r.Value),

            BinaryOperator.SUB when left is NumberVal l && right is NumberVal r
                => new NumberVal(l.Value - r.Value),

            BinaryOperator.MUL when left is NumberVal l && right is NumberVal r
                => new NumberVal(l.Value * r.Value),
            
            BinaryOperator.DIV when left is NumberVal l && right is NumberVal r
                => DivideNumbers(l, r, exp.LineNumber), // x / 0 = inf in C# (??)

            
            //Time operands:    dt + dur,    dt - dur
            BinaryOperator.ADD when left is DateTimeVal dt && right is DurationVal dur
                => new DateTimeVal(dt.Value + dur.Value),
            
            BinaryOperator.SUB when left is DateTimeVal dt && right is DurationVal dur
                => new DateTimeVal(dt.Value - dur.Value),
        

            /*_________________Relational operations: Numeric _______________*/
            // <
            BinaryOperator.LT when left is NumberVal l && right is NumberVal r
                => new BoolVal(l.Value < r.Value),

            // >
            BinaryOperator.GT when left is NumberVal l && right is NumberVal r
                => new BoolVal(l.Value > r.Value),

            // <=
            BinaryOperator.LTEQ when left is NumberVal l && right is NumberVal r
                => new BoolVal(l.Value <= r.Value),

            // >=
            BinaryOperator.GTEQ when left is NumberVal l && right is NumberVal r
                => new BoolVal(l.Value >= r.Value),


            /*_________________Relational operations: DateTime _______________*/
            BinaryOperator.LT when left is DateTimeVal l && right is DateTimeVal r
                => new BoolVal(l.Value < r.Value),

            // >
            BinaryOperator.GT when left is DateTimeVal l && right is DateTimeVal r
                => new BoolVal(l.Value > r.Value),

            // <=
            BinaryOperator.LTEQ when left is DateTimeVal l && right is DateTimeVal r
                => new BoolVal(l.Value <= r.Value),

            // >=
            BinaryOperator.GTEQ when left is DateTimeVal l && right is DateTimeVal r
                => new BoolVal(l.Value >= r.Value),

            /*_________________Relational operations: Duration _______________*/
            BinaryOperator.LT when left is DurationVal l && right is DurationVal r
                => new BoolVal(l.Value < r.Value),

            // >
            BinaryOperator.GT when left is DurationVal l && right is DurationVal r
                => new BoolVal(l.Value > r.Value),

            // <=
            BinaryOperator.LTEQ when left is DurationVal l && right is DurationVal r
                => new BoolVal(l.Value <= r.Value),

            // >=
            BinaryOperator.GTEQ when left is DurationVal l && right is DurationVal r
                => new BoolVal(l.Value >= r.Value),

            /*__________________Equality: ==, !=________________________*/

            //Numeric operands
            BinaryOperator.EQ when left is NumberVal l && right is NumberVal r
                => new BoolVal(NumberEquals(l.Value, r.Value)),

            BinaryOperator.NEQ when left is NumberVal l && right is NumberVal r
                => new BoolVal(!NumberEquals(l.Value, r.Value)),


            //Boolean operands
            BinaryOperator.EQ when left is BoolVal l && right is BoolVal r
                => new BoolVal(l.Value == r.Value),

            BinaryOperator.NEQ when left is BoolVal l && right is BoolVal r
                => new BoolVal(l.Value != r.Value),


            //String operands
            BinaryOperator.EQ when left is StringVal l && right is StringVal r
                => new BoolVal(l.Value == r.Value),

            BinaryOperator.NEQ when left is StringVal l && right is StringVal r
                => new BoolVal(l.Value != r.Value),

            // DateTime operands
            BinaryOperator.EQ when left is DateTimeVal l && right is DateTimeVal r
                => new BoolVal(l.Value == r.Value),

            BinaryOperator.NEQ when left is DateTimeVal l && right is DateTimeVal r
                => new BoolVal(l.Value != r.Value),

            // Duration operands
            BinaryOperator.EQ when left is DurationVal l && right is DurationVal r
                => new BoolVal(l.Value == r.Value),

            BinaryOperator.NEQ when left is DurationVal l && right is DurationVal r
                => new BoolVal(l.Value != r.Value),

            /*__________________Logical Operators_________________*/
            
            //Boolean operands
            BinaryOperator.AND when left is BoolVal l && right is BoolVal r
                => new BoolVal(l.Value && r.Value),

            BinaryOperator.OR when left is BoolVal l && right is BoolVal r
                => new BoolVal(l.Value || r.Value),

            // Reserve and seq

            _ => throw new Exception($"Line {exp.LineNumber}: Invalid binary operation.")
        };
    }

    static private Value EvalUnary(UnaryOperation exp) {

        //Evaluate inner expression
        Value value = EvalExp(exp.Expression);

        return exp.Operator switch {
            UnaryOperator.NOT when value is BoolVal b
                => new BoolVal(!b.Value),
            
            UnaryOperator.NEG when value is NumberVal n
                => new NumberVal(-n.Value),

            _ => throw new Exception($"Line {exp.LineNumber}: Invalid unary operation.")
        };
    }


    private static NumberVal DivideNumbers(NumberVal left, NumberVal right, int lineNumber) {
        CheckDivisionByZero(right.Value, lineNumber);
        return new NumberVal(left.Value / right.Value);
    }

    private static bool NumberEquals(float a, float b) {
        // Floating point numbers should not be compared using exact equality.
        return Math.Abs(a - b) < Epsilon;
    }

    private static void CheckDivisionByZero(float value, int lineNumber) {
        // Because Number is represented as float, zero is also checked using epsilon.
        if (NumberEquals(value, 0f))
            throw new Exception($"Line {lineNumber}: Division by zero.");
    }
}