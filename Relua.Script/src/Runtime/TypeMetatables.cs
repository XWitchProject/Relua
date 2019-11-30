using System;
using System.Collections.Generic;
using System.Reflection;
using Relua.AST;

namespace Relua.Script {
    public partial class ReluaRuntime {
        public Dictionary<string, Type> MetatableMap = new Dictionary<string, Type>();

        public Dictionary<Type, Dictionary<string, FieldInfo>> TypeFieldMap = new Dictionary<Type, Dictionary<string, FieldInfo>>();
        public Dictionary<Type, MethodInfo> ListIndexMethodMap = new Dictionary<Type, MethodInfo>();
        public Dictionary<Type, MethodInfo> ListNewIndexMethodMap = new Dictionary<Type, MethodInfo>();
        public Dictionary<Type, MethodInfo> ListCountMethodMap = new Dictionary<Type, MethodInfo>();
        public Dictionary<Type, MethodInfo> ListAddMethodMap = new Dictionary<Type, MethodInfo>();
        public Dictionary<Type, MethodInfo> ListRemoveMethodMap = new Dictionary<Type, MethodInfo>();
        public Dictionary<Type, MethodInfo> ListInsertMethodMap = new Dictionary<Type, MethodInfo>();
        public Dictionary<Type, MethodInfo> ListClearMethodMap = new Dictionary<Type, MethodInfo>();

        private void CreateListMethodMap(Type type) {
            ListIndexMethodMap[type] = type.GetMethod("get_Item");
            ListNewIndexMethodMap[type] = type.GetMethod("set_Item");
            ListCountMethodMap[type] = type.GetMethod("get_Count");
            ListAddMethodMap[type] = type.GetMethod("Add");
            ListRemoveMethodMap[type] = type.GetMethod("RemoveAt");
            ListInsertMethodMap[type] = type.GetMethod("Insert");
            ListClearMethodMap[type] = type.GetMethod("Clear");
        }

        private void CreateListMethodMaps() {
            CreateListMethodMap(typeof(List<IStatement>));
            CreateListMethodMap(typeof(List<IExpression>));
            CreateListMethodMap(typeof(List<IAssignable>));
            CreateListMethodMap(typeof(List<TableConstructor.Entry>));
            CreateListMethodMap(typeof(List<string>));
            CreateListMethodMap(typeof(List<ConditionalBlock>));
        }

        private void CreateGenericMetamethods() {
            Lua.lua_pushcfunction(LuaStatePtr, LuaObjectFinalizer);
            Lua.lua_setfield(LuaStatePtr, -2, "__gc");
            Lua.lua_pushcfunction(LuaStatePtr, LuaObjectToString);
            Lua.lua_setfield(LuaStatePtr, -2, "__tostring");
        }

        private void CreateListMetatable(Type type, string name) {
            MetatableMap[name] = type;
            Lua.luaL_newmetatable(LuaStatePtr, name);
            CreateGenericMetamethods();
            Lua.lua_pushcfunction(LuaStatePtr, LuaListIndex);
            Lua.lua_setfield(LuaStatePtr, -2, "__index");
            Lua.lua_pushcfunction(LuaStatePtr, LuaListNewIndex);
            Lua.lua_setfield(LuaStatePtr, -2, "__newindex");
        }

        private void CreateNodeMetatable(Type type, string name) {
            MetatableMap[name] = type;
            Lua.luaL_newmetatable(LuaStatePtr, name);
            CreateGenericMetamethods();

            var map = TypeFieldMap[type] = new Dictionary<string, FieldInfo>();
            var fields = type.GetFields();
            for (var i = 0; i < fields.Length; i++) {
                var field = fields[i];
                map[field.Name] = field;
            }

            Lua.lua_pushcfunction(LuaStatePtr, LuaNodeIndex);
            Lua.lua_setfield(LuaStatePtr, -2, "__index");
            Lua.lua_pushcfunction(LuaStatePtr, LuaNodeNewIndex);
            Lua.lua_setfield(LuaStatePtr, -2, "__newindex");
        }

