
using System;

namespace MyLangAstGen {



public class Parser {
	public const int _EOF = 0;
	public const int _ident = 1;
	public const int _number = 2;
	public const int maxT = 41;

	const bool _T = true;
	const bool _x = false;
	const int minErrDist = 2;
	
	public Scanner scanner;
	public Errors  errors;

	public Token t;    // last recognized token
	public Token la;   // lookahead token
	int errDist = minErrDist;

public ProgramNode ProgramResult;
public System.Collections.Generic.List<FunctionNode> FunctionsResult;
public FunctionNode FunctionResult;
public System.Collections.Generic.List<ParamNode> ParamsResult;
public ParamNode ParamResult;
public TypeNode TypeResult;
public System.Collections.Generic.List<StatementNode> StmtSeqResult;
public StatementNode StatementResult;
public RhsNode RhsResult;
public System.Collections.Generic.List<ExprNode> ArgsResult;
public ExprNode ExprResult;



	public Parser(Scanner scanner) {
		this.scanner = scanner;
		errors = new Errors();
	}

	void SynErr (int n) {
		if (errDist >= minErrDist) errors.SynErr(la.line, la.col, n);
		errDist = 0;
	}

	public void SemErr (string msg) {
		if (errDist >= minErrDist) errors.SemErr(t.line, t.col, msg);
		errDist = 0;
	}
	
	void Get () {
		for (;;) {
			t = la;
			la = scanner.Scan();
			if (la.kind <= maxT) { ++errDist; break; }

			la = t;
		}
	}
	
	void Expect (int n) {
		if (la.kind==n) Get(); else { SynErr(n); }
	}
	
	bool StartOf (int s) {
		return set[s, la.kind];
	}
	
	void ExpectWeak (int n, int follow) {
		if (la.kind == n) Get();
		else {
			SynErr(n);
			while (!StartOf(follow)) Get();
		}
	}


	bool WeakSeparator(int n, int syFol, int repFol) {
		int kind = la.kind;
		if (kind == n) {Get(); return true;}
		else if (StartOf(repFol)) {return false;}
		else {
			SynErr(n);
			while (!(set[syFol, kind] || set[repFol, kind] || set[0, kind])) {
				Get();
				kind = la.kind;
			}
			return StartOf(syFol);
		}
	}

	
	void MyLangAST() {
		FunctionsResult = new System.Collections.Generic.List<FunctionNode>(); 
		while (la.kind == 3) {
			FunctionDecl();
			FunctionsResult.Add(FunctionResult); 
		}
		ProgramResult = new ProgramNode(FunctionsResult); 
	}

	void FunctionDecl() {
		string functionName; TypeNode returnType; 
		Expect(3);
		Type();
		returnType = TypeResult; 
		Expect(1);
		functionName = t.val; 
		Expect(4);
		ParamsResult = new System.Collections.Generic.List<ParamNode>(); 
		if (la.kind == 9 || la.kind == 10 || la.kind == 11) {
			Params();
		}
		Expect(5);
		Expect(6);
		StmtSeq();
		Expect(7);
		FunctionResult = new FunctionNode(returnType, functionName, ParamsResult, StmtSeqResult); 
	}

	void Type() {
		if (la.kind == 9) {
			Get();
			TypeResult = TypeNode.Int; 
		} else if (la.kind == 10) {
			Get();
			TypeResult = TypeNode.Bool; 
		} else if (la.kind == 11) {
			Get();
			TypeResult = TypeNode.Pid; 
		} else SynErr(42);
	}

	void Params() {
		var paramsList = new System.Collections.Generic.List<ParamNode>(); 
		Param();
		paramsList.Add(ParamResult); 
		while (la.kind == 8) {
			Get();
			Param();
			paramsList.Add(ParamResult); 
		}
		ParamsResult = paramsList; 
	}

