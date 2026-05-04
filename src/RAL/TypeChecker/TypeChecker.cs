using System.Diagnostics.CodeAnalysis;
using RAL.AST;
namespace RAL.TC;

class TypeChecker {

    public List<string> errors = new();

    //Since un-bound variables throw exceptions, return value is not nullable. All other cases return types
    private TypeT ExpType(Exp exp, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR) =>
        exp switch {
            BoolV     => new BoolT(),
            StringV   => new StringT(),
            NumberV   => new NumberT(),
            DateTimeV => new DateTimeT(),     
            DurationV => new DurationT(),

            Reference r when r.PropertyId == null => envV.Lookup(r.VariableId), //id cases
            Reference r => HandlePropertyReference(r, envV, envR), 
            //Reference r => envR.LookupField(r.VariableId, r.PropertyId), // id.id cases.'PropertyId' can never be null given the above check

            Assignment a => HandleAssignment(a, envV, envC, envH, envT, envR),

            BinaryOperation b => HandleBinary(b, envV, envC, envH, envT, envR),

            UnaryOperation u => HandleUnary(u, envV, envC, envH, envT, envR),

            Reserve r when QueryIsWellTyped(r.Query, envV, envC, envH, envT, envR) => new ReservationT(),
            Reserve r  => new ReservationT(),

            Reschedule r => HandleReschedule(r, envV, envC, envH, envT, envR),

            _ => throw new Exception($"Unknown {exp.LineNumber} expression.") // should never happen
        };


    public void StmtType(Stmt stmt, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR) {
        switch(stmt) {
            case Skip: break;
            case Composite cmp: HandleComposite(cmp, envV, envC, envH, envT, envR); break;

            case VarDecl decl: HandleVarDecl(decl, envV, envC); break;

            case CategoryDecl cd: HandleCategoryDecl(cd, envC, envH); break;

            case Move mv: HandleMove(mv, envV, envC); break; 

            case Cancel c: HandleCancel(c, envV, envC, envH, envT, envR); break;

            case If i: HandleIf(i, envV, envC, envH, envT, envR); break;

            case TemplateDecl tmplDecl: HandleTemplateDecl(tmplDecl, envV, envC, envH, envT, envR); break;

            case TemplateCall tc: HandleTemplateCall(tc, envV, envC, envH, envT, envR); break;

            case ExpStmt s: ExpType(s.Expression, envV, envC, envH, envT, envR); break; 

            case Availability av: QueryIsWellTyped(av.Query, envV, envC, envH, envT, envR); break; // QueryIsWellTyped adds errors itself, no need to check in case as well
            
            case ResourceDecl rd: HandleResourceDecl(rd, envV, envC, envH, envT, envR); break;
            
            default: throw new Exception("Unknown statement."); // should never happen
        }
    }

    private void HandleFieldDecl(ResourceDecl resDecl, VarDecl varDecl, EnvR envR) {
        if (varDecl.Type is ResourceT || varDecl.Type is ReservationT) {
            errors.Add("Type not allowed");
        } else {
            envR.BindField(resDecl.Identifier, varDecl.Identifier, varDecl.Type);
        }
    }

    private void HandleResourceDecl(ResourceDecl resDecl, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR) {
        
        if(resDecl.Type is not ResourceT r) 
            errors.Add("Wrong type");

        else if(!envC.CategoryIsDeclared(r.Category))
            errors.Add($"Use of undeclared category");
        
        envV.Bind(resDecl.Identifier, resDecl.Type); // bind resource to variable environment

        envR.RegisterResource(resDecl.Identifier); // Invariant: All resources must be registered in resource environment, important for property checking

        if (resDecl.PropertyList == null) return; // impossible to be illtyped if not present

        foreach (Stmt stmt in resDecl.PropertyList) { // loop over fields
            switch (stmt) {
                case VarDecl varDecl: { // Field decl without assignment
                    HandleFieldDecl(resDecl, varDecl, envR);
                    break;
                }

                case Composite { // Field decl with assignment
                    Stmt1: VarDecl varDecl, // guaranteed to be VarDecl
                    Stmt2: ExpStmt exp      // guaranteed to be ExpStmt
                }:  
                    HandleFieldDecl(resDecl, varDecl, envR);

                    TypeT expType = ExpType(exp.Expression, envV, envC, envH, envT, envR);
                    if (varDecl.Type != expType) errors.Add($"Variable {varDecl.Identifier} expected type '{varDecl.Type}' got '{expType}'");

                    break;
            }
        }
    }

