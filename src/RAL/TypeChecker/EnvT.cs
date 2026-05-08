namespace RAL.TC;
using RAL.AST;

/// <summary> Environment for templates /// </summary>
public class EnvT {
    private readonly Dictionary<string, List<TypeT>> T = new();

    public bool Bind(string var, List<TypeT> types) {
        if (T.ContainsKey(var)) {
            return false;
        }

        T.Add(var, types);
        return true;
    }

    public List<TypeT>? Lookup(string var) {
        if (T.TryGetValue(var, out List<TypeT> types)) {
            return types;
        } else {
            return null;
        }
    }
}