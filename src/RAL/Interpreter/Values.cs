namespace RAL.Interpreter;
using System.Globalization;

/* Runtime values used by the interpreter.

 * Wraps c# typed values in record classes to have one common super "Value" as return type for EvalExp.

 *ToString method of each record type overridden to the underlying value's ToString method
*/

/* Context:
 These values are different from AST nodes:
 - AST nodes describe the source program.
 - Runtime values are the result of evaluating expressions.
*/


public abstract record Value {}

/* For now, Number is represented as float because the language 
   uses one Number type for both integer-like and decimal values. */
record NumberVal(float Value) : Value
{
    // "G" keeps the output compact, e.g. 2 instead of 2.000000
    public override string ToString() => Value.ToString("G");
}

record BoolVal(bool Value) : Value
{
    // Match common DSL syntax: true / false instead of True / False
    public override string ToString() => Value.ToString().ToLower();
}

record StringVal(string Value) : Value
{
    public override string ToString() => Value;
}

/*________________ Time ______________*/
record DateTimeVal(DateTime Value) : Value
{
    // Returns the DateTime formatted as "dd/MM-yyyy HH:mm" using French (fr-FR) culture.
    public override string ToString() => Value.ToString("dd/MM-yyyy HH:mm", new CultureInfo("fr-FR"));
}

record DurationVal(TimeSpan Value) : Value
{
    public override string ToString() => Value.ToString();
}