namespace RAL.TC;
using RAL.AST;


public class EnvH {
    private readonly Dictionary<ResourceT, ResourceT> H = new(); // child --> parent

    public bool IsSubtype(ResourceT child, ResourceT parent) {
        while (child != null) {
            if (child == parent) return true;
            if (!H.TryGetValue(child, out child)) break;
        }
        return false;
    }

    public void EstablishRelation(ResourceT child, ResourceT parent) {
        if(child == parent) throw new Exception("A category may not relate to itself");
        H[child] = parent;
    } 
}