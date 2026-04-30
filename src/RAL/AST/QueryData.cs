// Helper classes for extracting query data in "availability" and "reserve" nodes.
namespace RAL.AST;

/// <summary> Shared data payload for both "availability" and "reserve" queries. </summary>
record class QueryData(
    List<ResourceSpec> ResourceSpecs, //resources
    TimeSpec Interval,                //time
    Exp? Condition,                   //where clause / predicate
    RecurrenceSpec? Recurrence         //recurence information
);

/// <summary> Holds data for a single resource specification: a*rc id | r. </summary>
record class ResourceSpec(
    Exp? Quantity,      // a
    string? CategoryId, // rc
    string? Identifier  // id for declaring local variable (nullable) | id of single named resource r 
);

/// <summary> Data structure for time interval of query. 
/// Classes may be further divided for exhaustiveness checking. </summary>
record class TimeSpec(
    Exp Start,     // DateTime
    Exp EndMarker // "to" DateTime | "For" Duration
);

/// <summary>
/// Data structure for reccurrence information
/// </summary>
record class RecurrenceSpec(
    RecurrenceMode Mode, //STRICT | FLEXIBLE
    Exp EveryDuration,        //"every" Duration. Defines interval between reservations
    Exp EndMarker        // "until" DateTime | "for" Duration
);

enum RecurrenceMode { STRICT, FLEXIBLE }
