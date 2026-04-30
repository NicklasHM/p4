using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using RAL.AST;
using RAL.Interpreter;
namespace RAL.TC;

class TypeChecker {

    public List<string> errors = new();

    //Since un-bound variables throw exceptions, return value is not nullable. All other cases return types
    private TypeT ExpType(Exp exp, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR) =>
        exp switch {
            BoolV => new BoolT(),
            StringV => new StringT(),
            NumberV => new NumberT(),
            DateTimeV => new DateTimeT(),
            DurationV => new DurationT(),

            Reference r when r.PropertyId == null => envV.Lookup(r.VariableId),
            Reference r => envR.LookupField(r.VariableId, r.PropertyId),

            Assignment a => HandleAssignment(a, envV, envC, envH, envT, envR),

            BinaryOperation b => HandleBinary(b, envV, envC, envH, envT, envR),

            UnaryOperation u => HandleUnary(u, envV, envC, envH, envT, envR),

            // new scope environment? for query
            Reserve r when QueryIsWellTyped(r.Query, envV, envC, envH, envT, envR) => new ReservationT(),

            Reschedule r => HandleReschedule(r, envV, envC, envH, envT, envR),

            TemplateCall tc => HandleTemplateCall(tc, envV, envC, envH, envT, envR),

            _ => throw new Exception("Unknown type.") // should never happen
        };


    private void StmtType(Stmt stmt, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR) {
        switch(stmt) {
            case Composite cmp: {
                if(cmp.Stmt1 != null) StmtType(cmp.Stmt1, envV, envC, envH, envT, envR);
                if(cmp.Stmt2 != null) StmtType(cmp.Stmt2, envV, envC, envH, envT, envR);
                break;     
            }

            case VarDecl decl: {
                switch(decl.TypeT) {
                    case ResourceT r: if(!envC.C.Contains(r.Category)) throw new Exception($"Use of undeclared category '{r.Category}'."); break;
                    default: break;  
                }       
                envV.Bind(decl.Identifier, decl.TypeT); // still binds even if types dont match; is a problem
                break;
            }

            case ResourceDecl rd: { // WIP
                if(rd.Properties != null) {
                    
                }
                break;
            }
        
            case CategoryDecl cd: {
                if(envC.C.Contains(cd.CategoryId)) errors.Add($"Category '{cd.CategoryId}' has already been declared.");
                envC.C.Add(cd.CategoryId);
                if(cd.ParentId != null) {
                    if(!envC.C.Contains(cd.ParentId)) throw new Exception($"Use of undeclared category '{cd.ParentId}'.");
                    if(cd.CategoryId == cd.ParentId) throw new Exception($"A category may not relate to itself");
                    envH.H.Add(cd.CategoryId, cd.ParentId);        
                }
                break;
            }

            case Move mv: {
                switch(envV.Lookup(mv.ResourceId, out TypeT type)) { 
                    case ResourceT: {
                        if(!envC.C.Contains(mv.CategoryId)) throw new Exception($"Use of undeclared category '{mv.CategoryId}'.");
                        envV.ChangeCategory(mv.ResourceId, new ResourceT(mv.CategoryId)); break;          
                    }
                    default: {
                        errors.Add($"Expected type 'Resource' got '{type}'"); // type --> string conversion unsure
                        break;           
                    }
                }
                break;       
            }

            case Cancel c: {
                TypeT type = ExpType(c.Reservation, envV, envC, envH, envT, envR);
                switch(type) { 
                    case ReservationT: break;
                    default: errors.Add($"Expected type 'Reservation' got: {type}."); break;  
                }
                break;
            }

            case If i: {
                TypeT type = ExpType(i.Condition, envV, envC, envH, envT, envR);
                switch(type) {
                    case BoolT: break;
                    default: errors.Add($"If statement expects condition of type 'bool' got '{type}'"); break;      
                }
                if(i.ThenBody != null) StmtType(i.ThenBody, envV.NewScope(), envC, envH, envT, envR);        
                if(i.ElseBody != null) StmtType(i.ElseBody, envV.NewScope(), envC, envH, envT, envR);
                break;
            }

            case TemplateDecl tmplDecl: {
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
                break;
            }

            case ExpStmt: {

            }

            case Availability av: {
                QueryIsWellTyped(av.Query, envV, envC, envH, envT, envR); break;
            }

        }
    }
    
