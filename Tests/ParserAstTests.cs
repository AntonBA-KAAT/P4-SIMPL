using System.IO;
using Xunit;

public class ParserAstTests
{
    [Fact]
    public void ParsesMainProgramWithExpectedStatementShape()
    {
        const string source = """
            func Int main() {
                Int x = 1;
                Int y = 3;
                if (x < y) { print(x); } else { skip; }
                while (x < 10) { x = x + 1; }
                send x to self;
                return x;
            }
            """;

        var program = ParseProgram(source);

        var fn = Assert.Single(program.Functions);
        Assert.Equal(TypeNode.Int, fn.ReturnType);
        Assert.Equal("main", fn.Name);
        Assert.Empty(fn.Parameters);
        Assert.Equal(6, fn.Statements.Count);

        var ifStmt = Assert.IsType<IfNode>(fn.Statements[2]);
        Assert.Single(ifStmt.ThenBranch);
        Assert.Single(ifStmt.ElseBranch);

        var whileStmt = Assert.IsType<WhileNode>(fn.Statements[3]);
        Assert.Single(whileStmt.Body);
    }

    [Fact]
    public void ParsesExpressionPrecedenceCorrectly()
    {
        const string source = """
            func Int main() {
                Int x = 1 + 2 * 3;
                return x;
            }
            """;

        var program = ParseProgram(source);
        var fn = Assert.Single(program.Functions);

        var decl = Assert.IsType<DeclNode>(fn.Statements[0]);
        var exprRhs = Assert.IsType<ExprRhsNode>(decl.Value);
        var sum = Assert.IsType<BinaryExprNode>(exprRhs.Value);
        Assert.Equal("+", sum.Operator);
        Assert.IsType<NumberNode>(sum.Left);

        var product = Assert.IsType<BinaryExprNode>(sum.Right);
        Assert.Equal("*", product.Operator);
    }

    [Fact]
    public void ParsesEmptyThenAndElseBlocks()
    {
        const string source = """
            func Int main() {
                if (true) { } else { }
                return 0;
            }
            """;

        var program = ParseProgram(source);
        var fn = Assert.Single(program.Functions);

        var ifStmt = Assert.IsType<IfNode>(fn.Statements[0]);
        Assert.Empty(ifStmt.ThenBranch);
        Assert.Empty(ifStmt.ElseBranch);
    }

    [Fact]
    public void IgnoresPercentComments()
    {
        const string source = """
            % leading comment
            func Int main() {
                Int x = 1; % inline comment
                % comment between statements
                return x;
            }
            """;

        var program = ParseProgram(source);
        var fn = Assert.Single(program.Functions);
        Assert.Equal(2, fn.Statements.Count);
    }

    [Fact]
    public void ParsesReceiveSpawnAndCallRhs()
    {
        const string source = """
            func Pid worker(Int n) {
                Pid p = self;
                p = spawn worker(n);
                n = receive(self);
                n = call inc(n);
                return p;
            }
            """;

        var program = ParseProgram(source);
        var fn = Assert.Single(program.Functions);

        var spawnAssign = Assert.IsType<AssignNode>(fn.Statements[1]);
        var spawn = Assert.IsType<SpawnRhsNode>(spawnAssign.Value);
        Assert.Equal("worker", spawn.Callee);
        Assert.Single(spawn.Arguments);

        var receiveAssign = Assert.IsType<AssignNode>(fn.Statements[2]);
        var receive = Assert.IsType<ReceiveRhsNode>(receiveAssign.Value);
        Assert.IsType<SelfNode>(receive.Source);

        var callAssign = Assert.IsType<AssignNode>(fn.Statements[3]);
        var call = Assert.IsType<CallRhsNode>(callAssign.Value);
        Assert.Equal("inc", call.Callee);
        Assert.Single(call.Arguments);
    }