    private void HandleComposite(Composite cmp, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR) {
        if(cmp.Stmt1 != null) StmtType(cmp.Stmt1, envV, envC, envH, envT, envR); // newScope ??
        if(cmp.Stmt2 != null) StmtType(cmp.Stmt2, envV, envC, envH, envT, envR); // newScope ??
    }

    private void HandleVarDecl(VarDecl decl, EnvV envV, EnvC envC) {
        if(decl.Type is ResourceT r && !envC.CategoryIsDeclared(r.Category))
            throw new Exception($"Use of undeclared category '{r.ToString()}'.");
   
        envV.Bind(decl.Identifier, decl.Type);
    }

    private void HandleMove(Move move, EnvV envV, EnvC envC) {

        // Ensure id to move maps to a resource. V(r) = Resource. 
        TypeT type = envV.Lookup(move.ResourceId);
        if(type is ResourceT) {

            //Ensure id of category maps to a category
            if(!envC.CategoryIsDeclared(move.CategoryId)) 
                throw new Exception($"Use of undeclared category '{move.CategoryId}'.");
            
            //
            envV.ChangeCategory(move.ResourceId, new ResourceT(move.CategoryId));
        } else 
            errors.Add($"Expected type 'Resource' got '{type.ToString()}'");
    }

    private void HandleCancel(Cancel cancel, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR) {
        TypeT type = ExpType(cancel.Reservation, envV, envC, envH, envT, envR);
        if(type is not ReservationT) errors.Add($"Expected type 'Reservation' got: {type.ToString()}.");
    }

    private void HandleIf(If i, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR) {
        TypeT type = ExpType(i.Condition, envV, envC, envH, envT, envR);

        if(type is not BoolT) errors.Add($"If statement expects condition of type 'bool' got '{type.ToString()}'");

        if(i.ThenBody != null) StmtType(i.ThenBody, envV.NewScope(), envC, envH, envT, envR);        
        if(i.ElseBody != null) StmtType(i.ElseBody, envV.NewScope(), envC, envH, envT, envR);
    }
    
    private TypeT HandleAssignment(Assignment assign, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR) {
        TypeT varType = envV.Lookup(assign.VariableId);
        TypeT expType = ExpType(assign.Value, envV, envC, envH, envT, envR);
        
        if (varType is ResourceT) errors.Add($"Expression assignment to resources not allowed");
        if(expType != varType) errors.Add($"Variable ${assign.VariableId} expected type ${varType.ToString()} but got ${expType.ToString()}.");

        return varType; 
    }

    private TypeT HandlePropertyReference(Reference referenceNode, EnvV envV, EnvR envR)
    {
        // Ensure that the variable is a resource type. Only these should allow property access.
                
        TypeT varType = envV.Lookup(referenceNode.VariableId);
 
        if (varType is not ResourceT resT) {
            return Error($"Cannot access property '{referenceNode.PropertyId}' on non-resource '{referenceNode.VariableId}'.", new BoolT());
        }

        // Case 1: Declared resources. Upon declaration, reglardless of having fields, a resource is registered to envR.
        if (envR.HasResource(referenceNode.VariableId)) {

            // id.id cases.'PropertyId' can never be null given case from which this method is invoked. Handles cases 
            return envR.LookupField(referenceNode.VariableId, referenceNode.PropertyId);
        }

        // Case 2: Does not exist in resource environment -> Bounded query variable. Find any resource of this category that has this field.
        return LookupCategoryFieldType(resT.Category, referenceNode.PropertyId, envV, envR);
    }
    

