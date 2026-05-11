using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Laylua.Generators;

public partial class LuaTypeGenerator
{
    private static void EmitIndexCallback(CodeWriter writer, LuaTypeInfo typeInfo)
    {
        writer.Line("private static int __Index(lua_State* L)");
        writer.OpenBrace();
        EmitSelfVariable(writer, typeInfo);

        var switchEntries = new List<IndexSwitchEntry>();

        if ((typeInfo.IndexMembers & IndexMembersProperties) != 0)
        {
            foreach (var property in typeInfo.Properties.Where(static property => property.CanRead))
            {
                switchEntries.Add(new IndexSwitchEntry(property.LuaName, property, null));
            }

            foreach (var field in typeInfo.Fields.Where(static field => field.CanRead))
            {
                switchEntries.Add(new IndexSwitchEntry(field.LuaName, field, null));
            }
        }

        if ((typeInfo.IndexMembers & IndexMembersMethods) != 0)
        {
            foreach (var method in typeInfo.Methods)
            {
                switchEntries.Add(new IndexSwitchEntry(method.LuaName, null, method));
            }
        }

        if ((typeInfo.IndexMembers & IndexMembersEvents) != 0)
        {
            foreach (var eventInfo in typeInfo.Events)
            {
                switchEntries.Add(new IndexSwitchEntry(eventInfo.LuaName, null, null, eventInfo));
            }
        }

        var hasIndexer = (typeInfo.IndexMembers & IndexMembersIndexer) != 0
            && typeInfo.Indexers.Any(ix => ix.HasGetter);

        if (typeInfo.IndexPriority == IndexPriorityIndexerFirst && hasIndexer)
        {
            EmitIndexerGet(writer, typeInfo, "self");
            writer.Line();

            // After indexer dispatch, try named members without mutating non-string keys.
            if (switchEntries.Count > 0)
            {
                writer.Code("""
                    if (lua_type(L, 2) == LuaType.String)
                    {
                        var key = lua_tostring(L, 2).Span;
                    """);

                writer.Line();
                EmitIndexSwitch(writer, typeInfo, switchEntries, "self", isStatic: false);
                writer.CloseBrace();
            }

            writer.Line();
            EmitUnknownPropertyFallback(writer, typeInfo);
        }
        else
        {
            if (switchEntries.Count > 0)
            {
                if (hasIndexer)
                {
                    // Guard lua_tostring behind type check to avoid mutating numeric keys
                    writer.Line("if (lua_type(L, 2) == LuaType.String)");
                    writer.OpenBrace();
                }

                writer.Line("var key = lua_tostring(L, 2).Span;");
                writer.Line();
                EmitIndexSwitch(writer, typeInfo, switchEntries, "self", isStatic: false);

                if (hasIndexer)
                {
                    writer.CloseBrace();
                    writer.Line();
                    EmitIndexerGet(writer, typeInfo, "self");
                    writer.Line();
                    EmitUnknownPropertyFallback(writer, typeInfo);
                }
                else
                {
                    writer.Line();
                    EmitUnknownPropertyFallback(writer, typeInfo);
                }
            }
            else if (hasIndexer)
            {
                EmitIndexerGet(writer, typeInfo, "self");
                writer.Line();
                EmitUnknownPropertyFallback(writer, typeInfo);
            }
            else
            {
                EmitUnknownPropertyFallback(writer, typeInfo);
            }
        }

        writer.CloseBrace();
    }