    [Fact]
    public void ParsesNestedIfInsideWhileWithoutDuplicatingBranches()
    {
        const string source = """
            func Int main() {
                Int x = 0;
                while (x < 3) {
                    if (x == 1) { print(x); } else { skip; }
                    x = x + 1;
                }
                return x;
            }
            """;

        var program = ParseProgram(source);
        var fn = Assert.Single(program.Functions);

        var whileStmt = Assert.IsType<WhileNode>(fn.Statements[1]);
        Assert.Equal(2, whileStmt.Body.Count);

        var ifStmt = Assert.IsType<IfNode>(whileStmt.Body[0]);
        Assert.Single(ifStmt.ThenBranch);
        Assert.Single(ifStmt.ElseBranch);
        Assert.False(ReferenceEquals(ifStmt.ThenBranch, ifStmt.ElseBranch));
    }

    [Fact]
    public void ParsesCallRhsInDeclaration()
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
        Assert.Equal(2, program.Functions.Count);

        var main = program.Functions[1];
        var decl = Assert.IsType<DeclNode>(main.Statements[0]);
        var call = Assert.IsType<CallRhsNode>(decl.Value);
        Assert.Equal("inc", call.Callee);
        Assert.Single(call.Arguments);
    }

    [Fact]
    public void ParsesLogicalPrecedenceWithAndBeforeOr()
    {
        const string source = """
            func Bool main() {
                Bool b = true || false && false;
                return b;
            }
            """;

        var program = ParseProgram(source);
        var fn = Assert.Single(program.Functions);

        var decl = Assert.IsType<DeclNode>(fn.Statements[0]);
        var exprRhs = Assert.IsType<ExprRhsNode>(decl.Value);
        var orExpr = Assert.IsType<BinaryExprNode>(exprRhs.Value);
        Assert.Equal("||", orExpr.Operator);

        var andExpr = Assert.IsType<BinaryExprNode>(orExpr.Right);
        Assert.Equal("&&", andExpr.Operator);
    }

    [Fact]
    public void ParsesMultipleFunctionsAndParameterLists()
    {
        const string source = """
            func Int add(Int a, Int b) {
                return a + b;
            }

            func Int main() {
                Int z = call add(1, 2);
                return z;
            }
            """;

        var program = ParseProgram(source);
        Assert.Equal(2, program.Functions.Count);

        var add = program.Functions[0];
        Assert.Equal("add", add.Name);
        Assert.Equal(2, add.Parameters.Count);
        Assert.Equal("a", add.Parameters[0].Name);
        Assert.Equal("b", add.Parameters[1].Name);

        var main = program.Functions[1];
        var decl = Assert.IsType<DeclNode>(main.Statements[0]);
        var call = Assert.IsType<CallRhsNode>(decl.Value);
        Assert.Equal("add", call.Callee);
        Assert.Equal(2, call.Arguments.Count);
    }

    [Fact]
    public void AstPrinterProducesExpectedTreeForMixedProgram()
    {
        const string source = """
            func Int add(Int a, Int b) {
                return a + b;
            }

            func Int main() {
                Int x = call add(1, 2);
                Pid p = spawn worker(x);
                x = receive(self);
                if (x < 10) { print(x); } else { skip; }
                while (x < 12) { x = x + 1; }
                send x to self;
                return x;
            }
            """;

        var program = ParseProgram(source);
        var printed = AstPrinter.Print(program);

        var expectedLines = new[]
        {
            "Program",
            "  Function Int add",
            "    Params (2)",
            "      Int a",
            "      Int b",
            "    Body (1)",
            "      Return (a + b)",
            "  Function Int main",
            "    Params (0)",
            "    Body (7)",
            "      Decl Int x = call add(1, 2)",
            "      Decl Pid p = spawn worker(x)",
            "      Assign VarNode { Name = x } = receive(self)",
            "      If (x < 10)",
            "        Then",
            "          Print x",
            "        Else",
            "          Skip",
            "      While (x < 12)",
            "        Assign VarNode { Name = x } = (x + 1)",
            "      Send x to self",
            "      Return x"
        };

        var actualLines = printed.Replace("\r\n", "\n").TrimEnd('\n').Split('\n');
        Assert.Equal(expectedLines, actualLines);
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
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}
