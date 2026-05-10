using System;
using System.IO;
using Xunit;
[Trait("Category", "Integration")]
public class IntegrationTests
{
    private static ProgramNode ParseProgram(string source)
    {
        var temp = Path.GetTempFileName();

        try
        {
            File.WriteAllText(temp, source);

            var scanner = new MyLangAstGen.Scanner(temp);
            var parser = new MyLangAstGen.Parser(scanner);
            parser.Parse();

            Assert.Equal(0, parser.errors.count);
            Assert.NotNull(parser.ProgramResult);

            return parser.ProgramResult!;
        }
        finally
        {
            if (File.Exists(temp)) File.Delete(temp);
        }
    }

    [Fact]
    public void EndToEnd_RunProgramReturnsExpected()
    {
        const string source = """
            func Int main() {
                Int x = 1;
                x = x + 41;
                return x;
            }
            """;

        var program = ParseProgram(source);

        var checker = new TypeChecker();
        checker.CheckProgram(program);

        var interpreter = new Interpreter(program);
        var result = interpreter.Run();

        Assert.IsType<IntValue>(result);
        Assert.Equal(42, ((IntValue)result).Value);
    }

    [Fact]
    public void FullPipeline_FunctionCallAndControlFlow_Work()
    {
        const string source = """
            func Int double(Int x) {
                return x * 2;
            }

            func Int main() {
                Int x = call double(5);

                if (x == 10) {
                    return x;
                } else {
                    return 0;
                }
            }
            """;

        var program = ParseProgram(source);

        var checker = new TypeChecker();
        checker.CheckProgram(program);

        var interpreter = new Interpreter(program);
        var result = interpreter.Run();

        Assert.IsType<IntValue>(result);
        Assert.Equal(10, ((IntValue)result).Value);
    }

    [Fact]
    public void FullPipeline_SpawnSendReceive_Work()
    {
        const string source = """
            func Int worker(Pid parent) {
                send 21 to parent;
                return 0;
            }

            func Int main() {
                Pid p = spawn worker(self);
                Int x = receive(p);
                return x * 2;
            }
            """;

        var program = ParseProgram(source);

        var checker = new TypeChecker();
        checker.CheckProgram(program);

        var interpreter = new Interpreter(program);
        var result = interpreter.Run();

        Assert.IsType<IntValue>(result);
        Assert.Equal(42, ((IntValue)result).Value);
    }

    [Fact]
    public void FullPipeline_InvalidProgramFailsDuringTypeChecking()
    {
        const string source = """
            func Int main() {
                Int x = true;
                return x;
            }
            """;

        var program = ParseProgram(source);

        var checker = new TypeChecker();

        Assert.Throws<TypeCheckException>(() => checker.CheckProgram(program));
    }
    [Fact]
    public void FullPipeline_MultipleFunctionsAndSpawnedProcess_WorkTogether()
    {
        const string source = """
            func Int addOne(Int x) {
                return x + 1;
            }

            func Int worker(Pid parent, Int n) {
                Int x = call addOne(n);
                send x to parent;
                return 0;
            }

            func Int main() {
                Pid p = spawn worker(self, 41);
                Int result = receive(p);
                return result;
            }
            """;

        var program = ParseProgram(source);

        var checker = new TypeChecker();
        checker.CheckProgram(program);

        var interpreter = new Interpreter(program);
        var result = interpreter.Run();

        Assert.IsType<IntValue>(result);
        Assert.Equal(42, ((IntValue)result).Value);
    }
}