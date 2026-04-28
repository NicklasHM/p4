namespace RAL.AST;

abstract record class Exp(int LineNumber);

record class BinaryOperation(int LineNumber, Exp LeftExpression, BinaryOperator Operator, Exp RightExpression ) : Exp(LineNumber);

record class UnaryOperation(int LineNumber, UnaryOperator? Operator, Exp Expression ): Exp(LineNumber);

//See the dictionary key type in /Interpreter/EnvV.cs for the choice of string type Identifier.
record class Assignment(int LineNumber, string VariableId, string? PropertyId, Exp Value) : Exp(LineNumber);

record class BoolV(int LineNumber, bool Value) : Exp(LineNumber);

record class StringV(int LineNumber, string Value) : Exp(LineNumber);
record class Reference(int LineNumber, string VariableId, string? PropertyId) : Exp(LineNumber);

record class DateTimeV(int LineNumber, DateTime Value) : Exp(LineNumber);
record class DurationV(int LineNumber, TimeSpan Value) : Exp(LineNumber);

enum BinaryOperator
{
    OR, AND, SEQ,
    EQ, NEQ,
    LT, GT, LTEQ, GTEQ, 
    ADD, SUB, 
    MUL, DIV
}

enum UnaryOperator
{
    NOT//, NEG
}

/* Work in progress */
record class AvailabilityExp(int LineNumber ) : Exp(LineNumber);

record class QueryExp(int LineNumber, ResourceExp Resource, TimeExp Time, Exp? ExpCondition) : Exp(LineNumber);

record class TimeExp(int LineNumber, DateTime StartDate, DateTime? ToDate, Duration? ForTime) : Exp(LineNumber);
record class ResourceExp(int LineNumber, Exp? AdditiveExp, string? MoreOf, string Identifier, ResourceExp? AndResource) : Exp(LineNumber);