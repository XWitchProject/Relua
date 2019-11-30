using System;
using System.IO;
using Relua.AST;

namespace Relua.Script{
    public partial class ReluaRuntime {
        private int ASTNew(IntPtr state) {
            var s = ToString(-1);
            if (!MetatableMap.ContainsKey(s)) throw new Exception($"Non-existant type: '{s}'");
            var inst = Activator.CreateInstance(MetatableMap[s]);
            if (!(inst is Node)) throw new Exception($"Tried to instantiate non-Node type: '{s}'");
            PushObject(inst);
            return 1;
        }

        private int ASTExpr(IntPtr state) {
            var s = ToString(-1);
            var parser = new Parser(s);
            PushObject(parser.ReadExpression());
            return 1;
        }

        private int ASTStat(IntPtr state) {
            var s = ToString(-1);
            var parser = new Parser(s);
            PushObject(parser.ReadStatement());
            return 1;
        }

        private int ASTFile(IntPtr state) {
            var s = ToString(-1);
            var parser = new Parser(File.ReadAllText(s));
            PushObject(parser.Read());
            return 1;
        }

        private int ASTType(IntPtr state) {
            var reference = ResolveReference(ToReference());
            PushString(GetTypeMetatable(reference.GetType()));
            return 1;
        }

        private void AddASTFunctions() {
            Lua.lua_pushcfunction(LuaStatePtr, ASTNew);
            Lua.lua_setglobal(LuaStatePtr, "astnew");
            Lua.lua_pushcfunction(LuaStatePtr, ASTExpr);
            Lua.lua_setglobal(LuaStatePtr, "astexpr");
            Lua.lua_pushcfunction(LuaStatePtr, ASTStat);
            Lua.lua_setglobal(LuaStatePtr, "aststat");
            Lua.lua_pushcfunction(LuaStatePtr, ASTFile);
            Lua.lua_setglobal(LuaStatePtr, "astfile");
            Lua.lua_pushcfunction(LuaStatePtr, ASTType);
            Lua.lua_setglobal(LuaStatePtr, "asttype");
        }
    }
}
