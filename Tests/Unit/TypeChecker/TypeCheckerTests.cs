using System;
using System.Collections.Generic;
using Xunit;

public class TypeCheckerTests
{
    [Fact]
    public void AcceptsFunctionWithSimpleReturn()
    {
        var program = new ProgramNode(new List<FunctionNode>
        {
            new(
                TypeNode.Int,
                "main",
                new List<ParamNode>(),
                new List<StatementNode>
                {
                    new ReturnNode(new NumberNode("1"))
                })
        });

        var checker = new TypeChecker();
        checker.CheckProgram(program);
    }

    [Fact]
    public void RejectsFunctionWithoutReturnOnAllPaths()
    {
        var program = new ProgramNode(new List<FunctionNode>
        {
            new(
                TypeNode.Int,
                "main",
                new List<ParamNode>(),
                new List<StatementNode>
                {
                    new DeclNode(TypeNode.Int, "x", new ExprRhsNode(new NumberNode("1"))),
                    new SkipNode()
                })
        });

        var checker = new TypeChecker();
        var ex = Assert.Throws<TypeCheckException>(() => checker.CheckProgram(program));
        Assert.Contains("may not return a value on all paths", ex.Message);
    }

    [Fact]
    public void RejectsIfWhenOnlyOneBranchReturns()
    {
        var program = new ProgramNode(new List<FunctionNode>
        {
            new(
                TypeNode.Int,
                "main",
                new List<ParamNode>(),
                new List<StatementNode>
                {
                    new IfNode(
                        new BoolNode(true),
                        new List<StatementNode>
                        {
                            new ReturnNode(new NumberNode("1"))
                        },
                        new List<StatementNode>
                        {
                            new SkipNode()
                        })
                })
        });

        var checker = new TypeChecker();
        var ex = Assert.Throws<TypeCheckException>(() => checker.CheckProgram(program));
        Assert.Contains("may not return a value on all paths", ex.Message);
    }

    [Fact]
    public void AcceptsIfWhenBothBranchesReturn()
    {
        var program = new ProgramNode(new List<FunctionNode>
        {
            new(
                TypeNode.Int,
                "main",
                new List<ParamNode>(),
                new List<StatementNode>
                {
                    new IfNode(
                        new BoolNode(true),
                        new List<StatementNode>
                        {
                            new ReturnNode(new NumberNode("1"))
                        },
                        new List<StatementNode>
                        {
                            new ReturnNode(new NumberNode("2"))
                        })
                })
        });

        var checker = new TypeChecker();
        checker.CheckProgram(program);
    }

    [Fact]
    public void RejectsWrongReturnType()
    {
        var program = new ProgramNode(new List<FunctionNode>
        {
            new(
                TypeNode.Int,
                "main",
                new List<ParamNode>(),
                new List<StatementNode>
                {
                    new ReturnNode(new BoolNode(true))
                })
        });

        var checker = new TypeChecker();
        var ex = Assert.Throws<TypeCheckException>(() => checker.CheckProgram(program));
        Assert.Contains("Return type mismatch", ex.Message);
    }

