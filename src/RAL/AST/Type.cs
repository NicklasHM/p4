namespace RAL.AST;

///<summary> Singletons not needed, as record types have value equality: Two objects are equal if they're of the same type & storing same values. </summary>
public abstract record TypeT { }

public sealed record BoolT : TypeT { public override string ToString() { return "Bool"; }}

public sealed record NumberT : TypeT { public override string ToString() { return "Number"; }}

public sealed record StringT : TypeT { public override string ToString() { return "String"; }}

/// <summary> Conveys user-defined resource types and pre-defined root "Resource" type, which has Category = "Resource". </summary>
public sealed record ResourceT(string Category) : TypeT { public override string ToString() { return this.Category; }} // holds the specific category

public sealed record ReservationT : TypeT { public override string ToString() { return "Reservation"; }}

public sealed record DurationT : TypeT { public override string ToString() { return "Duration"; }}

public sealed record DateTimeT : TypeT { public override string ToString() { return "Datetime"; }}

public sealed record ErrorT(string Msg) : TypeT { public override string ToString() { return this.Msg; }};

public sealed record CategoryT : TypeT { public override string ToString() { return "Category"; }} // delete?
