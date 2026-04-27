using Laylua.Marshaling;

namespace Laylua.Tests;

public class EnumValueTests
{
    [Test]
    public void LuaNamingPolicy_Original_IsFirstValue()
    {
        // Act & Assert
        Assert.That((int) LuaNamingPolicy.Original, Is.EqualTo(1));
    }

    [Test]
    public void LuaNamingPolicy_UpperSnakeCase_FollowsLowerSnakeCase()
    {
        // Act & Assert
        Assert.That((int) LuaNamingPolicy.UpperSnakeCase, Is.EqualTo((int) LuaNamingPolicy.LowerSnakeCase + 1));
    }

    [Test]
    public void LuaTypeMembers_InstanceMembers_IsCorrectComposite()
    {
        // Arrange
        var expected = LuaTypeMembers.Properties
            | LuaTypeMembers.Fields
            | LuaTypeMembers.Methods
            | LuaTypeMembers.Events
            | LuaTypeMembers.Indexers
            | LuaTypeMembers.Constructors;

        // Act & Assert
        Assert.That(LuaTypeMembers.InstanceMembers, Is.EqualTo(expected));
    }

    [Test]
    public void LuaTypeMembers_StaticMembers_IsCorrectComposite()
    {
        // Arrange
        var expected = LuaTypeMembers.StaticProperties
            | LuaTypeMembers.StaticFields
            | LuaTypeMembers.StaticMethods
            | LuaTypeMembers.StaticEvents
            | LuaTypeMembers.Constants;

        // Act & Assert
        Assert.That(LuaTypeMembers.StaticMembers, Is.EqualTo(expected));
    }

    [Test]
    public void LuaTypeMembers_All_IsCorrectComposite()
    {
        // Act & Assert
        Assert.That(LuaTypeMembers.All, Is.EqualTo(LuaTypeMembers.InstanceMembers | LuaTypeMembers.StaticMembers));
    }

    [TestCase(LuaTypeMembers.Properties, 1 << 0)]
    [TestCase(LuaTypeMembers.Fields, 1 << 1)]
    [TestCase(LuaTypeMembers.Methods, 1 << 2)]
    [TestCase(LuaTypeMembers.Events, 1 << 3)]
    [TestCase(LuaTypeMembers.Indexers, 1 << 4)]
    [TestCase(LuaTypeMembers.Constructors, 1 << 5)]
    [TestCase(LuaTypeMembers.StaticProperties, 1 << 6)]
    [TestCase(LuaTypeMembers.StaticFields, 1 << 7)]
    [TestCase(LuaTypeMembers.StaticMethods, 1 << 8)]
    [TestCase(LuaTypeMembers.StaticEvents, 1 << 9)]
    [TestCase(LuaTypeMembers.Constants, 1 << 10)]
    public void LuaTypeMembers_SingleFlag_HasExpectedBit(LuaTypeMembers flag, int expectedBit)
    {
        // Act & Assert
        Assert.That((int) flag, Is.EqualTo(expectedBit));
    }

    [Test]
    public void LuaTypeFeatures_Operators_IsCorrectComposite()
    {
        // Arrange
        var expected = LuaTypeFeatures.ArithmeticOperators
            | LuaTypeFeatures.ComparisonOperators
            | LuaTypeFeatures.BitwiseOperators;

        // Act & Assert
        Assert.That(LuaTypeFeatures.Operators, Is.EqualTo(expected));
    }

    [Test]
    public void LuaTypeFeatures_Behaviors_IsCorrectComposite()
    {
        // Arrange
        var expected = LuaTypeFeatures.ToString
            | LuaTypeFeatures.Dispose
            | LuaTypeFeatures.Length
            | LuaTypeFeatures.Pairs;

        // Act & Assert
        Assert.That(LuaTypeFeatures.Behaviors, Is.EqualTo(expected));
    }

    [Test]
    public void LuaTypeFeatures_All_IsCorrectComposite()
    {
        // Act & Assert
        Assert.That(LuaTypeFeatures.All, Is.EqualTo(LuaTypeFeatures.Behaviors | LuaTypeFeatures.Operators));
    }

    [TestCase(LuaTypeFeatures.ToString, 1 << 0)]
    [TestCase(LuaTypeFeatures.Dispose, 1 << 1)]
    [TestCase(LuaTypeFeatures.Length, 1 << 2)]
    [TestCase(LuaTypeFeatures.Pairs, 1 << 3)]
    [TestCase(LuaTypeFeatures.Add, 1 << 4)]
    [TestCase(LuaTypeFeatures.Subtract, 1 << 5)]
    [TestCase(LuaTypeFeatures.Multiply, 1 << 6)]
    [TestCase(LuaTypeFeatures.Divide, 1 << 7)]
    [TestCase(LuaTypeFeatures.Modulus, 1 << 8)]
    [TestCase(LuaTypeFeatures.UnaryNegation, 1 << 9)]
    [TestCase(LuaTypeFeatures.Equality, 1 << 10)]
    [TestCase(LuaTypeFeatures.LessThan, 1 << 11)]
    [TestCase(LuaTypeFeatures.LessThanOrEqual, 1 << 12)]
    [TestCase(LuaTypeFeatures.BitwiseAnd, 1 << 13)]
    [TestCase(LuaTypeFeatures.BitwiseOr, 1 << 14)]
    [TestCase(LuaTypeFeatures.ExclusiveOr, 1 << 15)]
    [TestCase(LuaTypeFeatures.OnesComplement, 1 << 16)]
    [TestCase(LuaTypeFeatures.LeftShift, 1 << 17)]
    [TestCase(LuaTypeFeatures.RightShift, 1 << 18)]
    public void LuaTypeFeatures_SingleFlag_HasExpectedBit(LuaTypeFeatures flag, int expectedBit)
    {
        // Act & Assert
        Assert.That((int) flag, Is.EqualTo(expectedBit));
    }

    [Test]
    public void LuaTypeOptimizations_All_IsPrimitiveMarshalerBypass()
    {
        // Act & Assert
        Assert.That(LuaTypeOptimizations.All, Is.EqualTo(LuaTypeOptimizations.PrimitiveMarshalerBypass));
    }

    [Test]
    public void LuaIndexMembers_Events_IsCorrectBit()
    {
        // Act & Assert
        Assert.That((int) LuaIndexMembers.Events, Is.EqualTo(1 << 3));
    }

    [Test]
    public void LuaIndexMembers_All_IncludesEvents()
    {
        // Act & Assert
        Assert.That(LuaIndexMembers.All.HasFlag(LuaIndexMembers.Events), Is.True);
    }

    [Test]
    public void LuaIndexMembers_All_IsCorrectComposite()
    {
        // Arrange
        var expected = LuaIndexMembers.Properties
            | LuaIndexMembers.Methods
            | LuaIndexMembers.Indexer
            | LuaIndexMembers.Events;

        // Act & Assert
        Assert.That(LuaIndexMembers.All, Is.EqualTo(expected));
    }
}