    private static void EmitNewIndexCallback(CodeWriter writer, LuaTypeInfo typeInfo)
    {
        writer.Line("private static int __NewIndex(lua_State* L)");
        writer.OpenBrace();

        var includeProperties = (typeInfo.IndexMembers & IndexMembersProperties) != 0;

        var writableMembers = includeProperties
            ? typeInfo.Properties.Where(static property => property.CanWrite)
                .Concat(typeInfo.Fields.Where(static field => field.CanWrite))
                .ToList()
            : new List<LuaMemberInfo>();

        var readOnlyMembers = includeProperties
            ? typeInfo.Properties.Where(static property => property.CanRead && !property.CanWrite)
                .Concat(typeInfo.Fields.Where(static field => field.CanRead && !field.CanWrite))
                .ToList()
            : new List<LuaMemberInfo>();

        var hasIndexerSetter = (typeInfo.IndexMembers & IndexMembersIndexer) != 0
            && typeInfo.Indexers.Any(ix => ix.HasSetter);

        if (writableMembers.Count == 0 && !hasIndexerSetter && readOnlyMembers.Count == 0)
        {
            writer.Line($"return luaL_error(L, \"{typeInfo.Name}: no writable members\");");
            writer.CloseBrace();
            return;
        }

        EmitSelfVariable(writer, typeInfo);

        var hasNamedMembers = writableMembers.Count > 0 || readOnlyMembers.Count > 0;

        if (typeInfo.IndexPriority == IndexPriorityIndexerFirst && hasIndexerSetter)
        {
            EmitIndexerSet(writer, typeInfo, "self");
            writer.Line();

            if (hasNamedMembers)
            {
                writer.Code("""
                    if (lua_type(L, 2) == LuaType.String)
                    {
                        var key = lua_tostring(L, 2).Span;
                    """);

                writer.Line();

                if (writableMembers.Count > 0)
                {
                    EmitNewIndexSwitch(writer, typeInfo, writableMembers, "self", isStatic: false);
                }

                if (readOnlyMembers.Count > 0)
                {
                    EmitReadOnlyErrorSwitch(writer, typeInfo.Name, readOnlyMembers);
                }

                writer.CloseBrace();
            }

            writer.Line();
            EmitUnknownPropertyFallback(writer, typeInfo);
        }
        else
        {
            if (hasNamedMembers)
            {
                if (hasIndexerSetter)
                {
                    // Guard lua_tostring behind type check to avoid mutating numeric keys
                    writer.Line("if (lua_type(L, 2) == LuaType.String)");
                    writer.OpenBrace();
                }

                writer.Line("var key = lua_tostring(L, 2).Span;");
                writer.Line();

                if (writableMembers.Count > 0)
                {
                    EmitNewIndexSwitch(writer, typeInfo, writableMembers, "self", isStatic: false);
                }

                if (readOnlyMembers.Count > 0)
                {
                    EmitReadOnlyErrorSwitch(writer, typeInfo.Name, readOnlyMembers);
                }

                if (hasIndexerSetter)
                {
                    writer.CloseBrace();
                    writer.Line();
                    EmitIndexerSet(writer, typeInfo, "self");
                    writer.Line();
                    EmitUnknownPropertyFallback(writer, typeInfo);
                }
                else
                {
                    writer.Line();
                    EmitUnknownPropertyFallback(writer, typeInfo);
                }
            }
            else if (hasIndexerSetter)
            {
                EmitIndexerSet(writer, typeInfo, "self");
                writer.Line();
                EmitUnknownPropertyFallback(writer, typeInfo);
            }
            else
            {
                EmitUnknownPropertyFallback(writer, typeInfo);
            }
        }

        writer.CloseBrace();
    }

