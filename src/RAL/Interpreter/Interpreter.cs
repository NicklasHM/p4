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

    public static void EvalStmt(Stmt stmt, EnvV envV, EnvH envH)
    {
        switch(stmt)
        {   case Skip: break;
           
            case Composite c: HandleComposite(c, envV, envH); break;
           
            case If i: HandleIf(i, envV, envH); break;

            case VarDecl vd: HandleVarDecl(vd, envV); break;

            case CategoryDecl cd: ExecCategoryDecl(cd, envH); break;
            case ResourceDecl rd: ExecResourceDecl(rd, envV, envH); break;

            //case TemplateDecl
            //case TemplateCall

            case Move m: ExecMove(m, envV); break;

            //case Cancel c: 

            case ExpStmt s: Console.WriteLine(EvalExp(s.Expression, envV, envH)); break;

            // case Availability av
            default: throw new Exception($"Unknown Statement: " + stmt.ToString());
        }
    }

    //Evaluates and returns an expression. Pattern matching on the AST nodes
    public static Value EvalExp(Exp exp, EnvV envV, EnvH envH) {
        return exp switch {
            NumberV n => new NumberVal(n.Value),
            BoolV b => new BoolVal(b.Value),
            StringV s => new StringVal(s.Value),
            DateTimeV dt => new DateTimeVal(dt.Value),
            DurationV dur => new DurationVal(dur.Value),

            Reference r => EvalReference(r, envV),
            
            Reserve reserveNode => EvalReserve(reserveNode, envV, envH),
            // reschedule join

            Assignment a => EvalAssignment(a, envV, envH),

            UnaryOperation u => EvalUnary(u, envV, envH),
            BinaryOperation b => EvalBinary(b, envV, envH), //  (reserve1, reserve2) defined for or, and, seq. Returns a composite reservation


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
        Value condition = EvalExp(ifNode.Condition, envV, envH);

        //Downcast and extract bool value, guarenteed by typechecking. Bodies will be skip
        if (condition.AsBool())
            EvalStmt(ifNode.ThenBody, envV.NewScope(), envH); //will be skip if empty {}
        else 
            EvalStmt(ifNode.ElseBody, envV.NewScope(), envH); //will be skip if empty {} or 'else' ommitted entirely in src code
    }

    private static void HandleVarDecl(VarDecl vdNode, EnvV envV) {

        //bind identifier to a default value based on type
        Value value = GetDefaultValue(vdNode.Type);

        envV.Bind(vdNode.Identifier, value);
    }

    /// <summary> Returns default values of uninitialized variables </summary>
    private static Value GetDefaultValue(TypeT type) => 
    type switch {
            BoolT => new BoolVal(false),
            NumberT => new NumberVal(0),
            StringT => new StringVal(""),

            DateTimeT => new DateTimeVal(DateTime.Now),
            DurationT => new DurationVal(new TimeSpan(0,0,0,0)),

            //Empty list indicates rejected reservation attempt
            ReservationT => new ReservationVal(new List<ReservationAtomVal>()),

            _ => throw new Exception("VarDecl node with unsupported type")
        };

    private static void ExecResourceDecl(ResourceDecl resDecl, EnvV envV, EnvH envH) {

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
                    Value assignmentValue = EvalExp(expStmt.Expression, propertyScope, envH);

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

    private static Value EvalAssignment(Assignment a, EnvV envV, EnvH envH) {

        //Extracting reference node for readability
        Reference reference = a.Variable;

        //Evaluate right hand side expression
        Value value = EvalExp(a.Expression, envV, envH);

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

/// <summary> Evaluates reserve leaf nodes, covering two cases. 1: simple, 2: with recurring </summary>
    private static ReservationVal EvalReserve(Reserve reserveNode, EnvV envV, EnvH envH) {
        QueryData originalQuery = reserveNode.Query;        

        //Extract reservation time period for the base query
        (DateTime originalStart, DateTime originalEnd, TimeSpan duration) = ComputeTime(originalQuery.Interval, envV, envH);

        //Treat all queries equally. Wether an AST without recurring or simulated for requrrence         
        ResolvedQuery baseQuery = new ResolvedQuery(originalQuery.ResourceSpecs, originalStart, originalEnd, originalQuery.Condition);

        ReservationVal result = ExecuteSingleReservation(baseQuery, envV, envH); 

        //Case 1: Simple reserve node, no recurring
        if (originalQuery.Recurrence == null) {
            return result;
        } 
        
        //Case 2: reserve node with recurring -> simmulate making queryData objects of nodes. Resolved queries shows purpose here.
        
        List<ResolvedQuery> timeSlots = new() { baseQuery };
        (TimeSpan timeBetween, DateTime recurrenceEnd) = ComputeRecurrencePeriod(originalQuery.Recurrence.Time, originalStart, envV, envH);
        
        //Accumulate each simple reservation request, starting from the second reservation occurrence.
        for (DateTime slotStart = originalStart + timeBetween; slotStart < recurrenceEnd; slotStart += timeBetween) {

            timeSlots.Add(baseQuery with {Start = slotStart, End = slotStart + duration}); 
        }

        foreach (ResolvedQuery slot in timeSlots.Skip(1)) {
            result = originalQuery.Recurrence.Mode switch {
                RecurrenceMode.STRICT => EvalReserveAND(result, () => ExecuteSingleReservation(slot, envV, envH)),
                RecurrenceMode.FLEXIBLE => EvalReserveSEQ(result, () => ExecuteSingleReservation(slot, envV, envH)),
                _ => throw new Exception("Unknown recurrence mode")
            };
        }
        return result;
    }

     private static ReservationVal ExecuteSingleReservation(ResolvedQuery query, EnvV envV, EnvH envH) {
        /*var validCombination = FindAvailableResources(query, envV, envH);
        
        if (validCombination.Any()) {
            // Commit to registry
            ReservationVal newReservation = new ReservationVal(validCombination);
            ReservationRegistry.Instance().AddReservation(newReservation);
            return newReservation;
        }
        */
        
        return new ReservationVal(new List<ReservationAtomVal>()); // Failed
    }

    /// <summary> Returns a tupple of start DateTime and end DateTime in unwrapped, c# types </summary>
    private static (DateTime, DateTime, TimeSpan) ComputeTime(TimeSpec timeSpec, EnvV envV, EnvH envH){

        //Evaluate the expressions within timespec
        Value startVal = EvalExp(timeSpec.Start, envV, envH);
        Value endMarkerVal = EvalExp(timeSpec.EndMarker, envV, envH);

        //Return unwrapped c#
        return (startVal, endMarkerVal) switch {   //start dt  ,    end dt   , duration of reservation
            (DateTimeVal start, DateTimeVal to) => (start.Value, to.Value,  to.Value - start.Value),
            (DateTimeVal start, DurationVal For) =>(start.Value, start.Value + For.Value, For.Value),
            _ => throw new Exception("Invalid time specification.\n") 
        };

    }

    /// <summary> Returns a tupple of start DateTime and end DateTime in unwrapped, c# types </summary>
        private static (TimeSpan, DateTime) ComputeRecurrencePeriod(RecurrenceInterval recurrence, DateTime start, EnvV envV, EnvH envH)
    {
        
        Value everyVal = EvalExp(recurrence.Every, envV, envH);
        Value endMarkerVal = EvalExp(recurrence.EndMarker, envV, envH);

        return (everyVal, endMarkerVal) switch {
            
            (DurationVal every, DateTimeVal until) => (every.Value, until.Value),

            (DurationVal every, DurationVal For) => (every.Value, start + For.Value),

         _ => throw new Exception("Invalid recurrence specification.\n") 
            
        };
    }

    /// <summary> Regular path: Coming from a binary operation node. Right operand Exp rightExp is intentionally not evaluated here. </summary>
    private static ReservationVal EvalBinaryReserve(BinaryOperator op, ReservationVal leftReservation, Exp rightExp, EnvV envV, EnvH envH ) {

        return op switch {

            BinaryOperator.OR => EvalReserveOR(leftReservation, rightExp, envV, envH),

            BinaryOperator.AND => EvalReserveAND(leftReservation, () => (ReservationVal) EvalExp(rightExp, envV, envH)),

            BinaryOperator.SEQ => EvalReserveSEQ(leftReservation, () => (ReservationVal) EvalExp(rightExp, envV, envH)),

            _ => throw new Exception("Unexpected operator in reservation binary")
            
        };
        
    }

    /// <summary> Short cirquit evaluation  </summary>
    private static ReservationVal EvalReserveOR(ReservationVal leftReservation, Exp rightExp, EnvV envV, EnvH envH) {
        
        // Only attempt right reserve expression if left failed
        if (leftReservation.Failed()) 
            return (ReservationVal) EvalExp(rightExp, envV, envH);

        else //left reserve attempt did not fail, return it without having attempted reserving right
            return leftReservation; 
    }

    /// <summary> Both must succeed. Rolls back left if right fails. 
    /// Two paths lead here: 1. Binary AND operation, 2. STRICT reccurrence evaluation from a leaf. </summary>
    private static ReservationVal EvalReserveAND(ReservationVal leftReservation, Func<ReservationVal> evaluateRight ) {

        //Case 1: left failed, don't attempt right. No reservations to delete. The empty list in left indicates a failure.
        if (leftReservation.Failed()) 
            return leftReservation;

        //Left didnt fail, evaluate right with the passed function from parameter.
        ReservationVal rightReservation = evaluateRight();

        //Case 2: left succeded, right failed.
        if (rightReservation.Failed()) {

            // Delete all reservations in left. 
            leftReservation.Reservations.Clear();

            //Remove from register
            
            //return a reservationVal with an empty list -> indicates failed reservation attempt
            return leftReservation;
        }

        //Case 3: both succeded
        else {
            //Combine reservations to a composite, in the original
            leftReservation.Reservations.AddRange(rightReservation.Reservations);

            //return the reservation which is now interpreted as a composite
            return leftReservation;
        }
    }

    /// <summary> Two paths lead here: 1. Binary reserve operation, 2. FLEXIBLE reccurrence evaluation from a leaf. </summary>
    private static  ReservationVal EvalReserveSEQ(ReservationVal leftReservation, Func<ReservationVal> evaluateRight ) {
        ReservationVal rightReservation = evaluateRight();

        //Combine reservations to a composite, in the original - wether either were empty
        leftReservation.Reservations.AddRange(rightReservation.Reservations);
        
        return leftReservation;//stub for compiler errors  
    }

    private static Value EvalBinary(BinaryOperation exp, EnvV envV, EnvH envH) {
        
        // Always evaluate left - needed regardless of operator or overload
        Value left = EvalExp(exp.LeftExpression, envV, envH);

        /*Intercept Reservation typed operations, as righ operand should NOT be evaluated for:
            "or" - when first goes through
            "and" - when left operand doesn't go through */

        if (left is ReservationVal leftReservation &&  //Below check not needed per typechecking, exceptions wished for logic errors.
            exp.Operator is BinaryOperator.AND or BinaryOperator.OR or BinaryOperator.SEQ
        ) {
             return EvalBinaryReserve(exp.Operator, leftReservation, exp.RightExpression, envV, envH);            
        }

        Value right = EvalExp(exp.RightExpression, envV, envH);

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

            _ => throw new Exception($"Line {exp.LineNumber}: Invalid binary operation: {left} {exp.Operator} {right}\n.") // Should never happen
        };
    }

    private static Value EvalUnary(UnaryOperation exp, EnvV envV, EnvH envH) {

        //Evaluate inner expression
        Value value = EvalExp(exp.Expression, envV, envH);

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