    private TypeT HandleBinary(BinaryOperation exp, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR) {
        
        TypeT left  = ExpType(exp.LeftExpression,  envV, envC, envH, envT, envR);
        TypeT right = ExpType(exp.RightExpression, envV, envC, envH, envT, envR);
        
        string operatorAsString = EnumToOp(exp.Operator);
        return exp.Operator switch
        {
            BinaryOperator.ADD or 
            BinaryOperator.SUB => (left, right) switch {
                (NumberT, NumberT)     => new NumberT(),   // num + num
                (DateTimeT, DurationT) => new DateTimeT(), // dt + dur
                _  => Error($"Line {exp.LeftExpression.LineNumber}: Operand types '{left}' and '{right}' incompatible for '{operatorAsString}'", new NumberT())            },


            BinaryOperator.MUL or 
            BinaryOperator.DIV => (left, right) switch {
                (NumberT, NumberT) => new NumberT(), // num */ num
                _  => Error($"Operand types '{left}' and '{right}' incompatible for '{operatorAsString}'", new NumberT())},

            BinaryOperator.LT   or
            BinaryOperator.GT   or
            BinaryOperator.LTEQ or
            BinaryOperator.GTEQ => (left, right) switch {
                (NumberT, NumberT) => new BoolT(),
                _ => Error($"Operand types '{left}' and '{right}' incompatible for '{operatorAsString}'", new BoolT())},

            BinaryOperator.EQ or 
            BinaryOperator.NEQ => (left, right) switch {
                (StringT, StringT) => new BoolT(),
                (BoolT, BoolT)     => new BoolT(), // (4 < 7) == (7 > 11 and "hello" == "world")
                (NumberT, NumberT) => new BoolT(),
                _  => Error($"Operand types '{left}' and '{right}' incompatible for '{operatorAsString}'", new BoolT())},

            BinaryOperator.OR or BinaryOperator.AND => (left, right) switch {
                (ReservationT, ReservationT) => new ReservationT(), // reserve [...] and reserve [...]
                (BoolT, BoolT)               => new BoolT(),        // 4 < 7 and 7 < 11
                _ => Error($"Operand types '{left}' and '{right}' incompatible for '{operatorAsString}'", new ReservationT())},

            BinaryOperator.SEQ => (left, right) switch {
                (ReservationT, ReservationT) => new ReservationT(),
                _ => Error($"Operand types '{left}' and '{right}' incompatible for '{operatorAsString}'", new ReservationT())},

            _ => throw new Exception("Unknown binary operator.") // should never happen      
        };
    }

    private TypeT HandleUnary(UnaryOperation exp, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR) {
        TypeT operandType = ExpType(exp.Expression, envV, envC, envH, envT, envR);
        string operatorAsString = EnumToOp(exp.Operator);
        return exp.Operator switch {
            UnaryOperator.NOT when (operandType is BoolT) => new BoolT(),
            UnaryOperator.NOT => Error($"Operator '{operatorAsString}' expected bool but got '{operandType}'", new BoolT()),
            UnaryOperator.NEG when (operandType is NumberT) => new NumberT(),

            UnaryOperator.NEG => Error($"Operator '{operatorAsString}' expected number but got '{operandType}'", new NumberT()),
            _ => throw new Exception("Unknown type.") // should never happen
        };
    }
  
    private bool QueryIsWellTyped(QueryData queryData, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR) {
        bool isWellTyped = true;

        EnvV reserveScope = envV.NewScope(); // Reservations allow for local declarations; create local scope
        
        if(!ResourceSpecIsWellTyped(queryData.ResourceSpecs, reserveScope, envC, envH, envT, envR)) isWellTyped = false; // doesnt return given future errors still should be logged

        if(!TimeSpecIsWellTyped(queryData.Interval, reserveScope, envC, envH, envT, envR))          isWellTyped = false; // doesnt return given future errors still should be logged

        if(!ConditionIsWellTyped(queryData.Condition, reserveScope, envC, envH, envT, envR))        isWellTyped = false; // doesnt return given future errors still should be logged 

        if(!RecurrenceIsWellTyped(queryData.Recurrence, reserveScope, envC, envH, envT, envR))      isWellTyped = false; // doesnt return given future errors still should be logged

        return isWellTyped;
    }

