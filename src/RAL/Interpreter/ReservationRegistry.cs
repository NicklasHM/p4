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


}