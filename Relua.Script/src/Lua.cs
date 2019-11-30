using System;
using System.Runtime.InteropServices;

namespace Relua.Script {
    public enum LuaType : int {
        None = -1,

        Nil = 0,
        Boolean = 1,
        LightUserdata = 2,
        Number = 3,
        String = 4,
        Table = 5,
        Function = 6,
        Userdata = 7,
        Thread = 8,
    }

    public enum LuaGcOperation : int {
        Stop = 0,
        Restart = 1,
        Collect = 2,
        Count = 3,
        Countb = 4,
        Step = 5,
        SetPause = 6,
        SetStepMul = 7,
    }

    public enum LuaResult : int {
        OK = 0,
        Yield = 1,
        ErrRun = 2,
        ErrSyntax = 3,
        ErrMem = 4,
        ErrErr = 5
    }

    [UnmanagedFunctionPointer(Lua.CALLING_CONVENTION)]
    public delegate IntPtr lua_Alloc(IntPtr ud, IntPtr ptr, IntPtr osize, IntPtr nsize);

    [UnmanagedFunctionPointer(Lua.CALLING_CONVENTION)]
    public delegate int lua_CFunction(IntPtr L);

    [UnmanagedFunctionPointer(Lua.CALLING_CONVENTION)]
    public delegate int lua_Writer(IntPtr L, IntPtr p, UIntPtr sz, IntPtr ud);

    [UnmanagedFunctionPointer(Lua.CALLING_CONVENTION)]
    public delegate int lua_Reader(IntPtr L, IntPtr data, UIntPtr size);

    public static class Lua {
        internal const CallingConvention CALLING_CONVENTION = CallingConvention.Cdecl;
        internal const string DLL_NAME = "lua5.1";

        public const int LUA_REGISTRYINDEX = -10000;
        public const int LUA_ENVIRONINDEX = -10001;
        public const int LUA_GLOBALSINDEX = -10002;
        public const int LUA_MULTRET = -1;

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern lua_CFunction lua_atpanic(IntPtr L, lua_CFunction panicf);

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern void lua_call(IntPtr L, int nargs, int nresults);

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern int lua_checkstack(IntPtr L, int extra);

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern void lua_close(IntPtr L);

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern void lua_concat(IntPtr L, int n);

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern int lua_cpcall(IntPtr L, lua_CFunction func, IntPtr ud);

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern LuaResult lua_pcall(IntPtr L, int nargs, int nresults, int errfunc);

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern void lua_createtable(IntPtr L, int narr, int nrec);

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern int lua_dump(IntPtr L, lua_Writer writer, IntPtr data);

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern int lua_equal(IntPtr L, int index1, int index2);

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern int lua_error(IntPtr L);

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern int lua_gc(IntPtr L, LuaGcOperation what, int data);

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern lua_Alloc lua_getallocf(IntPtr L, IntPtr ud);

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern void lua_getfield(IntPtr L, int index, [MarshalAs(UnmanagedType.LPStr)] string k);

        public static void lua_getglobal(IntPtr L, string name) {
            lua_getfield(L, LUA_GLOBALSINDEX, name);
        }

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern void lua_setfield(IntPtr L, int index, [MarshalAs(UnmanagedType.LPStr)] string k);

        public static void lua_setglobal(IntPtr L, string name) {
            lua_setfield(L, LUA_GLOBALSINDEX, name);
        }

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern int lua_getmetatable(IntPtr L, int index);

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern void lua_gettable(IntPtr L, int index);

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern void lua_settable(IntPtr L, int index);

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern int lua_gettop(IntPtr L);

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern void lua_insert(IntPtr L, int index);

        public static bool lua_isboolean(IntPtr L, int n) {
            return lua_type(L, (n)) == LuaType.Boolean;
        }

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern bool lua_iscfunction(IntPtr L, int index);

        public static bool lua_isfunction(IntPtr L, int n) {
            return lua_type(L, (n)) == LuaType.Function;
        }

        public static bool lua_islightuserdata(IntPtr L, int n) {
            return lua_type(L, (n)) == LuaType.LightUserdata;
        }

        public static bool lua_isnil(IntPtr L, int n) {
            return lua_type(L, (n)) == LuaType.Nil;
        }

        public static bool lua_isnone(IntPtr L, int n) {
            return lua_type(L, (n)) == LuaType.None;
        }

