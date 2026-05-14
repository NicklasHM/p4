// Helper classes for extracting query data in "availability" and "reserve" nodes.
namespace RAL.AST;

/// <summary> Shared data payload for both "availability" and "reserve" queries. </summary>
record class QueryData(
    List<ResourceSpec> ResourceSpecs, //resources
    TimeSpec Interval,                //time
    Exp? Condition,                   //where clause / predicate
    RecurrenceSpec? Recurrence         //recurence information
);
/*________________________RESOURCES________________________*/
/// <summary> Super for data for a single resource specification: a*rc [id] | r. </summary>
abstract record class ResourceSpec;

/// <summary> id of single named resource </summary>
record class ResourceInstanceSpec(
    string ResourceId
) : ResourceSpec;

/// <summary> a rc. Quantity of a category (or subtypes). </summary>
record class CategorySpec(
    Exp Quantity,           // a
    string CategoryId       // rc
) : ResourceSpec;

/// <summary> a rc id. Quantity of a category (or subtypes) with local variable binding. </summary>
record class CategorySpecWithBinding(
    Exp Quantity,           // a
    string CategoryId,      // rc
    string LocalBindingId   // id
) : CategorySpec(Quantity, CategoryId);


/*________________________TIME________________________*/
/// <summary> Data structure for time interval of query.
abstract record class TimeSpec(
    Exp Start,     // DateTime
    Exp EndMarker // "to" DateTime | "For" Duration
);
record class TimeSpecTo(Exp Start, Exp To): TimeSpec(Start, To);
record class TimeSpecFor(Exp Start, Exp For): TimeSpec(Start, For);


/*________________________RECURRENCE________________________*/
/// <summary> Data structure for reccurrence information </summary>
record class RecurrenceSpec(RecurrenceMode Mode, RecurrenceInterval Time);

enum RecurrenceMode { STRICT, FLEXIBLE }

/// <summary> Defines interval between reservations until end. Recurrance interval hierarchy super. "every" Duration, ("until" DateTime | "for" Duration) </summary>
abstract record class RecurrenceInterval(Exp Every, Exp EndMarker);

///(Duration, DateTime)
record class RecurrenceUntil(Exp Every, Exp Until ): RecurrenceInterval(Every, Until);

/// (Duration, Duration)
record class RecurrenceFor(Exp Every, Exp For ): RecurrenceInterval(Every, For);