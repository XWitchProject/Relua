using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using Relua.AST;

namespace Relua.Script {
    public partial class ReluaRuntime {
        public IntPtr LuaStatePtr;

        public ReluaRuntime() {
            LuaStatePtr = Lua.luaL_newstate();
            Lua.luaL_openlibs(LuaStatePtr);
            CreateListMethodMaps();
            CreateMetatables();
            AddASTFunctions();
        }

        private int LuaObjectToString(IntPtr state) {
            var refid = ToReference();
            var obj = ResolveReference(refid);
            PushString(obj.ToString());
            return 1;
        }

        private int LuaObjectFinalizer(IntPtr state) {
            RemoveReference(ToReference());
            return 0;
        }

        private int LuaNodeIndex(IntPtr state) {
            var key = ToString(-1);
            Lua.lua_pop(state, 1);
            var refid = ToReference();

            var node = ResolveReference(refid);

            if (key == "Type") {
                if (node is UnaryOp) {
                    PushString(((UnaryOp)node).Type.ToString());
                    return 1;
                } else if (node is BinaryOp) {
                    PushString(((BinaryOp)node).Type.ToString());
                    return 1;
                } else {
                    Lua.lua_pushnil(LuaStatePtr);
                    return 1;
                }
            }

            if (TypeFieldMap.TryGetValue(node.GetType(), out Dictionary<string, FieldInfo> map)) {
                if (map.TryGetValue(key, out FieldInfo info)) {
                    Push(info.GetValue(node));
                    return 1;
                }
            }

            Lua.lua_pushnil(LuaStatePtr);
            return 1;
        }

        private int LuaNodeNewIndex(IntPtr state) {
            var val = ToObject();
            Lua.lua_pop(state, 1);
            var key = ToString(-1);
            Lua.lua_pop(state, 1);
            var refid = ToReference();

            var node = ResolveReference(refid);

            if (key == "Type") {
                if (node is UnaryOp) {
                    ((UnaryOp)node).Type = (UnaryOp.OpType)Enum.Parse(typeof(UnaryOp.OpType), val.ToString());
                    return 0;
                } else if (node is BinaryOp) {
                    ((BinaryOp)node).Type = (BinaryOp.OpType)Enum.Parse(typeof(BinaryOp.OpType), val.ToString());
                    return 0;
                }
            }

            if (TypeFieldMap.TryGetValue(node.GetType(), out Dictionary<string, FieldInfo> map)) {
                if (map.TryGetValue(key, out FieldInfo info)) {
                    info.SetValue(node, val);
                    return 0;
                }
            }

            return 0;
        }

        private int LuaListReverseIter(IntPtr state) {
            if (Lua.lua_type(state, -1) != LuaType.Function) throw new Exception($":reverse_iter must be given a function");
            var refid = ToReference(-2);
            var list = ResolveReference(refid);

            var count = (int)ListCountMethodMap[list.GetType()].Invoke(list, null);
            for (int i = count - 1; i >= 0; i--) {
                Lua.lua_pushvalue(state, -1);
                Lua.lua_pushinteger(state, (IntPtr)i + 1);
                var obj = ListIndexMethodMap[list.GetType()].Invoke(list, new object[] { i });
                PushObject(obj);
                ProtCall(2, 0);
            }

            Lua.lua_pop(state, 2);

            return 0;
        }

        private int LuaListIter(IntPtr state) {
            if (Lua.lua_type(state, -1) != LuaType.Function) throw new Exception($":iter must be given a function");
            var refid = ToReference(-2);
            var list = ResolveReference(refid);

            var count = (int)ListCountMethodMap[list.GetType()].Invoke(list, null);
            for (int i = 0; i < count; i++) {
                Lua.lua_pushvalue(state, -1);
                Lua.lua_pushinteger(state, (IntPtr)i + 1);
                var obj = ListIndexMethodMap[list.GetType()].Invoke(list, new object[] { i });
                PushObject(obj);
                ProtCall(2, 0);
            }

            Lua.lua_pop(state, 2);

            return 0;
        }

        private int LuaListClear(IntPtr state) {
            var refid = ToReference();
            var list = ResolveReference(refid);
            Lua.lua_pop(state, 1);

            ListClearMethodMap[list.GetType()].Invoke(list, null);

            return 0;
        }

        private int LuaListInsert(IntPtr state) {
            var valid = ToReference();
            var val = ResolveReference(valid);
            Lua.lua_pop(state, 1);
            var idx = (int)Lua.lua_tointeger(state, -1) - 1;
            if (Lua.lua_type(state, -1) != LuaType.Number) throw new Exception("Invalid index");
            Lua.lua_pop(state, 1);
            var refid = ToReference();
            var list = ResolveReference(refid);
            Lua.lua_pop(state, 1);

            ListInsertMethodMap[list.GetType()].Invoke(list, new object[] { idx, val });

            return 0;
        }