        public static bool lua_isnoneornil(IntPtr L, int n) {
            return lua_type(L, (n)) <= 0;
        }

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern bool lua_isnumber(IntPtr L, int index);

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern bool lua_isstring(IntPtr L, int index);

        public static bool lua_istable(IntPtr L, int n) {
            return lua_type(L, (n)) == LuaType.Table;
        }

        public static bool lua_isthread(IntPtr L, int n) {
            return lua_type(L, (n)) == LuaType.Thread;
        }

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern bool lua_isuserdata(IntPtr L, int index);

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern bool lua_lessthan(IntPtr L, int index1, int index2);

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern LuaResult lua_load(IntPtr L, lua_Reader reader, IntPtr data, [MarshalAs(UnmanagedType.LPStr)] string chunkname);

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern IntPtr lua_newstate(lua_Alloc f, IntPtr ud);

        public static void lua_newtable(IntPtr L) {
            lua_createtable(L, 0, 0);
        }

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern IntPtr lua_newthread(IntPtr L);

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern IntPtr lua_newuserdata(IntPtr L, UIntPtr size);

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern int lua_next(IntPtr L, int index);

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern UIntPtr lua_objlen(IntPtr L, int index);

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern LuaType lua_type(IntPtr L, int index);

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern void lua_settop(IntPtr L, int index);

        public static void lua_pop(IntPtr L, int n) {
            lua_settop(L, -(n) - 1);
        }

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern void lua_pushboolean(IntPtr L, bool b);

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern void lua_pushcclosure(IntPtr L, lua_CFunction fn, int n);

        public static void lua_pushcfunction(IntPtr L, lua_CFunction fn) {
            lua_pushcclosure(L, fn, 0);
        }

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern void lua_pushinteger(IntPtr L, IntPtr n);

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern void lua_pushlightuserdata(IntPtr L, IntPtr p);

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern void lua_pushlstring(IntPtr L, IntPtr s, int len);

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern void lua_pushnil(IntPtr L);

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern void lua_pushnumber(IntPtr L, double n);

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern int lua_pushthread(IntPtr L);

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern void lua_pushvalue(IntPtr L, int index);

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern bool lua_toboolean(IntPtr L, int index);

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern lua_CFunction lua_tocfunction(IntPtr L, int index);

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern long lua_tointeger(IntPtr L, int index);

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern IntPtr lua_tolstring(IntPtr L, int index, ref UIntPtr len);

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern double lua_tonumber(IntPtr L, int index);

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern IntPtr lua_topointer(IntPtr L, int index);

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern IntPtr lua_tothread(IntPtr L, int index);

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern IntPtr lua_touserdata(IntPtr L, int index);

        // lauxlib

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern IntPtr luaL_newstate();

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern void luaL_openlibs(IntPtr L);

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern LuaResult luaL_loadbuffer(IntPtr L, [MarshalAs(UnmanagedType.LPStr)] string buff, UIntPtr sz, [MarshalAs(UnmanagedType.LPStr)] string name);

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern LuaResult luaL_loadfile(IntPtr L, [MarshalAs(UnmanagedType.LPStr)] string filename);

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern LuaResult luaL_loadstring(IntPtr L, [MarshalAs(UnmanagedType.LPStr)] string s);

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern int luaL_newmetatable(IntPtr L, [MarshalAs(UnmanagedType.LPStr)] string tname);

        public static void luaL_getmetatable(IntPtr L, string tname) {
            lua_getfield(L, LUA_REGISTRYINDEX, tname);
        }

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern int lua_setmetatable(IntPtr L, int index);

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern bool lua_rawequal(IntPtr L, int index1, int index2);

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern void lua_remove(IntPtr L, int index);

        public static int lua_dostring(IntPtr L, string str) {
            if (luaL_loadstring(L, str) == LuaResult.OK) {
                lua_pcall(L, 0, LUA_MULTRET, 0);
                return 0;
            } else return 1;
        }

        public static int lua_upvalueindex(int idx) {
            return LUA_GLOBALSINDEX - idx;
        }

        public static int abs_index(IntPtr L, int i) {
            return (i > 0 || i <= LUA_REGISTRYINDEX) ? i : lua_gettop(L) + i + 1;
        }

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern void lua_rawgeti(IntPtr L, int index, int n);

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern void lua_rawseti(IntPtr L, int index, int n);

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern bool lua_setfenv(IntPtr L, int index);

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        public static extern void lua_getfenv(IntPtr L, int index);
    }
}
