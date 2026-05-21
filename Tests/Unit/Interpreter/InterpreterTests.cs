using System;
using System.Collections.Generic;
using Xunit;
[Trait("Category", "Unit")]
public class InterpreterTests
{
    [Fact]
    public void Interpreter_MissingMainThrowsRuntimeException()
    {
        var programWithoutMain = new ProgramNode(new List<FunctionNode>());
        var interpreter = new Interpreter(programWithoutMain);

        var ex = Assert.Throws<RuntimeException>(() => interpreter.Run());
        Assert.Contains("No main function found", ex.Message);
    }

    [Fact]
    public void Interpreter_SimpleIntReturn()
    {
        var mainFunc = new FunctionNode(
            ReturnType: TypeNode.Int,
            Name: "main",
            Parameters: new List<ParamNode>(),
            Statements: new List<StatementNode> {
                new ReturnNode(new NumberNode(42))
            }
        );
        var program = new ProgramNode(new List<FunctionNode> { mainFunc });
        var interpreter = new Interpreter(program);
        var result = interpreter.Run();

        Assert.IsType<IntValue>(result);
        Assert.Equal(42, ((IntValue)result).Value);
    }
        [Fact]
        public void Interpreter_SimpleBoolReturn()
        {
            // func Bool main() { return true; }
            var mainFunc = new FunctionNode(
                ReturnType: TypeNode.Bool,
                Name: "main",
                Parameters: new List<ParamNode>(),
                Statements: new List<StatementNode> {
                    new ReturnNode(new BoolNode(true))
                }
            );
            var program = new ProgramNode(new List<FunctionNode> { mainFunc });
            var interpreter = new Interpreter(program);
            var result = interpreter.Run();

            Assert.IsType<BoolValue>(result);
            Assert.True(((BoolValue)result).Value);
        }

        [Fact]
        public void Interpreter_VariableDeclarationAndReturn()
        {
            // func Int main() { Int x = 10; return x; }
            var mainFunc = new FunctionNode(
                ReturnType: TypeNode.Int,
                Name: "main",
                Parameters: new List<ParamNode>(),
                Statements: new List<StatementNode> {
                    new DeclNode(TypeNode.Int, "x", new ExprRhsNode(new NumberNode(10))),
                    new ReturnNode(new VarNode("x"))
                }
            );
            var program = new ProgramNode(new List<FunctionNode> { mainFunc });
            var interpreter = new Interpreter(program);
            var result = interpreter.Run();

            Assert.IsType<IntValue>(result);
            Assert.Equal(10, ((IntValue)result).Value);
        }

        [Fact]
        public void Interpreter_VariableAssignment()
        {
            // func Int main() { Int x = 5; x = 10; return x; }
            var mainFunc = new FunctionNode(
                ReturnType: TypeNode.Int,
                Name: "main",
                Parameters: new List<ParamNode>(),
                Statements: new List<StatementNode> {
                    new DeclNode(TypeNode.Int, "x", new ExprRhsNode(new NumberNode(5))),
                    new AssignNode(new VarNode("x"), new ExprRhsNode(new NumberNode(10))),
                    new ReturnNode(new VarNode("x"))
                }
            );
            var program = new ProgramNode(new List<FunctionNode> { mainFunc });
            var interpreter = new Interpreter(program);
            var result = interpreter.Run();

            Assert.IsType<IntValue>(result);
            Assert.Equal(10, ((IntValue)result).Value);
        }

        [Fact]
        public void Interpreter_ArithmeticAddition()
        {
            // func Int main() { return 5 + 3; }
            var mainFunc = new FunctionNode(
                ReturnType: TypeNode.Int,
                Name: "main",
                Parameters: new List<ParamNode>(),
                Statements: new List<StatementNode> {
                    new ReturnNode(new BinaryExprNode("+", new NumberNode(5), new NumberNode(3)))
                }
            );
            var program = new ProgramNode(new List<FunctionNode> { mainFunc });
            var interpreter = new Interpreter(program);
            var result = interpreter.Run();

            Assert.IsType<IntValue>(result);
            Assert.Equal(8, ((IntValue)result).Value);
        }

        [Fact]
        public void Interpreter_ArithmeticMultiplication()
        {
            // func Int main() { return 5 * 3; }
            var mainFunc = new FunctionNode(
                ReturnType: TypeNode.Int,
                Name: "main",
                Parameters: new List<ParamNode>(),
                Statements: new List<StatementNode> {
                    new ReturnNode(new BinaryExprNode("*", new NumberNode(5), new NumberNode(3)))
                }
            );
            var program = new ProgramNode(new List<FunctionNode> { mainFunc });
            var interpreter = new Interpreter(program);
            var result = interpreter.Run();

            Assert.IsType<IntValue>(result);
            Assert.Equal(15, ((IntValue)result).Value);
        }