        private string GetTypeMetatable(Type type) {
            if (type == typeof(Variable)) return "Variable";
            if (type == typeof(NilLiteral)) return "NilLiteral";
            if (type == typeof(VarargsLiteral)) return "VarargsLiteral";
            if (type == typeof(BoolLiteral)) return "BoolLiteral";
            if (type == typeof(UnaryOp)) return "UnaryOp";
            if (type == typeof(BinaryOp)) return "BinaryOp";
            if (type == typeof(StringLiteral)) return "StringLiteral";
            if (type == typeof(NumberLiteral)) return "NumberLiteral";
            if (type == typeof(LuaJITLongLiteral)) return "LuaJITLongLiteral";
            if (type == typeof(TableAccess)) return "TableAccess";
            if (type == typeof(FunctionCall)) return "FunctionCall";
            if (type == typeof(TableConstructor)) return "TableConstructor";
            if (type == typeof(TableConstructor.Entry)) return "TableConstructorEntry";
            if (type == typeof(Break)) return "Break";
            if (type == typeof(Return)) return "Return";
            if (type == typeof(Block)) return "Block";
            if (type == typeof(ConditionalBlock)) return "ConditionalBlock";
            if (type == typeof(If)) return "If";
            if (type == typeof(While)) return "While";
            if (type == typeof(Repeat)) return "Repeat";
            if (type == typeof(FunctionDefinition)) return "FunctionDefinition";
            if (type == typeof(Assignment)) return "Assignment";
            if (type == typeof(NumericFor)) return "NumericFor";
            if (type == typeof(GenericFor)) return "GenericFor";

            if (type == typeof(List<IStatement>)) return "List<IStatement>";
            if (type == typeof(List<IExpression>)) return "List<IExpression>";
            if (type == typeof(List<IAssignable>)) return "List<IAssignable>";
            if (type == typeof(List<TableConstructor.Entry>)) return "List<TableConstructor.Entry>";
            if (type == typeof(List<string>)) return "List<string>";
            if (type == typeof(List<ConditionalBlock>)) return "List<ConditionalBlock>";

            throw new Exception($"Unsupported type: '{type}'");
        }

        private void CreateMetatables() {
            CreateNodeMetatable(typeof(Variable), "Variable");
            CreateNodeMetatable(typeof(NilLiteral), "NilLiteral");
            CreateNodeMetatable(typeof(VarargsLiteral), "VarargsLiteral");
            CreateNodeMetatable(typeof(BoolLiteral), "BoolLiteral");
            CreateNodeMetatable(typeof(UnaryOp), "UnaryOp");
            CreateNodeMetatable(typeof(BinaryOp), "BinaryOp");
            CreateNodeMetatable(typeof(StringLiteral), "StringLiteral");
            CreateNodeMetatable(typeof(NumberLiteral), "NumberLiteral");
            CreateNodeMetatable(typeof(LuaJITLongLiteral), "LuaJITLongLiteral");
            CreateNodeMetatable(typeof(TableAccess), "TableAccess");
            CreateNodeMetatable(typeof(FunctionCall), "FunctionCall");
            CreateNodeMetatable(typeof(TableConstructor), "TableConstructor");
            CreateNodeMetatable(typeof(TableConstructor.Entry), "TableConstructorEntry");
            CreateNodeMetatable(typeof(Break), "Break");
            CreateNodeMetatable(typeof(Return), "Return");
            CreateNodeMetatable(typeof(Block), "Block");
            CreateNodeMetatable(typeof(ConditionalBlock), "ConditionalBlock");
            CreateNodeMetatable(typeof(If), "If");
            CreateNodeMetatable(typeof(While), "While");
            CreateNodeMetatable(typeof(Repeat), "Repeat");
            CreateNodeMetatable(typeof(FunctionDefinition), "FunctionDefinition");
            CreateNodeMetatable(typeof(Assignment), "Assignment");
            CreateNodeMetatable(typeof(NumericFor), "NumericFor");
            CreateNodeMetatable(typeof(GenericFor), "GenericFor");

            CreateListMetatable(typeof(List<IStatement>), "List<IStatement>");
            CreateListMetatable(typeof(List<IExpression>), "List<IExpression>");
            CreateListMetatable(typeof(List<IAssignable>), "List<IAssignable>");
            CreateListMetatable(typeof(List<TableConstructor.Entry>), "List<TableConstructor.Entry>");
            CreateListMetatable(typeof(List<string>), "List<string>");
            CreateListMetatable(typeof(List<ConditionalBlock>), "List<ConditionalBlock>");
        }
    }
}