    private TypeT HandleAssignment(Assignment a, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR)
    {
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
            BinaryOperator.SUB => (left, right) switch
            {
                (NumberT, NumberT)     => new NumberT(), // num + num
                (DateTimeT, DurationT) => new DateTimeT(), // dt + dur
                _ => Error(exp, left, right, operatorAsString)
            },


            BinaryOperator.MUL or 
            BinaryOperator.DIV => (left, right) switch
            {
                (NumberT, NumberT) => new NumberT(), // num */ num
                _ => Error(exp, left, right, operatorAsString)
            },

            BinaryOperator.LT   or 
            BinaryOperator.GT   or 
            BinaryOperator.LTEQ or 
            BinaryOperator.GTEQ => (left, right) switch
            {
                (NumberT, NumberT) => new BoolT(),
                _ => Error(exp, left, right, operatorAsString)
            },

            // neither are allowed for nonprimitive types
            BinaryOperator.EQ or 
            BinaryOperator.NEQ => (left, right) switch
            {
                (StringT, StringT) => new BoolT(),
                (BoolT, BoolT)     => new BoolT(),
                (NumberT, NumberT) => new BoolT(),
                _ => Error(exp, left, right, operatorAsString)
            },

            BinaryOperator.OR or BinaryOperator.AND => (left, right) switch
            {
                (ReservationT, ReservationT) => new ReservationT(), // conditional reservation
                (BoolT, BoolT)               => new BoolT(),
                _ => Error(exp, left, right, operatorAsString)
            },

            BinaryOperator.SEQ => (left, right) switch 
            {
                (ReservationT, ReservationT) => new ReservationT(),
                _ => Error(exp, left, right, operatorAsString)
            },
            _ => throw new Exception("Unknown type.") // should never happen      
        };
    }

    private TypeT HandleUnary(UnaryOperation exp, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR) 
    {
        TypeT operandType = ExpType(exp.Expression, envV, envC, envH, envT, envR);
        string operatorAsString = EnumToOp(exp.Operator);
        return exp.Operator switch 
        {
            UnaryOperator.NOT when (operandType is BoolT) => new BoolT(),
            UnaryOperator.NOT => Error("bool", operandType, operatorAsString),


            UnaryOperator.NEG when (operandType is NumberT) => new NumberT(),
            UnaryOperator.NEG => Error("number", operandType, operatorAsString),

            _ => throw new Exception("Unknown type.") // should never happen
        };
    }
  
    private bool QueryIsWellTyped(QueryData queryData, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR) {


        if(!ResourceSpecIsWellTyped(queryData.ResourceSpecs, envV, envC, envH, envT, envR)) return false;

        if(!TimeSpecIsWellTyped(queryData.Interval, envV, envC, envH, envT, envR)) return false;

        if(queryData.Condition != null) {
            TypeT condType = ExpType(queryData.Condition, envV, envC, envH, envT, envR);
            switch(condType) {
                case BoolT: break;
                default: {
                    errors.Add($"Condition expected type 'bool' got '{condType}'"); 
                    return false;  
                }
            }
        }

        if(queryData.Recurrence != null) {
            TypeT everyType = ExpType(queryData.Recurrence.EveryDuration, envV, envC, envH, envT, envR);
            TypeT endType = ExpType(queryData.Recurrence.EndMarker, envV, envC, envH, envT, envR);
            switch(everyType, endType) {
                case(DurationT, DateTimeT): break; // needs way to distinguish between for/until
                case(DurationT, DurationT): break;
                default: {
                    errors.Add($"Types '{everyType}' and '{endType}' incompatible for recurrence.");
                    return false;            
                }        
            }
        }
        return true;
    }

    //Check all resource specifications: a*rc ident | r
    private bool ResourceSpecIsWellTyped(List<ResourceSpec> resourceSpecs, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR)
    {
        //new environment used only if a local variable binding is encountered
        EnvV reserveScope = envV.NewScope();
        
        bool isWellTyped = true;
        
        foreach(ResourceSpec resourceSpec in resourceSpecs) {
            
            // a rc [ident]
            if(resourceSpec.Quantity != null) {
                TypeT quantityType = ExpType(resourceSpec.Quantity, envV, envC, envH, envT, envR);
                
                if (quantityType is not NumberT)
                {
                    errors.Add($"Expected type 'number' got '{quantityType}'");
            
                    isWellTyped = false;
                }

                //No need to check (resourceSpec.CategoryId != null) given quantity is not null, would be rejected by parser
                if(!envC.C.Contains(resourceSpec.CategoryId))
                {
                    errors.Add($"Use of undeclared category '{resourceSpec.CategoryId}'");
                    isWellTyped = false;
                }

                //hvis id findes så bind til nyt scope
                if(resourceSpec.Identifier != null)
                {
                    reserveScope.Bind(resourceSpec.Identifier, new ResourceT(resourceSpec.CategoryId));
                }
            } 
            else // r case, lookup in current scope, ensure its a Resource Type
            {
                if(envV.Lookup(resourceSpec.Identifier) is not ResourceT)
                {
                    errors.Add($"Wrong Type '{resourceSpec.Identifier}'");
                    isWellTyped = false;
                }
            }            
        }
        return isWellTyped;
    }
    private bool TimeSpecIsWellTyped(TimeSpec timeSpec, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR) {
        TypeT fromType = ExpType(timeSpec.Start, envV, envC, envH, envT, envR);
        TypeT toType = ExpType(timeSpec.EndMarker, envV, envC, envH, envT, envR);
        
        return (fromType, toType) switch
        {
            (DateTimeT, DateTimeT) => true, // needs way to distinguish between to/for
            (DateTimeT, DurationT) => true,

            _ => false // TODO: errors.Add($"Types {fromType} and {toType} incompatible with interval expression");
        };
    }
    

    private TypeT HandleReschedule(Reschedule r, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR) 
    {
        TimeSpecIsWellTyped(r.NewTimeInterval, envV, envC, envH, envT, envR);

        TypeT expType = ExpType(r.Reservation, envV, envC, envH, envT, envR);
        
        return expType switch
        {
            ReservationT => new ReservationT(),
            _ => errors.Add($"Expected type 'reservation' got '{expType}'")       
        };
    }
    private TypeT HandleTemplateCall(TemplateCall tc, EnvV envV, EnvC envC, EnvH envH, EnvT envT, EnvR envR) 
    {
        List<TypeT> formalParamsTypes = envT.Lookup(tc.TemplateId);

        if(formalParamsTypes != null && tc.ArgList != null) {

            if(formalParamsTypes.Count != tc.ArgList.Count) 
                errors.Add($"{tc.TemplateId} expected {formalParamsTypes.Count} arguments got {tc.ArgList.Count}");

            for (int i = 0; i < formalParamsTypes.Count; i++) {
                TypeT expected = formalParamsTypes[i];
                TypeT actual = ExpType(tc.ArgList[i], envV, envC, envH, envT, envR);
                
                
                //if resource, check category
                if (!envH.IsSubtype(actual, expected)) 
                    throw new Exception($"Argument {i + 1} of template '{tc.TemplateId}' expected {expected} but got {actual}.");
            }
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


    private void Error(BinaryOperation b, TypeT left, TypeT right, string op)
    {
        errors.Add($"Line {b.LeftExpression.LineNumber}: Operand types '{left}' and '{right}' incompatible for '{op}'");
    }

    private void Error(string expectedType, TypeT actualType, string op)
    {
        errors.Add($"Operator '{op}' expected type '{expectedType}', but got '{actualType}'.");
    }
}


// STATEMENTS:
// expStmt          ??
// resourceDecl     ??
