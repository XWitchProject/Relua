# Relua.Script API

## Visitor functions

Relua.Script uses the Visitor pattern to make AST transformations easy. Each node type (see Types section below) can have its own global function with the same name as the type. If such a function exists when the script is loaded, it will be used by the `enter(node)` builtin function.

The first function that is ran is always the `Block` visitor (because parsing a file returns a `Block`). A simple implementation might look like this:

```lua
function Block(node)
	node.Statements:iter(function(i, stat)
		enter(stat)
	end)
end
```

An implementation like this will remove all global top level assignments, alongside renaming all function calls to `print`.

```lua
function FunctionCall(node)
	node.Function = astexpr("print")
end

function Block(node)
	node.Statements:reverse_iter(function(i, stat)
		if (asttype(stat) == "Assignment") then
			node.Statements:remove(i)
		else
			enter(stat)
		end
	end)
end
```

## Builtin functions

### `enter(node)`

Runs the appropriate type function based on the type of `node`.

### `astnew(nodetype)`

Returns a new AST node of type `nodetype`. See Types section below.
Note that lists cannot be instantiated.

### `astexpr(expr)`

Parses the string in `expr` and returns an expression node.

### `aststat(stat)`

Parses the string in `stat` and returns a statement node.

### `astfile(path)`

Parses the file at `path` and returns its `Block` node.

### `asttype(node)`

Returns the type name of `node`.

## Types

### List

Note: lists are 1-indexed.

Available Properties and Functions:

* `list.count`
* getter (`list[idx]`)
* setter (`list[idx] = val`)
* `list:add(elem)`
* `list:insert(idx, elem)`
* `list:remove(idx)`
* `list:clear()`
* `list:iter(function(index, entry) ... end)`
* `list:reverse_iter(function(index, entry) ... end)`

### Variable (expression, assignable)

* `Name` (string)

### NilLiteral (expression)

No fields.

### VarargsLiteral (expression)

No fields.

### BoolLiteral (expression)

* `Value` (bool)

### UnaryOp (expression)

* `Type` (string)
	* `Negate` (`not`)
	* `Invert` (`-`)
	* `Length` (`#`)
* `Expression` (expression node)

### BinaryOp (expression)
* `Type` (string)
	* `Add` (`+`)
	* `Subtract` (`-`)
	* `Multiply` (`*`)
	* `Divide` (`/`)
	* `Power` (`^`)
	* `Modulo` (`%`)
	* `Concat` (`..`)
	* `GreaterThan` (`>`)
	* `GreaterOrEqual` (`>=`)
	* `LessThan` (`<`)
	* `LessOrEqual` (`<=`)
	* `Equal` (`==`)
	* `NotEqual` (`~=`)
	* `And` (`and`)
	* `Or` (`or`)
* `Left` (expression node)
* `Right` (expression node)

### StringLiteral (expression)

* `Value` (string)

### NumberLiteral (expression)

* `Value` (number)

### LuaJITLongLiteral (expression)

* `Value` (number)

### TableAccess (expression, assignable)

* `Table` (expression node)
* `Index` (expression node)

### FunctionCall (expression, statement)

* `Function` (expression node)
* `Arguments` (list of expression nodes)
* `ForceTruncateReturnValues` (bool)

### TableConstructor (expression)

* `Entries` (list of `TableConstructorEntry` nodes)

### TableConstructorEntry

* `Key` (expression node)
* `Value` (expression node)
* `ExplicitKey` (bool)

### Break (statement)

No fields.

### Return (statement)

* `Expressions` (list of expression nodes)

### Block (statement)

* `Statements` (list of statement nodes)
* `TopLevel` (bool)

### ConditionalBlock

* `Condition` (expression node)
* `Block` (`Block` node)

### If (statement)

* `MainIf` (`ConditionalBlock` node)
* `ElseIfs` (list of `ConditionalBlock` nodes)
* `Else` (`Block` node)

### While (statement)

* `Condition` (expression node)
* `Block` (`Block` node)

### Repeat (statement)

* `Condition` (expression node)
* `Block` (`Block` node)

### FunctionDefinition (expression, statement)

* `ArgumentNames` (list of strings)
* `Block` (`Block` node)
* `AcceptsVarargs` (bool)
* `ImplicitSelf` (bool)

### Assignment (statement)

* `IsLocal` (bool)
* `Targets` (list of assignable nodes)
* `Values` (list of expression nodes)

### NumericFor (statement)

* `VariableName` (string)
* `StartPoint` (expression node)
* `EndPoint` (expression node)
* `Step` (expression node)

### GenericFor (statement)

* `VariableNames` (list of strings)
* `Iterator` (expression node)