        [Fact]
        public void Interpreter_UnaryMinus()
        {
            // func Int main() { return -5; }
            var mainFunc = new FunctionNode(
                ReturnType: TypeNode.Int,
                Name: "main",
                Parameters: new List<ParamNode>(),
                Statements: new List<StatementNode> {
                    new ReturnNode(new UnaryExprNode("-", new NumberNode(5)))
                }
            );
            var program = new ProgramNode(new List<FunctionNode> { mainFunc });
            var interpreter = new Interpreter(program);
            var result = interpreter.Run();

            Assert.IsType<IntValue>(result);
            Assert.Equal(-5, ((IntValue)result).Value);
        }

        [Fact]
        public void Interpreter_BooleanNot()
        {
            // func Bool main() { return !false; }
            var mainFunc = new FunctionNode(
                ReturnType: TypeNode.Bool,
                Name: "main",
                Parameters: new List<ParamNode>(),
                Statements: new List<StatementNode> {
                    new ReturnNode(new UnaryExprNode("!", new BoolNode(false)))
                }
            );
            var program = new ProgramNode(new List<FunctionNode> { mainFunc });
            var interpreter = new Interpreter(program);
            var result = interpreter.Run();

            Assert.IsType<BoolValue>(result);
            Assert.True(((BoolValue)result).Value);
        }

        [Fact]
        public void Interpreter_IfStatementTrueBranch()
        {
            // func Int main() { if (true) { return 10; } else { return 20; } }
            var mainFunc = new FunctionNode(
                ReturnType: TypeNode.Int,
                Name: "main",
                Parameters: new List<ParamNode>(),
                Statements: new List<StatementNode> {
                    new IfNode(
                        Condition: new BoolNode(true),
                        ThenBranch: new List<StatementNode> { new ReturnNode(new NumberNode(10)) },
                        ElseBranch: new List<StatementNode> { new ReturnNode(new NumberNode(20)) }
                    )
                }
            );
            var program = new ProgramNode(new List<FunctionNode> { mainFunc });
            var interpreter = new Interpreter(program);
            var result = interpreter.Run();

            Assert.IsType<IntValue>(result);
            Assert.Equal(10, ((IntValue)result).Value);
        }

        [Fact]
        public void Interpreter_IfStatementFalseBranch()
        {
            // func Int main() { if (false) { return 10; } else { return 20; } }
            var mainFunc = new FunctionNode(
                ReturnType: TypeNode.Int,
                Name: "main",
                Parameters: new List<ParamNode>(),
                Statements: new List<StatementNode> {
                    new IfNode(
                        Condition: new BoolNode(false),
                        ThenBranch: new List<StatementNode> { new ReturnNode(new NumberNode(10)) },
                        ElseBranch: new List<StatementNode> { new ReturnNode(new NumberNode(20)) }
                    )
                }
            );
            var program = new ProgramNode(new List<FunctionNode> { mainFunc });
            var interpreter = new Interpreter(program);
            var result = interpreter.Run();

            Assert.IsType<IntValue>(result);
            Assert.Equal(20, ((IntValue)result).Value);
        }

        [Fact]
        public void Interpreter_WhileLoopCounts()
        {
            // func Int main() { Int x = 0; while (x < 3) { x = x + 1; } return x; }
            var mainFunc = new FunctionNode(
                ReturnType: TypeNode.Int,
                Name: "main",
                Parameters: new List<ParamNode>(),
                Statements: new List<StatementNode> {
                    new DeclNode(TypeNode.Int, "x", new ExprRhsNode(new NumberNode(0))),
                    new WhileNode(
                        Condition: new BinaryExprNode("<", new VarNode("x"), new NumberNode(3)),
                        Body: new List<StatementNode> {
                            new AssignNode(
                                new VarNode("x"),
                                new ExprRhsNode(new BinaryExprNode("+", new VarNode("x"), new NumberNode(1)))
                            )
                        }
                    ),
                    new ReturnNode(new VarNode("x"))
                }
            );
            var program = new ProgramNode(new List<FunctionNode> { mainFunc });
            var interpreter = new Interpreter(program);
            var result = interpreter.Run();

            Assert.IsType<IntValue>(result);
            Assert.Equal(3, ((IntValue)result).Value);
        }

