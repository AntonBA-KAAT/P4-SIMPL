using System.Collections.Generic;

public enum TypeNode
{
	Int,
	Bool,
	Pid,
}

public abstract record AstNode;

public sealed record ProgramNode(IReadOnlyList<FunctionNode> Functions) : AstNode;

public sealed record FunctionNode(TypeNode ReturnType, string Name, IReadOnlyList<ParamNode> Parameters, IReadOnlyList<StatementNode> Statements) : AstNode;

public sealed record ParamNode(TypeNode ParameterType, string Name) : AstNode;

public abstract record StatementNode : AstNode;

public sealed record IfNode(ExprNode Condition, IReadOnlyList<StatementNode> ThenBranch, IReadOnlyList<StatementNode> ElseBranch) : StatementNode;

public sealed record WhileNode(ExprNode Condition, IReadOnlyList<StatementNode> Body) : StatementNode;

public sealed record AssignNode(VarNode Name, RhsNode Value) : StatementNode;

public sealed record DeclNode(TypeNode DeclType, string Name, RhsNode Value) : StatementNode;

public sealed record PrintNode(ExprNode Value) : StatementNode;

public sealed record ReturnNode(ExprNode Value) : StatementNode;

public sealed record SendNode(ExprNode Message, ExprNode Target) : StatementNode;

public sealed record SkipNode() : StatementNode;

public abstract record RhsNode : AstNode;

public sealed record ReceiveRhsNode(ExprNode Source) : RhsNode;

public sealed record SpawnRhsNode(string Callee, IReadOnlyList<ExprNode> Arguments) : RhsNode;

public sealed record CallRhsNode(string Callee, IReadOnlyList<ExprNode> Arguments) : RhsNode;

public sealed record ExprRhsNode(ExprNode Value) : RhsNode;

public abstract record ExprNode : AstNode;

public sealed record NumberNode(int Value) : ExprNode
{
	public NumberNode(string value) : this(int.Parse(value)) { }
}

public sealed record BoolNode(bool Value) : ExprNode;

public sealed record VarNode(string Name) : ExprNode;

public sealed record SelfNode() : ExprNode;

public sealed record CallExprNode(string Name, IReadOnlyList<ExprNode> Arguments) : ExprNode;

public sealed record UnaryExprNode(string Operator, ExprNode Operand) : ExprNode;

public sealed record BinaryExprNode(string Operator, ExprNode Left, ExprNode Right) : ExprNode;
