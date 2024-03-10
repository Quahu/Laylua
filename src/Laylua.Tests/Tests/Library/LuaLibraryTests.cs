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
}