        [Fact]
        public void Interpreter_WhileLoopExitsWhenConditionFalse()
        {
            // func Int main() { Int x = 10; while (x > 5) { x = x - 1; } return x; }
            var mainFunc = new FunctionNode(
                ReturnType: TypeNode.Int,
                Name: "main",
                Parameters: new List<ParamNode>(),
                Statements: new List<StatementNode> {
                    new DeclNode(TypeNode.Int, "x", new ExprRhsNode(new NumberNode(10))),
                    new WhileNode(
                        Condition: new BinaryExprNode(">", new VarNode("x"), new NumberNode(5)),
                        Body: new List<StatementNode> {
                            new AssignNode(
                                new VarNode("x"),
                                new ExprRhsNode(new BinaryExprNode("-", new VarNode("x"), new NumberNode(1)))
                            )
                        }
                    ),
                    new ReturnNode(new VarNode("x"))
                }
            );
            var program = new ProgramNode(new List<FunctionNode> { mainFunc });
            var interpreter = new Interpreter(program);
            var result = interpreter.Run();

            Assert.IsType<IntValue>(result);
            Assert.Equal(5, ((IntValue)result).Value);
        }

    [Fact]
    public void MailboxReceiveFrom_RemovesFirstMatchingSenderEvenWhenNotAtHead()
    {
        var mailbox = new Mailbox();
        mailbox.Send(new Message(1, new IntValue(10)));
        mailbox.Send(new Message(2, new IntValue(20)));
        mailbox.Send(new Message(1, new IntValue(30)));

        var fromTwo = mailbox.ReceiveFrom(2);
        Assert.Equal(2, fromTwo.SenderPid);
        Assert.Equal(new IntValue(20), fromTwo.Value);

        var firstFromOne = mailbox.ReceiveFrom(1);
        Assert.Equal(1, firstFromOne.SenderPid);
        Assert.Equal(new IntValue(10), firstFromOne.Value);

        var secondFromOne = mailbox.ReceiveFrom(1);
        Assert.Equal(1, secondFromOne.SenderPid);
        Assert.Equal(new IntValue(30), secondFromOne.Value);
    }

    [Fact]
    public void MailboxReceiveFrom_PreservesOrderOfOtherMessages()
    {
        var mailbox = new Mailbox();
        mailbox.Send(new Message(1, new IntValue(100)));
        mailbox.Send(new Message(2, new IntValue(200)));
        mailbox.Send(new Message(3, new IntValue(300)));
        mailbox.Send(new Message(1, new IntValue(400)));

        var fromThree = mailbox.ReceiveFrom(3);
        Assert.Equal(3, fromThree.SenderPid);
        Assert.Equal(new IntValue(300), fromThree.Value);

        var fromOne = mailbox.ReceiveFrom(1);
        Assert.Equal(1, fromOne.SenderPid);
        Assert.Equal(new IntValue(100), fromOne.Value);

        var fromTwo = mailbox.ReceiveFrom(2);
        Assert.Equal(2, fromTwo.SenderPid);
        Assert.Equal(new IntValue(200), fromTwo.Value);

        var secondFromOne = mailbox.ReceiveFrom(1);
        Assert.Equal(1, secondFromOne.SenderPid);
        Assert.Equal(new IntValue(400), secondFromOne.Value);
    }

    [Fact]
    public void Mailbox_ReceiveFromSpecificSender_IgnoresOthers()
    {
        var mailbox = new Mailbox();

        mailbox.Send(new Message(1, new IntValue(100)));
        mailbox.Send(new Message(2, new IntValue(200)));
        mailbox.Send(new Message(3, new IntValue(300)));
        mailbox.Send(new Message(1, new IntValue(101)));

        var msg1 = mailbox.ReceiveFrom(1);
        Assert.Equal(1, msg1.SenderPid);
        Assert.Equal(100, ((IntValue)msg1.Value).Value);

        var msg3 = mailbox.ReceiveFrom(3);
        Assert.Equal(3, msg3.SenderPid);
        Assert.Equal(300, ((IntValue)msg3.Value).Value);

        var msg2 = mailbox.ReceiveFrom(2);
        Assert.Equal(2, msg2.SenderPid);
        Assert.Equal(200, ((IntValue)msg2.Value).Value);

        var msg1b = mailbox.ReceiveFrom(1);
        Assert.Equal(1, msg1b.SenderPid);
        Assert.Equal(101, ((IntValue)msg1b.Value).Value);
    }

