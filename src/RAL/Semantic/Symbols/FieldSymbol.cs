namespace RAL.Semantic.Symbols;

using RalType = RAL.AST.TypeT;

/*
 Field/property information for a resource.

 A FieldSymbol is stored inside EnvR.
 It contains the field name and the declared type of that field.
*/
public class FieldSymbol
{
    
    // The name of the field/property.
    public string Name { get; }

    /*
     The declared type of the field/property.

     RalType is an alias for RAL.AST.Type to avoid confusion
     with System.Type from C#.
    */
    public RalType Type { get; }

    public FieldSymbol(string name, RalType type)
    {
        Name = name;
        Type = type;
    }
}