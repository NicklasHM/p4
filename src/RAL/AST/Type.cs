namespace RAL.AST;

public interface Type {}

public sealed record BoolT : Type;

public sealed record NumberT : Type;

public sealed record StringT : Type;

public sealed record ResourceT(string Category) : Type; // holds the specific category

public sealed record ReservationT : Type;

public sealed record DurationT : Type;

public sealed record DateTimeT : Type;

public sealed record CategoryT : Type;
