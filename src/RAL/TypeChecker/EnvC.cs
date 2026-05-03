namespace RAL.TC;
using RAL.AST;

/// <summary> Environment containing the set of user-defined Categories. /// </summary>
public class EnvC {
    private HashSet<string> C = new();

    public bool CategoryIsDeclared(string category) {
        return C.Contains(category);
    }

    public void AddCategory(string category) {
        if(CategoryIsDeclared(category)) throw new Exception("Category has already been declared");
        C.Add(category);
    }
}