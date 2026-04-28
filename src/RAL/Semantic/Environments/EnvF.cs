namespace RAL.Semantic.Environments;

using RAL.Semantic.Symbols;

/*
 Template environment.
 
 In this DSL, templates are the equivalent of functions.
 Therefore, this environment stores all declared templates by name.
 
 It is separate from EnvV because variables and templates
 belong to different semantic namespaces.
 
 Templates are global declarations.
 Therefore, this environment does not need nested scopes.
*/
public class EnvF
{
    private readonly Dictionary<string, FunctionSymbol> functions = new();

    // Adds a new template to the environment. 
    // This prevents redeclaration in the same template environment.
    public void Bind(string name, FunctionSymbol function)
    {
        if (functions.ContainsKey(name))
            throw new Exception($"Template '{name}' is already defined.");

        functions[name] = function;
    }

    
    // Looks up a template by name.
    // Throws an error if the template has not been declared.
    public FunctionSymbol Lookup(string name)
    {
        if (functions.TryGetValue(name, out var function))
            return function;

        throw new Exception($"Unknown Template '{name}'.");
    }

    
    // Checks whether a template is already declared.
    public bool IsDefined(string name)
    {
        return functions.ContainsKey(name);
    }
}