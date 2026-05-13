using RAL.Interpreter;

public class ReservationRegistry
{
    //Singleton instance of registry. Static, i.e. a single class field for entire application 
    private static readonly ReservationRegistry instance = new ReservationRegistry();

    //Instance field on the singleton instance, therefor not static. Holds the registry data structure 
    private readonly HashSet<ReservationVal> _registry;

    //Private constructor initializing registry data structure
    private ReservationRegistry() {

        _registry = new HashSet<ReservationVal>();
        
    }

    //Method to access singleton, which is otherwise private
    public static ReservationRegistry Instance() { return instance; }

    /// <summary> Interface for registering a given reservation </summary>
    public void RegisterReservation(ReservationVal reservation) {
        _registry.Add(reservation);
    }

    /// <summary> Interface for cancelling a given reservation </summary>
    public void CancelReservation(ReservationVal reservation) {
        _registry.Remove(reservation);

        //Make a variable's value indicate 'null' reservation
        reservation.Reservations.Clear();        
    }

    /// <summary> Checks weather a specific resource is available in the given time slot </summary>
    public bool IsAvailable(ResourceVal resource, DateTime requestedStart, DateTime requestedEnd) {
        //Negated overlap check
        return 
            !(_registry
                //From each possibly composite Reservation, extract all inner atomic reservations into one flat list<ResourceAtomVal>
                .SelectMany(resourceVal => resourceVal.Reservations)

                //Foreach resourceAtomVal, which may contain a list of resources for a given timeslot. (re and re)
                .Any(reservationAtomVal => 
                    //is the resource reserved?
                    reservationAtomVal.Resources.Contains(resource) &&
                    //Overlaps?
                    requestedStart < reservationAtomVal.End.Value &&
                    requestedEnd > reservationAtomVal.Start.Value
                )          
            );
    }
    public override string ToString() {
        if (_registry.Count == 0)
            return "=== Reservation Registry ===\n(empty)\n";

        string reservations = string.Join(
            "\n--------------------------------------------------\n",
            _registry.Select((reservationVal, i) => $"Reservation #{i + 1}\n{reservationVal}"));

        return $"=== Reservation Registry ===\n\n{reservations}\n\n=== End Registry ===";
    }

}