    [Fact]
    public void RejectsWhileOnlyReturnAsNotGuaranteed()
    {
        var program = new ProgramNode(new List<FunctionNode>
        {
            new(
                TypeNode.Int,
                "main",
                new List<ParamNode>(),
                new List<StatementNode>
                {
                    new WhileNode(
                        new BoolNode(true),
                        new List<StatementNode>
                        {
                            new ReturnNode(new NumberNode("1"))
                        })
                })
        });

        var checker = new TypeChecker();
        var ex = Assert.Throws<TypeCheckException>(() => checker.CheckProgram(program));
        Assert.Contains("may not return a value on all paths", ex.Message);
    }
    [Fact]
    public void RejectsDuplicateParameterNames()
    {
        var program = new ProgramNode(new List<FunctionNode>
        {
            new(
                TypeNode.Int,
                "main",
                new List<ParamNode>
                {
                    new(TypeNode.Int, "x"),
                    new(TypeNode.Bool, "x")
                },
                new List<StatementNode>
                {
                    new ReturnNode(new NumberNode("0"))
                })
        });

        var checker = new TypeChecker();

        var ex = Assert.Throws<TypeCheckException>(() => checker.CheckProgram(program));
        Assert.Contains("conflicts with an existing identifier", ex.Message);
    }
    [Fact]
    public void RejectsSpawnWithWrongArgumentType()
    {
        var program = new ProgramNode(new List<FunctionNode>
        {
            new(
                TypeNode.Int,
                "worker",
                new List<ParamNode>
                {
                    new(TypeNode.Int, "n")
                },
                new List<StatementNode>
                {
                    new ReturnNode(new NumberNode("0"))
                }),

            new(
                TypeNode.Int,
                "main",
                new List<ParamNode>(),
                new List<StatementNode>
                {
                    new DeclNode(
                        TypeNode.Pid,
                        "p",
                        new SpawnRhsNode(
                            "worker",
                            new List<ExprNode>
                            {
                                new BoolNode(true)
                            })),

                    new ReturnNode(new NumberNode("0"))
                })
        });

        var checker = new TypeChecker();

        var ex = Assert.Throws<TypeCheckException>(() => checker.CheckProgram(program));
        Assert.Contains("Argument 1 of spawn worker", ex.Message);
    }
    [Fact]
    public void RejectsReceiveFromNonPid()
    {
        var program = new ProgramNode(new List<FunctionNode>
        {
            new(
                TypeNode.Int,
                "main",
                new List<ParamNode>(),
                new List<StatementNode>
                {
                    new DeclNode(
                        TypeNode.Int,
                        "x",
                        new ReceiveRhsNode(new NumberNode("1"))),

                    new ReturnNode(new NumberNode("0"))
                })
        });

        var checker = new TypeChecker();

        var ex = Assert.Throws<TypeCheckException>(() => checker.CheckProgram(program));
        Assert.Contains("Receive source must be of type Pid", ex.Message);
    }
    [Fact]
    public void RejectsSendToNonPid()
    {
        var program = new ProgramNode(new List<FunctionNode>
        {
            new(
                TypeNode.Int,
                "main",
                new List<ParamNode>(),
                new List<StatementNode>
                {
                    new SendNode(new NumberNode("1"), new NumberNode("2")),
                    new ReturnNode(new NumberNode("0"))
                })
        });

        var checker = new TypeChecker();

        var ex = Assert.Throws<TypeCheckException>(() => checker.CheckProgram(program));
        Assert.Contains("Send target must be of type Pid", ex.Message);
    }
    [Fact]
    public void RejectsDuplicateFunctionNames()
    {
        var program = new ProgramNode(new List<FunctionNode>
        {
            new(TypeNode.Int, "main", new List<ParamNode>(), new List<StatementNode>
            {
                new ReturnNode(new NumberNode("0"))
            }),
            new(TypeNode.Int, "main", new List<ParamNode>(), new List<StatementNode>
            {
                new ReturnNode(new NumberNode("1"))
            })
        });

        var checker = new TypeChecker();

        var ex = Assert.Throws<TypeCheckException>(() => checker.CheckProgram(program));
        Assert.Contains("Duplicate function name", ex.Message);
    }
    [Fact]
    public void RejectsCallWithWrongArgumentCount()
    {
        var program = new ProgramNode(new List<FunctionNode>
        {
            new(TypeNode.Int, "add", new List<ParamNode>
            {
                new(TypeNode.Int, "a"),
                new(TypeNode.Int, "b")
            },
            new List<StatementNode>
            {
                new ReturnNode(new BinaryExprNode("+", new VarNode("a"), new VarNode("b")))
            }),

            new(TypeNode.Int, "main", new List<ParamNode>(), new List<StatementNode>
            {
                new DeclNode(
                    TypeNode.Int,
                    "x",
                    new CallRhsNode("add", new List<ExprNode>
                    {
                        new NumberNode("1")
                    })),

                new ReturnNode(new VarNode("x"))
            })
        });

        var checker = new TypeChecker();

        var ex = Assert.Throws<TypeCheckException>(() => checker.CheckProgram(program));
        Assert.Contains("expects 2 arguments", ex.Message);
    }
    [Fact]
    public void RejectsSpawnWithWrongArgumentCount()
    {
        var program = new ProgramNode(new List<FunctionNode>
        {
            new(TypeNode.Int, "worker", new List<ParamNode>
            {
                new(TypeNode.Int, "x")
            },
            new List<StatementNode>
            {
                new ReturnNode(new NumberNode("0"))
            }),

            new(TypeNode.Int, "main", new List<ParamNode>(), new List<StatementNode>
            {
                new DeclNode(
                    TypeNode.Pid,
                    "p",
                    new SpawnRhsNode("worker", new List<ExprNode>())),

                new ReturnNode(new NumberNode("0"))
            })
        });

        var checker = new TypeChecker();

        var ex = Assert.Throws<TypeCheckException>(() => checker.CheckProgram(program));
        Assert.Contains("expects 1 arguments", ex.Message);
    }
    [Fact]
    public void RejectsSendMessageThatIsNotInt()
    {
        var program = new ProgramNode(new List<FunctionNode>
        {
            new(TypeNode.Int, "main", new List<ParamNode>(), new List<StatementNode>
            {
                new SendNode(new BoolNode(true), new SelfNode()),
                new ReturnNode(new NumberNode("0"))
            })
        });

        var checker = new TypeChecker();

        var ex = Assert.Throws<TypeCheckException>(() => checker.CheckProgram(program));
        Assert.Contains("Send message must be of type Int", ex.Message);
    }
    [Fact]
    public void RejectsCallToVariableName()
    {
        var program = new ProgramNode(new List<FunctionNode>
        {
            new(TypeNode.Int, "main", new List<ParamNode>(), new List<StatementNode>
            {
                new DeclNode(TypeNode.Int, "x", new ExprRhsNode(new NumberNode("1"))),

                new DeclNode(
                    TypeNode.Int,
                    "y",
                    new CallRhsNode("x", new List<ExprNode>())),

                new ReturnNode(new VarNode("y"))
            })
        });

        var checker = new TypeChecker();

        var ex = Assert.Throws<TypeCheckException>(() => checker.CheckProgram(program));
        Assert.Contains("is a variable, not a function", ex.Message);
    }
    [Fact]
    public void RejectsAssignmentToFunctionName()
    {
        var program = new ProgramNode(new List<FunctionNode>
        {
            new(TypeNode.Int, "foo", new List<ParamNode>(), new List<StatementNode>
            {
                new ReturnNode(new NumberNode("0"))
            }),

            new(TypeNode.Int, "main", new List<ParamNode>(), new List<StatementNode>
            {
                new AssignNode(new VarNode("foo"), new ExprRhsNode(new NumberNode("1"))),
                new ReturnNode(new NumberNode("0"))
            })
        });

        var checker = new TypeChecker();

        var ex = Assert.Throws<TypeCheckException>(() => checker.CheckProgram(program));
        Assert.Contains("is a function, not a variable", ex.Message);
    }
}
