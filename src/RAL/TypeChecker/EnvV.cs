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

        Console.WriteLine("EnvV Binding " + var + " to: " + V[var] + "\n");
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
}