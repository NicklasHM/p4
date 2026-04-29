using System.Net.NetworkInformation;
using RAL.AST;
namespace RAL.TC;

class TypeChecker {

    public List<string> errors = new();

    private Type ExpType(Exp exp, EnvV envV, EnvC envC, EnvH envH) {
        switch(exp) {
            case BoolV:        return new BoolT();
            case StringV:      return new StringT();
            case NumberV:      return new NumberT();
            case DurationV:    return new DurationT();
            case DateTimeV:    return new DateTimeT();
            case Reference r:  return envV.Lookup(r.VariableId); // return the type of the variable
            case BinaryOperation exp: { // turn into function to avoid confusing nesting?
                Type left  = ExpType(exp.LeftExpression,  envV, envC, envH);
                Type right = ExpType(exp.RightExpression, envV, envC, envH);
                switch(exp.Operator) {
                     case BinaryOperator.ADD:
                     case BinaryOperator.SUB: {
                        switch(left, right) {
                            case (NumberT, NumberT): return new NumberT(); // num + num
                            case (DateTimeT, DurationT): return new DateTimeT(); // dt + dur
                            default: errors.Add($"Line {exp.LeftExpression.LineNumber}: Operands '{left}' and '{right}' are incompatible for '{(exp.Operator == BinaryOperator.ADD ? '+' : '-')}'."); break;            
                        }
                        break;  
                    }
                    case BinaryOperator.MUL:
                    case BinaryOperator.DIV: {
                         switch(left, right) {
                            case (NumberT, NumberT): return new NumberT();
                            default: errors.Add($"Line {exp.LeftExpression.LineNumber}: Operands '{left}' and '{right}' are incompatible for '{(exp.Operator == BinaryOperator.MUL ? '*' : '/')}'."); break;                      
                        }       
                        break;
                    }
                    case BinaryOperator.LT:
                    case BinaryOperator.GT:
                    case BinaryOperator.LTEQ:
                    case BinaryOperator.GTEQ: {
                        switch(left, right) {
                            case (NumberT, NumberT): return new BoolT();
                            default: errors.Add($"Line {exp.LeftExpression.LineNumber}: Operands '{left}' and '{right}' are incompatible for '{(exp.Operator == BinaryOperator.LT ? "<" : exp.Operator == BinaryOperator.GT ? ">" : exp.Operator == BinaryOperator.LTEQ ? "<=" : ">=")}'."); break;           
                        }  
                        break;          
                    }
                    case BinaryOperator.EQ: // neither are allowed for nonprimitive types
                    case BinaryOperator.NEQ: {
                         switch(left, right) {
                            case(StringT, StringT): return new BoolT();
                            case(BoolT, BoolT): return new BoolT();
                            case(NumberT, NumberT): return new BoolT();
                            default: errors.Add($"Line {exp.LeftExpression.LineNumber}: Operands '{left}' and '{right}' are incompatible for '{(exp.Operator == BinaryOperator.EQ ? "==" : "!=")}'."); break;                                  
                        }       
                        break;
                    }

                    case BinaryOperator.OR:
                    case BinaryOperator.AND: {
                        switch(left, right) {
                            case(ReservationT, ReservationT): return new ReservationT(); // conditional reservation
                            case(BoolT, BoolT): return new BoolT();
                            default: errors.Add($"Line {exp.LeftExpression.LineNumber}: Operands '{left}' and '{right}' are incompatible for 'and'."); break;                                          
                        }         
                        break;   
                    }
                    case BinaryOperator.SEQ: {
                        switch(left, right) {
                            case(ReservationT, ReservationT): return new ReservationT();
                            default: errors.Add($"Line {exp.LeftExpression.LineNumber}: Operands '{left}' and '{right}' are incompatible for 'seq'."); break;                                              
                        } 
                        break;
                    }
                }
            }
            case UnaryOperation uExp: {
                Type type = ExpType(uExp.Expression, envV, envC, envH);
                switch(uExp.Operator) {
                    case UnaryOperator.NOT: {
                        switch(type) {
                            case BoolT: break;
                            default: errors.Add($"Operator 'not' expected type 'bool' got '${type}'."); break;
                        }
                    }
                    break;
                    case UnaryOperator.NEG: {
                        switch(type) {
                            case NumberT: break;
                            default: errors.Add($"Operator 'not' expected type 'number' got '${type}'."); break;      
                        }
                        break;
                    }

                    default: errors.Add($"Unknown operator '{uExp.Operator}'"); // should never happen
                    break;
                }
                break;
            }
            case Assignment a: {
                Type varType = envV.Lookup(a.VariableId);
                switch(varType) {
                    case ResourceT: errors.Add($"Assignment to resources not allowed"); break;
                    default: break;        
                }
                Type expType = ExpType(a.Value, envV, envC, envH);
                if(expType != varType) errors.Add($"Variable ${a.VariableId} expected type ${varType} but got ${expType}.");      
                break;
            }

            default: throw new Exception("Unknown type."); // should never happen
        }

    }

