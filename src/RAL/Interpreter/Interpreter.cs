using Microsoft.Win32;
using RAL.AST;
using RAL.TC;

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

    public static void EvalStmt(Stmt stmt, EnvV envV, EnvH envH)
    {
        switch(stmt)
        {   case Skip: break;
           
            case Composite c: HandleComposite(c, envV, envH); break;
           
            case If i: HandleIf(i, envV, envH); break;

            case VarDecl vd: HandleVarDecl(vd, envV); break;

            case CategoryDecl cd: ExecCategoryDecl(cd, envH); break;
            case ResourceDecl rd: ExecResourceDecl(rd, envV); break;

            case Move m: ExecMove(m, envV); break;

            case ExpStmt s: Console.WriteLine(EvalExp(s.Expression, envV)); break;
            default: throw new Exception($"Unknown Statement: " + stmt.ToString());
        }
    }

    //Evaluates and returns an expression. Pattern matching on the AST nodes
    public static Value EvalExp(Exp exp, EnvV envV) {
        return exp switch {
            NumberV n => new NumberVal(n.Value),
            BoolV b => new BoolVal(b.Value),
            StringV s => new StringVal(s.Value),
            DateTimeV dt => new DateTimeVal(dt.Value),
            DurationV dur => new DurationVal(dur.Value),

            Reference r => EvalReference(r, envV),

            Assignment a => EvalAssignment(a, envV),

            UnaryOperation u => EvalUnary(u, envV),
            BinaryOperation b => EvalBinary(b, envV),

            _ => throw new Exception($"Line {exp.LineNumber}: Unsupported expression.")
        };
    }

    /* ________________________Statement Handlers______________________________*/
    
    private static void HandleComposite (Composite c, EnvV envV, EnvH envH) {
        // Evaluates left subtree first
        if(c.Stmt1 != null) EvalStmt(c.Stmt1, envV, envH);
        // Right subtree
        if(c.Stmt2 != null) EvalStmt(c.Stmt2, envV, envH);        
    }

    private static void HandleIf(If ifNode, EnvV envV, EnvH envH) {
        //Evaluate condition to interpreter values
        Value condition = EvalExp(ifNode.Condition, envV);

        //Downcast and extract bool value, guarenteed by typechecking. Bodies will be skip
        if (condition.AsBool())
            EvalStmt(ifNode.ThenBody, envV.NewScope(), envH); //will be skip if empty {}
        else 
            EvalStmt(ifNode.ElseBody, envV.NewScope(), envH); //will be skip if excluded or empty {}
    }

    private static void HandleVarDecl(VarDecl vdNode, EnvV envV) {

        //bind identifier to a default value based on type
        Value value = GetDefaultValue(vdNode.Type);

        envV.Bind(vdNode.Identifier, value);
    }

    /// <summary> Returns default values of uninitialized variables </summary>
    private static Value GetDefaultValue(TypeT type) => 
    type switch 
        {
            BoolT => new BoolVal(false),
            NumberT => new NumberVal(0),
            StringT => new StringVal(""),

            DateTimeT => new DateTimeVal(DateTime.Now),
            DurationT => new DurationVal(new TimeSpan(0,0,0,0)),

            //Empty list indicates rejected reservation attempt
            ReservationT => new ReservationVal(new List<ReservationAtomVal>()),

            _ => throw new Exception("VarDecl node with unsupported type")
        };

    private static void ExecResourceDecl(ResourceDecl resDecl, EnvV envV) {

        //resource 'body' {} gets new scope for properties to refer to each other by simple name. { Number x; Number y = x = 2; } 
        EnvV propertyScope = envV.NewScope();
        
        //Due to side-effects of assignments, populate the propertylist from propertyScope.Lookup(id) after loop. Collect only the ids
        HashSet<string> declaredPropertyIds = new();

        foreach (Stmt stmt in resDecl.PropertyList) {

            switch(stmt) {
                //Case 1: property declaration without assignment
                case VarDecl vd: 
                
                    Value defaultValue = GetDefaultValue(vd.Type);

                    propertyScope.Bind(vd.Identifier, defaultValue);

                    //For poupulating ResourceVal's propertyList from propertyScope after loop. 
                    declaredPropertyIds.Add(vd.Identifier);

                    break;
                
                //Case 2: property declaration with assignment
                case Composite { Stmt1: VarDecl varDecl, Stmt2: ExpStmt expStmt}: //Property pattern matching, both must be met
                    
                    //Bind lhs property id within scope, such that the reference of the assignment is bound when evaluating right hand side. 
                    propertyScope.Bind(varDecl.Identifier, GetDefaultValue(varDecl.Type));

                    //Recursively evaluates rhs of the assignment, including compound assignments                    
                    Value assignmentValue = EvalExp(expStmt.Expression, propertyScope);

                    //Set the actual value of lhs property id
                    propertyScope.Set(varDecl.Identifier, assignmentValue);

                    //For poupulating ResourceVal's propertyList from propertyScope after loop. 
                    declaredPropertyIds.Add(varDecl.Identifier);
                    
                    break;
            }   
        }

        //From the newly bound variables in propertyScope, build the property list, by lookups in said scope of collected ids
        Dictionary<string, Value> propertyList = declaredPropertyIds.ToDictionary(
            //Key selector function:        From the elements (ids) of declaredIds HashSet, set it as the key of dictonary entry.
            id => id, 
            //Value selector function:      Value of new dictionary entry is set to the returned Value from a lookup in the propertyScope.
            id => propertyScope.Lookup(id) 
        );

        ResourceVal resource = new ResourceVal(resDecl.Identifier, resDecl.Type.Category, propertyList);

        //Finally Bind the resource to envV (not propertyScope)
        envV.Bind(resDecl.Identifier, resource);

        //Add resource to the global registry, indexed by its category
        ResourceRegistry.Instance().AddResource(resDecl.Type.Category, resource);
    }

    private static void ExecCategoryDecl(CategoryDecl categoryDecl, EnvH envH) {

        //Add it to the registry, such that resources of this category can be added to it
        ResourceRegistry.Instance().RegisterCategory(categoryDecl.CategoryId);        

        //Relate it to its immediate super in envH
        envH.EstablishRelation(categoryDecl.CategoryId, categoryDecl.ParentId);
    }

    private static void ExecMove(Move moveNode, EnvV envV) {

        ResourceVal resourceToMove = (ResourceVal) envV.Lookup(moveNode.ResourceId);

        //Move to the righ category in the registry
        ResourceRegistry.Instance().MoveResource(resourceToMove, moveNode.Type.Category); 

        //Update categoryId on the resource for easy, currently innit only. Fix
        resourceToMove.CategoryId = moveNode.Type.Category;
    }

    /*_____________________Expression Handlers_____________________*/

    private static Value EvalReference(Reference r, EnvV envV) {
        
        //Id case
        if (r.PropertyId == null) {
            return envV.Lookup(r.VariableId);         
        }
        //id.id case, must be a resource property per type-checker
        else {
            //Downcast justified by typechecker, exception -> type-checker logic issue
            ResourceVal resource = (ResourceVal) envV.Lookup(r.VariableId);
            //Again an exception -> logic error in type-checker
            return resource.Properties[r.PropertyId];
        }        
    }

    private static Value EvalAssignment(Assignment a, EnvV envV) {

        //Extracting reference node for readability
        Reference reference = a.Variable;

        //Evaluate right hand side expression
        Value value = EvalExp(a.Expression, envV);

        //id case, write directly in envV
        if (reference.PropertyId == null) {
            envV.Set(reference.VariableId, value);         
        }
        //id.id case, must be a resource per type-checker
        else {
            //Downcast justified by typechecker, exception -> type-checker logic issue
            ResourceVal resource = (ResourceVal) envV.Lookup(reference.VariableId);

            //Like EnvR.Set: set the value of the resource's property
            resource.Properties[reference.PropertyId] = value;
        }
        //Either case: value of an assignment is the right hand side
        return value;
    }

    private static Value EvalBinary(BinaryOperation exp, EnvV envV) {
        Value left = EvalExp(exp.LeftExpression, envV);

        Value right = EvalExp(exp.RightExpression, envV);

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

            _ => throw new Exception($"Line {exp.LineNumber}: Invalid binary operation.") // Should never happen
        };
    }

    private static Value EvalUnary(UnaryOperation exp, EnvV envV) {

        //Evaluate inner expression
        Value value = EvalExp(exp.Expression, envV);

        return exp.Operator switch {
            UnaryOperator.NOT when value is BoolVal b
                => new BoolVal(!b.Value),
            
            UnaryOperator.NEG when value is NumberVal n
                => new NumberVal(-n.Value),

            _ => throw new Exception($"Line {exp.LineNumber}: Invalid unary operation.") // Should never happen
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