using RAL.AST;
using RAL.Interpreter;
namespace RAL.TC;

class TypeChecker {

    public List<string> errors = new();

    public void StmtType(Stmt stmt, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR, EnvCPT envCPT) {
        switch(stmt) {
            case Skip: break;
            
            case Composite cmp: HandleComposite(cmp, envV, envC, envH, envT, envR, envCPT); break;
            
            case If i: HandleIf(i, envV, envC, envH, envT, envR, envCPT); break;

            case VarDecl decl: HandleVarDecl(decl, envV, envC); break;


            case CategoryDecl cd: HandleCategoryDecl(cd, envC, envH); break;

            case ResourceDecl rd: HandleResourceDecl(rd, envV, envC, envH, envT, envR, envCPT); break;

            
            case TemplateDecl tmplDecl: HandleTemplateDecl(tmplDecl, envV, envC, envH, envT, envR, envCPT); break;

            case TemplateCall tc: HandleTemplateCall(tc, envV, envC, envH, envT, envR, envCPT); break;


            case Move mv: HandleMove(mv, envV, envC, envH, envR, envCPT); break; //envCPT

            case Cancel c: HandleCancel(c, envV, envC, envH, envT, envR, envCPT); break;
            

            case ExpStmt s: ExpType(s.Expression, envV, envC, envH, envT, envR, envCPT); break; 

            case Availability av: QueryIsWellTyped(av.Query, envV, envC, envH, envT, envR, envCPT); break; // QueryIsWellTyped adds errors itself, no need to check in case as well
            
            default: throw new Exception("Unknown statement."); // should never happen
        }
    }

    //Since un-bound variables throw exceptions, return value is not nullable. All other cases return types
    private TypeT ExpType(Exp exp, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR, EnvCPT envCPT) =>
        exp switch {
            BoolV     => new BoolT(),
            StringV   => new StringT(),
            NumberV   => new NumberT(),
            DateTimeV => new DateTimeT(),     
            DurationV => new DurationT(),

            Reference r => HandleReference(r, envV, envH, envR, envCPT), //cases: id || id.id

            Assignment a => HandleAssignment(a, envV, envC, envH, envT, envR, envCPT),

            Reserve r => HandleReserve(r, envV, envC, envH, envT, envR, envCPT),

            Reschedule r => HandleReschedule(r, envV, envC, envH, envT, envR, envCPT),

            BinaryOperation b => HandleBinary(b, envV, envC, envH, envT, envR, envCPT),

            UnaryOperation u => HandleUnary(u, envV, envC, envH, envT, envR, envCPT),


            _ => throw new Exception($"Unknown {exp.LineNumber} expression.") // should never happen
        };

/* ________________________Statement Handlers______________________________*/
    private void HandleComposite(Composite cmp, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR, EnvCPT envCPT) {
        if(cmp.Stmt1 != null) StmtType(cmp.Stmt1, envV, envC, envH, envT, envR, envCPT);
        if(cmp.Stmt2 != null) StmtType(cmp.Stmt2, envV, envC, envH, envT, envR, envCPT); 
    }

    private void HandleIf(If i, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR, EnvCPT envCPT) {
        TypeT type = ExpType(i.Condition, envV, envC, envH, envT, envR, envCPT);

        if(type is not BoolT) errors.Add($"If statement expects condition of type 'bool' got '{type.ToString()}'");

        if(i.ThenBody != null) StmtType(i.ThenBody, envV.NewScope(), envC, envH, envT, envR, envCPT);        
        if(i.ElseBody != null) StmtType(i.ElseBody, envV.NewScope(), envC, envH, envT, envR, envCPT);
    }

    private void HandleVarDecl(VarDecl decl, EnvV envV, EnvC envC) {
        if(decl.Type is ResourceT r && !envC.CategoryIsDeclared(r.Category))
            throw new Exception($"Use of undeclared category '{r.ToString()}'.");
   
        envV.Bind(decl.Identifier, decl.Type);
    }


