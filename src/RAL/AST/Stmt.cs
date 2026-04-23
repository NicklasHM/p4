namespace RAL;
abstract record class Stmt
{
     int LineNumber { get; }
}

record class VarDecl(Type Type, string Identifier, Exp? Expression) : Stmt;

record class CategoryDecl (string CategoryId, string? ParentId) : Stmt;

record class TempDecl(string TemplateId, string paramList, List<Stmt> tempBody) : Stmt;

record class Param(Type Type, string Identifier) : Stmt;

record class MoveTo(string ResourceId, string CategoryId ) : Stmt;

record class Cancel(string ReservationId) : Stmt;

record class If(Exp Condition, List<Stmt> ThenBody, List<Stmt> ElseBody) : Stmt;

record class ExpStmt(Exp Expression) : Stmt;