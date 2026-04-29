using System.Text;

public static class AstPrinter
{
	public static string Print(ProgramNode program)
	{
		var sb = new StringBuilder();
		sb.AppendLine("Program");
		for (var i = 0; i < program.Functions.Count; i++)
		{
			PrintFunction(sb, program.Functions[i], "  ");
		}

		return sb.ToString();
	}

	private static void PrintFunction(StringBuilder sb, FunctionNode fn, string indent)
	{
		sb.AppendLine($"{indent}Function {fn.ReturnType} {fn.Name}");
		sb.AppendLine($"{indent}  Params ({fn.Parameters.Count})");
		foreach (var p in fn.Parameters)
		{
			sb.AppendLine($"{indent}    {p.ParameterType} {p.Name}");
		}

		sb.AppendLine($"{indent}  Body ({fn.Statements.Count})");
		foreach (var s in fn.Statements)
		{
			PrintStatement(sb, s, indent + "    ");
		}
	}

	private static void PrintStatement(StringBuilder sb, StatementNode stmt, string indent)
	{
		switch (stmt)
		{
			case IfNode n:
				sb.AppendLine($"{indent}If {PrintExpr(n.Condition)}");
				sb.AppendLine($"{indent}  Then");
				foreach (var s in n.ThenBranch) PrintStatement(sb, s, indent + "    ");
				sb.AppendLine($"{indent}  Else");
				foreach (var s in n.ElseBranch) PrintStatement(sb, s, indent + "    ");
				break;
			case WhileNode n:
				sb.AppendLine($"{indent}While {PrintExpr(n.Condition)}");
				foreach (var s in n.Body) PrintStatement(sb, s, indent + "  ");
				break;
			case AssignNode n:
				sb.AppendLine($"{indent}Assign {n.Name} = {PrintRhs(n.Value)}");
				break;
			case DeclNode n:
				sb.AppendLine($"{indent}Decl {n.DeclType} {n.Name} = {PrintRhs(n.Value)}");
				break;
			case PrintNode n:
				sb.AppendLine($"{indent}Print {PrintExpr(n.Value)}");
				break;
			case ReturnNode n:
				sb.AppendLine($"{indent}Return {PrintExpr(n.Value)}");
				break;
			case SendNode n:
				sb.AppendLine($"{indent}Send {PrintExpr(n.Message)} to {PrintExpr(n.Target)}");
				break;
			case SkipNode:
				sb.AppendLine($"{indent}Skip");
				break;
		}
	}

	private static string PrintRhs(RhsNode rhs) => rhs switch
	{
		ReceiveRhsNode r => $"receive({PrintExpr(r.Source)})",
		SpawnRhsNode s => $"spawn {s.Callee}({string.Join(", ", s.Arguments.Select(PrintExpr))})",
		CallRhsNode c => $"call {c.Callee}({string.Join(", ", c.Arguments.Select(PrintExpr))})",
		ExprRhsNode e => PrintExpr(e.Value),
		_ => "<rhs?>",
	};

	private static string PrintExpr(ExprNode expr) => expr switch
	{
		NumberNode n => n.Value.ToString(),
		BoolNode b => b.Value ? "true" : "false",
		VarNode v => v.Name,
		SelfNode => "self",
		CallExprNode c => $"{c.Name}({string.Join(", ", (c.Arguments ?? []).Select(PrintExpr))})",
		UnaryExprNode u => $"({u.Operator}{PrintExpr(u.Operand)})",
		BinaryExprNode b => $"({PrintExpr(b.Left)} {b.Operator} {PrintExpr(b.Right)})",
		_ => "<expr?>",
	};
}