    [Fact]
    public void Mailbox_BooleanValueMessages()
    {
        var mailbox = new Mailbox();

        mailbox.Send(new Message(1, new BoolValue(true)));
        mailbox.Send(new Message(2, new BoolValue(false)));

        var msg1 = mailbox.ReceiveFrom(1);
        Assert.IsType<BoolValue>(msg1.Value);
        Assert.True(((BoolValue)msg1.Value).Value);

        var msg2 = mailbox.ReceiveFrom(2);
        Assert.IsType<BoolValue>(msg2.Value);
        Assert.False(((BoolValue)msg2.Value).Value);
    }

    [Fact]
    public void Mailbox_PidValueMessages()
    {
        var mailbox = new Mailbox();

        mailbox.Send(new Message(1, new PidValue(42)));
        mailbox.Send(new Message(2, new PidValue(99)));

        var msg1 = mailbox.ReceiveFrom(1);
        Assert.IsType<PidValue>(msg1.Value);
            Assert.Equal(42, ((PidValue)msg1.Value).Value);

        var msg2 = mailbox.ReceiveFrom(2);
        Assert.IsType<PidValue>(msg2.Value);
            Assert.Equal(99, ((PidValue)msg2.Value).Value);
    }

    [Fact]
    public void Mailbox_LargeNumberOfMessages()
    {
        var mailbox = new Mailbox();
        const int messageCount = 100;

        for (int i = 0; i < messageCount; i++)
        {
            mailbox.Send(new Message(1, new IntValue(i)));
        }

        for (int i = 0; i < messageCount; i++)
        {
            var msg = mailbox.ReceiveFrom(1);
            Assert.Equal(1, msg.SenderPid);
        }
    }

    [Fact]
    public void Interpreter_SpawnSendReceive_ReturnsMessageFromChild()
    {
        var childFunc = new FunctionNode(
            ReturnType: TypeNode.Int,
            Name: "child",
            Parameters: new List<ParamNode> { new ParamNode(TypeNode.Pid, "parent") },
            Statements: new List<StatementNode> {
                new SendNode(new NumberNode(123), new VarNode("parent")),
                new ReturnNode(new NumberNode(0))
            }
        );

        var mainFunc = new FunctionNode(
            ReturnType: TypeNode.Int,
            Name: "main",
            Parameters: new List<ParamNode>(),
            Statements: new List<StatementNode> {
                new DeclNode(TypeNode.Pid, "childPid", new SpawnRhsNode("child", new List<ExprNode> { new SelfNode() })),
                new DeclNode(TypeNode.Int, "msg", new ReceiveRhsNode(new VarNode("childPid"))),
                new ReturnNode(new VarNode("msg"))
            }
        );

        var program = new ProgramNode(new List<FunctionNode> { childFunc, mainFunc });
        var interpreter = new Interpreter(program);
        var result = interpreter.Run();

        Assert.IsType<IntValue>(result);
        Assert.Equal(123, ((IntValue)result).Value);
    }

    [Fact]
    public void Interpreter_MultipleChildren_SumMessages()
    {
        var childFunc = new FunctionNode(
            ReturnType: TypeNode.Int,
            Name: "child_sum",
            Parameters: new List<ParamNode> { new ParamNode(TypeNode.Pid, "parent"), new ParamNode(TypeNode.Int, "val") },
            Statements: new List<StatementNode> {
                new SendNode(new VarNode("val"), new VarNode("parent")),
                new ReturnNode(new NumberNode(0))
            }
        );

        var mainFunc = new FunctionNode(
            ReturnType: TypeNode.Int,
            Name: "main",
            Parameters: new List<ParamNode>(),
            Statements: new List<StatementNode> {
                new DeclNode(TypeNode.Pid, "c1", new SpawnRhsNode("child_sum", new List<ExprNode> { new SelfNode(), new NumberNode(10) })),
                new DeclNode(TypeNode.Pid, "c2", new SpawnRhsNode("child_sum", new List<ExprNode> { new SelfNode(), new NumberNode(20) })),
                new DeclNode(TypeNode.Pid, "c3", new SpawnRhsNode("child_sum", new List<ExprNode> { new SelfNode(), new NumberNode(30) })),
                new DeclNode(TypeNode.Int, "a", new ReceiveRhsNode(new VarNode("c1"))),
                new DeclNode(TypeNode.Int, "b", new ReceiveRhsNode(new VarNode("c2"))),
                new DeclNode(TypeNode.Int, "c", new ReceiveRhsNode(new VarNode("c3"))),
                new DeclNode(TypeNode.Int, "sum", new ExprRhsNode(new BinaryExprNode("+", new BinaryExprNode("+", new VarNode("a"), new VarNode("b")), new VarNode("c")))),
                new ReturnNode(new VarNode("sum"))
            }
        );

        var program = new ProgramNode(new List<FunctionNode> { childFunc, mainFunc });
        var interpreter = new Interpreter(program);
        var result = interpreter.Run();

        Assert.IsType<IntValue>(result);
        Assert.Equal(60, ((IntValue)result).Value);
    }

