using System;
using System.Collections.Generic;
using System.IO;
using Relua.AST;
using XTRuntime;

namespace Relua.Script {
    public partial class LuaVisitor {
        private void CreateListMethodMaps() {
            Runtime.CreateListMethodMap(typeof(List<IStatement>));
            Runtime.CreateListMethodMap(typeof(List<IExpression>));
            Runtime.CreateListMethodMap(typeof(List<IAssignable>));
            Runtime.CreateListMethodMap(typeof(List<TableConstructor.Entry>));
            Runtime.CreateListMethodMap(typeof(List<string>));
            Runtime.CreateListMethodMap(typeof(List<ConditionalBlock>));
        }

        private void CreateMetatables() {
            Runtime.CreateNodeMetatable(typeof(Variable), "Variable");
            Runtime.CreateNodeMetatable(typeof(NilLiteral), "NilLiteral");
            Runtime.CreateNodeMetatable(typeof(VarargsLiteral), "VarargsLiteral");
            Runtime.CreateNodeMetatable(typeof(BoolLiteral), "BoolLiteral");

            Runtime.CreateNodeMetatable(typeof(UnaryOp), "UnaryOp");
            Runtime.RegisterSpecialIndexFunc(typeof(UnaryOp), "Type", (unaryop) => {
                Runtime.PushString(((UnaryOp)unaryop).Type.ToString());
                return 1;
            });
            Runtime.RegisterSpecialNewIndexFunc(typeof(UnaryOp), "Type", (unaryop, val) => {
                ((UnaryOp)unaryop).Type = (UnaryOp.OpType)Enum.Parse(typeof(UnaryOp.OpType), val.ToString());
                return 0;
            });

            Runtime.CreateNodeMetatable(typeof(BinaryOp), "BinaryOp");
            Runtime.RegisterSpecialIndexFunc(typeof(BinaryOp), "Type", (binop) => {
                Runtime.PushString(((BinaryOp)binop).Type.ToString());
                return 1;
            });
            Runtime.RegisterSpecialNewIndexFunc(typeof(BinaryOp), "Type", (binop, val) => {
                ((BinaryOp)binop).Type = (BinaryOp.OpType)Enum.Parse(typeof(BinaryOp.OpType), val.ToString());
                return 0;
            });

            Runtime.CreateNodeMetatable(typeof(StringLiteral), "StringLiteral");
            Runtime.CreateNodeMetatable(typeof(NumberLiteral), "NumberLiteral");
            Runtime.CreateNodeMetatable(typeof(LuaJITLongLiteral), "LuaJITLongLiteral");
            Runtime.CreateNodeMetatable(typeof(TableAccess), "TableAccess");
            Runtime.CreateNodeMetatable(typeof(FunctionCall), "FunctionCall");
            Runtime.CreateNodeMetatable(typeof(TableConstructor), "TableConstructor");
            Runtime.CreateNodeMetatable(typeof(TableConstructor.Entry), "TableConstructorEntry");
            Runtime.CreateNodeMetatable(typeof(Break), "Break");
            Runtime.CreateNodeMetatable(typeof(Return), "Return");
            Runtime.CreateNodeMetatable(typeof(Block), "Block");
            Runtime.CreateNodeMetatable(typeof(ConditionalBlock), "ConditionalBlock");
            Runtime.CreateNodeMetatable(typeof(If), "If");
            Runtime.CreateNodeMetatable(typeof(While), "While");
            Runtime.CreateNodeMetatable(typeof(Repeat), "Repeat");
            Runtime.CreateNodeMetatable(typeof(FunctionDefinition), "FunctionDefinition");
            Runtime.CreateNodeMetatable(typeof(Assignment), "Assignment");
            Runtime.CreateNodeMetatable(typeof(NumericFor), "NumericFor");
            Runtime.CreateNodeMetatable(typeof(GenericFor), "GenericFor");

            Runtime.CreateListMetatable(typeof(List<IStatement>), "List<IStatement>");
            Runtime.CreateListMetatable(typeof(List<IExpression>), "List<IExpression>");
            Runtime.CreateListMetatable(typeof(List<IAssignable>), "List<IAssignable>");
            Runtime.CreateListMetatable(typeof(List<TableConstructor.Entry>), "List<TableConstructor.Entry>");
            Runtime.CreateListMetatable(typeof(List<string>), "List<string>");
            Runtime.CreateListMetatable(typeof(List<ConditionalBlock>), "List<ConditionalBlock>");
        }

        private int ASTNew(IntPtr state) {
            var s = Runtime.ToString(-1);
            if (!Runtime.MetatableMap.ContainsKey(s)) throw new Exception($"Non-existant type: '{s}'");
            var inst = Activator.CreateInstance(Runtime.MetatableMap[s]);
            if (!(inst is Node)) throw new Exception($"Tried to instantiate non-Node type: '{s}'");
            Runtime.PushObject(inst);
            return 1;
        }

        private int ASTExpr(IntPtr state) {
            var s = Runtime.ToString(-1);
            var parser = new Parser(s);
            Runtime.PushObject(parser.ReadExpression());
            return 1;
        }

        private int ASTStat(IntPtr state) {
            var s = Runtime.ToString(-1);
            var parser = new Parser(s);
            Runtime.PushObject(parser.ReadStatement());
            return 1;
        }

        private int ASTFile(IntPtr state) {
            var s = Runtime.ToString(-1);
            var parser = new Parser(File.ReadAllText(s));
            Runtime.PushObject(parser.Read());
            return 1;
        }

        private int ASTType(IntPtr state) {
            var reference = Runtime.ResolveReference(Runtime.ToReference());
            Runtime.PushString(Runtime.GetTypeMetatable(reference.GetType()));
            return 1;
        }

        private void AddASTFunctions() {
            Lua.lua_pushcfunction(Runtime.LuaStatePtr, ASTNew);
            Lua.lua_setglobal(Runtime.LuaStatePtr, "astnew");
            Lua.lua_pushcfunction(Runtime.LuaStatePtr, ASTExpr);
            Lua.lua_setglobal(Runtime.LuaStatePtr, "astexpr");
            Lua.lua_pushcfunction(Runtime.LuaStatePtr, ASTStat);
            Lua.lua_setglobal(Runtime.LuaStatePtr, "aststat");
            Lua.lua_pushcfunction(Runtime.LuaStatePtr, ASTFile);
            Lua.lua_setglobal(Runtime.LuaStatePtr, "astfile");
            Lua.lua_pushcfunction(Runtime.LuaStatePtr, ASTType);
            Lua.lua_setglobal(Runtime.LuaStatePtr, "asttype");
        }
    }
}
