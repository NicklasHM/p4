namespace RAL.TC;
using RAL.AST;

public class EnvH {
    public readonly Dictionary<ResourceT, ResourceT> H = new(); // child --> parent

    public bool IsSubtype(ResourceT child, ResourceT parent) {
        while (child != null) {
            if (child == parent) return true;
            if (!H.TryGetValue(child, out child)) break;
        }
        return false;
    }


}