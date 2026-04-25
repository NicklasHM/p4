namespace RAL.AST;

abstract record class Exp(int LineNumber);

record class BinaryOp(int LineNumber, Exp LeftExpression, BinaryOperator Operator, Exp? RightExpression ) : Exp(LineNumber);

record class UnaryOp(int LineNumber, UnaryOperator? Operator, Exp Expression ): Exp(LineNumber);

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