    private void HandleCategoryDecl(CategoryDecl cd, EnvC envC, EnvH envH) {
        //Ensure id is not already in the set of categories
        if(envC.CategoryIsDeclared(cd.CategoryId))
            errors.Add($"Category '{cd.CategoryId}' has already been declared.");

        //Add the new category to the set
        envC.AddCategory(cd.CategoryId);

        //Handle relation to parent - 'is a id' part of [category id is a id]. If no relation is explicitly provided, parent is 'Resource'
        if(cd.ParentId == null) {
            envH.EstablishRelation(new ResourceT(cd.CategoryId), new ResourceT("Resource"));
        } else { 
            
            //Ensure parent is in the set of categories
            if(!envC.CategoryIsDeclared(cd.ParentId)) 
                throw new Exception($"Use of undeclared category '{cd.ParentId}'.");

            //Delegate establishment of parent relation to hierarchy environment. Guards cyclic relations
            envH.EstablishRelation(new ResourceT(cd.CategoryId), new ResourceT(cd.ParentId)); 
        }
    }

    private void HandleResourceDecl(ResourceDecl resDecl, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR, EnvCPT envCPT) {
        
        if(resDecl.Type is not ResourceT r) 
            errors.Add($"Expected type 'resource' got '{resDecl.Type}'");

        else if(!envC.CategoryIsDeclared(r.Category))
            errors.Add($"Use of undeclared category '{r.Category}'");
        
        envV.Bind(resDecl.Identifier, resDecl.Type); // bind resource to variable environment

        envR.RegisterResource(resDecl.Identifier); // Invariant: All resources must be registered in resource environment, important for property checking

        if (resDecl.PropertyList == null) return; // impossible to be illtyped if not present

        foreach (Stmt stmt in resDecl.PropertyList) { // loop over fields
            switch (stmt) {
                case VarDecl varDecl: { // Field decl without assignment
                    HandlePropertyDecl(resDecl, varDecl, envV, envH, envR, envCPT, false);
                    break;
                }

                case Composite { // Field decl with assignment
                    Stmt1: VarDecl varDecl, // guaranteed to be VarDecl
                    Stmt2: ExpStmt exp      // guaranteed to be ExpStmt
                }:  

                    EnvV propertyScope = envV.NewScope();
                    HandlePropertyDecl(resDecl, varDecl, propertyScope, envH, envR, envCPT, true);

                    TypeT expType = ExpType(exp.Expression, propertyScope, envC, envH, envT, envR, envCPT);
                    if (varDecl.Type != expType) errors.Add($"Variable '{varDecl.Identifier}' expected type '{varDecl.Type}' got '{expType}'");

                    break;
            }
        }
    }

    private void HandlePropertyDecl(ResourceDecl resDecl, VarDecl varDecl, EnvV envV, EnvH envH, EnvR envR, EnvCPT envCPT, bool withAssignment) {
        if (varDecl.Type is ResourceT || varDecl.Type is ReservationT) {
            errors.Add($"'{varDecl.Type}' not allowed as field type for resource");
        } else {

            //A new scope is passed so within the resource envV is used as lookup, when assigning
            if(withAssignment)
            {
                envV.Bind(varDecl.Identifier, varDecl.Type);  
            }

            //For use outside resource
            envR.BindField(resDecl.Identifier, varDecl.Identifier, varDecl.Type);

             //
            CheckCategoryPropertyConflict(resDecl.Type, varDecl.Identifier, varDecl.Type, envH, envCPT);

        }
    }

