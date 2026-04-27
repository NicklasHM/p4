using System;

public class EnvTest
{
    public static void Run()
    {
        TestBindAndLookup();
        TestSet();
        TestNestedLookup();
        TestShadowing();
        TestRedeclarationError();
        TestUnknownLookupError();

        Console.WriteLine("All Env tests passed!");
    }

    private static void TestBindAndLookup()
    {
        var env = new Env<int>();

        env.Bind("x", 10);

        AssertEquals(10, env.Lookup("x"), "Lookup should return bound value");
    }

    private static void TestSet()
    {
        var env = new Env<int>();

        env.Bind("x", 10);
        env.Set("x", 20);

        AssertEquals(20, env.Lookup("x"), "Set should update value");
    }

    private static void TestNestedLookup()
    {
        var global = new Env<int>();
        global.Bind("x", 5);

        var local = global.NewScope();

        AssertEquals(5, local.Lookup("x"), "Local scope should find value in parent scope");
    }

    private static void TestShadowing()
    {
        var global = new Env<int>();
        global.Bind("x", 5);

        var local = global.NewScope();
        local.Bind("x", 99);

        AssertEquals(99, local.Lookup("x"), "Local x should shadow global x");
        AssertEquals(5, global.Lookup("x"), "Global x should still be unchanged");
    }

    private static void TestRedeclarationError()
    {
        var env = new Env<int>();
        env.Bind("x", 1);

        AssertThrows(() => env.Bind("x", 2), "Redeclaration should throw error");
    }

    private static void TestUnknownLookupError()
    {
        var env = new Env<int>();

        AssertThrows(() => env.Lookup("missing"), "Lookup of unknown name should throw error");
    }

    private static void AssertEquals<T>(T expected, T actual, string message)
    {
        if (!Equals(expected, actual))
            throw new Exception($"Test failed: {message}. Expected {expected}, got {actual}.");
    }

    private static void AssertThrows(Action action, string message)
    {
        try
        {
            action();
            throw new Exception($"Test failed: {message}. Expected exception, but none was thrown.");
        }
        catch
        {
            // Test passed
        }
    }
}
