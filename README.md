#Relua

Relua is a handwritten two-way parser and scripted code transformation engine for Lua, written in C#. It has the capability to read Lua source code into a runtime representation (AST), and write that runtime representation back into Lua source code with no semantic differences. The Relua.Script subproject provides a runtime and an API for Lua scripts to transform any script in a fairly straightforward manner.

##Lua Version

Relua is created to work out of the box with Lua 5.1 and LuaJIT, however the design should make it fairly simple to add support for 5.2 and 5.3.

##Parsing

Relua does not use any library to aid the parsing. Instead, it implements everything from the bare primitives up to complex expressions in a procedural manner. While this does significantly increase the size and complexity of the code, it makes it much easier to modify the parser to suit various use cases and to implement language quirks that would require ugly hacks to implement on top of primitives provided by parsing engines (more on that below).

##Why not just emit bytecode?

A significant portion of software with some form of Lua scripting makes use of sandboxing. This usually includes disabling the ability to load bytecode (due to possible exploits), which would significantly decrease the usefulness of a tool like this, especially for videogame modding and similar. Reading and writing code is also much more natural than operating on bytecode.

##Performance

According to my own limited testing with GNU `time` and a small test program, Relua is about six times faster at parsing files and chunks than the parser used by the archived [NetLua](https://github.com/frabert/NetLua/tree/master/NetLua) project. It also uses on average a bit less than half of the memory (likely due to avoiding the use of so many intermediate parser objects).

On my machine, it takes NetLua on average 1.3 sec to parse a 750K file, while Relua reads that same file in about 0.2-0.3 sec. It takes about an extra 0.1 sec to write back the source code from the AST, meaning that by the time NetLua has parsed a file, a program could have already read a file with Relua, parsed it, modified the AST and written it back.

# Relua.Script

Relua.Script runs on top of the standard Lua 5.1 implementation (or LuaJIT) to expose an API for AST node manipulation. In essence, Relua.Script enables you to use a Lua script to modify a Lua script using its runtime representation. This combined with Relua's ability to write the AST back into code makes for a very powerful and extensible transformation engine.

## Example

```lua
function FunctionDefinition(node)
	local new_block = astnew("Block")

	local block_copy = astnew("Block")
	node.Block.Statements:iter(function(i, stat)
		enter(stat)
		block_copy.Statements:add(stat)
	end)

	local pcall_call = aststat("local result, err = pcall(function() end)")
	pcall_call.Values[1].Arguments[1].Block = block_copy
	new_block.Statements:add(pcall_call)

	local pcall_check = aststat("if err ~= nil then error(err) end")
	pcall_check.MainIf.Block.Statements:insert(1, aststat("print('do something here')"))
	new_block.Statements:add(pcall_check)

	new_block.Statements:add(aststat("return result"))

	node.Block = new_block
end
```

Before:
```lua
function test()
        print("Hello, world!")
        print("blah")
end

test()
```

After:
```lua
function test()
    local result, err = pcall(function()
        print("Hello, world!")
        print("blah")
    end)
    if (err ~= nil) then
        print("do something here")
        error(err)
    end
    return result
end
test()
```