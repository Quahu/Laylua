using System.Diagnostics;
using System.Security;
using Laylua.Marshaling;

namespace Laylua.Tests;

[Explicit]
public sealed class LuaTypeAnalyzerTests
{
    private const string LuaTypeNotPartialDiagnosticId = "LAYLUA003";
    private const string LuaTypeRefStructDiagnosticId = "LAYLUA004";
    private const string LuaTypeOpenGenericDiagnosticId = "LAYLUA005";
    private const string LuaTypeGeneratorGenericDiagnosticId = "LAYLUA104";
    private const string LuaTypeGeneratorRefStructDiagnosticId = "LAYLUA105";
    private const string LuaTypeGeneratorNestedGenericDiagnosticId = "LAYLUA106";
    private const string LuaTypeUnsupportedMethodDiagnosticId = "LAYLUA100";
    private const string LuaTypeUnsupportedEventDiagnosticId = "LAYLUA102";
    private const string LuaTypeUnsupportedConstructorDiagnosticId = "LAYLUA103";
    private const string LuaTypeInvalidEnumConfigDiagnosticId = "LAYLUA107";
    private const string LuaTypeInvalidFlagsConfigDiagnosticId = "LAYLUA108";
    private const string LuaTypeNameCollisionDiagnosticId = "LAYLUA109";

#if NET8_0
    private const string TargetFramework = "net8.0";
#elif NET9_0
    private const string TargetFramework = "net9.0";
#else
    private const string TargetFramework = "net10.0";
#endif

    [Test]
    public void OpenGenericLuaType_ReportsAnalyzerError()
    {
        // Arrange
        const string source = """
                              using Laylua.Marshaling;

                              [LuaType]
                              public partial class GenericType<T>
                              {
                              }
                              """;

        // Act
        var result = BuildSource(source);

        // Assert
        Assert.That(result.ExitCode, Is.Not.EqualTo(0));
        Assert.That(result.Output, Does.Contain(LuaTypeOpenGenericDiagnosticId));
        Assert.That(result.Output, Does.Not.Contain(LuaTypeGeneratorGenericDiagnosticId));
    }

    [Test]
    public void NestedInsideGenericLuaType_ReportsAnalyzerError()
    {
        // Arrange
        const string source = """
                              using Laylua.Marshaling;

                              public class Outer<T>
                              {
                                  [LuaType]
                                  public partial class Inner
                                  {
                                  }
                              }
                              """;

        // Act
        var result = BuildSource(source);

        // Assert
        Assert.That(result.ExitCode, Is.Not.EqualTo(0));
        Assert.That(result.Output, Does.Contain(LuaTypeOpenGenericDiagnosticId));
        Assert.That(result.Output, Does.Not.Contain(LuaTypeGeneratorNestedGenericDiagnosticId));
    }

    [Test]
    public void RefStructLuaType_ReportsAnalyzerErrorWithoutDuplicateGeneratorError()
    {
        // Arrange
        const string source = """
                              using Laylua.Marshaling;

                              [LuaType]
                              public ref struct Sample
                              {
                              }
                              """;

        // Act
        var result = BuildSource(source);

        // Assert
        Assert.That(result.ExitCode, Is.Not.EqualTo(0));
        Assert.That(result.Output, Does.Contain(LuaTypeRefStructDiagnosticId));
        Assert.That(result.Output, Does.Not.Contain(LuaTypeGeneratorRefStructDiagnosticId));
    }

    [Test]
    public void NotPartialAnalyzer_IgnoresUnrelatedLuaTypeAttribute()
    {
        // Arrange
        const string source = """
                              using System;

                              public sealed class LuaTypeAttribute : Attribute
                              {
                              }

                              [LuaType]
                              public class Sample
                              {
                              }
                              """;

        // Act
        var result = BuildSource(source);

        // Assert
        Assert.That(result.ExitCode, Is.EqualTo(0), result.Output);
        Assert.That(result.Output, Does.Not.Contain(LuaTypeNotPartialDiagnosticId));
    }

