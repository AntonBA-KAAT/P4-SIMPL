# P4-SIMPL

Semester 4 bachelor project implementing a small language and runtime for SIMPL programs.

This repository contains:
- scanner/parser generation artifacts,
- AST construction and printing,
- static type checking,
- an interpreter with process-style primitives (`spawn`, `send`, `receive`, `self`),
- unit and integration tests.

## Features

- Parsing SIMPL source files
- AST-based parsing mode with typed AST nodes
- Type checker with return-path validation and type mismatch detection
- Interpreter execution for arithmetic, control flow, function calls, and process primitives
- Example SIMPL programs and automated tests

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
dotnet build P4-SIMPL.sln
```

Run parser + AST + type checker + interpreter mode:

```bash
dotnet run --project Src -- Examples/arithmetic.simpl
```

Expected successful output includes:
- `Parse OK`
- `Typecheck OK`
- `Program returned: <value>`
- interpreter prints/behavior from the SIMPL program

## Running Tests

Run all tests:

```bash
dotnet test P4-SIMPL.sln
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

- Default app builds keep the legacy parser sources enabled (`IncludeLegacyParser=true`).
- Test builds disable the legacy parser through the `ProjectReference` in `Tests/AstTests.csproj` (`AdditionalProperties="IncludeLegacyParser=false"`).
- Tests therefore use the AST parser (`MyLangAstGen.Parser` / `MyLangAstGen.Scanner`) and do not compile the legacy `Grammar/Parser.cs` and `Grammar/Scanner.cs` for the test build path.

## SIMPL Language Snapshot

The current examples/tests exercise:
- Primitive types: `Int`, `Bool`, `Pid`
- Functions with typed parameters and return types
- Control flow: `if/else`, `while`, `return`, `skip`
- Expressions: arithmetic, comparison, logical operators
- Process-style primitives: `spawn`, `send <expr> to <target>`, `receive(<source>)`, `self`
- Function invocation via `call <name>(...)`

Minimal example:

```
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
- `Examples/` - SIMPL sample programs used by tests and manual runs
- `P4-SIMPL.sln` - solution file

## Example Programs

- `Examples/arithmetic.simpl`
- `Examples/spawn_pid.simpl`
- `Examples/send_receive.simpl`
- `Examples/full_showcase.simpl`

These are useful for both manual runs (`dotnet run --project Src -- <file>`) and for understanding supported syntax.

## Common Issues

- `Input file not found`: run commands from repository root or provide a correct relative/absolute path.
- SDK mismatch errors: install/update to an SDK supporting `net10.0`.
