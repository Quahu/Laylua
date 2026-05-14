using Laylua.Marshaling;

namespace Laylua.Tests;

public class LuaConversionDiagnosticsTests
{
    [Test]
    public void LuaStackValue_DefaultInstance_TryGetValue_ThrowsInvalidOperationException()
    {
        // Arrange
        var stackValue = default(LuaStackValue);

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            stackValue.TryGetValue(out int _);
        });

        // Assert
        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Message, Is.EqualTo("The index of this LuaStackValue is not valid."));
    }

    [Test]
    public void LuaStackValue_DefaultInstance_TryGetValueWithError_ThrowsInvalidOperationException()
    {
        // Arrange
        var stackValue = default(LuaStackValue);

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            stackValue.TryGetValue(out int _, out LuaConversionError? _);
        });

        // Assert
        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Message, Is.EqualTo("The index of this LuaStackValue is not valid."));
    }

    [Test]
    public void LuaConversionError_IncompatibleLuaType_UsesLuaStyleMessage()
    {
        // Arrange
        var error = LuaConversionError.IncompatibleLuaType(typeof(int), LuaType.String);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(error.Message, Is.EqualTo("expected int, got string"));
            Assert.That(error.Path, Is.EqualTo("$"));
            Assert.That(error.TargetType, Is.EqualTo(typeof(int)));
            Assert.That(error.LuaType, Is.EqualTo(LuaType.String));
        });
    }

    [Test]
    public void LuaConversionError_ConversionPolicyDisabled_UsesLuaStyleMessage()
    {
        // Arrange
        var error = LuaConversionError.ConversionPolicyDisabled(typeof(char), LuaType.String, "string-to-char");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(error.Message, Is.EqualTo("string-to-char conversion is disabled"));
            Assert.That(error.Path, Is.EqualTo("$"));
            Assert.That(error.TargetType, Is.EqualTo(typeof(char)));
            Assert.That(error.LuaType, Is.EqualTo(LuaType.String));
        });
    }

    [Test]
    public void LuaConversionError_NumericValueOutOfRange_UsesLuaStyleMessage()
    {
        // Arrange
        var error = LuaConversionError.NumericValueOutOfRange(typeof(byte));

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(error.Message, Is.EqualTo("value is outside the range of byte"));
            Assert.That(error.Path, Is.EqualTo("$"));
            Assert.That(error.TargetType, Is.EqualTo(typeof(byte)));
            Assert.That(error.LuaType, Is.EqualTo(LuaType.Number));
        });
    }

    [Test]
    public void LuaConversionError_UnexpectedTableKeyType_UsesLuaStyleMessage()
    {
        // Arrange
        var error = LuaConversionError.UnexpectedTableKeyType(typeof(LuaStackValue), LuaType.Number);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(error.Message, Is.EqualTo("table keys must be strings"));
            Assert.That(error.Path, Is.EqualTo("$"));
            Assert.That(error.TargetType, Is.EqualTo(typeof(LuaStackValue)));
            Assert.That(error.LuaType, Is.EqualTo(LuaType.Number));
        });
    }

    [Test]
    public void LuaConversionError_NullPath_UsesConstructorNormalization()
    {
        // Arrange
        var error = LuaConversionError.NoTableConversionRegistered(typeof(LuaStackValue), LuaType.Table, null);

        // Assert
        Assert.That(error.Path, Is.EqualTo("$"));
    }

    [Test]
    public void LuaConversionError_NonRootedPath_NormalizesToDollarRoot()
    {
        // Arrange
        var bareSegment = new LuaConversionError("expected int, got string", "foo.bar", typeof(int), LuaType.String);
        var dotSegment = new LuaConversionError("expected int, got string", ".foo", typeof(int), LuaType.String);
        var indexSegment = new LuaConversionError("expected int, got string", "[0]", typeof(int), LuaType.String);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(bareSegment.Path, Is.EqualTo("$.foo.bar"));
            Assert.That(dotSegment.Path, Is.EqualTo("$.foo"));
            Assert.That(indexSegment.Path, Is.EqualTo("$[0]"));
        });
    }

    [Test]
    public void LuaConversionError_WithPathPrefix_HandlesNonRootedInnerError()
    {
        // Arrange
        var error = new LuaConversionError("expected int, got string", "foo.bar", typeof(int), LuaType.String);

        // Act
        var prefixedError = error.WithPathPrefix("$.parent");

        // Assert
        Assert.That(prefixedError.Path, Is.EqualTo("$.parent.foo.bar"));
    }

    [Test]
    public void LuaConversionError_WithPathPrefix_PrefixesNestedErrorPaths()
    {
        // Arrange
        var innerError = new LuaConversionError(
            "expected string, got table",
            "$.embed.description",
            typeof(string),
            LuaType.Table);
        var error = new LuaConversionError(
            "failed to convert member 'embed' to TestEmbed",
            "$.embed",
            typeof(LuaStackValue),
            LuaType.Table,
            "embed",
            innerError);

        // Act
        var prefixedError = error.WithPathPrefix("$.target");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(prefixedError.Path, Is.EqualTo("$.target.embed"));
            Assert.That(prefixedError.InnerError, Is.Not.Null);
            Assert.That(prefixedError.InnerError!.Path, Is.EqualTo("$.target.embed.description"));
        });
    }

    [Test]
    public void LuaConversionException_Message_UsesDotNetFriendlyPrefix()
    {
        // Arrange
        var error = LuaConversionError.MissingRequiredMember(typeof(LuaStackValue), "Value", "$.Value");

        // Act
        var exception = new LuaConversionException(error);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(exception.Message, Is.EqualTo("Lua conversion failed: $.Value: missing required member 'Value'"));
            Assert.That(exception.Error, Is.SameAs(error));
        });
    }
}
