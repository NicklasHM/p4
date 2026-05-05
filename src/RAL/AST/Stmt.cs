namespace RAL.AST;

/* Primary constructor syntax with positional parameters define constructor and properties in a single line.
*  Properties are init-only (immutable after construction).*/

/// <summary> Base record class representing Stmt nodes. Establishes a reference type. </summary>
public abstract record class Stmt(int LineNumber);

/* Derived records declare positional parameters for all parameters in base record primary constructor. 
   Base record declares and initializes those properties. 
   The derived record doesn't hide them, only creates & initializes properties for parameters not declared in its base record.
*/
record class Composite(int LineNumber, Stmt? Stmt1, Stmt? Stmt2 ) : Stmt(LineNumber);

record class VarDecl(int LineNumber, TypeT Type, string Identifier) : Stmt(LineNumber);
//Null -> no properties. Non-null -> at least one property. List<Stmt> -> each element may be Vardecl | Comp(Vardecl, Assignment)
record class ResourceDecl(int LineNumber, TypeT Type, string Identifier, List<Stmt>? PropertyList) : Stmt(LineNumber);

record class CategoryDecl (int LineNumber, string CategoryId, string? ParentId) : Stmt(LineNumber);

record class TemplateDecl(int LineNumber, string TemplateId, List<VarDecl>? ParamList, Stmt? TemplateBody) : Stmt(LineNumber);

record class Move(int LineNumber, string ResourceId, string CategoryId ) : Stmt(LineNumber);

/*                                 might be id, might be a reserve expression */
record class Cancel(int LineNumber, Exp Reservation) : Stmt(LineNumber);

record class If(int LineNumber, Exp Condition, Stmt? ThenBody, Stmt? ElseBody) : Stmt(LineNumber);

record class ExpStmt(int LineNumber, Exp Expression) : Stmt(LineNumber);

record class Availability(int LineNumber, QueryData Query) : Stmt(LineNumber);

record class TemplateCall(int LineNumber, string TemplateId, List<Exp>? ArgList) : Stmt(LineNumber);

record class Skip(int LineNumber) : Stmt(LineNumber);
