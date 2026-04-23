namespace RAL.AST
{
    public abstract class AstNode
    {
        public int Line { get; set; }
        public int Col  { get; set; }
    }
}