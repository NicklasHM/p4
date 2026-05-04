namespace RAL.Semantic.Symbols;

using RalType = RAL.AST.TypeT;
/*
Symbol information for a template parameter.
Parameters are not stored directly in EnvF. Instead, they are stored
inside the FunctionSymbol and later added to EnvAT when the body of the template is type-checked.
*/
public class ParameterSymbol
{
    
    // The parameter name.
    public string Name { get; }

    
    // The declared type of the parameter. 
    public RalType Type { get; }

    public ParameterSymbol(string name, RalType type)
    {
        Name = name;
        Type = type;
    }
}