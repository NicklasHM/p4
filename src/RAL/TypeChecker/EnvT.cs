namespace RAL.TC;
using RAL.AST;

public class EnvT {
    public readonly Dictionary<string, List<TypeT>> T = new();

    public void Bind(string var, List<TypeT> types) {
        if (T.ContainsKey(var)) {
            throw new Exception($"Template '{var}' already declared.");
        }

        T[var] = types;
    }

    public List<TypeT> Lookup(string var) {
        if (T.TryGetValue(var, out List<TypeT> types)) {
            return types;
        } else {
            throw new Exception($"Use of undeclared template: '{var}'.");
        }
    }
}