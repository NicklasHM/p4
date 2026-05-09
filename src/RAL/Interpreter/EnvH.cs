public class EnvH()
{
    //Map: Each category to its immediate supertype. If none supplied at declaration, Root "Resource" is immediate super.
    private readonly Dictionary<string, string?> parentCatRelations = new() { 
        {"Resource", null} 
    }; 


    /// <summary>  </summary>
    /// <param name="categoryId"> The newly declared category. </param>
    /// <param name="parentId"> If null supplied, "Resource" is parent </param>
    public void EstablishRelation(string categoryId, string? parentId)
    {
        parentCatRelations.Add(categoryId, parentId ?? "Resource");
    }

}