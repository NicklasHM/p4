using RAL.AST;

namespace RAL;

public class PrettyPrinter
{
    public static string PrintStmt(Stmt? stmt, int depth = 0)
    {
        return stmt switch
        {
            null or Skip => "",
            Composite comp => PrintStmt(comp.Stmt1, depth) + "\n" + PrintStmt(comp.Stmt2, depth),
            VarDecl varDecl => Indent(depth) + PrintType(varDecl.Type) + " " + varDecl.Identifier + ";",
            ResourceDecl resDecl => PrintResourceDecl(resDecl, depth),
            CategoryDecl catDecl => PrintCategoryDecl(catDecl, depth),
            TemplateDecl tmplDecl => PrintTemplateDecl(tmplDecl, depth),
            Move move => Indent(depth) + $"move {move.ResourceId} to {move.CategoryId};",
            Cancel cancel => Indent(depth) + $"cancel {PrintExpr(cancel.Reservation)};",
            If ifStmt => PrintIfStmt(ifStmt, depth),
            ExpStmt expStmt => Indent(depth) + PrintExpr(expStmt.Expression) + ";",
            Availability avail => Indent(depth) + $"check {PrintQueryData(avail.Query)};",
            TemplateCall tmplCall => PrintTemplateCall(tmplCall, depth),
            _ => ""
        };
    }

    public static string PrintExpr(Exp? expr)
    {
        return expr switch
        {
            null => "",
            BoolV boolV => boolV.Value.ToString().ToLower(),
            StringV strV => $"\"{strV.Value}\"",
            NumberV numV => numV.Value.ToString(),
            DateTimeV dtV => dtV.Value,
            DurationV durV => durV.Value,
            Reference rf => rf.PropertyId == null 
                ? rf.VariableId 
                : $"{rf.VariableId}.{rf.PropertyId}",
            Assignment assign => assign.PropertyId == null
                ? $"{assign.VariableId} := {PrintExpr(assign.Value)}"
                : $"{assign.VariableId}.{assign.PropertyId} := {PrintExpr(assign.Value)}",
            UnaryOperation unOp => UnaryOpString(unOp.Operator) + Surround(unOp.Expression),
            BinaryOperation binOp => Surround(binOp.LeftExpression) + BinaryOpString(binOp.Operator) + Surround(binOp.RightExpression),
            Reserve reserve => $"reserve {PrintQueryData(reserve.Query)}",
            Reschedule resched => $"reschedule {PrintExpr(resched.Reservation)} {PrintTimeSpec(resched.NewTimeInterval)}",
            _ => ""
        };
    }

    public static string PrintType(TypeT? type)
    {
        return type switch
        {
            null => "",
            BoolT => "bool",
            NumberT => "number",
            StringT => "string",
            CategoryT => "category",
            ReservationT => "reservation",
            DateTimeT => "datetime",
            DurationT => "duration",
            ResourceT resT => string.IsNullOrEmpty(resT.Category) ? "resource" : resT.Category,
            _ => ""
        };
    }

    private static string PrintResourceDecl(ResourceDecl resDecl, int depth)
    {
        var result = Indent(depth) + $"{PrintType(resDecl.Type)} {resDecl.Identifier} {{\n";
        
        if (resDecl.PropertyList != null && resDecl.PropertyList.Count > 0)
        {
            foreach (var prop in resDecl.PropertyList)
            {
                result += PrintStmt(prop, depth + 1);
                if (prop != resDecl.PropertyList[^1])
                    result += "\n";
            }
        }
        
        result += "\n" + Indent(depth) + "}";
        return result;
    }

    private static string PrintCategoryDecl(CategoryDecl catDecl, int depth)
    {
        var result = Indent(depth) + $"category {catDecl.CategoryId}";
        if (catDecl.ParentId != null)
            result += $" is a {catDecl.ParentId}";
        result += ";";
        return result;
    }

    private static string PrintTemplateDecl(TemplateDecl tmplDecl, int depth)
    {
        var paramStr = "";
        if (tmplDecl.ParamList != null && tmplDecl.ParamList.Count > 0)
        {
            var paramStrs = tmplDecl.ParamList.Select(p => $"{PrintType(p.Type)} {p.Identifier}").ToList();
            paramStr = string.Join(", ", paramStrs);
        }

        var result = Indent(depth) + $"template {tmplDecl.TemplateId}({paramStr}) {{\n";
        result += PrintStmt(tmplDecl.TemplateBody, depth + 1);
        result += "\n" + Indent(depth) + "}";
        return result;
    }