    [Fact]
    public void Interpreter_MessageOrderFromSameSender_IsPreserved()
    {
        var childFunc = new FunctionNode(
            ReturnType: TypeNode.Int,
            Name: "child_order",
            Parameters: new List<ParamNode> { new ParamNode(TypeNode.Pid, "parent") },
            Statements: new List<StatementNode> {
                new SendNode(new NumberNode(1), new VarNode("parent")),
                new SendNode(new NumberNode(2), new VarNode("parent")),
                new ReturnNode(new NumberNode(0))
            }
        );

        var mainFunc = new FunctionNode(
            ReturnType: TypeNode.Int,
            Name: "main",
            Parameters: new List<ParamNode>(),
            Statements: new List<StatementNode> {
                new DeclNode(TypeNode.Pid, "childPid", new SpawnRhsNode("child_order", new List<ExprNode> { new SelfNode() })),
                new DeclNode(TypeNode.Int, "first", new ReceiveRhsNode(new VarNode("childPid"))),
                new DeclNode(TypeNode.Int, "second", new ReceiveRhsNode(new VarNode("childPid"))),
                new DeclNode(TypeNode.Int, "combined", new ExprRhsNode(new BinaryExprNode("+", new BinaryExprNode("*", new VarNode("first"), new NumberNode(10)), new VarNode("second")))),
                new ReturnNode(new VarNode("combined"))
            }
        );

        var program = new ProgramNode(new List<FunctionNode> { childFunc, mainFunc });
        var interpreter = new Interpreter(program);
        var result = interpreter.Run();

        Assert.IsType<IntValue>(result);
        Assert.Equal(12, ((IntValue)result).Value);
    }

    [Fact]
    public void Interpreter_ChildSendsItsPid_MessageIsPidValue()
    {
        var childFunc = new FunctionNode(
            ReturnType: TypeNode.Int,
            Name: "child_pid",
            Parameters: new List<ParamNode> { new ParamNode(TypeNode.Pid, "parent") },
            Statements: new List<StatementNode> {
                new SendNode(new SelfNode(), new VarNode("parent")),
                new ReturnNode(new NumberNode(0))
            }
        );

        var mainFunc = new FunctionNode(
            ReturnType: TypeNode.Pid,
            Name: "main",
            Parameters: new List<ParamNode>(),
            Statements: new List<StatementNode> {
                new DeclNode(TypeNode.Pid, "childPid", new SpawnRhsNode("child_pid", new List<ExprNode> { new SelfNode() })),
                new DeclNode(TypeNode.Pid, "msg", new ReceiveRhsNode(new VarNode("childPid"))),
                new ReturnNode(new VarNode("msg"))
            }
        );

        var program = new ProgramNode(new List<FunctionNode> { childFunc, mainFunc });
        var interpreter = new Interpreter(program);
        var result = interpreter.Run();

        Assert.IsType<PidValue>(result);
        Assert.Equal(((PidValue)result).Value, ((PidValue)result).Value); // trivial check that it's a PidValue
    }

    // ===== Unit Tests: RuntimeProcess (Isolated process state) =====
    [Fact]
    public void RuntimeProcess_CreatedWithPid()
    {
        var process = new RuntimeProcess(42);
        
        Assert.Equal(42, process.Pid);
        Assert.NotNull(process.Store);
        Assert.Empty(process.Store);
    }

    [Fact]
    public void RuntimeProcess_StoresAndRetrievesVariables()
    {
        var process = new RuntimeProcess(1);
        process.Store["x"] = new IntValue(100);

        Assert.True(process.Store.ContainsKey("x"));
        Assert.Equal(100, ((IntValue)process.Store["x"]).Value);
    }

    [Fact]
    public void RuntimeProcess_IsolatesMemoryFromOtherProcesses()
    {
        var p1 = new RuntimeProcess(1);
        var p2 = new RuntimeProcess(2);

        p1.Store["var"] = new IntValue(100);
        p2.Store["var"] = new IntValue(200);

        Assert.Equal(100, ((IntValue)p1.Store["var"]).Value);
        Assert.Equal(200, ((IntValue)p2.Store["var"]).Value);
    }

    [Fact]
    public void RuntimeProcess_UpdatesExistingVariables()
    {
        var process = new RuntimeProcess(1);
        process.Store["x"] = new IntValue(5);
        process.Store["x"] = new IntValue(10);

        Assert.Equal(10, ((IntValue)process.Store["x"]).Value);
    }
}