    [Test]
    public void RefStructAnalyzer_IgnoresUnrelatedLuaTypeAttribute()
    {
        // Arrange
        const string source = """
                              using System;

                              public sealed class LuaTypeAttribute : Attribute
                              {
                              }

                              [LuaType]
                              public ref struct Sample
                              {
                              }
                              """;

        // Act
        var result = BuildSource(source);

        // Assert
        Assert.That(result.ExitCode, Is.EqualTo(0), result.Output);
        Assert.That(result.Output, Does.Not.Contain(LuaTypeRefStructDiagnosticId));
    }

    [Test]
    public void EventWithByRefParameter_ReportsGeneratorWarning()
    {
        // Arrange
        const string source = """
                              using Laylua.Marshaling;

                              public delegate void BadHandler(ref int value);

                              [LuaType]
                              public partial class Sample
                              {
                                  public event BadHandler? BadEvent;
                              }
                              """;

        // Act
        var result = BuildSource(source);

        // Assert
        Assert.That(result.Output, Does.Contain(LuaTypeUnsupportedEventDiagnosticId));
    }

    [Test]
    public void MethodWithByRefReturn_ReportsGeneratorWarning()
    {
        // Arrange
        const string source = """
                              using Laylua.Marshaling;

                              [LuaType]
                              public partial class Sample
                              {
                                  private int _value;

                                  public ref int GetValue()
                                  {
                                      return ref _value;
                                  }
                              }
                              """;

        // Act
        var result = BuildSource(source);

        // Assert
        Assert.That(result.Output, Does.Contain(LuaTypeUnsupportedMethodDiagnosticId));
    }

    [Test]
    public void MethodWithAwaitableReturnAndOutParameter_ReportsGeneratorWarning()
    {
        // Arrange
        const string source = """
                              using System.Threading.Tasks;
                              using Laylua.Marshaling;

                              [LuaType]
                              public partial class Sample
                              {
                                  public Task<int> GetValueAsync(out int extra)
                                  {
                                      extra = 1;
                                      return Task.FromResult(2);
                                  }
                              }
                              """;

        // Act
        var result = BuildSource(source);

        // Assert
        Assert.That(result.Output, Does.Contain(LuaTypeUnsupportedMethodDiagnosticId));
    }

    [Test]
    public void MethodWithAwaitableReturnAndLuaStackValueRangeParameter_ReportsGeneratorWarning()
    {
        // Arrange
        const string source = """
                              using System.Threading.Tasks;
                              using Laylua;
                              using Laylua.Marshaling;

                              [LuaType]
                              public partial class Sample
                              {
                                  public Task<int> GetValueAsync(LuaStackValueRange arguments)
                                  {
                                      return Task.FromResult(arguments.Count);
                                  }
                              }
                              """;

        // Act
        var result = BuildSource(source);

        // Assert
        Assert.That(result.Output, Does.Contain(LuaTypeUnsupportedMethodDiagnosticId));
    }

    [Test]
    public void EventWithUnsupportedReturnType_ReportsGeneratorWarning()
    {
        // Arrange
        const string source = """
                              using System;
                              using Laylua.Marshaling;

                              public delegate ReadOnlySpan<char> BadHandler();

                              [LuaType]
                              public partial class Sample
                              {
                                  public event BadHandler? BadEvent;
                              }
                              """;

        // Act
        var result = BuildSource(source);

        // Assert
        Assert.That(result.Output, Does.Contain(LuaTypeUnsupportedEventDiagnosticId));
    }

    [Test]
    public void ConstructorWithUnsupportedParameter_ReportsGeneratorWarning()
    {
        // Arrange
        const string source = """
                               using System;
                              using Laylua.Marshaling;

                              [LuaType]
                              public partial class Sample
                              {
                                  public Sample(ReadOnlySpan<char> value)
                                  {
                                  }
                              }
                              """;

        // Act
        var result = BuildSource(source);

        // Assert
        Assert.That(result.Output, Does.Contain(LuaTypeUnsupportedConstructorDiagnosticId));
    }

    [Test]
    public void InvalidNamingPolicyConfig_ReportsGeneratorError()
    {
        // Arrange
        const string source = """
                               using Laylua.Marshaling;

                               [LuaType(NamingPolicy = (LuaNamingPolicy) 999)]
                               public partial class Sample
                               {
                               }
                               """;

        // Act
        var result = BuildSource(source);

        // Assert
        Assert.That(result.ExitCode, Is.Not.EqualTo(0));
        Assert.That(result.Output, Does.Contain(LuaTypeInvalidEnumConfigDiagnosticId));
    }