    private static string PrintIfStmt(If ifStmt, int depth)
    {
        var result = Indent(depth) + $"if ({PrintExpr(ifStmt.Condition)}) then\n";
        result += PrintStmt(ifStmt.ThenBody, depth + 1);
        
        if (ifStmt.ElseBody is not Skip)
        {
            result += "\n" + Indent(depth) + "else\n";
            result += PrintStmt(ifStmt.ElseBody, depth + 1);
        }
        
        result += "\n" + Indent(depth) + "endif";
        return result;
    }

    private static string PrintTemplateCall(TemplateCall tmplCall, int depth)
    {
        var argStr = "";
        if (tmplCall.ArgList != null && tmplCall.ArgList.Count > 0)
        {
            var argStrs = tmplCall.ArgList.Select(PrintExpr).ToList();
            argStr = string.Join(", ", argStrs);
        }

        return Indent(depth) + $"use {tmplCall.TemplateId}({argStr});";
    }

    private static string PrintQueryData(QueryData query)
    {
        var resources = string.Join(" and ", query.ResourceSpecs.Select(PrintResourceSpec));
        var result = resources + " " + PrintTimeSpec(query.Interval);
        
        if (query.Condition != null)
            result += $" where {PrintExpr(query.Condition)}";
        
        if (query.Recurrence != null)
            result += " " + PrintRecurrenceSpec(query.Recurrence);
        
        return result;
    }

    private static string PrintResourceSpec(ResourceSpec spec)
    {
        if (spec.Quantity == null && spec.CategoryId == null)
            return spec.Identifier ?? "";
        
        var result = "";
        if (spec.Quantity != null)
            result += PrintExpr(spec.Quantity) + " * ";
        
        result += spec.CategoryId ?? "";
        
        if (spec.Identifier != null)
            result += " " + spec.Identifier;
        
        return result;
    }

    private static string PrintTimeSpec(TimeSpec timeSpec)
    {
        var result = $"from {PrintExpr(timeSpec.Start)}";
        result += timeSpec.EndMarker switch
        {
            DurationV => $" for {PrintExpr(timeSpec.EndMarker)}",
            _ => $" to {PrintExpr(timeSpec.EndMarker)}"
        };
        return result;
    }

    private static string PrintRecurrenceSpec(RecurrenceSpec recurrence)
    {
        var mode = recurrence.Mode == RecurrenceMode.STRICT ? "strict" : "flexible";
        var result = $"recurring {mode} every {PrintExpr(recurrence.EveryDuration)}";
        
        result += recurrence.EndMarker switch
        {
            DurationV => $" for {PrintExpr(recurrence.EndMarker)}",
            _ => $" until {PrintExpr(recurrence.EndMarker)}"
        };
        
        return result;
    }

    private static string Indent(int depth) => "    ".PadRight(depth * 4);

    private static string BinaryOpString(BinaryOperator op) => op switch
    {
        BinaryOperator.OR => " or ",
        BinaryOperator.AND => " and ",
        BinaryOperator.SEQ => " seq ",
        BinaryOperator.EQ => " == ",
        BinaryOperator.NEQ => " != ",
        BinaryOperator.LT => " < ",
        BinaryOperator.GT => " > ",
        BinaryOperator.LTEQ => " <= ",
        BinaryOperator.GTEQ => " >= ",
        BinaryOperator.ADD => " + ",
        BinaryOperator.SUB => " - ",
        BinaryOperator.MUL => " * ",
        BinaryOperator.DIV => " / ",
        _ => " "
    };

    private static string UnaryOpString(UnaryOperator op) => op switch
    {
        UnaryOperator.NOT => "not ",
        UnaryOperator.NEG => "-",
        _ => ""
    };

    private static string Surround(Exp? expr) => expr switch
    {
        BinaryOperation binOp => $"({PrintExpr(binOp)})",
        _ => PrintExpr(expr)
    };
}
