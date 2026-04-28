using RAL.AST;
namespace RAL.TC;

class TypeChecker {

    public List<string> errors = new();

    private Type ExpType(Exp exp, EnvV envV, EnvC envC, EnvH envH) {
        switch(exp) {
            case BoolV:        return BoolT;
            case StringV:      return StringT;
            case NumberV:      return NumberT;
            case DurationV:    return DurationT;
            case DateTimeV:    return DateTimeT;
            case ResourceV:    return ResourceT;
            case ReservationV: return ReservationT;
            case RefV r:       return envV.Lookup(r.VariableId); // return the type of the variable
            case BinaryOperation exp: { // turn into function to avoid confusing nesting?
                Type left  = ExpType(exp.LeftExpression,  envV, envC, envH);
                Type right = ExpType(exp.RightExpression, envV, envC, envH);
                switch(exp.Operator) {
                     case BinaryOperator.ADD:
                     case BinaryOperator.SUB: {
                        switch(left, right) {
                            case (NumberT, NumberT): return NumberT; // num + num
                            case (DateTimeT, DurationT): return DateTimeT; // dt + dur
                            default: errors.Add($"Line {exp.LeftExpression.LineNumber}: Operands '{left}' and '{right}' are incompatible for '{(exp.Operator == BinaryOperator.ADD ? '+' : '-')}'."); break;            
                        }  
                    }
                    case BinaryOperator.MUL:
                    case BinaryOperator.DIV: {
                         switch(left, right) {
                            case (NumberT, NumberT): return NumberT;
                            default: errors.Add($"Line {exp.LeftExpression.LineNumber}: Operands '{left}' and '{right}' are incompatible for '{(exp.Operator == BinaryOperator.MUL ? '*' : '/')}'."); break;                      
                        }       
                    }
                    case BinaryOperator.LT:
                    case BinaryOperator.GT:
                    case BinaryOperator.LTEQ:
                    case BinaryOperator.GTEQ: {
                        switch(left, right) {
                            case (NumberT, NumberT): return BoolT;
                            default: errors.Add($"Line {exp.LeftExpression.LineNumber}: Operands '{left}' and '{right}' are incompatible for '{(exp.Operator == BinaryOperator.LT ? "<" : exp.Operator == BinaryOperator.GT ? ">" : exp.Operator == BinaryOperator.LTEQ ? "<=" : ">=")}'."); break;           
                        }            
                    }
                    case BinaryOperator.EQ:
                    case BinaryOperator.NEQ: {
                         switch(left, right) {
                            case(BoolT, BoolT): return BoolT;
                            case(NumberT, NumberT): return BoolT;
                            default: errors.Add($"Line {exp.LeftExpression.LineNumber}: Operands '{left}' and '{right}' are incompatible for '{(exp.Operator == BinaryOperator.EQ ? "==" : "!=")}'."); break;                                  
                        }       
                    }
                    case BinaryOperator.OR: {
                        switch(left, right) {
                             case(BoolT, BoolT): BoolT;
                             case(ReservationT, ReservationT): ReservationT;
                             default: errors.Add($"Line {exp.LeftExpression.LineNumber}: Operands '{left}' and '{right}' are incompatible for 'or'."); break;                                           
                        }            
                    }
                    case BinaryOperator.AND: {
                        switch(left, right) {
                            case(ResourceT, ResourceT): break; // ?????
                            case(ReservationT, ReservationT): ReservationT;
                            case(BoolT, BoolT): BoolT;
                            default: errors.Add($"Line {exp.LeftExpression.LineNumber}: Operands '{left}' and '{right}' are incompatible for 'and'."); break;                                          
                        }            
                    }
                    case BinaryOperator.SEQ: {
                        switch(left, right) {
                            case(ReservationT, ReservationT): ReservationT;
                            default: errors.Add($"Line {exp.LeftExpression.LineNumber}: Operands '{left}' and '{right}' are incompatible for 'seq'."); break;                                              
                        } 
                    }
                }
            }
            case UnaryOperation: {
                    
            }

            default: throw new Exception("Unknown type."); // should never happen
        }
    }


    private void StmtType(Stmt stmt) {
        switch(stmt) {
            case VarDecl: // if(lookup[t.val] != expr(e)) error


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
