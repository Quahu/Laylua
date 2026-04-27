; Shipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 2.0.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|--------------------
LAYLUA001 | Reliability | Warning | UndisposedCallResultAnalyzer, Undisposed LuaFunctionResults value
LAYLUA002 | Reliability | Warning | UndisposedCallResultInLoopAnalyzer, Undisposed LuaFunctionResults value in loop
LAYLUA003 | Usage | Error | LuaTypeNotPartialAnalyzer, [LuaType] requires partial type
LAYLUA004 | Usage | Error | LuaTypeRefStructAnalyzer, [LuaType] cannot be applied to ref struct types
LAYLUA005 | Usage | Error | LuaTypeOpenGenericAnalyzer, [LuaType] cannot be applied to open generic types
