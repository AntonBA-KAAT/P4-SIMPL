using System;
using System.IO;

if (args.Length == 0)
{
    Console.WriteLine("Usage: dotnet run --project Src -- <path-to-.simtl-file>");
    return;
}

var inputPath = args[0];

if (!File.Exists(inputPath))
{
    Console.Error.WriteLine($"Input file not found: {inputPath}");
    Environment.Exit(1);
    return;
}

try
{
    var scanner = new MyLangAstGen.Scanner(inputPath);
    var parser = new MyLangAstGen.Parser(scanner);
    parser.Parse();

    if (parser.errors.count != 0)
    {
        Console.Error.WriteLine($"Parse failed with {parser.errors.count} error(s).");
        Environment.Exit(1);
        return;
    }

    if (parser.ProgramResult == null)
    {
        Console.Error.WriteLine("Parse succeeded, but no AST was produced.");
        Environment.Exit(1);
        return;
    }

    Console.WriteLine($"Parse OK: {inputPath}");

    var checker = new TypeChecker();
    checker.CheckProgram(parser.ProgramResult);

    Console.WriteLine("Typecheck OK");

    var interpreter = new Interpreter(parser.ProgramResult);
    var result = interpreter.Run();
}
catch (TypeCheckException ex)
{
    Console.Error.WriteLine($"Type error: {ex.Message}");
    Environment.Exit(1);
}
catch (RuntimeException ex)
{
    Console.Error.WriteLine($"Runtime error: {ex.Message}");
    Environment.Exit(1);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Unexpected error: {ex.Message}");
    Environment.Exit(1);
}