    //Check all resource specifications: a*rc ident | r
    private bool ResourceSpecIsWellTyped(List<ResourceSpec> resourceSpecs, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR) {
      
        bool isWellTyped = true;
        
        foreach(ResourceSpec resourceSpec in resourceSpecs) { // loop over the [[a rc ident] and [a rc ident] and [a rc ident] and ...]
            
            // "a rc" or "a rc id"
            if (resourceSpec.Quantity != null && resourceSpec.CategoryId != null) {
    
                TypeT quantityType = ExpType(resourceSpec.Quantity, envV, envC, envH, envT, envR);

                if (quantityType is not NumberT) {
                    errors.Add($"Expected type 'number' got '{quantityType}");
                    isWellTyped = false;
                }

                if (!envC.CategoryIsDeclared(resourceSpec.CategoryId)) {
                    errors.Add($"Use of undeclared category '{resourceSpec.CategoryId}'");
                    isWellTyped = false;
                }

                if (resourceSpec.Identifier != null) {
                    envV.Bind(resourceSpec.Identifier, new ResourceT(resourceSpec.CategoryId));
                }
            }

            // "id"
            else if (resourceSpec.Identifier != null && resourceSpec.Quantity == null) {
                if (envV.Lookup(resourceSpec.Identifier) is not ResourceT) {
                    errors.Add($"Expected type 'resource' got '{resourceSpec.Identifier}'");
                    isWellTyped = false;
                }
            }

            else {
                // invalid combination
                errors.Add("Invalid resource specification");
                isWellTyped = false;
            }
        }

        return isWellTyped;
    }
    private bool TimeSpecIsWellTyped(TimeSpec timeSpec, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR) {
        TypeT fromType = ExpType(timeSpec.Start, envV, envC, envH, envT, envR);
        TypeT toType = ExpType(timeSpec.EndMarker, envV, envC, envH, envT, envR);
        
        return (fromType, toType) switch {
            (DateTimeT, DateTimeT) => true, // dt to dt
            (DateTimeT, DurationT) => true, // dt for dur
            _  => Error($"Types {fromType.ToString()} and {toType.ToString()} incompatible with interval expression")
        };
    }

    private bool ConditionIsWellTyped(Exp? condition, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR) {
        if(condition == null) return true; // cannot be illtyped if not present

        TypeT condType = ExpType(condition, envV, envC, envH, envT, envR);
        return condType switch {
            BoolT => true,
            _  => Error($"Condition expected type 'bool' got '{condType.ToString()}'")
        };
    }

    private bool RecurrenceIsWellTyped(RecurrenceSpec? recurrence, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR) {
        if(recurrence == null) return true; // cannot be illtyped if not present

        TypeT everyType = ExpType(recurrence.EveryDuration, envV, envC, envH, envT, envR);
        TypeT endType = ExpType(recurrence.EndMarker, envV, envC, envH, envT, envR);
        return (everyType, endType) switch {
            (DurationT, DateTimeT) => true, // needs way to distinguish between for/until
            (DurationT, DurationT) => true,
            _  => Error($"Types '{everyType.ToString()}' and '{endType.ToString()}' incompatible for recurrence.")       
        };
    }
    

    private TypeT HandleReschedule(Reschedule r, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR) {
        TimeSpecIsWellTyped(r.NewTimeInterval, envV, envC, envH, envT, envR); // Simply checks for errors in the [dt to dt / for dur]. Return value is discarded

        TypeT expType = ExpType(r.Reservation, envV, envC, envH, envT, envR);
        
        return expType switch {
            ReservationT => new ReservationT(),
            _ => Error($"Reschedule expected type 'reservation' got '{expType}'", new ReservationT())
        };
    }
    private void HandleTemplateCall(TemplateCall tc, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR) {
        List<TypeT> formalParamTypes = envT.Lookup(tc.TemplateId);

        if(formalParamTypes == null && tc.ArgList == null) return; // both lists are empty; illtyping is impossible

        if(formalParamTypes != null && tc.ArgList == null || 
           formalParamTypes == null && tc.ArgList != null) { // XOR?
            errors.Add("Number of expected arguments don't match actual.");
            return; // return to avoid null dereference exceptions
        }

        if(formalParamTypes.Count != tc.ArgList.Count) // argument count must match: false-positives could arise otherwise
            errors.Add($"{tc.TemplateId} expected {formalParamTypes.Count} arguments got {tc.ArgList.Count}");

        for (int i = 0; i < formalParamTypes.Count; i++) { // could loop over formal or actual parameter count: they are interchangable at this point
            TypeT expected = formalParamTypes[i];
            TypeT actual = ExpType(tc.ArgList[i], envV, envC, envH, envT, envR);
            
            if(actual is ResourceT a && expected is ResourceT e && !envH.IsSubtype(a, e)) // if both are resources but not subtypes: produce an error
                    errors.Add($"Argument {i + 1} of template '{tc.TemplateId}' of type {actual.ToString()} is not compatible with {expected.ToString()}.");

            else if(expected != actual) // if both not resources: check simple equality
                    errors.Add($"Argument {i + 1} of template '{tc.TemplateId}' expected {expected.ToString()} got {actual.ToString()}."); 
        }
    }
    
