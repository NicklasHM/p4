using System.Runtime.InteropServices.Marshalling;
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

            Reference r when r.PropertyId == null => envV.Lookup(r.VariableId),
            Reference r => envR.LookupField(r.VariableId, r.PropertyId), // can never be null given the above check

            Assignment a => HandleAssignment(a, envV, envC, envH, envT, envR),

            BinaryOperation b => HandleBinary(b, envV, envC, envH, envT, envR),

            UnaryOperation u => HandleUnary(u, envV, envC, envH, envT, envR),

            Reserve r when QueryIsWellTyped(r.Query, envV, envC, envH, envT, envR) => new ReservationT(),

            Reschedule r => HandleReschedule(r, envV, envC, envH, envT, envR),

            _ => throw new Exception("Unknown expression.") // should never happen
        };


    private void StmtType(Stmt stmt, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR) {
        switch(stmt) {
            case Composite cmp: handleComposite(cmp, envV, envC, envH, envT, envR); break;

            case VarDecl decl: handleVarDecl(decl, envV, envC); break;

            case CategoryDecl cd: handleCategoryDecl(cd, envV, envC, envH, envT, envR); break;

            case Move mv: handleMove(mv, envV, envC); break; 

            case Cancel c: handleCancel(c, envV, envC, envH, envT, envR); break;

            case If i: handleIf(i, envV, envC, envH, envT, envR); break;

            case TemplateDecl tmplDecl: handleTemplateDecl(tmplDecl, envV, envC, envH, envT, envR); break;

            case TemplateCall tc: HandleTemplateCall(tc, envV, envC, envH, envT, envR); break;

            case ExpStmt s: ExpType(s.Expression, envV, envC, envH, envT, envR); break; 

            case Availability av: QueryIsWellTyped(av.Query, envV, envC, envH, envT, envR); break; // QueryIsWellTyped adds errors itself, no need to check in case as well
            
            /*
            case ResourceDecl rd: { // WIP
                if(rd.Properties != null) {
                    
                }
                break;
            }
            */
            default: throw new Exception("Unknown statement."); // should never happen
        }
    }

    private void HandleResourceDecl(ResourceDecl rd, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR) {
        if(rd.PropertyList != null) {
            TypeT varType = null;
            foreach(Stmt s in rd.PropertyList){
                switch(s) {
                    case VarDecl v: if(v.Type is ResourceT || v.Type is ReservationT) errors.Add("Type not allowed"); break;
                    case Composite c: 
                        switch(c.Stmt1) {
                            case VarDecl cv: {
                                if(cv.Type is ResourceT || cv.Type is ReservationT) {
                                    errors.Add("Type not allowed");
                                } else {
                                    varType = cv.Type;        
                                }
                                break;
                            }
                            default: break;
                        }
                        break;
                    case ExpStmt e: {
                        TypeT expType = ExpType(e.Expression, envV, envC, envH, envT, envR); 
                        if(varType != expType) errors.Add("Type mismatch!!!");
                        break;       
                    }
                    default: break;
                }
            }
        }
    }

    private void handleComposite(Composite cmp, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR) {
        if(cmp.Stmt1 != null) StmtType(cmp.Stmt1, envV.NewScope(), envC, envH, envT, envR);
        if(cmp.Stmt2 != null) StmtType(cmp.Stmt2, envV.NewScope(), envC, envH, envT, envR);
    }

    private void handleVarDecl(VarDecl decl, EnvV envV, EnvC envC) {
        switch(decl.Type) {
            case ResourceT r: if(!envC.C.Contains(r.Category)) throw new Exception($"Use of undeclared category '{r.Category}'."); break;
            default: break;  
        }       
        envV.Bind(decl.Identifier, decl.Type);
    }

    private void handleMove(Move mv, EnvV envV, EnvC envC) {
        TypeT type;
        switch(type = envV.Lookup(mv.ResourceId)) { 
            case ResourceT: {
                if(!envC.C.Contains(mv.CategoryId)) throw new Exception($"Use of undeclared category '{mv.CategoryId}'.");
                envV.ChangeCategory(mv.ResourceId, new ResourceT(mv.CategoryId)); break;          
            }
            default: {
                errors.Add($"Expected type 'Resource' got '{type}'"); // type --> string conversion unsure
                break;           
            }
        }
    }

    private void handleCancel(Cancel c, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR) {
        TypeT type = ExpType(c.Reservation, envV, envC, envH, envT, envR);
        switch(type) { 
            case ReservationT: break;
            default: errors.Add($"Expected type 'Reservation' got: {type}."); break;  
        }
    }

    private void handleIf(If i, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR) {
        TypeT type = ExpType(i.Condition, envV, envC, envH, envT, envR);
        switch(type) {
            case BoolT: break;
            default: errors.Add($"If statement expects condition of type 'bool' got '{type}'"); break;      
        }
        if(i.ThenBody != null) StmtType(i.ThenBody, envV.NewScope(), envC, envH, envT, envR);        
        if(i.ElseBody != null) StmtType(i.ElseBody, envV.NewScope(), envC, envH, envT, envR);
    }
    
    private TypeT HandleAssignment(Assignment a, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR) {
        TypeT varType = envV.Lookup(a.VariableId);
        
        if (varType is ResourceT)
            errors.Add($"Assignment to resources not allowed");
    
        TypeT expType = ExpType(a.Value, envV, envC, envH, envT, envR);

        if(expType != varType) 
            errors.Add($"Variable ${a.VariableId} expected type ${varType} but got ${expType}.");

        return varType; 
    }

    private TypeT HandleBinary(BinaryOperation exp, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR) {
        
        TypeT left  = ExpType(exp.LeftExpression,  envV, envC, envH, envT, envR);
        TypeT right = ExpType(exp.RightExpression, envV, envC, envH, envT, envR);
        
        string operatorAsString = EnumToOp(exp.Operator);
        return exp.Operator switch
        {
            BinaryOperator.ADD or 
            BinaryOperator.SUB => (left, right) switch {
                (NumberT, NumberT)     => new NumberT(), // num + num
                (DateTimeT, DurationT) => new DateTimeT(), // dt + dur
                _ => Error(exp, left, right, operatorAsString, new NumberT()) // assume user wanted number; terribleness
            },


            BinaryOperator.MUL or 
            BinaryOperator.DIV => (left, right) switch {
                (NumberT, NumberT) => new NumberT(), // num */ num
                _ => Error(exp, left, right, operatorAsString, new NumberT())
            },

            BinaryOperator.LT   or 
            BinaryOperator.GT   or 
            BinaryOperator.LTEQ or 
            BinaryOperator.GTEQ => (left, right) switch {
                (NumberT, NumberT) => new BoolT(),
                _ => Error(exp, left, right, operatorAsString, new BoolT())
            },

            BinaryOperator.EQ or 
            BinaryOperator.NEQ => (left, right) switch {
                (StringT, StringT) => new BoolT(),
                (BoolT, BoolT)     => new BoolT(),
                (NumberT, NumberT) => new BoolT(),
                _ => Error(exp, left, right, operatorAsString, new BoolT())
            },

            BinaryOperator.OR or BinaryOperator.AND => (left, right) switch {
                (ReservationT, ReservationT) => new ReservationT(),
                (BoolT, BoolT)               => new BoolT(),
                _ => Error(exp, left, right, operatorAsString, new ReservationT()) // assume user wanted reservation; terribleness
            },

            BinaryOperator.SEQ => (left, right) switch {
                (ReservationT, ReservationT) => new ReservationT(),
                _ => Error(exp, left, right, operatorAsString, new ReservationT())
            },
            _ => throw new Exception("Unknown type.") // should never happen      
        };
    }

    private TypeT HandleUnary(UnaryOperation exp, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR) {
        TypeT operandType = ExpType(exp.Expression, envV, envC, envH, envT, envR);
        string operatorAsString = EnumToOp(exp.Operator);
        return exp.Operator switch {
            UnaryOperator.NOT when (operandType is BoolT) => new BoolT(),
            UnaryOperator.NOT => Error("bool", operandType, operatorAsString, new BoolT()),


            UnaryOperator.NEG when (operandType is NumberT) => new NumberT(),
            UnaryOperator.NEG => Error("number", operandType, operatorAsString, new NumberT()),

            _ => throw new Exception("Unknown type.") // should never happen
        };
    }
  
    private bool QueryIsWellTyped(QueryData queryData, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR) {

        bool isWellTyped = true;

        if(!ResourceSpecIsWellTyped(queryData.ResourceSpecs, envV, envC, envH, envT, envR)) isWellTyped = false; // doesnt return given future errors still should be logged

        if(!TimeSpecIsWellTyped(queryData.Interval, envV, envC, envH, envT, envR))          isWellTyped = false; // doesnt return given future errors still should be logged

        if(!ConditionIsWellTyped(queryData.Condition, envV, envC, envH, envT, envR))        isWellTyped = false; // doesnt return given future errors still should be logged 

        if(!RecurrenceIsWellTyped(queryData.Recurrence, envV, envC, envH, envT, envR))      isWellTyped = false; // doesnt return given future errors still should be logged

        return isWellTyped;
    }

    //Check all resource specifications: a*rc ident | r
    private bool ResourceSpecIsWellTyped(List<ResourceSpec> resourceSpecs, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR) {
        // new environment used only if a local variable binding is encountered
        // new scope should be used no matter what? what if a rc id is mixed with id? new scope for all
        EnvV reserveScope = envV.NewScope();
        
        bool isWellTyped = true;
        
        foreach(ResourceSpec resourceSpec in resourceSpecs) {
            
            // a rc [ident]
            if(resourceSpec.Quantity != null) {
                TypeT quantityType = ExpType(resourceSpec.Quantity, envV, envC, envH, envT, envR); // new scope ?
                
                if (quantityType is not NumberT) {
                    errors.Add($"Expected type 'number' got '{quantityType}'");
                    isWellTyped = false; // doesnt break given future errors still should be logged
                }

                //No need to check (resourceSpec.CategoryId != null) given quantity is not null, would be rejected by parser
                if(!envC.C.Contains(resourceSpec.CategoryId)) {
                    errors.Add($"Use of undeclared category '{resourceSpec.CategoryId}'");
                    isWellTyped = false; // doesnt break given future errors still should be logged
                }

                //hvis id findes så bind til nyt scope
                if(resourceSpec.Identifier != null) {
                    reserveScope.Bind(resourceSpec.Identifier, new ResourceT(resourceSpec.CategoryId));
                }
            } 
            else { // r case, lookup in current scope, ensure its a Resource Type
                if(reserveScope.Lookup(resourceSpec.Identifier) is not ResourceT) { // only resources may be declared in a reservation context
                    errors.Add($"Wrong Type '{resourceSpec.Identifier}'");
                    isWellTyped = false; // doesnt break given future errors still should be logged
                }
            }            
        }
        return isWellTyped;
    }
    private bool TimeSpecIsWellTyped(TimeSpec timeSpec, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR) {
        TypeT fromType = ExpType(timeSpec.Start, envV, envC, envH, envT, envR);
        TypeT toType = ExpType(timeSpec.EndMarker, envV, envC, envH, envT, envR);
        
        return (fromType, toType) switch {
            (DateTimeT, DateTimeT) => true, // needs way to distinguish between to/for
            (DateTimeT, DurationT) => true,
            _ => Error($"Types {fromType} and {toType} incompatible with interval expression")
        };
    }

    private bool ConditionIsWellTyped(Exp? condition, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR) {
        if(condition != null) {
            TypeT condType = ExpType(condition, envV, envC, envH, envT, envR);
            return condType switch {
                BoolT => true,
                _ => Error($"Condition expected type 'bool' got '{condType}'")
            };
        }
        return true;
    }

    private bool RecurrenceIsWellTyped(RecurrenceSpec? recurrence, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR) {
        if(recurrence != null) {
            TypeT everyType = ExpType(recurrence.EveryDuration, envV, envC, envH, envT, envR);
            TypeT endType = ExpType(recurrence.EndMarker, envV, envC, envH, envT, envR);
            return (everyType, endType) switch {
                (DurationT, DateTimeT) => true, // needs way to distinguish between for/until
                (DurationT, DurationT) => true,
                _ => Error($"Types '{everyType}' and '{endType}' incompatible for recurrence.")       
            };
        }
        return true;
    }
    

    private TypeT HandleReschedule(Reschedule r, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR) {
        TimeSpecIsWellTyped(r.NewTimeInterval, envV, envC, envH, envT, envR);

        TypeT expType = ExpType(r.Reservation, envV, envC, envH, envT, envR);
        
        return expType switch {
            ReservationT => new ReservationT(),
            _ => Error("reschedule", expType, "reservation", new ReservationT())
        };
    }
    private void HandleTemplateCall(TemplateCall tc, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR) {
        List<TypeT> formalParamTypes = envT.Lookup(tc.TemplateId);

        if(formalParamTypes != null && tc.ArgList != null) {

            if(formalParamTypes.Count != tc.ArgList.Count) 
                errors.Add($"{tc.TemplateId} expected {formalParamTypes.Count} arguments got {tc.ArgList.Count}");

            for (int i = 0; i < formalParamTypes.Count; i++) {
                TypeT expected = formalParamTypes[i];
                TypeT actual = ExpType(tc.ArgList[i], envV, envC, envH, envT, envR);
                
                //if resource, check subtype, otherwise just check equality
                switch(actual, expected) {
                    case (ResourceT a, ResourceT e): { // both are resources: check subtyping
                        if (!envH.IsSubtype(a, e)) 
                            errors.Add($"Argument {i + 1} of template '{tc.TemplateId}' of type {actual} is not compatible with {expected}.");
                        break;
                    }
                    default: { // both not resources: check equality
                        if(expected != actual) 
                            errors.Add($"Argument {i + 1} of template '{tc.TemplateId}' expected {expected} but got {actual}."); 
                        break;       
                    }
                }
            }
        } else if(formalParamTypes != null && tc.ArgList == null || 
                  formalParamTypes == null && tc.ArgList != null) {
            errors.Add("Number of expected arguments don't match actual.");
        }
    }

    private void handleTemplateDecl(TemplateDecl tmplDecl, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR) {
        EnvV tmplScope = envV.NewScope();
        if(tmplDecl.ParamList != null) {
            List<TypeT> paramTypes = new();
            foreach(VarDecl param in tmplDecl.ParamList) {
                paramTypes.Add(param.Type);
                tmplScope.Bind(param.Identifier, param.Type);
            }
            envT.Bind(tmplDecl.TemplateId, paramTypes);
        }
        if(tmplDecl.TemplateBody != null) StmtType(tmplDecl.TemplateBody, tmplScope, envC, envH, envT, envR);
    }

    private void handleCategoryDecl(CategoryDecl cd, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR) {
        if(envC.C.Contains(cd.CategoryId)) errors.Add($"Category '{cd.CategoryId}' has already been declared.");
        envC.C.Add(cd.CategoryId);
        if(cd.ParentId != null) {
            if(!envC.C.Contains(cd.ParentId)) throw new Exception($"Use of undeclared category '{cd.ParentId}'.");
            if(cd.CategoryId == cd.ParentId) throw new Exception($"A category may not relate to itself");
            envH.H.Add(new ResourceT(cd.CategoryId), new ResourceT(cd.ParentId)); 
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


    private bool Error(string msg) {
        errors.Add(msg);
        return false;
    }
    private TypeT Error(BinaryOperation b, TypeT left, TypeT right, string op, TypeT expected) {
        errors.Add($"Line {b.LeftExpression.LineNumber}: Operand types '{left}' and '{right}' incompatible for '{op}'");
        return expected;
    }

    private TypeT Error(string expectedType, TypeT actualType, string op, TypeT expected) {
        errors.Add($"Operator '{op}' expected type '{expectedType}', but got '{actualType}'.");
        return expected;
    }
}


// STATEMENTS:
// expStmt          ??
// resourceDecl     ??
