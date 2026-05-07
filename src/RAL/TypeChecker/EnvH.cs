namespace RAL.TC;
using RAL.AST;


public class EnvH {

    //Map: Each category to its immediate supertype. If none supplied at declaration, Root "Resource" is immediate super.
    private readonly Dictionary<ResourceT, ResourceT?> parentCatRelations = new() { 
        {new ResourceT("Resource"), null} 
    }; 

    /// <summary> Boolean lookup function "envH()": True if first argument is of same type or descendant of second </summary>
    public bool IsSubtype(ResourceT childCat, ResourceT parentCat) {

        while (childCat != null) {
            if (childCat == parentCat) 
                return true;
            
            if (!parentCatRelations.TryGetValue(childCat, out childCat)) 
                break;
        }
        return false;
    }



    public void EstablishRelation(ResourceT childCat, ResourceT parentCat) {
        if(childCat == parentCat) throw new Exception("A category may not relate to itself");
        parentCatRelations[childCat] = parentCat;
    } 

/// <summary> Under valid input, guarentees parentCat. Nullable due to 'Resource' having no parentCat. Otherwise, throws exception <summary>
    public ResourceT? GetParent (ResourceT childCat) {
        
        //Guard invalid childCat input. tryGetValue returns false if dictionary did not contain key 'childCat', parentCat would be null
        if (parentCatRelations.TryGetValue(childCat, out ResourceT parentCat) == false)
            throw new Exception("Invalid childCat argument to GetparentCat");
       
       //childCat input valid. 'parentCat' contains value at key 'childCat'
        return parentCat;
    }

    //Returns a 'list' of all immediate children categories
    public IEnumerable<ResourceT> GetChildren(ResourceT category) =>
    parentCatRelations
        .Where(kvp => kvp.Value == category)
        .Select(kvp => kvp.Key);

     public IEnumerable<ResourceT> GetAllRelated(ResourceT category) {
        var result = new HashSet<ResourceT>();

        //Upwards
        ResourceT? current = category;
        while (current != null) {
            result.Add(current);
            current = GetParent(current);
        }

        //Downwards
        var stack = new Stack<ResourceT>(GetChildren(category));
        while (stack.Count > 0) {
            var node = stack.Pop();
            if (result.Add(node))
                foreach (var child in GetChildren(node))
                    stack.Push(child);
        }

        return result;
    }

    // Used by: LookupCategoryPropertyType (reserve where-clause only)
    public IEnumerable<ResourceT> GetSubtree(ResourceT category) {
        var result = new HashSet<ResourceT>();
        var stack = new Stack<ResourceT>();
        stack.Push(category);
        while (stack.Count > 0) {
            var node = stack.Pop();
            if (result.Add(node))
                foreach (var child in GetChildren(node))
                    stack.Push(child);
        }
        return result;
    }

}