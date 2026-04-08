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
}
catch (Exception ex)
{
	Console.Error.WriteLine($"Parser crashed: {ex.Message}");
	Environment.Exit(1);
}
