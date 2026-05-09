using System;
using System.IO;
using Xunit;

public class IntegrationTests
{
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

        var temp = Path.GetTempFileName();
        try
        {
            File.WriteAllText(temp, source);

            var scanner = new MyLangAstGen.Scanner(temp);
            var parser = new MyLangAstGen.Parser(scanner);
            parser.Parse();

            Assert.Equal(0, parser.errors.count);
            Assert.NotNull(parser.ProgramResult);

            var checker = new TypeChecker();
            checker.CheckProgram(parser.ProgramResult);

            var interpreter = new Interpreter(parser.ProgramResult);
            var result = interpreter.Run();

            Assert.IsType<IntValue>(result);
            Assert.Equal(42, ((IntValue)result).Value);
        }
        finally
        {
            if (File.Exists(temp)) File.Delete(temp);
        }
    }
}
