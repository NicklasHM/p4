namespace RAL.TC;

public class EnvR {
    public readonly Dictionary<string, Dictionary<string, Type>> R = new();

    public void BindField(string resource, string field, Type type) {
        if (!R.TryGetValue(resource, out var fields))
            throw new Exception($"Unknown resource/category '{resource}'.");

        if (fields.ContainsKey(field))
            throw new Exception($"Field '{field}' is already defined in resource '{resource}'.");

        fields[field] = type;
    }

    public Type LookupField(string resource, string fieldName) {
        if (!R.TryGetValue(resource, out var fields)) throw new Exception($"Unknown resource '{resource}'.");
        if (fields.TryGetValue(fieldName, out Type field)) return field;
        throw new Exception($"Unknown field '{fieldName}' in resource '{resource}'.");
    }

}