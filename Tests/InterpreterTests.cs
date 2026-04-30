using System;
using System.IO;
using Xunit;

public class InterpreterTests
{
    private static ProgramNode ParseProgram(string source)
    {
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, source);
        try
        {
            var scanner = new MyLangAstGen.Scanner(tempFile);
            var parser = new MyLangAstGen.Parser(scanner);
            parser.Parse();
            Assert.Equal(0, parser.errors.count);
            Assert.NotNull(parser.ProgramResult);
            return parser.ProgramResult!;
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void DeclarationAndReturn_IntegerValue()
    {
        const string source = """
            func Int main() {
                Int x = 5;
                x = x + 2;
                return x;
            }
            """;

        var program = ParseProgram(source);
        var checker = new TypeChecker();
        checker.CheckProgram(program);

        var interpreter = new Interpreter(program);
        var result = interpreter.Run();

        Assert.IsType<IntValue>(result);
        Assert.Equal(7, ((IntValue)result).Value);
    }

    [Fact]
    public void SpawnReturnsPid()
    {
        const string source = """
            func Int worker(Int n) { return n; }
            func Pid main() { Pid p = spawn worker(1); return p; }
            """;

        var program = ParseProgram(source);
        var checker = new TypeChecker();
        checker.CheckProgram(program);

        var interpreter = new Interpreter(program);
        var result = interpreter.Run();

        Assert.IsType<PidValue>(result);
        Assert.True(((PidValue)result).Value > 0);
    }

    [Fact]
    public void SendReceiveBetweenProcesses()
    {
        const string source = """
            func Int worker(Pid parent) { Int n = receive(parent); send n to parent; return 0; }
            func Int main() { Pid w = spawn worker(self); send 7 to w; Int r = receive(w); return r; }
            """;

        var program = ParseProgram(source);
        var checker = new TypeChecker();
        checker.CheckProgram(program);

        var interpreter = new Interpreter(program);
        var result = interpreter.Run();

        Assert.IsType<IntValue>(result);
        Assert.Equal(7, ((IntValue)result).Value);
    }
}