    private static void EmitIndexSwitch(
        CodeWriter writer,
        LuaTypeInfo typeInfo,
        List<IndexSwitchEntry> entries,
        string target,
        bool isStatic)
    {
        if (entries.Count == 0)
        {
            return;
        }

        // Group entries by UTF-8 byte length for a proper two-level dispatch:
        // outer switch on length (jump table), inner SequenceEqual per bucket
        var buckets = entries
            .GroupBy(entry => Encoding.UTF8.GetByteCount(entry.LuaName))
            .OrderBy(group => group.Key);

        writer.Line("switch (key.Length)");
        writer.OpenBrace();

        foreach (var bucket in buckets)
        {
            writer.Line($"case {bucket.Key}:");
            writer.OpenBrace();

            foreach (var entry in bucket)
            {
                writer.Line($"if (key.SequenceEqual(\"{entry.LuaName}\"u8))");
                writer.OpenBrace();

                if (entry.Member != null)
                {
                    var isObsolete = entry.Member.IsObsolete;
                    if (isObsolete)
                        writer.DisableWarning("CS0618");

                    var memberAccess = isStatic ? $"{typeInfo.Name}.{entry.Member.ClrName}" : $"{target}.{entry.Member.ClrName}";
                    if (entry.Member.IsLuaString)
                    {
                        writer.Line($"{EmitLuaStringPushExpression(memberAccess, entry.Member.TypeName)};");
                    }
                    else
                    {
                        EmitPushExpression(writer, typeInfo, entry.Member.ValueKind, entry.Member.TypeName, memberAccess, scopeThread: false);
                    }

                    if (isObsolete)
                        writer.RestoreWarning("CS0618");
                }
                else if (entry.Method != null)
                {
                    var ptrField = isStatic
                        ? $"_definitionMethod{GetMethodGeneratedIdentifier(entry.Method)}FunctionPtr"
                        : $"_method{GetMethodGeneratedIdentifier(entry.Method)}FunctionPtr";

                    writer.Line($"lua_pushcfunction(L, {ptrField});");
                }
                else if (entry.Event != null)
                {
                    var canAdd = entry.Event.CanAdd ? "true" : "false";
                    var canRemove = entry.Event.CanRemove ? "true" : "false";
                    if (isStatic)
                    {
                        writer.Line($"LuaEventAccessor.Push(L, null, _event{entry.Event.ClrName}Info!, _eventTracker!, 1, _staticEventSubscriptionKey, {canAdd}, {canRemove});");
                    }
                    else
                    {
                        writer.Line($"LuaEventAccessor.Push(L, __target!, _event{entry.Event.ClrName}Info!, _eventTracker!, 1, null, {canAdd}, {canRemove});");
                    }
                }
                else if (entry.IsFactory)
                {
                    writer.Line("lua_pushcfunction(L, _callFunctionPtr);");
                }

                writer.Line("return 1;");
                writer.CloseBrace();
            }

            writer.Line();
            writer.Code("break;");
            writer.CloseBrace();
        }

        writer.CloseBrace();
    }

    private static void EmitNewIndexSwitch(
        CodeWriter writer,
        LuaTypeInfo typeInfo,
        List<LuaMemberInfo> writableMembers,
        string target,
        bool isStatic)
    {
        var buckets = writableMembers
            .GroupBy(member => Encoding.UTF8.GetByteCount(member.LuaName))
            .OrderBy(group => group.Key);

        writer.Line("switch (key.Length)");
        writer.OpenBrace();

        foreach (var bucket in buckets)
        {
            writer.Line($"case {bucket.Key}:");
            writer.OpenBrace();

            foreach (var member in bucket)
            {
                var memberAccess = isStatic ? $"{typeInfo.Name}.{member.ClrName}" : $"{target}.{member.ClrName}";
                writer.Line($"if (key.SequenceEqual(\"{member.LuaName}\"u8))");
                writer.OpenBrace();

                if (member.IsObsolete)
                    writer.DisableWarning("CS0618");

                EmitWriteMember(writer, typeInfo, member, memberAccess, 3);

                if (member.IsObsolete)
                    writer.RestoreWarning("CS0618");

                writer.Line("return 0;");
                writer.CloseBrace();
            }

            writer.Line();
            writer.Code("break;");
            writer.CloseBrace();
        }

        writer.CloseBrace();
    }

    private static void EmitReadOnlyErrorSwitch(CodeWriter writer, string typeName, List<LuaMemberInfo> readOnlyMembers)
    {
        writer.Line();
        var buckets = readOnlyMembers
            .GroupBy(member => Encoding.UTF8.GetByteCount(member.LuaName))
            .OrderBy(group => group.Key);

        writer.Line("switch (key.Length)");
        writer.OpenBrace();

        foreach (var bucket in buckets)
        {
            writer.Line($"case {bucket.Key}:");
            writer.OpenBrace();

            foreach (var member in bucket)
            {
                writer.Code($$"""
                    if (key.SequenceEqual("{{member.LuaName}}"u8))
                    {
                        return luaL_error(L, "{{typeName}}.{{member.LuaName}}: property is read-only");
                    }
                    """);
            }

            writer.Line();
            writer.Code("break;");
            writer.CloseBrace();
        }

        writer.CloseBrace();
    }

