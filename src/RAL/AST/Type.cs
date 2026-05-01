namespace RAL.AST;

public interface TypeT {}

public sealed record BoolT : TypeT;

public sealed record NumberT : TypeT;

public sealed record StringT : TypeT;

public sealed record ResourceT(string Category) : TypeT; // holds the specific category

public sealed record ReservationT : TypeT;

public sealed record DurationT : TypeT;

public sealed record DateTimeT : TypeT;

public sealed record CategoryT : TypeT;