    private void HandleTemplateDecl(TemplateDecl tmplDecl, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR) {
        if(tmplDecl.ParamList == null) return; // possible problems in template lookup due to null pointer dereference?

        EnvV tmplScope = envV.NewScope();
        List<TypeT> paramTypes = new();

        foreach(VarDecl param in tmplDecl.ParamList) {
            paramTypes.Add(param.Type); // extract types from param list and add to template environment
            tmplScope.Bind(param.Identifier, param.Type); // make formal parameters accessible (only) in template body
        }
        envT.Bind(tmplDecl.TemplateId, paramTypes); // bind template id to formal param types

        if(tmplDecl.TemplateBody != null) 
            StmtType(tmplDecl.TemplateBody, tmplScope, envC, envH, envT, envR); // type check body. 
    }
    private void HandleCategoryDecl(CategoryDecl cd, EnvC envC, EnvH envH) {
        //Ensure id is not already in the set of categories
        if(envC.CategoryIsDeclared(cd.CategoryId))
            errors.Add($"Category '{cd.CategoryId}' has already been declared.");

        //Add the new category to the set
        envC.AddCategory(cd.CategoryId);

        //Handle relation to parent if any - 'is a id' part of [category id is a id]
        if(cd.ParentId != null) { 
            
            //Ensure parent is in the set of categories
            if(!envC.CategoryIsDeclared(cd.ParentId)) 
                throw new Exception($"Use of undeclared category '{cd.ParentId}'.");

            //Delegate establishment of parent relation to hierarchy environment. Guards cyclic relations
            envH.EstablishRelation(new ResourceT(cd.CategoryId), new ResourceT(cd.ParentId)); 
        }
    }

    private string EnumToOp(BinaryOperator op) {
        return
        op == BinaryOperator.ADD  ? "+"  :
        op == BinaryOperator.SUB  ? "-"  :
        op == BinaryOperator.MUL  ? "*"  :
        op == BinaryOperator.DIV  ? "/"  :
        op == BinaryOperator.EQ   ? "==" :
        op == BinaryOperator.NEQ  ? "!=" :
        op == BinaryOperator.LT   ? "<"  :
        op == BinaryOperator.GT   ? ">"  :
        op == BinaryOperator.LTEQ ? "<=" :
        op == BinaryOperator.GTEQ ? ">=" :
        op == BinaryOperator.AND  ? "and":
        op == BinaryOperator.OR   ? "or" :
        op == BinaryOperator.SEQ  ? "seq":
        "";
    }

    private string EnumToOp(UnaryOperator op) {
        return
        op == UnaryOperator.NOT  ? "not"  :
        op == UnaryOperator.NEG  ? "-"  :
        "";
    }

    // the below two error functions are simply to circumvent switch expression limitations of not being able to add statements
    private bool Error(string msg) {
        errors.Add(msg);
        return false;
    }

    private TypeT Error(string msg, TypeT fallback) {
        errors.Add(msg);
        return fallback;
    }

    private TypeT LookupCategoryFieldType(string category, string fieldName, EnvV envV, EnvR envR) {
    // Get all concrete resource IDs belonging to this category from EnvV
    List<string> resourcesOfCategory = envV.GetResourcesByCategory(category);
    
    TypeT? resolvedType = null;

    foreach (string resId in resourcesOfCategory) {
        if (envR.HasResource(resId) && envR.HasField(resId, fieldName)) {
            TypeT fieldType = envR.LookupField(resId, fieldName);
            
            // Catch type collisions (e.g., one DoubleRoom has int floor, another has string floor)
            if (resolvedType != null && resolvedType.GetType() != fieldType.GetType()) {
                errors.Add($"Type collision: Field '{fieldName}' in category '{category}' has conflicting types.");
                return resolvedType; 
            }
            resolvedType = fieldType;
        }
    }

    if (resolvedType != null) return resolvedType;

    return new NumberT();

    //error? new Exception($"No resource in category '{category}' contains the field '{fieldName}'.");
    }
}
