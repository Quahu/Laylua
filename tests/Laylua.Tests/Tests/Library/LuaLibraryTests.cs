namespace Laylua.Tests;

public class LuaLibraryTests : LuaTestBase
{
    private static IEnumerable<TestCaseData> StandardLibraries
    {
        get
        {
            foreach (var library in LuaLibraries.Standard.EnumerateAll())
            {
                yield return new TestCaseData(library)
                    .SetArgDisplayNames($"{library.GetType().Name}: '{library.Name}'");
            }
        }
    }

    [Test]
    [TestCaseSource(nameof(StandardLibraries))]
    public void OpenLibrary_AddsGlobals(LuaLibrary library)
    {
        // Act
        var result = Lua.OpenLibrary(library);
        Assume.That(result, Is.True);

        // Assert
        Assert.That(library.Globals, Is.All.Matches<string>(Lua.Globals.ContainsKey));
    }

    [Test]
    [TestCaseSource(nameof(StandardLibraries))]
    public void CloseLibrary_RemovesGlobals(LuaLibrary library)
    {
        // Act
        var openResult = Lua.OpenLibrary(library);
        Assume.That(openResult, Is.True);
        var closeResult = Lua.CloseLibrary(library);

        // Assert
        Assert.That(closeResult, Is.True);
        Assert.That(library.Globals, Is.All.Not.Matches<string>(Lua.Globals.ContainsKey));
    }

    [Test]
    public void BaseLibrary_ExposesLoadingGlobals()
    {
        // Arrange
        var library = LuaLibraries.Standard.Base;

        // Act
        var result = Lua.OpenLibrary(library);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(Lua.Globals.ContainsKey("load"), Is.True);
        Assert.That(Lua.Globals.ContainsKey("loadfile"), Is.True);
        Assert.That(Lua.Globals.ContainsKey("dofile"), Is.True);
    }

    [Test]
    public void OpenStandardLibraries_IncludesLoadingGlobals()
    {
        // Act
        Lua.OpenStandardLibraries();

        // Assert
        Assert.That(Lua.Globals.ContainsKey("load"), Is.True);
        Assert.That(Lua.Globals.ContainsKey("loadfile"), Is.True);
        Assert.That(Lua.Globals.ContainsKey("dofile"), Is.True);
    }
}