    private bool checkQueryExp(QueryData queryData, EnvV envV, EnvC envC, EnvH envH) {
        foreach(ResourceSpec resource in queryData.ResourceSpecs) {
            if(resource.Quantity != null) {
                Type resQuantType = ExpType(resource.Quantity, envV, envC, envH);
                switch(resQuantType) {
                    case NumberT: break;
                    default: {
                        errors.Add($"Expected type 'number' got '{resQuantType}'");
                        return false;
                    }
                }
                if(resource.CategoryId != null && !envC.C.Contains(resource.CategoryId)) errors.Add($"Undeclared category '{resource.CategoryId}'");
                // identifier
            }
        }
        Type fromType = ExpType(queryData.Interval.Start, envV, envC, envH);
        Type toType = ExpType(queryData.Interval.EndMarker, envV, envC, envH);
        switch(fromType, toType) {
            case(DateTimeT, DateTimeT): break;
            case(DateTimeT, DurationT): break;
            default: {
                errors.Add($"Types {fromType} and {toType} incompatible with interval expression");
                return false;            
            }
        }
        if(queryData.Condition != null) {
            Type condType = ExpType(queryData.Condition, envV, envC, envH);
            switch(condType) {
                case BoolT: break;
                default: {
                    errors.Add($"Condition expected type 'bool' got '{condType}'"); 
                    return false;  
                }
            }
        }

        if(queryData.Recurrence != null) {
            Type everyType = ExpType(queryData.Recurrence.EveryDuration, envV, envC, envH);
            Type endType = ExpType(queryData.Recurrence.EndMarker, envV, envC, envH);
            switch(everyType, endType) {
                case(DurationT, DateTimeT): break;
                case(DurationT, DurationT): break;
                default: {
                    errors.Add($"Types '{everyType}' and '{endType}' incompatible for recurrence.");
                    return false;            
                }        
            }
        }
        return true;
    }


    private void StmtType(Stmt stmt, EnvV envV, EnvC envC, EnvH envH) {
        switch(stmt) {
            case Composite cmp: {
                StmtType(cmp.Stmt1, envV, envC, envH); // check null
                StmtType(cmp.Stmt2, envV, envC, envH); // check null
                break;     
            }
            case VarDecl decl: {
                switch(decl.Type) {
                    case ResourceT r: if(!envC.C.Contains(r.Category)) throw new Exception($"Use of undeclared category '{r.Category}'."); break;
                    default: break;  
                    }       
                }
                envV.Bind(decl.Identifier, decl.Type); // still binds even if types dont match; is a problem
                break;
        
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
                switch(envV.Lookup(mv.ResourceId, out Type type)) { 
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
                Type type = ExpType(c.Reservation, envV, envC, envH);
                switch(type) { 
                    case ReservationT: break;
                    default: errors.Add($"Expected type 'Reservation' got: {type}."); break;  
                }
                break;
            }

            case If i: {
                Type type = ExpType(i.Condition, envV, envC, envH);
                switch(type) {
                    case BoolT: break;
                    default: errors.Add($"If statement expects condition of type 'bool' got '{type}'"); break;      
                }
                if(i.ThenBody != null) StmtType(i.ThenBody, envV.NewScope(), envC, envH);        
                if(i.ElseBody != null) StmtType(i.ElseBody, envV.NewScope(), envC, envH);
                break;
            }

            case TemplateDecl tmplDecl: {
                //some code
                break;
            }

            case ExpStmt: {

            }

            case Availability av: {
                
            }

        }
    }

}



/*
abstract record TypeInfo;

record VariableType(string TypeName) : TypeInfo;

record FunctionType(List<string> ParameterTypes) : TypeInfo;

Dictionary<string, TypeInfo> EnvV = new();

if (EnvV["f"] is FunctionType func)
{
    Console.WriteLine(string.Join(", ", func.ParameterTypes));
}
else if (EnvV["x"] is VariableType v)
{
    Console.WriteLine(v.TypeName);
}

EnvV["x"] = new VariableType("int");

EnvV["f"] = new FunctionType(
    new List<string> { "int", "bool" },
    "string"
);

TypeInfo info = EnvV["f"];

switch (info)
{
    case VariableType v:
        Console.WriteLine($"Variable of type {v.TypeName}");
        break;

    case FunctionType f:
        Console.WriteLine($"Function with params: {string.Join(", ", f.ParameterTypes)}");
        break;
}

*/