     private void CheckCategoryPropertyConflict(ResourceT resDeclType, string propertyId, TypeT incomingType, EnvH envH, EnvCPT envCPT) {


        foreach (ResourceT related in envH.GetAllRelated(resDeclType))
        {
            //Case 1: the property does not exist within current category
            if (!envCPT.HasProperty(related.Category, propertyId))
                 continue; //Skips rest of loop
            
            //Case 2: If this is reached. 2 The proper propertyId within category is of a different type than the declaration: Ok
            TypeT existingType = envCPT.Lookup(related.Category, propertyId);

            //2-fail: propertyId within category is of a different type than the declaration: Ok
            if (existingType != incomingType) {
                errors.Add(
                    $"Conflicting types: property '{propertyId}' is declared as " +
                    $"'{incomingType}' but already exists as '{existingType}' " +
                    $"in category '{related.Category}'."
                );
            }
                
            return; // found in tree — consistent or error logged; do not bind again
        }
         //2.Success New to entire tree — register it
        envCPT.Bind(resDeclType.Category, propertyId, incomingType);
     }

    private void HandleTemplateDecl(TemplateDecl tmplDecl, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR, EnvCPT envCPT) {
        if(tmplDecl.ParamList == null) return; // possible problems in template lookup due to null pointer dereference?

        EnvV tmplScope = envV.NewScope();
        List<TypeT> paramTypes = new();

        foreach(VarDecl param in tmplDecl.ParamList) {
            paramTypes.Add(param.Type); // extract types from param list and add to template environment
            tmplScope.Bind(param.Identifier, param.Type); // make formal parameters accessible (only) in template body
        }
        envT.Bind(tmplDecl.TemplateId, paramTypes); // bind template id to formal param types

        if(tmplDecl.TemplateBody != null) 
            StmtType(tmplDecl.TemplateBody, tmplScope, envC, envH, envT, envR, envCPT); // type check body. 
    }

    private void HandleTemplateCall(TemplateCall tc, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR, EnvCPT envCPT) {
        List<TypeT> formalParamTypes = envT.Lookup(tc.TemplateId);

        if(formalParamTypes == null && tc.ArgList == null) return; // both lists are empty; illtyping is impossible

        if(formalParamTypes != null && tc.ArgList == null || 
           formalParamTypes == null && tc.ArgList != null ||
           formalParamTypes.Count != tc.ArgList.Count) { // XOR?
            errors.Add($"{tc.TemplateId} expected {(formalParamTypes == null ? "0" : formalParamTypes.Count)}" +
                                        $"arguments got {(tc.ArgList == null ? "0" : tc.ArgList.Count)}");
            return; // return to avoid null dereference exceptions
        }

        for (int i = 0; i < formalParamTypes.Count; i++) { // could loop over formal or actual parameter count: they are interchangable at this point
            TypeT expected = formalParamTypes[i];
            TypeT actual = ExpType(tc.ArgList[i], envV, envC, envH, envT, envR, envCPT);
            
            if(actual is ResourceT a && expected is ResourceT e) { // if both are resources
                if(!envH.IsSubtype(a, e)) // but not compatible types
                    errors.Add($"Argument {i + 1} of template '{tc.TemplateId}' of type '{actual.ToString()}' is not compatible with '{expected.ToString()}'.");
            }
            // If both not resources: check simple equality
            else if(expected != actual) {
                errors.Add($"Argument {i + 1} of template '{tc.TemplateId}' expected '{expected.ToString()}' got '{actual.ToString()}'."); 
            }
        } 
    }


    private void HandleMove(Move move, EnvV envV, EnvC envC, EnvH envH ,EnvR envR, EnvCPT envCPT) {

        // Ensure id to move maps to a resource. V(r) = Resource. 
        TypeT type = envV.Lookup(move.ResourceId);
        if(type is ResourceT) {

            //Ensure id of category maps to a category
            if(!envC.CategoryIsDeclared(move.Type.Category)) 
                throw new Exception($"Use of undeclared category '{move.Type.Category}'.");
            
            //Before moving check for conflicts between the resource's property types and those of the category
            Dictionary<string, TypeT> propertyTypeMap = envR.GetPropertyTypeMap(move.ResourceId);
            
            foreach(var property in propertyTypeMap ) {
                
                CheckCategoryPropertyConflict(move.Type, property.Key, property.Value, envH, envCPT);
            }

            //Currently moving wether or not conflicts occur
            envV.ChangeCategory(move.ResourceId, new ResourceT(move.Type.Category));

        } else 
            errors.Add($"Expected type 'Resource' got '{type.ToString()}'");
    }

