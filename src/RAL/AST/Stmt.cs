namespace RAL.AST;

/// <summary> Base record class representing Stmt nodes. Establishes a reference type.</summary>
/// <param name="LineNumber"></param>
/* Primary constructor syntax with positional parameters define constructor and properties in a single line.
*  Properties are init-only (immutable after construction).
*/
abstract record class Stmt(int LineNumber );

/* Derived records declare positional parameters for all parameters in base record primary constructor. 
   Base record declares and initializes those properties. 
   The derived record doesn't hide them, but only creates and initializes properties for parameters that aren't declared in its base record.
*/
record class VarDecl(int LineNumber, Type Type, string Identifier, Exp? Expression) : Stmt;

record class CategoryDecl (int LineNumber, string CategoryId, string? ParentId) : Stmt;

record class TempDecl(int LineNumber, string TemplateId, string paramList, List<Stmt> tempBody) : Stmt;

record class Param(int LineNumber, Type Type, string Identifier) : Stmt;

record class MoveTo(int LineNumber, string ResourceId, string CategoryId ) : Stmt;

record class Cancel(int LineNumber, string ReservationId) : Stmt;

record class If(int LineNumber, Exp Condition, List<Stmt> ThenBody, List<Stmt> ElseBody) : Stmt;

record class ExpStmt(int LineNumber, Exp Expression) : Stmt;