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
dotnet run --project Src -- Examples/arithmetic.simtl
```

Run AST + type checker + interpreter mode:

```bash
dotnet run --project Src -- --ast Examples/arithmetic.simtl
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
dotnet test Tests/AstTests.csproj --filter "FullyQualifiedName~Unit"
```

Run integration tests only:

```bash
dotnet test Tests/AstTests.csproj --filter "FullyQualifiedName~Integration"
```

Run acceptance tests only:

```bash
dotnet test Tests/AstTests.csproj --filter "FullyQualifiedName~Acceptance"
```

### Parser Selection During Test Builds

`Src/Src.csproj` has a build property named `IncludeLegacyParser`.

- Default app builds keep the legacy parser enabled (`IncludeLegacyParser=true`) so parser-only mode remains available.
- Test builds disable the legacy parser through the `ProjectReference` in `Tests/AstTests.csproj` (`AdditionalProperties="IncludeLegacyParser=false"`).
- Tests therefore use the AST parser (`MyLangAstGen.Parser` / `MyLangAstGen.Scanner`) and do not compile the legacy `Grammar/Parser.cs` and `Grammar/Scanner.cs` for the test build path.

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
- `Tests/` - unit, integration, and acceptance tests
- `Examples/` - SIMTL sample programs used by tests and manual runs
- `P4-SIMTL.sln` - solution file

## Example Programs

- `Examples/arithmetic.simtl`
- `Examples/spawn_pid.simtl`
- `Examples/send_receive.simtl`
- `Examples/full_showcase.simtl`

These are useful for both manual runs (`dotnet run --project Src -- --ast <file>`) and for understanding supported syntax.

## Common Issues

- `Input file not found`: run commands from repository root or provide a correct relative/absolute path.
- SDK mismatch errors: install/update to an SDK supporting `net10.0`.
