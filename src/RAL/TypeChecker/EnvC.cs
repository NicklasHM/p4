namespace RAL.TC;
using RAL.AST;

/// <summary> Environment containing the set of user-defined Categories. /// </summary>
public class EnvC {
    private HashSet<string> C = new() {"Resource"};

    public bool CategoryIsDeclared(string category) {
        return C.Contains(category);
    }
    /// <summary>
    /// adds a category to the environment allowing it to be used in resource declarations
    /// </summary>
    /// <returns> false for duplicate entries. true otherwise
    /// </returns>
    public bool AddCategory(string category) {
        if(CategoryIsDeclared(category)) 
            return false;
            
        C.Add(category);
        return true;
    }
}