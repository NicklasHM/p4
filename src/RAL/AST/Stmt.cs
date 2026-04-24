namespace RAL.AST;
/*
Record class declaration: https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/records#declare-a-record
Primary constructors: https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/tutorials/primary-constructors 
*/

abstract record class Stmt
{
     int LineNumber { get; }
}


/* Positional parameters define both constructor and properties in a single line.
* Establishes a reference type
* Properties are init-only (immutable after construction).
*/
record class VarDecl(Type Type, string Identifier, Exp? Expression) : Stmt;

record class CategoryDecl (string CategoryId, string? ParentId) : Stmt;

record class TempDecl(string TemplateId, string paramList, List<Stmt> tempBody) : Stmt;

record class Param(Type Type, string Identifier) : Stmt;

record class MoveTo(string ResourceId, string CategoryId ) : Stmt;

record class Cancel(string ReservationId) : Stmt;

record class If(Exp Condition, List<Stmt> ThenBody, List<Stmt> ElseBody) : Stmt;

record class ExpStmt(Exp Expression) : Stmt;