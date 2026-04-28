namespace RAL.TC;
using RAL.AST;

public class EnvH {
    private readonly Dictionary<string, string> H = new(); // child --> parent

    bool IsSubtype(string child, string parent) {
        while (child != null) {
            if (child == parent) return true;
            if (!H.TryGetValue(child, out child)) break;
        }
        return false;
    }
}