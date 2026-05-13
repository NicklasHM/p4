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

    /// <summary> flat enumarable of subCategories (including itself), organised by descendant level </summary>
    public IEnumerable<string> GetSubCategories(string categoryId) {
        
        HashSet<string> subCategories  = [];
        
        //breadth first, i.e. organised by nearest descendants
        var queue = new Queue<string>();
        queue.Enqueue(categoryId);

        while (queue.Count > 0) {
            string current = queue.Dequeue();
            subCategories.Add(current);

            foreach (string child in GetChildren(current))
                queue.Enqueue(child);
        }

        return subCategories;
    }

    /// <summary> Find all categories whose immediate parent is 'current' </summary>
    private IEnumerable<string> GetChildren(string categoryId) =>  
        parentCatRelations
            .Where(relation => relation.Value == categoryId)
            .Select(relation => relation.Key);

}