    private void HandleCancel(Cancel cancel, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR, EnvCPT envCPT) {
        TypeT type = ExpType(cancel.Reservation, envV, envC, envH, envT, envR, envCPT);
        if(type is not ReservationT) errors.Add($"Expected type 'Reservation' got: {type.ToString()}.");
    }

    /* ________________________END: Statement Handlers______________________________*/

     /*________________________Query Well-Typedness. Availability and reserve_________________________________*/
  
    private bool QueryIsWellTyped(QueryData queryData, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR, EnvCPT envCPT) {
        bool isWellTyped = true;

        EnvV reserveScope = envV.NewScope(); // Reservations allow for local declarations; create local scope
        
        if(!ResourceSpecIsWellTyped(queryData.ResourceSpecs, reserveScope, envC, envH, envT, envR, envCPT)) isWellTyped = false; // doesnt return given future errors still should be logged

        if(!TimeSpecIsWellTyped(queryData.Interval, reserveScope, envC, envH, envT, envR, envCPT))          isWellTyped = false; // doesnt return given future errors still should be logged

        if(!ConditionIsWellTyped(queryData.Condition, reserveScope, envC, envH, envT, envR, envCPT))        isWellTyped = false; // doesnt return given future errors still should be logged 

        if(!RecurrenceIsWellTyped(queryData.Recurrence, reserveScope, envC, envH, envT, envR, envCPT))      isWellTyped = false; // doesnt return given future errors still should be logged

        return isWellTyped;
    }

    /// <summary> Check all resource specifications: a*rc ident | r </summary>
    private bool ResourceSpecIsWellTyped(List<ResourceSpec> resourceSpecs, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR, EnvCPT envCPT) {
      
        bool isWellTyped = true;
        
        foreach(ResourceSpec resourceSpec in resourceSpecs) { // loop over the [[a rc ident] and [a rc ident] and [a rc ident] and ...]
            
            // "a rc" or "a rc id"
            if (resourceSpec.Quantity != null && resourceSpec.CategoryId != null) {
    
                TypeT quantityType = ExpType(resourceSpec.Quantity, envV, envC, envH, envT, envR, envCPT);

                if (quantityType is not NumberT) {
                    errors.Add($"Expected type 'Number' got '{quantityType}");
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
                    errors.Add($"Expected type 'Resource' got '{resourceSpec.Identifier}'");
                    isWellTyped = false;
                }
            }

            else {
                // invalid combination
                errors.Add("Invalid resource specification"); // should never happen
                isWellTyped = false;
            }
        }

        return isWellTyped;
    }
    private bool TimeSpecIsWellTyped(TimeSpec timeSpec, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR, EnvCPT envCPT) {
        TypeT fromType = ExpType(timeSpec.Start, envV, envC, envH, envT, envR, envCPT );
        TypeT toType = ExpType(timeSpec.EndMarker, envV, envC, envH, envT, envR, envCPT);
        
        return (fromType, toType) switch {
            (DateTimeT, DateTimeT) => true, // dt to dt
            (DateTimeT, DurationT) => true, // dt for dur
            _  => Error($"Types {fromType.ToString()} and {toType.ToString()} incompatible with interval expression")
        };
    }

    private bool ConditionIsWellTyped(Exp? condition, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR, EnvCPT envCPT) {
        if(condition == null) return true; // cannot be illtyped if not present

        TypeT condType = ExpType(condition, envV, envC, envH, envT, envR, envCPT);
        return condType switch {
            BoolT => true,
            _  => Error($"Condition expected type 'bool' got '{condType.ToString()}'")
        };
    }

