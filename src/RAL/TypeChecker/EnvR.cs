namespace RAL.TC;
using RAL.AST;

/// <summary> Resource field Environment. (ResourceName, (FieldName, Type)) /// </summary>
public class EnvR {
    private readonly Dictionary<string, Dictionary<string, TypeT>> R = new();

    public void BindField(string resource, string field, TypeT type) {
        if (!R.TryGetValue(resource, out var fields))
            throw new Exception($"Unknown resource/category '{resource}'.");

        if (fields.ContainsKey(field))
            throw new Exception($"Field '{field}' is already defined in resource '{resource}'.");

        fields[field] = type;
    }

    public TypeT LookupField(string resource, string fieldName) {
        if (!R.TryGetValue(resource, out var fields)) 
            throw new Exception($"Unknown resource '{resource}'.");

        if (fields.TryGetValue(fieldName, out TypeT fieldType)) 
            return fieldType;

        throw new Exception($"Unknown field '{fieldName}' in resource '{resource}'.");
    }
}