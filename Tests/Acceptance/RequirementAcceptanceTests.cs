using System;
using System.IO;
using Xunit;

public class RequirementAcceptanceTests
{
    private static ProgramNode ParseFile(string path)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var fullPath = Path.GetFullPath(Path.Combine(root, path));

        var scanner = new MyLangAstGen.Scanner(fullPath);
        var parser = new MyLangAstGen.Parser(scanner);
        parser.Parse();
        Assert.Equal(0, parser.errors.count);
        Assert.NotNull(parser.ProgramResult);
        return parser.ProgramResult!;
    }

    [Fact]
    public void MustHave_ArithmeticOperations_Work()
    {
        var program = ParseFile("Examples/arithmetic.simtl");
        var checker = new TypeChecker();
        checker.CheckProgram(program);
        var interpreter = new Interpreter(program);
        var result = interpreter.Run();
        Assert.IsType<IntValue>(result);
        Assert.Equal(12, ((IntValue)result).Value);
    }

    [Fact]
    public void MustHave_SpawnPid_ReturnsPid()
    {
        var program = ParseFile("Examples/spawn_pid.simtl");
        var checker = new TypeChecker();
        checker.CheckProgram(program);
        var interpreter = new Interpreter(program);
        var result = interpreter.Run();
        Assert.IsType<PidValue>(result);
    }

    [Fact]
    public void MustHave_SendReceive_CommunicatesBetweenProcesses()
    {
        var program = ParseFile("Examples/send_receive.simtl");
        var checker = new TypeChecker();
        checker.CheckProgram(program);
        var interpreter = new Interpreter(program);
        var result = interpreter.Run();
        Assert.IsType<IntValue>(result);
        Assert.Equal(1, ((IntValue)result).Value);
    }
    [Fact]
    public void MustHave_DistributedMemory_ProcessesDoNotShareVariables()
    {
        var program = ParseFile("Examples/ProcessIsolation.simtl");
        var checker = new TypeChecker();
        checker.CheckProgram(program);

        var interpreter = new Interpreter(program);
        var result = interpreter.Run();

        Assert.IsType<IntValue>(result);
        Assert.Equal(104, ((IntValue)result).Value);
    }
    [Fact]
    public void MustHave_BooleanAndRelationalExpressions_Work()
    {
        var program = ParseFile("Examples/BooleanRelational.simtl");

        var checker = new TypeChecker();
        checker.CheckProgram(program);

        var interpreter = new Interpreter(program);
        var result = interpreter.Run();

        Assert.IsType<IntValue>(result);
        Assert.Equal(1, ((IntValue)result).Value);
    }
    [Fact]
    public void MustHave_TypeErrors_AreRejected()
    {
        var program = ParseFile("Examples/typeError.simtl");

        var checker = new TypeChecker();

        Assert.Throws<TypeCheckException>(() => checker.CheckProgram(program));
    }
    [Fact]
    public void MustHave_ReturnPath_IsChecked()
    {
        var program = ParseFile("Examples/returnErrorParse.simtl");

        var checker = new TypeChecker();

        var ex = Assert.Throws<TypeCheckException>(() => checker.CheckProgram(program));
        Assert.Contains("may not return a value on all paths", ex.Message);
    }
    [Fact]
    public void MustHave_WhileAndIf_ControlFlowWorks()
    {
        var program = ParseFile("Examples/while_if.simtl");

        var checker = new TypeChecker();
        checker.CheckProgram(program);

        var interpreter = new Interpreter(program);
        var result = interpreter.Run();

        Assert.IsType<IntValue>(result);
        Assert.Equal(1, ((IntValue)result).Value);
    }
}
