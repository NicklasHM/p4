namespace RAL.TC;
using RAL.AST;

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

    public List<string> GetResourcesByCategory(string category) {
        List<string> resources = new();
        
        // Check current scope
        foreach (var kvp in V) {
            // Warning: In your current HandleResourceDecl, you bind the ResourceId as the Category.
            // Assuming 'Category' holds the actual category name:
            if (kvp.Value is ResourceT resT && resT.Category == category) {
                resources.Add(kvp.Key);
            }
        }

        // Recursively check parent scopes
        if (parent != null) {
            resources.AddRange(parent.GetResourcesByCategory(category));
        }

        return resources.Distinct().ToList();
    }
}