	void StmtSeq() {
		var statements = new System.Collections.Generic.List<StatementNode>(); 
		if (StartOf(1)) {
			Statement();
			statements.Add(StatementResult); 
			while (la.kind == 12) {
				Get();
				Statement();
				statements.Add(StatementResult); 
			}
			if (la.kind == 12) {
				Get();
			}
		}
		StmtSeqResult = statements; 
	}

	void Param() {
		string paramName; 
		Type();
		Expect(1);
		paramName = t.val; 
		ParamResult = new ParamNode(TypeResult, paramName); 
	}

	void Statement() {
		switch (la.kind) {
		case 13: {
			IfStmt();
			break;
		}
		case 15: {
			WhileStmt();
			break;
		}
		case 1: {
			AssignStmt();
			break;
		}
		case 9: case 10: case 11: {
			DeclStmt();
			break;
		}
		case 17: {
			PrintStmt();
			break;
		}
		case 18: {
			ReturnStmt();
			break;
		}
		case 19: {
			SendStmt();
			break;
		}
		case 21: {
			SkipStmt();
			break;
		}
		default: SynErr(43); break;
		}
	}

	void IfStmt() {
		ExprNode condition; System.Collections.Generic.List<StatementNode> thenBranch = null; System.Collections.Generic.List<StatementNode> elseBranch = null; 
		Expect(13);
		Expect(4);
		Expr();
		condition = ExprResult; 
		Expect(5);
		Expect(6);
		StmtSeq();
		thenBranch = StmtSeqResult; 
		Expect(7);
		Expect(14);
		Expect(6);
		StmtSeq();
		elseBranch = StmtSeqResult; 
		Expect(7);
		StatementResult = new IfNode(condition, thenBranch, elseBranch); 
	}

	void WhileStmt() {
		ExprNode condition; System.Collections.Generic.List<StatementNode> body = null; 
		Expect(15);
		Expect(4);
		Expr();
		condition = ExprResult; 
		Expect(5);
		Expect(6);
		StmtSeq();
		body = StmtSeqResult; 
		Expect(7);
		StatementResult = new WhileNode(condition, body); 
	}

	void AssignStmt() {
		string name; 
		Expect(1);
		name = t.val; 
		Expect(16);
		RHS();
		StatementResult = new AssignNode(new VarNode(name), RhsResult); 
	}

	void DeclStmt() {
		string name; 
		Type();
		Expect(1);
		name = t.val; 
		Expect(16);
		RHS();
		StatementResult = new DeclNode(TypeResult, name, RhsResult); 
	}

	void PrintStmt() {
		Expect(17);
		Expect(4);
		Expr();
		Expect(5);
		StatementResult = new PrintNode(ExprResult); 
	}

	void ReturnStmt() {
		Expect(18);
		Expr();
		StatementResult = new ReturnNode(ExprResult); 
	}

	void SendStmt() {
		ExprNode message; ExprNode target; 
		Expect(19);
		Expr();
		message = ExprResult; 
		Expect(20);
		Expr();
		target = ExprResult; 
		StatementResult = new SendNode(message, target); 
	}

	void SkipStmt() {
		Expect(21);
		StatementResult = new SkipNode(); 
	}

	void Expr() {
		OrExp();
	}

	void RHS() {
		if (la.kind == 22) {
			Get();
			Expect(4);
			Expr();
			Expect(5);
			RhsResult = new ReceiveRhsNode(ExprResult); 
		} else if (la.kind == 23) {
			Get();
			Expect(1);
			string spawnName = t.val; 
			Expect(4);
			if (StartOf(2)) {
				Args();
			}
			if (ArgsResult == null) ArgsResult = new System.Collections.Generic.List<ExprNode>(); 
			Expect(5);
			RhsResult = new SpawnRhsNode(spawnName, ArgsResult); 
		} else if (la.kind == 24) {
			Get();
			Expect(1);
			string callName = t.val; 
			Expect(4);
			if (StartOf(2)) {
				Args();
			}
			if (ArgsResult == null) ArgsResult = new System.Collections.Generic.List<ExprNode>(); 
			Expect(5);
			RhsResult = new CallRhsNode(callName, ArgsResult); 
		} else if (StartOf(2)) {
			Expr();
			RhsResult = new ExprRhsNode(ExprResult); 
		} else SynErr(44);
	}

