namespace RAL.AST;

abstract record class Exp
{
    int lineNumber { get; }
}

record class BinaryOp(Exp LeftExpression, BinaryOperator Operator, Exp? RightExpression ) : Exp;

record class UnaryOp(UnaryOperator? Operator, Exp Expression ): Exp;

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