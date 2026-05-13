using RAL.AST;

namespace RAL.Interpreter;

public class EnvTem {

    private readonly Dictionary<string, tAttribute > tTable = new();

    public void Bind(string templateId, List<string> parameterNames, Stmt body) {

        tTable.Add(templateId, new tAttribute(parameterNames, body));        
    }

    //Lookup

    internal tAttribute Lookup(string templateId) {
        return tTable[templateId];        
    }

}

internal record class tAttribute(
        List<string> ParameterNames,
        Stmt Body
    );
