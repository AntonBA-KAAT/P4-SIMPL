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

    [Fact]
    public void FullPipeline_DeclarationAndArithmetic()
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
    public void FullPipeline_SpawnReturnsPid()
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
    public void FullPipeline_SendReceiveBetweenProcesses()
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

    [Fact]
    public void FullPipeline_MultipleWorkersCanSendToMain()
    {
        const string source = """
            func Int worker(Pid parent) {
                send 1 to parent;
                return 0;
            }

            func Int main() {
                Pid p1 = spawn worker(self);
                Pid p2 = spawn worker(self);

                Int a = receive(p1);
                Int b = receive(p2);

                return a + b;
            }
            """;

        var program = ParseProgram(source);
        var checker = new TypeChecker();
        checker.CheckProgram(program);

        var interpreter = new Interpreter(program);
        var result = interpreter.Run();

        Assert.IsType<IntValue>(result);
        Assert.Equal(2, ((IntValue)result).Value);
    }

    [Fact]
    public void FullPipeline_SpawnedProcessCanUseSelf()
    {
        const string source = """
            func Int worker(Pid parent) {
                send 5 to parent;
                return 0;
            }

            func Int main() {
                Pid p = spawn worker(self);
                Int x = receive(p);
                return x;
            }
            """;

        var program = ParseProgram(source);
        var checker = new TypeChecker();
        checker.CheckProgram(program);

        var interpreter = new Interpreter(program);
        var result = interpreter.Run();

        Assert.IsType<IntValue>(result);
        Assert.Equal(5, ((IntValue)result).Value);
    }

    [Fact]
    public void FullPipeline_FunctionCallReturnsValue()
    {
        const string source = """
            func Int inc(Int n) {
                return n + 1;
            }

            func Int main() {
                Int x = call inc(41);
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
    public void FullPipeline_IfFalseExecutesElseBranch()
    {
        const string source = """
            func Int main() {
                if (false) {
                    return 1;
                } else {
                    return 2;
                }
            }
            """;

        var program = ParseProgram(source);
        var checker = new TypeChecker();
        checker.CheckProgram(program);

        var interpreter = new Interpreter(program);
        var result = interpreter.Run();

        Assert.IsType<IntValue>(result);
        Assert.Equal(2, ((IntValue)result).Value);
    }

    [Fact]
    public void FullPipeline_WhileLoopComputesExpectedResult()
    {
        const string source = """
            func Int main() {
                Int x = 0;

                while (x < 5) {
                    x = x + 1;
                }

                return x;
            }
            """;

        var program = ParseProgram(source);
        var checker = new TypeChecker();
        checker.CheckProgram(program);

        var interpreter = new Interpreter(program);
        var result = interpreter.Run();

        Assert.IsType<IntValue>(result);
        Assert.Equal(5, ((IntValue)result).Value);
    }

    [Fact]
    public void FullPipeline_UnaryMinusWorks()
    {
        const string source = """
            func Int main() {
                Int x = -5;
                return x;
            }
            """;

        var program = ParseProgram(source);
        var checker = new TypeChecker();
        checker.CheckProgram(program);

        var interpreter = new Interpreter(program);
        var result = interpreter.Run();

        Assert.IsType<IntValue>(result);
        Assert.Equal(-5, ((IntValue)result).Value);
    }

    [Fact]
    public void FullPipeline_BooleanNotWorks()
    {
        const string source = """
            func Int main() {
                if (!false) {
                    return 1;
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
        Assert.Equal(1, ((IntValue)result).Value);
    }

    [Fact]
    public void FullPipeline_MissingMainThrowsRuntimeException()
    {
        const string source = """
            func Int other() {
                return 0;
            }
            """;

        var program = ParseProgram(source);
        var checker = new TypeChecker();
        checker.CheckProgram(program);

        var interpreter = new Interpreter(program);

        var ex = Assert.Throws<RuntimeException>(() => interpreter.Run());
        Assert.Contains("No main function found", ex.Message);
    }

    [Fact]
    public void FullPipeline_SpawnedProcessCanSendAndReceiveConcurrently()
    {
        const string source = """
            func Int worker(Pid parent) {
                Int x = receive(parent);
                Int y = receive(parent);
                send x + y to parent;
                return 0;
            }

            func Int main() {
                Pid p = spawn worker(self);
                send 10 to p;
                send 20 to p;
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
        Assert.Equal(30, ((IntValue)result).Value);
    }

    [Fact]
    public void FullPipeline_MultipleSpawnedProcessesCommunicateIndependently()
    {
        const string source = """
            func Int worker(Pid parent, Int id) {
                send id * 100 to parent;
                return 0;
            }

            func Int main() {
                Pid p1 = spawn worker(self, 1);
                Pid p2 = spawn worker(self, 2);
                Pid p3 = spawn worker(self, 3);

                Int v1 = receive(p1);
                Int v2 = receive(p2);
                Int v3 = receive(p3);

                return v1 + v2 + v3;
            }
            """;

        var program = ParseProgram(source);
        var checker = new TypeChecker();
        checker.CheckProgram(program);

        var interpreter = new Interpreter(program);
        var result = interpreter.Run();

        Assert.IsType<IntValue>(result);
        Assert.Equal(600, ((IntValue)result).Value);
    }

    [Fact]
    public void FullPipeline_ProcessChainPassingMessagesThrough()
    {
        const string source = """
            func Int relay(Pid source, Pid destination) {
                Int x = receive(source);
                send x + 1 to destination;
                return 0;
            }

            func Int main() {
                Pid relay1 = spawn relay(self, self);
                send 5 to relay1;
                Int result = receive(relay1);
                return result;
            }
            """;

        var program = ParseProgram(source);
        var checker = new TypeChecker();
        checker.CheckProgram(program);

        var interpreter = new Interpreter(program);
        var result = interpreter.Run();

        Assert.IsType<IntValue>(result);
        Assert.Equal(6, ((IntValue)result).Value);
    }

    [Fact]
    public void FullPipeline_RuntimeProcessIsolatedMemorySpaces()
    {
        const string source = """
            func Int worker(Pid parent) {
                Int localVar = 42;
                send localVar to parent;
                return 0;
            }

            func Int main() {
                Int localVar = 10;
                Pid p = spawn worker(self);
                Int received = receive(p);
                return localVar + received;
            }
            """;

        var program = ParseProgram(source);
        var checker = new TypeChecker();
        checker.CheckProgram(program);

        var interpreter = new Interpreter(program);
        var result = interpreter.Run();

        Assert.IsType<IntValue>(result);
        Assert.Equal(52, ((IntValue)result).Value);
    }
}