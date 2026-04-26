/*
new Env<T>() builds a scope.
NewScope() builds a child scope inside the current scope

Declaration ->  Bind   = create new name in current scope
Assignment  ->  Set    = update existing name in nearest scope
Variable    ->  Lookup = find name from local or outward
*/

public class Env<T>
{
    private readonly Env<T>? parent; // parent scope. Null means this is the global/root scope.
    private readonly Dictionary<string, T?> bindings = new(); // One dictionary(hashmap) per scope. Maps names to values.

    public Env(Env<T>? parent = null) // Creates an environment. If parent is null, this is the global scope.
    {
        this.parent = parent;
    }
    //Analogy: new Env<T>(above) builds the house, NewScope() (below) builds the room inside the house.
    public Env<T> NewScope() // Creates a child scope where this Env is the parent.
    {
        return new Env<T>(this);
    }

    public void Bind(string name, T? value) // bind a name to a value in the current scope
    {
        if (IsLocal(name)) // check if the name is already defined in the current scope
            throw new Exception($"'{name}' is already defined in this scope.");

        bindings[name] = value; // bind the name to the value in the current scope
    }

    public void Set(string name, T? value) // Updates an existing binding. Searches current scope, then parent scopes.
    {
        if (bindings.ContainsKey(name))
        {
            bindings[name] = value;
        }
        else if (parent != null) 
        {
            parent.Set(name, value); 
        }
        else // Throws error if the name does not exist anywhere.
        {
            throw new Exception($"Cannot assign to unknown name '{name}'.");
        }
    }

    public T? Lookup(string name) // lookup a value for a name in the current scope. local -> parent -> global -> error
    {
        if (bindings.ContainsKey(name))
            return bindings[name]; // return the value for the name in the current scope

        if (parent != null) // if the name is not defined in the current scope, check the parent scope
            return parent.Lookup(name); // return the value for the name in the parent scope

        throw new Exception($"Unknown name '{name}'."); // if the name is not defined in the current scope or the parent scope, throw an error
    }

    public bool IsLocal(string name)
    /*Checks if a name is defined in the current scope only.
    Used to prevent redeclaration in the same scope.*/
    {
        return bindings.ContainsKey(name); // return true if the name is defined in the current scope, false otherwise
    }
}