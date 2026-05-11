; Shipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 2.0.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|--------------------
LAYLUA100 | Usage | Warning | LuaTypeGenerator, Method skipped due to unsupported Lua boundary type
LAYLUA101 | Usage | Info | LuaTypeGenerator, Inherited method skipped due to unsupported Lua boundary type
LAYLUA102 | Usage | Warning | LuaTypeGenerator, Event skipped due to unsupported delegate signature
LAYLUA103 | Usage | Warning | LuaTypeGenerator, Constructor skipped due to unsupported Lua boundary type
LAYLUA107 | Usage | Error | LuaTypeGenerator, Invalid enum configuration
LAYLUA108 | Usage | Error | LuaTypeGenerator, Invalid flags configuration
LAYLUA109 | Usage | Error | LuaTypeGenerator, Member name collides with another generated member
LAYLUA110 | Usage | Error | LuaTypeGenerator, Generated table conversion is unsupported
