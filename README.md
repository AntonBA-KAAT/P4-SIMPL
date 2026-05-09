# P4-SIMTL

Semester 4 bachelor project implementing a small language and runtime for SIMTL programs.

This repository contains:
- scanner/parser generation artifacts,
- AST construction and printing,
- static type checking,
- an interpreter with process-style primitives (`spawn`, `send`, `receive`, `self`),
- unit and integration tests.

## Features

- Parsing SIMTL source files
- AST-based parsing mode with typed AST nodes
- Type checker with return-path validation and type mismatch detection
- Interpreter execution for arithmetic, control flow, function calls, and process primitives
- Example SIMTL programs and automated tests

## Prerequisites

- .NET SDK with support for `net10.0`
- Windows, macOS, or Linux

Check your SDK:

```bash
dotnet --info
```

## Quick Start

From the repository root:

```bash
dotnet build P4-SIMTL.sln
```

Run parser-only mode:

```bash
dotnet run --project Src -- Tests/Examples/arithmetic.simtl
```

Run AST + type checker + interpreter mode:

```bash
dotnet run --project Src -- --ast Tests/Examples/arithmetic.simtl
```

Expected successful output includes:
- `Parse OK` (or `Parse OK (AST)`)
- `Typecheck OK` (in `--ast` mode)
- interpreter prints/behavior from the SIMTL program

## Running Tests

Run all tests:

```bash
dotnet test P4-SIMTL.sln
```

Run unit tests only:

```bash
dotnet test Tests/AstTests.csproj
```

Run integration tests only:

```bash
dotnet test Tests.Integration/IntegrationTests.csproj
```

## SIMTL Language Snapshot

The current examples/tests exercise:
- Primitive types: `Int`, `Bool`, `Pid`
- Functions with typed parameters and return types
- Control flow: `if/else`, `while`, `return`, `skip`
- Expressions: arithmetic, comparison, logical operators
- Process-style primitives: `spawn`, `send <expr> to <target>`, `receive(<source>)`, `self`
- Function invocation via `call <name>(...)`

Minimal example:

```simtl
func Int main() {
	Int x = 40;
	x = x + 2;
	print(x);
	return x;
}
```

## Repository Structure

- `Src/` - runtime implementation (AST nodes, type checker, interpreter, entry point)
- `Grammar/` - grammar and generated scanner/parser sources (including AST generator output)
- `CoCoR/` - parser/scanner frame files used by generator tooling
- `Tests/` - unit tests and SIMTL example programs under `Tests/Examples/`
- `Tests.Integration/` - end-to-end integration tests
- `P4-SIMTL.sln` - solution file

## Example Programs

- `Tests/Examples/arithmetic.simtl`
- `Tests/Examples/spawn_pid.simtl`
- `Tests/Examples/send_receive.simtl`
- `Tests/Examples/full_showcase.simtl`

These are useful for both manual runs (`dotnet run --project Src -- --ast <file>`) and for understanding supported syntax.

## Common Issues

- `Input file not found`: run commands from repository root or provide a correct relative/absolute path.
- SDK mismatch errors: install/update to an SDK supporting `net10.0`.
