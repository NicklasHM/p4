namespace RAL.Tests.Semantic.Environments;

using RAL.Semantic.Environments;
using RAL.Semantic.Symbols;
using Xunit;

public class EnvFTests
{
    [Fact]
    public void Bind_ShouldStoreTemplate()
    {
        // Arrange
        var env = new EnvF();

        var symbol = new FunctionSymbol(
            "booking",
            new List<ParameterSymbol>(),
            body: new object()
        );

        // Act
        env.Bind("booking", symbol);

        // Assert
        var result = env.Lookup("booking");

        Assert.Same(symbol, result);
    }

    [Fact]
    public void Bind_ShouldRejectDuplicateTemplateInSameEnvironment()
    {
        // Arrange
        var env = new EnvF();

        var first = new FunctionSymbol(
            "booking",
            new List<ParameterSymbol>(),
            body: new object()
        );

        var second = new FunctionSymbol(
            "booking",
            new List<ParameterSymbol>(),
            body: new object()
        );

        env.Bind("booking", first);

        // Act + Assert
        var exception = Assert.Throws<Exception>(() =>
            env.Bind("booking", second)
        );

        Assert.Contains("already defined", exception.Message);
    }

    [Fact]
    public void Lookup_ShouldReturnDeclaredTemplate()
    {
        // Arrange
        var env = new EnvF();

        var symbol = new FunctionSymbol(
            "reserveRoom",
            new List<ParameterSymbol>
            {
                new ParameterSymbol("room", null!),
                new ParameterSymbol("duration", null!)
            },
            body: new object()
        );

        env.Bind("reserveRoom", symbol);

        // Act
        var result = env.Lookup("reserveRoom");

        // Assert
        Assert.Equal("reserveRoom", result.Name);
        Assert.Equal(2, result.Parameters.Count);
    }

    [Fact]
    public void Lookup_ShouldThrowForUnknownTemplate()
    {
        // Arrange
        var env = new EnvF();

        // Act + Assert
        var exception = Assert.Throws<Exception>(() =>
            env.Lookup("missingTemplate")
        );

        Assert.Contains("Unknown", exception.Message);
    }

    [Fact]
    public void IsDefined_ShouldReturnTrueForDeclaredTemplate()
    {
        // Arrange
        var env = new EnvF();

        var symbol = new FunctionSymbol(
            "booking",
            new List<ParameterSymbol>(),
            body: new object()
        );

        env.Bind("booking", symbol);

        // Act
        var result = env.IsDefined("booking");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsDefined_ShouldReturnFalseForUnknownTemplate()
    {
        // Arrange
        var env = new EnvF();

        // Act
        var result = env.IsDefined("missingTemplate");

        // Assert
        Assert.False(result);
    }
}