    [Test]
    public void InvalidBindingConversionsConfig_ReportsGeneratorError()
    {
        // Arrange
        const string source = """
                               using Laylua.Marshaling;

                               [LuaType(BindingConversions = (LuaAllowedValueConversions) 256)]
                               public partial class Sample
                               {
                               }
                               """;

        // Act
        var result = BuildSource(source);

        // Assert
        Assert.That(result.ExitCode, Is.Not.EqualTo(0));
        Assert.That(result.Output, Does.Contain(LuaTypeInvalidFlagsConfigDiagnosticId));
    }

    [Test]
    public void NamingPolicyCollision_ReportsGeneratorError()
    {
        // Arrange
        const string source = """
                               using Laylua.Marshaling;

                               [LuaType(NamingPolicy = LuaNamingPolicy.LowerCase)]
                               public partial class Sample
                               {
                                   public int Value { get; set; }

                                   public int value { get; set; }
                               }
                               """;

        // Act
        var result = BuildSource(source);

        // Assert
        Assert.That(result.ExitCode, Is.Not.EqualTo(0));
        Assert.That(result.Output, Does.Contain(LuaTypeNameCollisionDiagnosticId));
    }

    [Test]
    public void FactoryMethodNameCollision_ReportsGeneratorError()
    {
        // Arrange
        const string source = """
                               using Laylua.Marshaling;

                               [LuaType(Construction = LuaConstructionMode.Factory, FactoryMethodName = "create")]
                               public partial class Sample
                               {
                                   public Sample()
                                   {
                                   }

                                   public static int create()
                                   {
                                       return 1;
                                   }
                               }
                               """;

        // Act
        var result = BuildSource(source);

        // Assert
        Assert.That(result.ExitCode, Is.Not.EqualTo(0));
        Assert.That(result.Output, Does.Contain(LuaTypeNameCollisionDiagnosticId));
    }

    private static BuildResult BuildSource(string source)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "LayluaAnalyzerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempPath);

        try
        {
            File.WriteAllText(Path.Combine(tempPath, "AnalyzerTests.csproj"), CreateProjectFile());
            File.WriteAllText(Path.Combine(tempPath, "TestFile.cs"), source);

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "build --nologo -v minimal",
                WorkingDirectory = tempPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };

            using var process = Process.Start(startInfo);
            Assert.That(process, Is.Not.Null);

            var output = process!.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
            process.WaitForExit();
            return new BuildResult(process.ExitCode, output);
        }
        finally
        {
            Directory.Delete(tempPath, recursive: true);
        }
    }

    private static string CreateProjectFile()
    {
        var layluaAssemblyPath = SecurityElement.Escape(typeof(LuaTypeAttribute).Assembly.Location);
        var layluaAssemblyDirectory = Path.GetDirectoryName(typeof(LuaTypeAttribute).Assembly.Location)!;
        var srcDirectory = Path.GetFullPath(Path.Combine(layluaAssemblyDirectory, "..", "..", "..", ".."));
        var analyzerAssemblyPath = SecurityElement.Escape(Path.Combine(srcDirectory, "..", "tools", "Laylua.Analyzers", "bin", "Release", "netstandard2.0", "Laylua.Analyzers.dll"));
        var generatorAssemblyPath = SecurityElement.Escape(Path.Combine(srcDirectory, "..", "tools", "Laylua.Generators", "bin", "Release", "netstandard2.0", "Laylua.Generators.dll"));

        return $$"""
                 <Project Sdk="Microsoft.NET.Sdk">
                     <PropertyGroup>
                         <TargetFramework>{{TargetFramework}}</TargetFramework>
                         <Nullable>enable</Nullable>
                         <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
                     </PropertyGroup>

                     <ItemGroup>
                         <Reference Include="Laylua">
                             <HintPath>{{layluaAssemblyPath}}</HintPath>
                         </Reference>
                         <Analyzer Include="{{analyzerAssemblyPath}}" />
                         <Analyzer Include="{{generatorAssemblyPath}}" />
                     </ItemGroup>
                 </Project>
                 """;
    }

    private sealed class BuildResult
    {
        public int ExitCode { get; }

        public string Output { get; }

        public BuildResult(int exitCode, string output)
        {
            ExitCode = exitCode;
            Output = output;
        }
    }
}
