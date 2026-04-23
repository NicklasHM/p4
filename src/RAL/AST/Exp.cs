namespace RAL;
abstract class Exp
{
    int lineNumber { get; }
}

class BinaryOp : Exp
{
    
}

enum BinaryOperators 
{
    OR, AND, SEQ,
    EQ, NEQ,
    LT, GT, LTEQ, GTEQ, 
    ADD, SUB, 
    MUL, DIV
}

enum UnaryOperators 
{
    NOT//, NEG
}