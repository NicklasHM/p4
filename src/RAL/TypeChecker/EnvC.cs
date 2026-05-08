namespace RAL.TC;
using RAL.AST;

/// <summary> Environment containing the set of user-defined Categories. /// </summary>
public class EnvC {
    private HashSet<string> C = new() {"Resource"};

    public bool CategoryIsDeclared(string category) {
        return C.Contains(category);
    }

    public bool AddCategory(string category) {
        if(CategoryIsDeclared(category)) 
            return false;
            
        C.Add(category);
        return true;
    }
}