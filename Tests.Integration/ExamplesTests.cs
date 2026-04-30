using System;
using System.IO;
using Xunit;

public class ExamplesTests
{
    private static ProgramNode ParseFile(string path)
    {
        var scanner = new MyLangAstGen.Scanner(path);
        var parser = new MyLangAstGen.Parser(scanner);
        parser.Parse();
        Assert.Equal(0, parser.errors.count);
        Assert.NotNull(parser.ProgramResult);
        return parser.ProgramResult!;
    }

    [Fact]
    public void ArithmeticExample_Returns42()
    {
        var program = ParseFile("Tests/Examples/arithmetic.simtl");
        var checker = new TypeChecker();
        checker.CheckProgram(program);
        var interpreter = new Interpreter(program);
        var result = interpreter.Run();
        Assert.IsType<IntValue>(result);
        Assert.Equal(42, ((IntValue)result).Value);
    }

    [Fact]
    public void SpawnPidExample_ReturnsPid()
    {
        var program = ParseFile("Tests/Examples/spawn_pid.simtl");
        var checker = new TypeChecker();
        checker.CheckProgram(program);
        var interpreter = new Interpreter(program);
        var result = interpreter.Run();
        Assert.IsType<PidValue>(result);
    }

    [Fact]
    public void SendReceiveExample_Returns7()
    {
        var program = ParseFile("Tests/Examples/send_receive.simtl");
        var checker = new TypeChecker();
        checker.CheckProgram(program);
        var interpreter = new Interpreter(program);
        var result = interpreter.Run();
        Assert.IsType<IntValue>(result);
        Assert.Equal(7, ((IntValue)result).Value);
    }
}
