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
}
