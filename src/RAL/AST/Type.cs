namespace RAL.AST;

public interface TypeT { public string ToString(); }

public sealed record BoolT : TypeT { public override string ToString() { return "bool"; }}

public sealed record NumberT : TypeT { public override string ToString() { return "number"; }}

public sealed record StringT : TypeT { public override string ToString() { return "string"; }}

public sealed record ResourceT(string Category) : TypeT { public override string ToString() { return this.Category; }} // holds the specific category

public sealed record ReservationT : TypeT { public override string ToString() { return "reservation"; }}

public sealed record DurationT : TypeT { public override string ToString() { return "duration"; }}

public sealed record DateTimeT : TypeT { public override string ToString() { return "Datetime"; }}

public sealed record CategoryT : TypeT { public override string ToString() { return "category"; }} // delete?