    private bool RecurrenceIsWellTyped(RecurrenceSpec? recurrence, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR, EnvCPT envCPT) {
        if(recurrence == null) return true; // cannot be illtyped if not present

        TypeT everyType = ExpType(recurrence.EveryDuration, envV, envC, envH, envT, envR, envCPT);
        TypeT endType = ExpType(recurrence.EndMarker, envV, envC, envH, envT, envR, envCPT);
        return (everyType, endType) switch {
            (DurationT, DateTimeT) => true, // needs way to distinguish between for/until
            (DurationT, DurationT) => true,
            _  => Error($"Types '{everyType.ToString()}' and '{endType.ToString()}' incompatible for recurrence.")       
        };
    }    
    
    /*________________________END: Query___________________________________________*/

    /*_______________________Expression handlers__________________________________*/

    private TypeT HandleReserve(Reserve reserveNode, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR, EnvCPT envCPT ) {
        
        //Logs erros if any
        QueryIsWellTyped(reserveNode.Query, envV, envC, envH, envT, envR, envCPT);
        
        //Best effort continued type-checking, should return a reservation wether or not query is well typed
        return new ReservationT();
    }

    private TypeT HandleReference(Reference referenceNode, EnvV envV, EnvH envH, EnvR envR, EnvCPT envCPT)
    {
        //id case
        if (referenceNode.PropertyId == null) {
            return envV.Lookup(referenceNode.VariableId);
        }
        //id.id case   
        else {
            return HandlePropertyReference(referenceNode, envV, envH, envR, envCPT);
        }        
    }

    private TypeT HandlePropertyReference(Reference reference, EnvV envV, EnvH envH, EnvR envR, EnvCPT envCPT) {
        
        //Guard: Ensure that the variable is a resource type. Only these should allow property access.
        TypeT varType = envV.Lookup(reference.VariableId);
 
        if (varType is not ResourceT resource) {
            return Error($"Cannot access property '{reference.PropertyId}' on non-resource '{reference.VariableId}'.", new BoolT());
        }

        // Case 1: Declared resources. Upon declaration, reglardless of having fields, a resource is registered to envR.
        if (envR.HasResource(reference.VariableId)) {

            // id.id cases.'PropertyId' can never be null given case from which this method is invoked in HandleReference
            return envR.LookupField(reference.VariableId, reference.PropertyId);
        }

        // Case 2: Does not exist in resource environment -> Bounded query variable. Check envCPT categoryId x propertyId -> Type
        return LookupCategoryPropertyType(resource, reference.PropertyId, envH, envCPT );
    }

    /// <summary> Helper for property reference in bounded query variables within reserve or availability</summary>
    private TypeT LookupCategoryPropertyType(ResourceT category, string propertyId, EnvH envH, EnvCPT envCPT) {
        
        //Check all categories in subtree
       foreach (ResourceT related in envH.GetSubtree(category)) {
            
            if (envCPT.HasProperty(related.Category, propertyId)) {
                return envCPT.Lookup(related.Category, propertyId);
            }
        }

        return Error(
            $"No resource in the category tree of '{category.Category}' " +
            $"declares a field '{propertyId}'.",
            new BoolT()
        );
    }

     private TypeT HandleAssignment(Assignment assign, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR, EnvCPT envCPT) {
        TypeT varType = HandleReference(assign.Variable, envV, envH, envR, envCPT);

        TypeT expType = ExpType(assign.Value, envV, envC, envH, envT, envR, envCPT);
        
        if (varType is ResourceT) 
            errors.Add($"Assignment of expression to resource not allowed");

        if (expType != varType) 
        /*  Message with ??:        if propertyId not null return it, otherwise return variableId  to*/
            errors.Add($"Variable '{assign.Variable.PropertyId ?? assign.Variable.VariableId}' " +
                       $"expected type '{varType.ToString()}' got '{expType.ToString()}'."
            );

        return varType; 
    }

