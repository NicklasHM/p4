namespace RAL.AST;

record class Reservation(int startTime, int endTime) {}

record class Resource(List<string> attributeKeys, List<Reservation> reservations) {}

abstract record class Exp(int LineNumber);

record class BinaryOperation(int LineNumber, Exp LeftExpression, BinaryOperator Operator, Exp? RightExpression ) : Exp(LineNumber);

record class UnaryOperation(int LineNumber, UnaryOperator? Operator, Exp Expression ): Exp(LineNumber);

record class BoolV(int LineNumber, bool Value) : Exp(LineNumber);

record class StringV(int LineNumber, string Value) : Exp(LineNumber);

record class NumberV(int LineNumber, float Value) : Exp(LineNumber);

record class DurationV(int LineNumber, int Value) : Exp(LineNumber);

record class DateTimeV(int LineNumber, int Value) : Exp(LineNumber);

record class ReservationV(int LineNumber, Reservation Value) : Exp(LineNumber);

record class ResourceV(int LineNumber, Resource Value) : Exp(LineNumber);

record class VariableV(int LineNumber, string Value) : Exp(LineNumber);

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