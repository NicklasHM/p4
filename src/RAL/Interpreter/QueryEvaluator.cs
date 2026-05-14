using RAL.AST;
namespace RAL.Interpreter;

using Candidate = List<ResourceVal>;
using CandidateSet =  List<List<ResourceVal>>;          //List<Candidate>;
using AllCandidateSets = List<List<List<ResourceVal>>>; //List<CandidateSet>;

/*

reserve:                 _room205    and    2 Room          and                ...      time    condition

spec:                    _room205           2 Room                             ...

candidate:              [_room205]         [r2, r4 ]                           ...

candidateSet:         [ [_room205] ]     [ [r2, r4], [r2, r3], [r3, r4] ]      ...

all_CandidateSets  [  [ [_room205] ]  ,  [ [r2, r4], [r2, r3], [r3, r4] ]  ,   ...  ]
*/

internal static class QueryEvaluator {
    /// <summary> Evaluates an atomic query an returns a list of all valid combinations of resources.  
    /// Atomic, since recurrence has been pulled out. 
    /// (Availability, Reservation) </summary>
    internal static IEnumerable<List<ResourceVal>> EvaluateQuery(ResolvedQuery query, EnvV envV, EnvH envH) {

        // Step 1: List of all candidates for all resource specifications. 
        List<ResolvedSpec>? all_CandidateSets = ResolveAllResourceSpecs(query, envV, envH);
        
        //Indicates failure from resource availability at requested time
        if (all_CandidateSets == null)
            return [[]];

        //Pick one candidate from each spec's candidateSet; discard tuples with overlapping resources between elements of the tuple
        IEnumerable<List<List<ResourceVal>>> all_crossSpecCandidateTuples = BuildDistinctCandidates(all_CandidateSets);

       // Filter by the query condition, if present
        IEnumerable<List<List<ResourceVal>>> validCrossSpecCandidateTuples = query.Condition == null
            ? all_crossSpecCandidateTuples
            : all_crossSpecCandidateTuples.Where(t => CandidateSatisfiesCondition(t, all_CandidateSets, query.Condition, envV, envH));

         // Grouping by spec no longer needed for id binding — flatten permanently into the output format
        return validCrossSpecCandidateTuples.Select(
            crossSpecCandidateTuple => crossSpecCandidateTuple.SelectMany(candidate => candidate).ToList()
        );
    }
    
/* ___________________________________________________
*   Step 1: Resolve resource specs against the time window 
* __________________________________________________________*/

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
            Candidates:
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
            Candidates: combinations,
            //as: like a typecast, but returns null if not possible rather than throwing exception. Null-safe operator
            LocalBinding: (catSpec as CategorySpecWithBinding)?.LocalBindingId);
    }

/*_______________________________________________________
 *   Step 2+3: Build cross-spec candidate tuples
 *________________________________________________________________*/
  private static IEnumerable<List<List<ResourceVal>>> BuildDistinctCandidates(List<ResolvedSpec> all_CandidateSets) {

        //Discards binding ids in each resolved (category) Spec
        AllCandidateSets all_CandidateSets_withoutBinding = all_CandidateSets.Select(resolvedSpec => resolvedSpec.Candidates).ToList();

        /* Step 2: Cartesian product  pick one candidate from each spec's candidateSet - i.e. all possible combinations bewteen resolved spec sets:     
        [  [ [_room205] , [r2, r4] ] , [ [_room205] , [r2, r3] ] , [ [_room205] , [r3, r4] ]    ]*/ 
        IEnumerable<List<List<ResourceVal>>> all_crossSpecCandidateTuples = CartesianProduct(all_CandidateSets_withoutBinding);

        // Step 3: Discard cross-spec candidate tuples where the same resource appears in more than one element (_room205, _room205,  )
        return all_crossSpecCandidateTuples.Where(HasNoResourceOverlap);
    }

    /* reserve _room205 and 2 Room [time] [condition]
     e.g. crossSpecCandidate: [  [_room205] , [_room205, r4]  ] 
     Both valid candidates, should however get rejected given the same resource can't be booked twice accross "and" at same time */
    private static bool HasNoResourceOverlap(List<List<ResourceVal>> crossSpecCandidate) {

        // [ _room205, _room205, r4 ]        <- [   [_room205] , [_room205, r4]  ]
        List<ResourceVal> flatCandidateTupple =  crossSpecCandidate.SelectMany(resources => resources).ToList();

        //               | [ _room205 , r4] |         != |[ _room205, _room205, r4  ]| i.e. duplicate elements within tuple 
        return flatCandidateTupple.Distinct().Count() == flatCandidateTupple.Count;
        
        // Comparing amount of unique elements in list to amount of actual elements. If they differ, duplicates were present)
    }

