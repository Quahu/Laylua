namespace Laylua.Moon;

internal sealed unsafe class LuaChildState : LuaState
{
    public override LuaAllocator? Allocator => _mainState.Allocator;

    public override LuaGC GC => _mainState.GC;

    protected override lua_State* LCore => _L;

    private lua_State* _L;
    private LuaState _mainState = null!;

    public void Initialize(LuaState mainState, lua_State* childState)
    {
        _mainState = mainState;
        _L = childState;

        CopyHook(mainState);
    }

    internal override void Close()
    {
        // TODO: reset/close thread? Adjust LuaChildThread if so
        _L = null;
    }
}
