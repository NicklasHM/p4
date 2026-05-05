namespace RAL.TC;
using RAL.AST;

/// <summary> Resource Environment
/// Must given a Resource name r, hold information about the variables bound within resource r. 
/// </summary> 
public class EnvR {

    ///Main data structure. R = ResourceId -> (FieldId -> Type)
    private readonly Dictionary<string, Dictionary<string, TypeT>> R = new();

    /// <summary> All resources must be registered. Whether fields or not  /// </summary>
    public void RegisterResource(string resourceId)
    {
        //If it is not registered already 
        if (!R.ContainsKey(resourceId)) {

            //Register resource with empty field environment
            R.Add(resourceId, new Dictionary<string, TypeT>() );
        }   
    }

    public bool HasResource(string resourceId) {
        return R.ContainsKey(resourceId);
    }
    public bool HasField(string resource, string field) {
        return R.TryGetValue(resource, out var fields) && fields.ContainsKey(field);
    }

    /// <summary> Associates a resource </summary> 
    public void BindField(string resourceId, string fieldId, TypeT type) {
  
        //Retrieve the relevant resource's field environment (nest)
        Dictionary<string, TypeT> fieldEnvironment = R[resourceId];

        //Guard against the resource containing duplicate field id's
        if (fieldEnvironment.ContainsKey(fieldId))
            throw new Exception($"Field '{fieldId}' is already defined in resource '{resourceId}'.");

        //Bind type to field
        fieldEnvironment.Add(fieldId, type);
    }

    public TypeT LookupField(string resourceId, string fieldName) {

        // Check if the resource has been declared. If it has, return it's inner dictionary mapping field -> type
        if (!R.TryGetValue(resourceId, out Dictionary<string, TypeT> fieldEnvironment)) 
            throw new Exception($"Unknown resource '{resourceId}'.");

        // Do a lookup in the inner dictionary, getting the type of the field
        if (fieldEnvironment.TryGetValue(fieldName, out TypeT fieldType)) 
            return fieldType;

        throw new Exception($"Unknown field '{fieldName}' in resource '{resourceId}'.");
    }
}