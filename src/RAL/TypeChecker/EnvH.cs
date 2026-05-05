namespace RAL.TC;
using RAL.AST;


public class EnvH {

    //Map: Each category to its immediate supertype. If none supplied at declaration, Root "Resource" is immediate super.
    private readonly Dictionary<ResourceT, ResourceT> H = new(); // child --> parent


    /// <summary> Boolean function: True if first argument is descendant of second </summary>
    public bool IsSubtype(ResourceT child, ResourceT parent) {

        while (child != null) {
            if (child == parent) 
                return true;
            
            if (!H.TryGetValue(child, out child)) 
                break;
        }
        return false;
    }

    public void EstablishRelation(ResourceT child, ResourceT parent) {
        if(child == parent) throw new Exception("A category may not relate to itself");
        H[child] = parent;
    } 
}