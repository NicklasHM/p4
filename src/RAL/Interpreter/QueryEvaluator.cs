using RAL.AST;
namespace RAL.Interpreter;

internal static class QueryEvaluator {
    /// <summary> Evaluates an atomic query an returns a list of all valid combinations of resources.  
    /// Atomic, since recurrence has been pulled out. 
    /// (Availability, Reservation) </summary>
    internal static IEnumerable<List<ResourceVal>> EvaluateQuery(ResolvedQuery query, EnvV envV, EnvH envH) {

        // Step 1: List of all candidate combinations for all resource specification. 
        List<ResolvedSpec>? groupedCandidateCombinations = ResolveAllResourceSpecs(query, envV, envH);
        
        if (groupedCandidateCombinations == null)
            return [];

        // Step 2: Cartesian product of all resource specs' combinations, unwraps resolvedspec
        var cartesianProduct = CartesianProduct(groupedCandidateCombinations.Select(resolvedSpec => resolvedSpec.CandidateCombinations));

        // Step 3: Ensure resources across specs are distinct
        var validAssignments = cartesianProduct.Where(assignment => {
            var flat = assignment.SelectMany(r => r).ToList();
            return flat.Distinct().Count() == flat.Count;
        });

        // Step 4: Evaluate Condition if present
        if (query.Condition != null) {
            validAssignments = validAssignments.Where(assignment => {
                var bindingSets = new List<KeyValuePair<string, List<ResourceVal>>>();
                for (int i = 0; i < assignment.Count; i++) {
                    if (groupedCandidateCombinations[i].LocalBinding != null) {
                    bindingSets.Add(new KeyValuePair<string, List<ResourceVal>>(
                        groupedCandidateCombinations[i].LocalBinding!,
                        assignment[i]));
                    }
                }

                if (!bindingSets.Any()) {
                    // No bound variables; evaluate once
                    try {
                        return Interpreter.EvalExp(query.Condition, envV, envH).AsBool();
                    } catch (Interpreter.MissingPropertyException) {
                        return false;
                    }
                }

                // Generate Cartesian product of individual resources for the bound variables
                var elementSets = bindingSets.Select(kvp => kvp.Value.Select(r => new KeyValuePair<string, Value>(kvp.Key, r)));
                var bindingCombinations = CartesianProduct(elementSets);

                // The condition must be true for EVERY element combination
                foreach (var bindCombo in bindingCombinations) {
                    EnvV scope = envV.NewScope();
                    foreach (var binding in bindCombo) {
                        scope.Bind(binding.Key, binding.Value);
                    }
                    try {
                        if (!Interpreter.EvalExp(query.Condition, scope, envH).AsBool()) {
                            return false;
                        }
                    } catch (Interpreter.MissingPropertyException) { 
                        return false;
                    }
                }
                return true;
            });
        }

        // Return flattened list of resources for each valid assignment
        return validAssignments.Select(assignment => assignment.SelectMany(r => r).ToList());
    }

    //i.e. resolve re. (re and re...)
    private static List<ResolvedSpec>? ResolveAllResourceSpecs(ResolvedQuery query, EnvV envV, EnvH envH) {

        List<ResolvedSpec> groupedCandidateCombinations = new();

         // Step 1: Find all valid resource combinations for each ResourceSpec (r | a rc[id])
        foreach (ResourceSpec spec in query.ResourceSpecs) {
            
            // for one ResourceSpec (r | a rc[id])
            ResolvedSpec? candidateCombinations = spec switch {
                
                //Case 1 - named resourceId: i.e. room204
                ResourceInstanceSpec resourceInstance => ResolveInstanceSpec(resourceInstance, query.Start, query.End, envV),
                
                //Case 2 & 3: [a rc], [a rc id] respectively
                CategorySpec categorySpec => ResolveCategorySpec(categorySpec, query.Start, query.End, envV, envH),

                _ => throw new Exception($"Invalid resource specification.")
            };

            //If any one spec fails (not available at requested time) -> fail the entire request immediately. re and re
            if (candidateCombinations == null)
                return null;

            groupedCandidateCombinations.Add(candidateCombinations);

        }
        //If this is reached, each spec is satisfied based solely on the given timeslot (condition yet to be evaluated)
        return groupedCandidateCombinations;
    }
    
    private static ResolvedSpec? ResolveInstanceSpec(ResourceInstanceSpec spec, DateTime requestedStart, DateTime requestedEnd, EnvV envV) {
        
        //lookup in envV - ResourceVal guaranteed by typechecker
        ResourceVal resource = (ResourceVal)envV.Lookup(spec.ResourceId);

        // Required instance not available at given time. Whole query fails fast.
        if (! ReservationRegistry.Instance().IsAvailable(resource, requestedStart, requestedEnd) )
            return null;

        //Available
        return new ResolvedSpec(
            //a named resource has only one possible combination (itself). wraped in double lists for equal treatment.
            CandidateCombinations:
            [
                [resource]
            ],
            LocalBinding: null //impossible by concrete syntax
        );
    }

    private static ResolvedSpec? ResolveCategorySpec(CategorySpec catSpec, DateTime requestedStart, DateTime requestedEnd, EnvV envV, EnvH envH) {
        //extract a
        int quantity = (int)((NumberVal)Interpreter.EvalExp(catSpec.Quantity, envV, envH)).Value;

        IEnumerable<string> subCategories = envH.GetSubCategories(catSpec.CategoryId);
        List<ResourceVal> availableResources =
            ResourceRegistry.Instance()
                .GetAllResourcesInCategorySubtree(subCategories)
                .Where(resource => ReservationRegistry.Instance().IsAvailable(resource, requestedStart, requestedEnd))
                .ToList();

        var combinations = GetCombinations(availableResources, quantity).ToList();

        if (combinations.Count == 0)
            return null;

        return new ResolvedSpec(
            CandidateCombinations: combinations,
            //as: like a typecast, but returns null if not possible rather than throwing exception. Null-safe operator
            LocalBinding: (catSpec as CategorySpecWithBinding)?.LocalBindingId);
    }


    /// <summary> Generates all combinations of size 'quantity' from a list of resources. </summary>
    private static IEnumerable<List<ResourceVal>> GetCombinations(List<ResourceVal> resources, int quantity) {
        if (quantity == 0)                yield return new List<ResourceVal>();
        else if (resources.Count == quantity) yield return new List<ResourceVal>(resources);
        else if (resources.Count > quantity) {
            ResourceVal head = resources[0];
            List<ResourceVal> tail = resources.GetRange(1, resources.Count - 1);

            // Include head
            foreach (List<ResourceVal> combination in GetCombinations(tail, quantity - 1)) {
                List<ResourceVal> newCombination = new List<ResourceVal>(combination);
                newCombination.Insert(0, head);
                yield return newCombination;
            }

            // Exclude head
            foreach (List<ResourceVal> combination in GetCombinations(tail, quantity)) {
                yield return combination;
            }
        }
    }

    /// <summary> Generates the Cartesian product of a sequence of sequences. </summary>
    private static IEnumerable<List<T>> CartesianProduct<T>(IEnumerable<IEnumerable<T>> sequences) {
        IEnumerable<List<T>> result = new List<List<T>> { new List<T>() };

        foreach (IEnumerable<T> sequence in sequences) {
            result =
                from partial in result
                from item in sequence
                select partial.Append(item).ToList();
        }

        return result;
    }

    /// <summary> List of all candidate combinations(a list itself) for ONE resource spec: (r | a rc [id]) /// </summary>
    private record ResolvedSpec (
        List<List<ResourceVal>> CandidateCombinations,
        string? LocalBinding
    );

}