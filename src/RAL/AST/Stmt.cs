namespace RAL;
abstract class Stmt
{
     int lineNumber { get; }
}

record class VarDecl(string Type, string Identifier, Exp?  ) : Stmt;

record class MoveTo(string ResourceId, string CatId ) : Stmt;
record class CategoryDecl (string CatId, string? ParentId) : Stmt;
record class Param(string TypeName, string Identifier) : Stmt;
record class TempDecl(string TempName, string paramList, List<Stmt> tempBody) : Stmt;
record class Cancel(string CancelIdentifier) : Stmt;
record class If(Exp Condition, List<Stmt> ThenBody, List<Stmt> ElseBody) : Stmt;
record class ExpStmt(Exp Expression) : Stmt;