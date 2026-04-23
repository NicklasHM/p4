namespace RAL.AST
{
    public abstract class Statement : AstNode { }

    public class VarDeclStmt : Statement
    {
        public string            TypeName   { get; set; }
        public string            Name       { get; set; }
        public Expr              Init       { get; set; }  // := Exp
        public List<VarDeclStmt> Block      { get; set; }  // { VarDecl; }
    }

    public class CategoryDeclStmt : Statement
    {
        public string Name       { get; set; }
        public string ParentName { get; set; }  // null if no "is a"
    }

    public class TemplateDeclStmt : Statement
    {
        public string          Name   { get; set; }
        public List<Param>     Params { get; set; }
        public List<Statement> Body   { get; set; }
    }

    public class MoveStmt : Statement
    {
        public string Resource { get; set; }
        public string Category { get; set; }
    }

    public class CancelStmt : Statement
    {
        public string ReservationName { get; set; }
    }

    public class IfStmt : Statement
    {
        public Expr            Condition  { get; set; }
        public List<Statement> ThenBranch { get; set; }
        public List<Statement> ElseBranch { get; set; }  // null if no else
    }

    public class ExprStmt : Statement
    {
        public Expr Expression { get; set; }
    }

    public class Param : AstNode
    {
        public string TypeName { get; set; }
        public string Name     { get; set; }
    }
}