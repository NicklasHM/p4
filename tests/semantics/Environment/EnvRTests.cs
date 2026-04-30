namespace RAL.Tests.Semantic.Environments;

using RAL.Semantic.Environments;
using RAL.Semantic.Symbols;
using Xunit;

using RalType = RAL.AST.Type;

public class EnvRTests
{
    [Fact]
    public void BindResource_ShouldAddResource()
    {
        // Arrange
        var env = new EnvR();

        // Act
        env.BindResource("room");

        // Assert
        Assert.True(env.IsResourceDefined("room"));
    }

    [Fact]
    public void BindResource_ShouldRejectDuplicateResource()
    {
        // Arrange
        var env = new EnvR();

        env.BindResource("room");

        // Act + Assert
        var exception = Assert.Throws<Exception>(() =>
            env.BindResource("room")
        );

        Assert.Contains("already defined", exception.Message);
    }

    [Fact]
    public void BindField_ShouldAddFieldToExistingResource()
    {
        // Arrange
        var env = new EnvR();
        env.BindResource("room");

        var field = new FieldSymbol(
            "capacity",
            type: null! // Replace with concrete RAL.AST.Type when available
        );

        // Act
        env.BindField("room", field);

        // Assert
        Assert.True(env.IsFieldDefined("room", "capacity"));
    }

    [Fact]
    public void BindField_ShouldRejectFieldForUnknownResource()
    {
        // Arrange
        var env = new EnvR();

        var field = new FieldSymbol(
            "capacity",
            type: null!
        );

        // Act + Assert
        var exception = Assert.Throws<Exception>(() =>
            env.BindField("room", field)
        );

        Assert.Contains("Unknown resource", exception.Message);
    }

    [Fact]
    public void BindField_ShouldRejectDuplicateFieldInSameResource()
    {
        // Arrange
        var env = new EnvR();
        env.BindResource("room");

        var firstField = new FieldSymbol("capacity", null!);
        var secondField = new FieldSymbol("capacity", null!);

        env.BindField("room", firstField);

        // Act + Assert
        var exception = Assert.Throws<Exception>(() =>
            env.BindField("room", secondField)
        );

        Assert.Contains("already defined", exception.Message);
    }

    [Fact]
    public void LookupField_ShouldReturnDeclaredField()
    {
        // Arrange
        var env = new EnvR();
        env.BindResource("room");

        var field = new FieldSymbol("capacity", null!);
        env.BindField("room", field);

        // Act
        var result = env.LookupField("room", "capacity");

        // Assert
        Assert.Same(field, result);
        Assert.Equal("capacity", result.Name);
    }

    [Fact]
    public void LookupField_ShouldThrowForUnknownResource()
    {
        // Arrange
        var env = new EnvR();

        // Act + Assert
        var exception = Assert.Throws<Exception>(() =>
            env.LookupField("room", "capacity")
        );

        Assert.Contains("Unknown resource", exception.Message);
    }

    [Fact]
    public void LookupField_ShouldThrowForUnknownField()
    {
        // Arrange
        var env = new EnvR();
        env.BindResource("room");

        // Act + Assert
        var exception = Assert.Throws<Exception>(() =>
            env.LookupField("room", "capacity")
        );

        Assert.Contains("Unknown field", exception.Message);
    }

    [Fact]
    public void IsFieldDefined_ShouldReturnFalseForUnknownResource()
    {
        // Arrange
        var env = new EnvR();

        // Act
        var result = env.IsFieldDefined("room", "capacity");

        // Assert
        Assert.False(result);
    }
}