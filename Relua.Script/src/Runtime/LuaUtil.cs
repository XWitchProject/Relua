using System;
using System.Runtime.InteropServices;

namespace Relua.Script {
    public partial class ReluaRuntime {
        public int ToReference(int index = -1) {
            var ud = Lua.lua_touserdata(LuaStatePtr, index);
            if (ud == IntPtr.Zero) {
                throw new Exception($"Tried to get reference to a null object.");
            }
            var refidx = Marshal.ReadIntPtr(ud);
            return (int)refidx;
        }

        public string ToString(int index = -1) {
            var len = UIntPtr.Zero;
            var ptr = Lua.lua_tolstring(LuaStatePtr, index, ref len);
            if (ptr == IntPtr.Zero) {
                return null;
            }
            var str = Marshal.PtrToStringAnsi(ptr, (int)len);

            return str;
        }

        public int PushObject(object obj) {
            var reference = CreateReference(obj);
            var ud = Lua.lua_newuserdata(LuaStatePtr, (UIntPtr)Marshal.SizeOf(typeof(IntPtr)));
            Marshal.WriteIntPtr(ud, new IntPtr(reference));
            Lua.luaL_getmetatable(LuaStatePtr, GetTypeMetatable(obj.GetType()));
            Lua.lua_setmetatable(LuaStatePtr, -2);
            return reference;
        }

        public void PushString(string s) {
            IntPtr sptr = Marshal.AllocHGlobal(s.Length + 1);
            for (int i = 0; i < s.Length; i++) {
                Marshal.WriteByte(sptr, i, (byte)s[i]);
            }
            Marshal.WriteByte(sptr, s.Length, 0);

            Lua.lua_pushlstring(LuaStatePtr, sptr, s.Length);
        }

        public void Push(object o) {
            if (o == null) Lua.lua_pushnil(LuaStatePtr);
            else if (o is string) PushString((string)o);
            else if (o is bool) Lua.lua_pushboolean(LuaStatePtr, (bool)o);
            else if (o is int || o is long) Lua.lua_pushinteger(LuaStatePtr, (IntPtr)o);
            else if (o is float || o is double) Lua.lua_pushnumber(LuaStatePtr, (double)o);
            else {
                PushObject(o);
            }
        }

        public object ToObject() {
            var type = Lua.lua_type(LuaStatePtr, -1);
            if (type == LuaType.String) return ToString(-1);
            else if (type == LuaType.Userdata) return ResolveReference(ToReference());
            else if (type == LuaType.Boolean) return Lua.lua_toboolean(LuaStatePtr, -1);
            else if (type == LuaType.Number) return Lua.lua_tonumber(LuaStatePtr, -1);
            else throw new Exception($"Invalid Lua type: {type}");
        }
    }
}
