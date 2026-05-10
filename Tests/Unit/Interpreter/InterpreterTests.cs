using System;
using System.IO;
using Xunit;
[Trait("Category", "Unit")]
public class InterpreterTests
{
    [Fact]
    public void MailboxReceiveFrom_RemovesFirstMatchingSenderEvenWhenNotAtHead()
    {
        var mailbox = new Mailbox();
        mailbox.Send(new Message(1, 10));
        mailbox.Send(new Message(2, 20));
        mailbox.Send(new Message(1, 30));

        var fromTwo = mailbox.ReceiveFrom(2);
        Assert.Equal(2, fromTwo.SenderPid);
        Assert.Equal(20, fromTwo.Value);

        var firstFromOne = mailbox.ReceiveFrom(1);
        Assert.Equal(1, firstFromOne.SenderPid);
        Assert.Equal(10, firstFromOne.Value);

        var secondFromOne = mailbox.ReceiveFrom(1);
        Assert.Equal(1, secondFromOne.SenderPid);
        Assert.Equal(30, secondFromOne.Value);
    }

    [Fact]
    public void MailboxReceiveFrom_PreservesOrderOfOtherMessages()
    {
        var mailbox = new Mailbox();
        mailbox.Send(new Message(1, 100));
        mailbox.Send(new Message(2, 200));
        mailbox.Send(new Message(3, 300));
        mailbox.Send(new Message(1, 400));

        var fromThree = mailbox.ReceiveFrom(3);
        Assert.Equal(3, fromThree.SenderPid);
        Assert.Equal(300, fromThree.Value);

        var fromOne = mailbox.ReceiveFrom(1);
        Assert.Equal(1, fromOne.SenderPid);
        Assert.Equal(100, fromOne.Value);

        var fromTwo = mailbox.ReceiveFrom(2);
        Assert.Equal(2, fromTwo.SenderPid);
        Assert.Equal(200, fromTwo.Value);

        var secondFromOne = mailbox.ReceiveFrom(1);
        Assert.Equal(1, secondFromOne.SenderPid);
        Assert.Equal(400, secondFromOne.Value);
    }

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
    [Fact]
    public void MultipleWorkersCanSendToMain()
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
    public void SpawnedProcessCanUseSelf()
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
    public void FunctionCallReturnsValue()
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
    public void IfFalseExecutesElseBranch()
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
    public void WhileLoopComputesExpectedResult()
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
    public void UnaryMinusWorks()
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
    public void BooleanNotWorks()
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
    public void MissingMainThrowsRuntimeException()
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
}
