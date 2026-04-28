namespace RAL.TC;
using RAL.AST;

public class EnvV {
    private readonly Dictionary<string, Type> E = new();
    private readonly EnvV? parent;

    public EnvV(EnvV? parent = null) {
        this.parent = parent;
    }

    public EnvV NewScope() {
        return new EnvV(this);
    }

    public void Bind(string var, Type type) {
        if (E.ContainsKey(var)) {
            throw new Exception($"Variable '{var}' already declared in current scope.");
        }

        E[var] = type;
    }


    public void ChangeCategory(string var, Type type) {
        if (E.ContainsKey(var)) {
            E[var] = type;
        } else {
            throw new Exception($"Use of undeclared variable: '{var}'.");
        }
    }


    public Type Lookup(string var) {
        if (E.TryGetValue(var, out var type)) {
            return type;
        } else if (parent != null) {
            return parent.Lookup(var);
        } else {
            throw new Exception($"Use of undeclared variable: '{var}'.");
        }
    }


    public bool IsLocal(string var) {
        return E.ContainsKey(var);
    }
}