namespace RAL.AST;

public interface Type {}

public sealed class BoolT : Type {
    public static readonly BoolT Instance = new();
    private BoolT() {}
}

public sealed class NumberT : Type {
    public static readonly NumberT Instance = new();
    private NumberT() {}
}

public sealed class StringT : Type {
    public static readonly StringT Instance = new();
    private StringT() {}
}

public sealed class ResourceT : Type {
    public static readonly ResourceT Instance = new();
    private ResourceT() {}
}

public sealed class ReservationT : Type {
    public static readonly ReservationT Instance = new();
    private ReservationT() {}
}

public sealed class DurationT : Type {
    public static readonly DurationT Instance = new();
    private DurationT() {}
}

public sealed class DateTimeT : Type {
    public static readonly DateTimeT Instance = new();
    private DateTimeT() {}
}

public sealed class CategoryT : Type {
    public static readonly CategoryT Instance = new();
    private CategoryT() {}
}
