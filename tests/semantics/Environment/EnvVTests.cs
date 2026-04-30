namespace RAL.Tests.Semantic.Environments;

using RAL.Interpreter;
using Xunit;

public class EnvTests
{
    [Fact]
    public void BindAndLookup_ShouldReturnBoundValue()
    {
        // Arrange
        var env = new Env<int>();

        // Act
        env.Bind("x", 10);

        // Assert
        Assert.Equal(10, env.Lookup("x"));
    }

    [Fact]
    public void Set_ShouldUpdateValue()
    {
        // Arrange
        var env = new Env<int>();
        env.Bind("x", 10);

        // Act
        env.Set("x", 20);

        // Assert
        Assert.Equal(20, env.Lookup("x"));
    }

    [Fact]
    public void NestedLookup_ShouldFindValueInParentScope()
    {
        // Arrange
        var global = new Env<int>();
        global.Bind("x", 5);

        var local = global.NewScope();

        // Act
        var result = local.Lookup("x");

        // Assert
        Assert.Equal(5, result);
    }

    [Fact]
    public void Shadowing_ShouldPreferLocalBinding()
    {
        // Arrange
        var global = new Env<int>();
        global.Bind("x", 5);

        var local = global.NewScope();
        local.Bind("x", 99);

        // Assert
        Assert.Equal(99, local.Lookup("x"));
        Assert.Equal(5, global.Lookup("x"));
    }

    [Fact]
    public void Bind_ShouldThrowWhenRedeclaringInSameScope()
    {
        // Arrange
        var env = new Env<int>();
        env.Bind("x", 1);

        // Act + Assert
        Assert.Throws<Exception>(() =>
            env.Bind("x", 2)
        );
    }

    [Fact]
    public void Lookup_ShouldThrowForUnknownName()
    {
        // Arrange
        var env = new Env<int>();

        // Act + Assert
        Assert.Throws<Exception>(() =>
            env.Lookup("missing")
        );
    }
}