    private TypeT HandleReschedule(Reschedule r, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR, EnvCPT envCPT) {
        TimeSpecIsWellTyped(r.NewTimeInterval, envV, envC, envH, envT, envR, envCPT); // Simply checks for errors in the [dt to dt / for dur]. Return value is discarded

        TypeT expType = ExpType(r.Reservation, envV, envC, envH, envT, envR, envCPT);
        
        return expType switch {
            ReservationT => new ReservationT(),
            _ => Error($"Reschedule expected type 'Reservation' got '{expType}'", new ReservationT())
        };
    }

    private TypeT HandleBinary(BinaryOperation exp, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR, EnvCPT envCPT) {
        
        TypeT left  = ExpType(exp.LeftExpression,  envV, envC, envH, envT, envR, envCPT);
        TypeT right = ExpType(exp.RightExpression, envV, envC, envH, envT, envR, envCPT);
        
        string operatorAsString = EnumToOp(exp.Operator);
        return exp.Operator switch
        {
            // + -
            BinaryOperator.ADD or 
            BinaryOperator.SUB => (left, right) switch {
                (NumberT, NumberT)     => new NumberT(),   // num + num
                (DateTimeT, DurationT) => new DateTimeT(), // dt + dur
                _  => Error($"Line {exp.LeftExpression.LineNumber}: Operand types '{left}' and '{right}' incompatible for '{operatorAsString}'", new NumberT())},

            // * /
            BinaryOperator.MUL or 
            BinaryOperator.DIV => (left, right) switch {
                (NumberT, NumberT) => new NumberT(), // num */ num
                _  => Error($"Operand types '{left}' and '{right}' incompatible for '{operatorAsString}'", new NumberT())},
            
            // <, >, 
            BinaryOperator.LT   or // <
            BinaryOperator.GT   or // >
            BinaryOperator.LTEQ or // <=, >=
            BinaryOperator.GTEQ => (left, right) switch {
                (NumberT, NumberT) => new BoolT(),
                (DateTimeT, DateTimeT) => new BoolT(),
                (DurationT, DurationT) => new BoolT(),
                _ => Error($"Operand types '{left}' and '{right}' incompatible for '{operatorAsString}'", new BoolT())},

            BinaryOperator.EQ or 
            BinaryOperator.NEQ => (left, right) switch {
                (StringT, StringT) => new BoolT(),
                (BoolT, BoolT)     => new BoolT(), // (4 < 7) == (7 > 11 and "hello" == "world")
                (NumberT, NumberT) => new BoolT(),
                (DurationT, DurationT) => new BoolT(),
                (DateTimeT, DateTimeT) => new BoolT(),
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

    private TypeT HandleUnary(UnaryOperation exp, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR, EnvCPT envCPT) {
        TypeT operandType = ExpType(exp.Expression, envV, envC, envH, envT, envR, envCPT);
        string operatorAsString = EnumToOp(exp.Operator);
        return exp.Operator switch {
            UnaryOperator.NOT when (operandType is BoolT) => new BoolT(),
            UnaryOperator.NOT => Error($"Operator '{operatorAsString}' expected 'Bool' got '{operandType}'", new BoolT()),

            UnaryOperator.NEG when (operandType is NumberT) => new NumberT(),
            UnaryOperator.NEG => Error($"Operator '{operatorAsString}' expected 'Number' got '{operandType}'", new NumberT()),
            
            _ => throw new Exception("Unknown unary operator.") // should never happen
        };
    }

    /* ____________________________Errors and message helpers____________________________*/

     // Below two error functions are simply to circumvent switch expression limitations of not being able to add statements
    private bool Error(string msg) {
        errors.Add(msg);
        return false;
    }

    private TypeT Error(string msg, TypeT fallback) {
        errors.Add(msg);
        return fallback;
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
}
