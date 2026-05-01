https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/patterns#relational-patterns

The type checker is split into two parts:
ExpType
A function returning a type. It works recursively, descending the AST until it hits a literal or variable. This value (type) then bubbles up the call stack until it hits the currently evaluated node, where it is evaluated.
 



StmtType