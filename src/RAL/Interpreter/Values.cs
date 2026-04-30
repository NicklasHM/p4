namespace RAL.Interpreter;

/*
Runtime values used by the interpreter.

These values are different from AST nodes:
- AST nodes describe the source program.
- Runtime values are the result of evaluating expressions.

For now, Number is represented as float because the language uses one Number type
for both integer-like and decimal values.
*/

public interface Value {}

public record NumberVal(float Value) : Value
{
    // "G" keeps the output compact, e.g. 2 instead of 2.000000
    public override string ToString() => Value.ToString("G");
}

public record BoolVal(bool Value) : Value
{
    // Match common DSL syntax: true / false instead of True / False
    public override string ToString() => Value.ToString().ToLower();
}

public record StringVal(string Value) : Value
{
    public override string ToString() => Value;
}