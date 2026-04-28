namespace RAL.Semantic.Environments;

using RAL.Semantic.Symbols;

/*
 Resource/record structure environment.

 EnvR stores the fields/properties that belong to a resource.
 It does not store runtime resource values.

 Conceptually:
 resourceName -> fieldName -> FieldSymbol

 Example:
 Resource Room -> {
     capacity -> Number
     projector -> Bool
 }
*/
public class EnvR
{
    private readonly Dictionary<string, Dictionary<string, FieldSymbol>> resources = new();

    
    // Declares a new resource structure.
    // This must happen before fields can be added to it.
    public void BindResource(string name)
    {
        if (resources.ContainsKey(name))
            throw new Exception($"Resource '{name}' is already defined.");

        resources[name] = new Dictionary<string, FieldSymbol>();
    }

    
    // Adds a field/property to an existing resource.
    // Redeclaring the same field in the same resource is not allowed.
    public void BindField(string resourceName, FieldSymbol field)
    {
        if (!resources.ContainsKey(resourceName))
            throw new Exception($"Unknown resource/category '{resourceName}'.");

        if (resources[resourceName].ContainsKey(field.Name))
            throw new Exception(
                $"Field '{field.Name}' is already defined in resource '{resourceName}'."
            );

        resources[resourceName][field.Name] = field;
    }

    
    // Looks up a field/property on a resource.
    // Throws an error if either the resource or the field is unknown.
    public FieldSymbol LookupField(string resourceName, string fieldName)
    {
        if (!resources.ContainsKey(resourceName))
            throw new Exception($"Unknown resource '{resourceName}'.");

        if (resources[resourceName].TryGetValue(fieldName, out var field))
            return field;

        throw new Exception(
            $"Unknown field '{fieldName}' in resource '{resourceName}'."
        );
    }

    
    // Checks whether a resource has been declared.
    public bool IsResourceDefined(string name)
    {
        return resources.ContainsKey(name);
    }

    
    // Checks whether a field/property exists on a resource.
    public bool IsFieldDefined(string resourceName, string fieldName)
    {
        return resources.ContainsKey(resourceName)
            && resources[resourceName].ContainsKey(fieldName);
    }
}