    private static void EmitUnknownPropertyFallback(CodeWriter writer, LuaTypeInfo typeInfo)
    {
        if (typeInfo.UnknownPropertyBehavior == UnknownPropError)
        {
            writer.Code($$"""
                if (lua_type(L, 2) == LuaType.String)
                {
                    var keyStr = lua_tostring(L, 2).ToString();
                    return luaL_error(L, $"{{typeInfo.Name}}: unknown property '{keyStr}'");
                }
                else
                {
                    var keyTypeName = lua_typename(L, lua_type(L, 2)).ToString();
                    return luaL_error(L, $"{{typeInfo.Name}}: invalid key type '{keyTypeName}'");
                }
                """);
        }

        writer.Line("return 0;");
    }

    /// <summary>
    ///     Emits code to write a value from the Lua stack into a member.
    ///     Handles both primitive and marshaled types.
    /// </summary>
    private static void EmitWriteMember(CodeWriter writer, LuaTypeInfo typeInfo, LuaMemberInfo member, string memberAccess, int stackIndex)
    {
        if (UseMarshaledInterop(typeInfo, member.ValueKind))
        {
            writer.Code($$"""
                using var thread = LuaThread.FromExtraSpace(L);
                {{memberAccess}} = thread.Stack[{{stackIndex}}].GetValue<{{member.TypeName}}>((global::Laylua.Marshaling.LuaAllowedValueConversions) BindingConversions)!;
                """);
        }
        else if (member.ValueKind == LuaValueKind.String)
        {
            writer.Line($"{memberAccess} = lua_type(L, {stackIndex}) == LuaType.Nil ? null! : {EmitReadExpression(typeInfo, member.ValueKind, member.TypeName, stackIndex)};");
        }
        else
        {
            writer.Line($"{memberAccess} = {EmitReadExpression(typeInfo, member.ValueKind, member.TypeName, stackIndex)};");
        }
    }

    private static void EmitIndexerGet(CodeWriter writer, LuaTypeInfo typeInfo, string target)
    {
        var getters = typeInfo.Indexers.Where(ix => ix.HasGetter).ToList();
        if (getters.Count == 1)
        {
            var indexer = getters[0];
            var condition = EmitIndexerKeyCondition(typeInfo, indexer, 2);
            writer.Line($"if ({condition.Replace("keyType", "lua_type(L, 2)")})");
            writer.OpenBrace();
            var needsThread = UseMarshaledInterop(typeInfo, indexer.KeyValueKind) || UseMarshaledInterop(typeInfo, indexer.ValueKind);
            if (needsThread)
            {
                writer.Line("using var thread = LuaThread.FromExtraSpace(L);");
            }

            var keyRead = EmitReadExpression(typeInfo, indexer.KeyValueKind, indexer.KeyTypeName, 2);
            writer.Code($$"""
                var indexerKey = {{keyRead}};
                var indexerValue = {{target}}[indexerKey];
                """);

            EmitPushExpression(writer, typeInfo, indexer.ValueKind, indexer.ValueTypeName, "indexerValue", threadAvailable: needsThread, scopeThread: false);
            writer.Line("return 1;");
            writer.CloseBrace();
        }
        else
        {
            writer.Line("var keyType = lua_type(L, 2);");
            for (var i = 0; i < getters.Count; i++)
            {
                var indexer = getters[i];
                var condition = EmitIndexerKeyCondition(typeInfo, indexer, 2, "keyType");
                var prefix = i == 0 ? "if" : "else if";
                writer.Line($"{prefix} ({condition})");
                writer.OpenBrace();
                var needsThread = UseMarshaledInterop(typeInfo, indexer.KeyValueKind) || UseMarshaledInterop(typeInfo, indexer.ValueKind);
                if (needsThread)
                {
                    writer.Line("using var thread = LuaThread.FromExtraSpace(L);");
                }

                var keyRead = EmitReadExpression(typeInfo, indexer.KeyValueKind, indexer.KeyTypeName, 2);
                writer.Code($$"""
                    var indexerKey = {{keyRead}};
                    var indexerValue = {{target}}[indexerKey];
                    """);

                EmitPushExpression(writer, typeInfo, indexer.ValueKind, indexer.ValueTypeName, "indexerValue", threadAvailable: needsThread, scopeThread: false);
                writer.Line("return 1;");
                writer.CloseBrace();
            }
        }
    }

