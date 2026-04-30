namespace RAL.Semantic.Symbols;

/*
Symbol information for a declared template.

A FunctionSymbol is stored in EnvF and contains the semantic
information needed to type-check or later execute a template call.
*/
public class FunctionSymbol
{
   
    // The name of the emplate.
        public string Name { get; }

   
    // The ordered list of parameters declared by the template.
    // Order matters when checking template calls.
        public List<ParameterSymbol> Parameters { get; }

   
    // The AST node representing the template body.
    public object Body { get; } // IMPORTANT:Replace object with the correct AST type when the template/body AST node is finalized.

    public FunctionSymbol(string name, List<ParameterSymbol> parameters, object body)
    {
        Name = name;
        Parameters = parameters;
        Body = body;
    }
}