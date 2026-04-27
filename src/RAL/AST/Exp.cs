namespace RAL.AST;

abstract record class Exp(int LineNumber);

record class BinaryOperation(int LineNumber, Exp LeftExpression, BinaryOperator Operator, Exp? RightExpression ) : Exp(LineNumber);

record class UnaryOperation(int LineNumber, UnaryOperator? Operator, Exp Expression ): Exp(LineNumber);


record class BoolV(int LineNumber, bool Value) : Exp(LineNumber);

record class StringV(int LineNumber, string Value) : Exp(LineNumber);
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