    private static void EmitIndexerSet(CodeWriter writer, LuaTypeInfo typeInfo, string target)
    {
        var setters = typeInfo.Indexers.Where(ix => ix.HasSetter).ToList();
        if (setters.Count == 1)
        {
            var indexer = setters[0];
            var condition = EmitIndexerKeyCondition(typeInfo, indexer, 2);
            writer.Line($"if ({condition.Replace("keyType", "lua_type(L, 2)")})");
            writer.OpenBrace();
            var needsThread = UseMarshaledInterop(typeInfo, indexer.KeyValueKind) || UseMarshaledInterop(typeInfo, indexer.ValueKind);
            if (needsThread)
            {
                writer.Line("using var thread = LuaThread.FromExtraSpace(L);");
            }

            var keyRead = EmitReadExpression(typeInfo, indexer.KeyValueKind, indexer.KeyTypeName, 2);

            if (UseMarshaledInterop(typeInfo, indexer.ValueKind))
            {
                writer.Code($$"""
                    var indexerKey = {{keyRead}};
                    {{target}}[indexerKey] = thread.Stack[3].GetValue<{{indexer.ValueTypeName}}>((global::Laylua.Marshaling.LuaAllowedValueConversions) BindingConversions)!;
                    """);
            }
            else
            {
                writer.Code($$"""
                    var indexerKey = {{keyRead}};
                    {{target}}[indexerKey] = {{EmitReadExpression(typeInfo, indexer.ValueKind, indexer.ValueTypeName, 3)}};
                    """);
            }

            writer.Line("return 0;");
            writer.CloseBrace();
        }
        else
        {
            writer.Line("var keyType = lua_type(L, 2);");
            for (var i = 0; i < setters.Count; i++)
            {
                var indexer = setters[i];
                var condition = EmitIndexerKeyCondition(typeInfo, indexer, 2, "keyType");
                var prefix = i == 0 ? "if" : "else if";
                writer.Line($"{prefix} ({condition})");
                writer.OpenBrace();
                var needsThread = UseMarshaledInterop(typeInfo, indexer.KeyValueKind) || UseMarshaledInterop(typeInfo, indexer.ValueKind);
                if (needsThread)
                {
                    writer.Line("using var thread = LuaThread.FromExtraSpace(L);");
                }

                var keyRead = EmitReadExpression(typeInfo, indexer.KeyValueKind, indexer.KeyTypeName, 2);

                if (UseMarshaledInterop(typeInfo, indexer.ValueKind))
                {
                    writer.Code($$"""
                        var indexerKey = {{keyRead}};
                        {{target}}[indexerKey] = thread.Stack[3].GetValue<{{indexer.ValueTypeName}}>((global::Laylua.Marshaling.LuaAllowedValueConversions) BindingConversions)!;
                        """);
                }
                else
                {
                    writer.Code($$"""
                        var indexerKey = {{keyRead}};
                        {{target}}[indexerKey] = {{EmitReadExpression(typeInfo, indexer.ValueKind, indexer.ValueTypeName, 3)}};
                        """);
                }

                writer.Line("return 0;");
                writer.CloseBrace();
            }
        }
    }

    private static string EmitIndexerKeyCondition(LuaTypeInfo typeInfo, LuaIndexerInfo indexer, int stackIndex, string? keyTypeExpression = null)
    {
        var keyType = keyTypeExpression ?? $"lua_type(L, {stackIndex})";
        return GetInteropKind(typeInfo, indexer.KeyValueKind) switch
        {
            LuaValueKind.Integer => $"{keyType} == LuaType.Number && lua_isinteger(L, {stackIndex})",
            LuaValueKind.Number => $"{keyType} == LuaType.Number",
            LuaValueKind.String => $"{keyType} == LuaType.String",
            LuaValueKind.Boolean => $"{keyType} == LuaType.Boolean",
            _ => $"{keyType} == LuaType.UserData && LuaTypeServices.TryGet<{indexer.KeyTypeName}>(L, {stackIndex}, out _)",
        };
    }
}