	void Args() {
		var args = new System.Collections.Generic.List<ExprNode>(); 
		Expr();
		args.Add(ExprResult); 
		while (la.kind == 8) {
			Get();
			Expr();
			args.Add(ExprResult); 
		}
		ArgsResult = args; 
	}

	void OrExp() {
		AndExp();
		ExprNode left = ExprResult; 
		while (la.kind == 25) {
			Get();
			AndExp();
			left = new BinaryExprNode("||", left, ExprResult); 
		}
		ExprResult = left; 
	}

	void AndExp() {
		EqExp();
		ExprNode left = ExprResult; 
		while (la.kind == 26) {
			Get();
			EqExp();
			left = new BinaryExprNode("&&", left, ExprResult); 
		}
		ExprResult = left; 
	}

	void EqExp() {
		RelExp();
		ExprNode left = ExprResult; 
		while (la.kind == 27 || la.kind == 28) {
			if (la.kind == 27) {
				Get();
			} else {
				Get();
			}
			string op = t.val; 
			RelExp();
			left = new BinaryExprNode(op, left, ExprResult); 
		}
		ExprResult = left; 
	}

	void RelExp() {
		AddExp();
		ExprNode left = ExprResult; 
		while (StartOf(3)) {
			if (la.kind == 29) {
				Get();
			} else if (la.kind == 30) {
				Get();
			} else if (la.kind == 31) {
				Get();
			} else {
				Get();
			}
			string op = t.val; 
			AddExp();
			left = new BinaryExprNode(op, left, ExprResult); 
		}
		ExprResult = left; 
	}

	void AddExp() {
		MulExp();
		ExprNode left = ExprResult; 
		while (la.kind == 33 || la.kind == 34) {
			if (la.kind == 33) {
				Get();
			} else {
				Get();
			}
			string op = t.val; 
			MulExp();
			left = new BinaryExprNode(op, left, ExprResult); 
		}
		ExprResult = left; 
	}

	void MulExp() {
		UnaryExp();
		ExprNode left = ExprResult; 
		while (la.kind == 35 || la.kind == 36) {
			if (la.kind == 35) {
				Get();
			} else {
				Get();
			}
			string op = t.val; 
			UnaryExp();
			left = new BinaryExprNode(op, left, ExprResult); 
		}
		ExprResult = left; 
	}

	void UnaryExp() {
		if (la.kind == 37) {
			Get();
			UnaryExp();
			ExprResult = new UnaryExprNode("!", ExprResult); 
		} else if (la.kind == 34) {
			Get();
			UnaryExp();
			ExprResult = new UnaryExprNode("-", ExprResult); 
		} else if (StartOf(4)) {
			Primary();
		} else SynErr(45);
	}

	void Primary() {
		switch (la.kind) {
		case 2: {
			Get();
			ExprResult = new NumberNode(int.Parse(t.val)); 
			break;
		}
		case 38: {
			Get();
			ExprResult = new BoolNode(true); 
			break;
		}
		case 39: {
			Get();
			ExprResult = new BoolNode(false); 
			break;
		}
		case 1: {
			Get();
			ExprResult = new VarNode(t.val); 
			break;
		}
		case 40: {
			Get();
			ExprResult = new SelfNode(); 
			break;
		}
		case 4: {
			Get();
			Expr();
			Expect(5);
			break;
		}
		default: SynErr(46); break;
		}
	}



	public void Parse() {
		la = new Token();
		la.val = "";		
		Get();
		MyLangAST();
		Expect(0);

	}
	
