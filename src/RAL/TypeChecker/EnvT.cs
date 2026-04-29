namespace RAL.TC;

public class EnvT {
    public readonly Dictionary<string, List<Type>> T = new();

        public void Bind(string var, List<Type> types) {
        if (T.ContainsKey(var)) {
            throw new Exception($"Template '{var}' already declared.");
        }

        T[var] = types;
    }


    public List<Type> Lookup(string var) {
        if (T.TryGetValue(var, out List<Type> types)) {
            return types;
        } else {
            throw new Exception($"Use of undeclared template: '{var}'.");
        }
    }
}