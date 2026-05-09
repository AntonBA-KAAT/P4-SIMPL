using System;
using System.IO;

if (args.Length == 0)
{
	Console.WriteLine("Usage: dotnet run --project Src -- [--ast] <path-to-.simtl-file>");
	return;
}

var useAst = args.Length >= 2 && args[0] == "--ast";
var inputPath = useAst ? args[1] : args[0];
if (!File.Exists(inputPath))
{
	Console.Error.WriteLine($"Input file not found: {inputPath}");
	Environment.Exit(1);
	return;
}

try
{
	if (useAst)
	{
		var scanner = new MyLangAstGen.Scanner(inputPath);
		var parser = new MyLangAstGen.Parser(scanner);
		parser.Parse();

		if (parser.errors.count == 0)
		{
			Console.WriteLine($"Parse OK (AST): {inputPath}");
			if (parser.ProgramResult != null)
			{
				var checker = new TypeChecker();
				checker.CheckProgram(parser.ProgramResult);

				Console.WriteLine("Typecheck OK");
				var interpreter = new Interpreter(parser.ProgramResult);
				interpreter.Run();
			}
		}
		else
		{
			Console.Error.WriteLine($"Parse failed with {parser.errors.count} error(s).");
			Environment.Exit(1);
		}
	}
	else
	{
#if INCLUDE_LEGACY_PARSER
		var scanner = new Scanner(inputPath);
		var parser = new Parser(scanner);
		parser.Parse();

		if (parser.errors.count == 0)
		{
			Console.WriteLine($"Parse OK: {inputPath}");
		}
		else
		{
			Console.Error.WriteLine($"Parse failed with {parser.errors.count} error(s).");
			Environment.Exit(1);
		}
#else
		Console.Error.WriteLine("Legacy parser is disabled in this build. Use --ast mode.");
		Environment.Exit(1);
#endif
	}
}
catch (TypeCheckException ex)
{
    Console.Error.WriteLine($"Type error: {ex.Message}");
    Environment.Exit(1);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Parser crashed: {ex.Message}");
    Environment.Exit(1);
}