	static readonly bool[,] set = {
		{_T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x},
		{_x,_T,_x,_x, _x,_x,_x,_x, _x,_T,_T,_T, _x,_T,_x,_T, _x,_T,_T,_T, _x,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x},
		{_x,_T,_T,_x, _T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_T,_x, _x,_T,_T,_T, _T,_x,_x},
		{_x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_T,_T,_T, _T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x},
		{_x,_T,_T,_x, _T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_T,_T, _T,_x,_x}

	};
} // end Parser


public class Errors {
	public int count = 0;                                    // number of errors detected
	public System.IO.TextWriter errorStream = Console.Out;   // error messages go to this stream
	public string errMsgFormat = "-- line {0} col {1}: {2}"; // 0=line, 1=column, 2=text

	public virtual void SynErr (int line, int col, int n) {
		string s;
		switch (n) {
			case 0: s = "EOF expected"; break;
			case 1: s = "ident expected"; break;
			case 2: s = "number expected"; break;
			case 3: s = "\"func\" expected"; break;
			case 4: s = "\"(\" expected"; break;
			case 5: s = "\")\" expected"; break;
			case 6: s = "\"{\" expected"; break;
			case 7: s = "\"}\" expected"; break;
			case 8: s = "\",\" expected"; break;
			case 9: s = "\"Int\" expected"; break;
			case 10: s = "\"Bool\" expected"; break;
			case 11: s = "\"Pid\" expected"; break;
			case 12: s = "\";\" expected"; break;
			case 13: s = "\"if\" expected"; break;
			case 14: s = "\"else\" expected"; break;
			case 15: s = "\"while\" expected"; break;
			case 16: s = "\"=\" expected"; break;
			case 17: s = "\"print\" expected"; break;
			case 18: s = "\"return\" expected"; break;
			case 19: s = "\"send\" expected"; break;
			case 20: s = "\"to\" expected"; break;
			case 21: s = "\"skip\" expected"; break;
			case 22: s = "\"receive\" expected"; break;
			case 23: s = "\"spawn\" expected"; break;
			case 24: s = "\"call\" expected"; break;
			case 25: s = "\"||\" expected"; break;
			case 26: s = "\"&&\" expected"; break;
			case 27: s = "\"==\" expected"; break;
			case 28: s = "\"!=\" expected"; break;
			case 29: s = "\"<\" expected"; break;
			case 30: s = "\">\" expected"; break;
			case 31: s = "\"<=\" expected"; break;
			case 32: s = "\">=\" expected"; break;
			case 33: s = "\"+\" expected"; break;
			case 34: s = "\"-\" expected"; break;
			case 35: s = "\"*\" expected"; break;
			case 36: s = "\"/\" expected"; break;
			case 37: s = "\"!\" expected"; break;
			case 38: s = "\"true\" expected"; break;
			case 39: s = "\"false\" expected"; break;
			case 40: s = "\"self\" expected"; break;
			case 41: s = "??? expected"; break;
			case 42: s = "invalid Type"; break;
			case 43: s = "invalid Statement"; break;
			case 44: s = "invalid RHS"; break;
			case 45: s = "invalid UnaryExp"; break;
			case 46: s = "invalid Primary"; break;

			default: s = "error " + n; break;
		}
		errorStream.WriteLine(errMsgFormat, line, col, s);
		count++;
	}

	public virtual void SemErr (int line, int col, string s) {
		errorStream.WriteLine(errMsgFormat, line, col, s);
		count++;
	}
	
	public virtual void SemErr (string s) {
		errorStream.WriteLine(s);
		count++;
	}
	
	public virtual void Warning (int line, int col, string s) {
		errorStream.WriteLine(errMsgFormat, line, col, s);
	}
	
	public virtual void Warning(string s) {
		errorStream.WriteLine(s);
	}
} // Errors


public class FatalError: Exception {
	public FatalError(string m): base(m) {}
}
}