//---------------------------------Condition
// Step 4: Evaluate Condition {}
/*        all_distinctCandidateTuples = all_distinctCandidateTuples.Where(assignment => {
            var bindingSets = new List<KeyValuePair<string, List<ResourceVal>>>();
            for (int i = 0; i < assignment.Count; i++) {
                if (all_CandidateSets[i].LocalBinding != null) {
                bindingSets.Add(new KeyValuePair<string, List<ResourceVal>>(
                    all_CandidateSets[i].LocalBinding!,
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
        });*/

    
 /* _________________________________________________________
     Step 4: Filter by condition
    ______________________________________________________*/

/// <summary>
    /// Returns true if the condition holds for this cross-spec candidate tuple.
    /// When a spec carries a local binding (e.g. <c>3 DoubleRoom drs</c>), the condition
    /// is evaluated once per individual resource in that candidate — and must hold for all of them.
    /// </summary>
    private static bool CandidateSatisfiesCondition(
        List<List<ResourceVal>> crossSpecCandidateTuple,
        List<ResolvedSpec> all_CandidateSets,
        Exp condition,
        EnvV envV,
        EnvH envH)
    {
        List<(string BindingName, List<ResourceVal> BoundResources)> bindings =
            CollectBoundVariables(crossSpecCandidateTuple, all_CandidateSets);

        return bindings.Count == 0
            ? EvaluateCondition(condition, envV, envH)
            : ConditionHoldsForAllBindings(condition, bindings, envV, envH);
    }

    /// <summary> Pairs each local binding name with the candidate chosen for its spec. </summary>
    private static List<(string BindingName, List<ResourceVal> BoundResources)> CollectBoundVariables(
        List<List<ResourceVal>> crossSpecCandidateTuple,
        List<ResolvedSpec> all_CandidateSets)
    {
        List<(string, List<ResourceVal>)> bindings = new();
        for (int i = 0; i < all_CandidateSets.Count; i++) {
            if (all_CandidateSets[i].LocalBinding is string name)
                bindings.Add((name, crossSpecCandidateTuple[i]));
        }
        return bindings;
    }

    /// <summary>
    /// For a binding like <c>3 DoubleRoom drs</c>, the chosen candidate is e.g. [dr1, dr3, dr5].
    /// The condition must hold with drs=dr1, drs=dr3, and drs=dr5 individually — and for
    /// every combination across all bound specs. Cartesian product covers the multi-binding case.
    /// </summary>
    private static bool ConditionHoldsForAllBindings(
        Exp condition,
        List<(string BindingName, List<ResourceVal> BoundResources)> bindings,
        EnvV envV,
        EnvH envH)
    {
        // For each binding, produce one (name, resource) pair per resource in its candidate
        IEnumerable<IEnumerable<(string, ResourceVal)>> perBindingElements =
            bindings.Select(b => b.BoundResources.Select(resource => (b.BindingName, resource)));

        // Cartesian product: all combinations of one element per bound variable
        IEnumerable<List<(string, ResourceVal)>> elementCombinations = CartesianProduct(perBindingElements);

        // Condition must hold for every combination
        foreach (List<(string bindingName, ResourceVal resource)> combination in elementCombinations) {
            EnvV scope = envV.NewScope();
            foreach ((string bindingName, ResourceVal resource) in combination)
                scope.Bind(bindingName, resource);

            if (!EvaluateCondition(condition, scope, envH))
                return false;
        }
        return true;
    }

    private static bool EvaluateCondition(Exp condition, EnvV envV, EnvH envH) {
        try {
            return Interpreter.EvalExp(condition, envV, envH).AsBool();
        } catch (Interpreter.MissingPropertyException) {
            return false;
        }
    }



 /*__________________________________________
*   utilities
*  ______________________________________*/

    /// <summary> All combinations of exactly <paramref name="quantity"/> resources from <paramref name="resources"/>. </summary>
    private static IEnumerable<Candidate> GetCombinations(List<ResourceVal> resources, int quantity) {
        if (quantity == 0)                    yield return [];
        else if (resources.Count == quantity) yield return new Candidate(resources);
        else if (resources.Count > quantity) {
            ResourceVal       head = resources[0];
            List<ResourceVal> tail = resources.GetRange(1, resources.Count - 1);

            foreach (Candidate combo in GetCombinations(tail, quantity - 1)) yield return [head, ..combo];
            foreach (Candidate combo in GetCombinations(tail, quantity))     yield return combo;
        }
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
        List<List<ResourceVal>> Candidates,
        string? LocalBinding
    );
}