        private int LuaListRemove(IntPtr state) {
            var idx = (int)Lua.lua_tointeger(state, -1) - 1;
            if (Lua.lua_type(state, -1) != LuaType.Number) throw new Exception("Invalid index");
            Lua.lua_pop(state, 1);
            var refid = ToReference();
            var list = ResolveReference(refid);
            Lua.lua_pop(state, 1);

            ListRemoveMethodMap[list.GetType()].Invoke(list, new object[] { idx });

            return 0;
        }

        private int LuaListAdd(IntPtr state) {
            var valid = ToReference();
            var val = ResolveReference(valid);
            Lua.lua_pop(state, 1);
            var refid = ToReference();
            var list = ResolveReference(refid);
            Lua.lua_pop(state, 1);

            ListAddMethodMap[list.GetType()].Invoke(list, new object[] { val });

            return 0;
        }

        private int LuaListFieldIndex(IntPtr state) {
            var field_name = ToString(-1);
            Lua.lua_pop(state, 1);
            var refid = ToReference();
            var list = ResolveReference(refid);
            Lua.lua_pop(state, 1);

            if (field_name == "count") {
                var type = list.GetType();
                var count_method = ListCountMethodMap[type];
                var count = (int)count_method.Invoke(list, null);
                Lua.lua_pushinteger(state, (IntPtr)count);
            } else if (field_name == "add") {
                Lua.lua_pushcfunction(state, LuaListAdd);
            } else if (field_name == "remove") {
                Lua.lua_pushcfunction(state, LuaListRemove);
            } else if (field_name == "insert") {
                Lua.lua_pushcfunction(state, LuaListInsert);
            } else if (field_name == "clear") {
                Lua.lua_pushcfunction(state, LuaListClear);
            } else if (field_name == "iter") {
                Lua.lua_pushcfunction(state, LuaListIter);
            } else if (field_name == "reverse_iter") {
                Lua.lua_pushcfunction(state, LuaListReverseIter);
            } else {
                Lua.lua_pushnil(state);
            }

            return 1;
        }

        private int LuaListIndex(IntPtr state) {
            if (Lua.lua_type(state, -1) == LuaType.String) {
                return LuaListFieldIndex(state);
            }
            var idx = (int)Lua.lua_tointeger(state, -1) - 1;
            if (Lua.lua_type(state, -1) != LuaType.Number) throw new Exception("Invalid index");
            Lua.lua_pop(state, 1);
            var refid = ToReference();
            var list = ResolveReference(refid);
            Lua.lua_pop(state, 1);

            var type = list.GetType();
            var count_method = ListCountMethodMap[type];
            var count = (int)count_method.Invoke(list, null);
            if (idx < 0 || idx >= count) {
                Lua.lua_pushnil(state);
                return 1;
            }
            var indexer = ListIndexMethodMap[type];

            Push(indexer.Invoke(list, new object[] { idx }));

            return 1;
        }

        private int LuaListNewIndex(IntPtr state) {
            var valid = ToReference();
            var val = ResolveReference(valid);
            Lua.lua_pop(state, 1);
            if (Lua.lua_type(state, -1) != LuaType.Number) throw new Exception("Invalid index");
            var idx = (int)Lua.lua_tointeger(state, -1) - 1;
            Lua.lua_pop(state, 1);
            var refid = ToReference();
            var list = ResolveReference(refid);
            Lua.lua_pop(state, 1);

            var type = list.GetType();
            var count_method = ListCountMethodMap[type];
            var count = (int)count_method.Invoke(list, null);
            if (idx < 0 || idx >= count) {
                throw new Exception($"Invalid index: {idx}");
            }
            var indexer = ListNewIndexMethodMap[type];

            indexer.Invoke(list, new object[] { idx, val });
            return 0;
        }

        public void ProtCall(int nargs, int nreturn) {
            var result = Lua.lua_pcall(LuaStatePtr, nargs, nreturn, 0);
            if (result != LuaResult.OK) {
                var msg = ToString(-1);
                throw new ReluaScriptException(msg);
            }
        }

        public void DoFile(string path) {
            var result = Lua.luaL_loadfile(LuaStatePtr, path);
            if (result != LuaResult.OK) {
                throw new ReluaScriptException(ToString(-1));
            }
            ProtCall(0, 0);
        }

        public void DoString(string str) {
            var result = Lua.luaL_loadstring(LuaStatePtr, str);
            if (result != LuaResult.OK) {
                throw new ReluaScriptException(ToString(-1));
            }
            ProtCall(0, 0);
        }
    }
}
