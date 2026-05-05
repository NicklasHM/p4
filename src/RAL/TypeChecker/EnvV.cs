namespace RAL.TC;
using RAL.AST;
using RAL.Interpreter;

/// <summary>  </summary>
public class EnvV {
    private readonly Dictionary<string, TypeT> V = new();
    private readonly EnvV? parent;

    public EnvV(EnvV? parent = null) {
        this.parent = parent;
    }

    public EnvV NewScope() {
        return new EnvV(this);
    }

    public void Bind(string var, TypeT type) {
        if (V.ContainsKey(var)) {
            throw new Exception($"Variable '{var}' already declared in current scope.");
        }

        V[var] = type;
    }

    public void ChangeCategory(string var, TypeT type) {
        if (V.ContainsKey(var)) {
            V[var] = type;
        } else {
            throw new Exception($"Use of undeclared variable: '{var}'.");
        }
    }

    public TypeT Lookup(string var) {
        if (V.TryGetValue(var, out TypeT type)) 
            return type;

        if (parent != null) 
            return parent.Lookup(var);
        
        throw new Exception($"Use of undeclared variable: '{var}'.");
    }

    // unneeded
    public bool IsLocal(string var) {
        return V.ContainsKey(var);
    }

/// <summary> Traverses up the parent envVs and returns the envV for global scope </summary>
    public EnvV GetGlobalScope()
    {
        //
        EnvV current = this;

        //Traverse up scopes until global, which has no parrent
        while (current.parent != null)
        {
            current = current.parent;
        }

        //Return global scope
        return current;
    }

    ///<summary> Returns a list of all ids of variablesthat are of (or subtype of) the provided category /// </summary>
    public List<string> GetResourcesByCategory(ResourceT category, EnvV envV, EnvH envH) {
        List<string> resourceIds = new();

        //Resources are only declarable in global scope, traverse til there
        EnvV globalScope = envV.GetGlobalScope();
        
        // Check current scope
        foreach (KeyValuePair<string, TypeT> pair in globalScope.V) {
            //Pattern match on those elements of resource types, add the 
            if (pair.Value is ResourceT resource && envH.IsSubtype(resource, category)) {
                resourceIds.Add(pair.Key);
            }
        }

        return resourceIds;
    }
}