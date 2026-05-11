using RAL.AST;

namespace RAL.Interpreter;

//Represents data for a single reservation attempt for a specific, calculated timeframe.
//Purpose: Decouple runtime execution from AST, particularly due to recurrence. */
internal record ResolvedQuery(
    IReadOnlyList<ResourceSpec> ResourceSpecs, //specs come from the AST and must never be mutated at runtime 
    DateTime Start,
    DateTime End,
    Exp? Condition
);