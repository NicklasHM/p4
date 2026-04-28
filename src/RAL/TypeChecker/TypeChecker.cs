using RAL.AST;
namespace RAL.TC;

class TypeChecker {
    private Type ExpType(Exp exp) {
        switch(exp) {
            case BoolV:        return BoolT;
            case StringV:      return StringT;
            case NumberV:      return NumberT;
            case DurationV:    return DurationT;
            case DateTimeV:    return DateTimeT;
            case ResourceV:    return ResourceT;
            case ReservationV: return ReservationT;
            // case RefV r: environment lookup to check type: tuple for template calls ?
            default: {




            }
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

Dictionary<string, TypeInfo> env = new();

if (env["f"] is FunctionType func)
{
    Console.WriteLine(string.Join(", ", func.ParameterTypes));
}
else if (env["x"] is VariableType v)
{
    Console.WriteLine(v.TypeName);
}

env["x"] = new VariableType("int");

env["f"] = new FunctionType(
    new List<string> { "int", "bool" },
    "string"
);

TypeInfo info = env["f"];

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
