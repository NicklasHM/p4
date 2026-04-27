namespace RAL.AST;

/// <summary> Base record class representing Stmt nodes. Establishes a reference type.</summary>
/// <param name="LineNumber"></param>
/* Primary constructor syntax with positional parameters define constructor and properties in a single line.
*  Properties are init-only (immutable after construction).
*/
abstract record class Stmt(int LineNumber);

/* Derived records declare positional parameters for all parameters in base record primary constructor. 
   Base record declares and initializes those properties. 
   The derived record doesn't hide them, but only creates and initializes properties for parameters that aren't declared in its base record.
*/
record class Composite(int LineNumber, Stmt? Stmt1, Stmt? Stmt2 ) : Stmt(LineNumber);

record class VarDecl(int LineNumber, Type Type, string Identifier) : Stmt(LineNumber);

record class CategoryDecl (int LineNumber, string CategoryId, string? ParentId) : Stmt(LineNumber);

record class TemplateDecl(int LineNumber, string TemplateId, string paramList, Stmt? TemplateBody) : Stmt(LineNumber);

record class Param(int LineNumber, Type Type, string Identifier) : Stmt(LineNumber);

record class Move(int LineNumber, string ResourceId, string CategoryId ) : Stmt(LineNumber);

record class Cancel(int LineNumber, string ReservationId) : Stmt(LineNumber);

record class If(int LineNumber, Exp Condition, Stmt? ThenBody, Stmt? ElseBody) : Stmt(LineNumber);

record class ExpStmt(int LineNumber, Exp Expression) : Stmt(LineNumber);
