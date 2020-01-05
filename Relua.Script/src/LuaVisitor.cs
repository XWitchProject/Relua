using System;
using Relua;
using Relua.AST;
using System.Collections.Generic;
using XTRuntime;

namespace Relua.Script {
    public partial class LuaVisitor : IVisitor {
        public XTRuntime.XTRuntime Runtime;

        public HashSet<string> UnavailableFunctions = new HashSet<string>();
        public HashSet<string> AvailableFunctions = new HashSet<string>();

        public LuaVisitor(XTRuntime.XTRuntime runtime) {
            Runtime = runtime;
            AddVisitorFunctions();
            CreateListMethodMaps();
            CreateMetatables();
            AddASTFunctions();
        }

        public bool FunctionExists(string name) {
            if (AvailableFunctions.Contains(name)) return true;
            if (UnavailableFunctions.Contains(name)) return false;

            Lua.lua_getglobal(Runtime.LuaStatePtr, name);
            if (Lua.lua_type(Runtime.LuaStatePtr, -1) != LuaType.Function) {
                Lua.lua_pop(Runtime.LuaStatePtr, 1);
                UnavailableFunctions.Add(name);
                return false;
            }

            AvailableFunctions.Add(name);
            Lua.lua_pop(Runtime.LuaStatePtr, 1);
            return true;
        }

        private int Enter(IntPtr state) {
            var valid = Runtime.ToReference();
            var val = Runtime.ResolveReference(valid);
            Lua.lua_pop(state, 1);

            if (!(val is Node)) throw new Exception($"Cannot enter an object that isn't a Node. ({val})");

            ((Node)val).Accept(this);

            return 0;
        }

        private void AddVisitorFunctions() {
            Lua.lua_pushcfunction(Runtime.LuaStatePtr, Enter);
            Lua.lua_setglobal(Runtime.LuaStatePtr, "enter");
        }

        private void VisitLuaFunction<T>(T obj) {
            var name = obj.GetType().Name;
            if (obj is TableConstructor.Entry) name = "TableConstructorEntry";
            if (FunctionExists(name)) {
                Lua.lua_getglobal(Runtime.LuaStatePtr, name);
                Runtime.PushObject(obj);
                Runtime.ProtCall(1, 0);
            }
        }

        private T NotNull<T>(T obj) {
            if (obj == null) throw new Exception($"Object may not be null");
            return obj;
        }

        public void Visit(Variable node) {
            VisitLuaFunction(node);
        }

        public void Visit(NilLiteral node) {
            VisitLuaFunction(node);
        }

        public void Visit(VarargsLiteral node) {
            VisitLuaFunction(node);
        }

        public void Visit(BoolLiteral node) {
            VisitLuaFunction(node);
        }

        public void Visit(UnaryOp node) {
            VisitLuaFunction(node);
        }

        public void Visit(BinaryOp node) {
            VisitLuaFunction(node);
        }

        public void Visit(StringLiteral node) {
            VisitLuaFunction(node);
        }

        public void Visit(NumberLiteral node) {
            VisitLuaFunction(node);
        }

        public void Visit(LuaJITLongLiteral node) {
            VisitLuaFunction(node);
        }

        public void Visit(TableAccess node) {
            VisitLuaFunction(node);
        }

        public void Visit(FunctionCall node) {
            VisitLuaFunction(node);
        }

        public void Visit(TableConstructor node) {
            VisitLuaFunction(node);
        }

        public void Visit(TableConstructor.Entry node) {
            VisitLuaFunction(node);
        }

        public void Visit(Break node) {
            VisitLuaFunction(node);
        }

        public void Visit(Return node) {
            VisitLuaFunction(node);
        }

        public void Visit(Block node) {
            VisitLuaFunction(node);
        }

        public void Visit(ConditionalBlock node) {
            VisitLuaFunction(node);
        }

        public void Visit(If node) {
            VisitLuaFunction(node);
        }

        public void Visit(While node) {
            VisitLuaFunction(node);
        }

        public void Visit(Repeat node) {
            VisitLuaFunction(node);
        }

        public void Visit(FunctionDefinition node) {
            VisitLuaFunction(node);
        }

        public void Visit(Assignment node) {
            VisitLuaFunction(node);
        }

        public void Visit(NumericFor node) {
            VisitLuaFunction(node);
        }

        public void Visit(GenericFor node) {
            VisitLuaFunction(node);
        }
    }
}
