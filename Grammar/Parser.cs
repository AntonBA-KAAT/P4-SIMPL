
using System;



public class Parser {
	public const int _EOF = 0;
	public const int _ident = 1;
	public const int _number = 2;
	public const int maxT = 40;

	const bool _T = true;
	const bool _x = false;
	const int minErrDist = 2;
	
	public Scanner scanner;
	public Errors  errors;

	public Token t;    // last recognized token
	public Token la;   // lookahead token
	int errDist = minErrDist;



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

	
	void MyLang() {
		while (la.kind == 3) {
			Function();
		}
	}

	void Function() {
		Expect(3);
		Type();
		Expect(1);
		Expect(4);
		Params();
		Expect(5);
		Expect(6);
		StatementList();
		Expect(7);
	}

	void Type() {
		if (la.kind == 9) {
			Get();
		} else if (la.kind == 10) {
			Get();
		} else if (la.kind == 11) {
			Get();
		} else SynErr(41);
	}

	void Params() {
		if (la.kind == 9 || la.kind == 10 || la.kind == 11) {
			Param();
			while (la.kind == 8) {
				Get();
				Param();
			}
		} else if (la.kind == 5) {
		} else SynErr(42);
	}

	void StatementList() {
		if (StartOf(1)) {
			Statement();
			while (la.kind == 12) {
				Get();
				Statement();
			}
		}
	}

	void Param() {
		Type();
		Expect(1);
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
		Expect(13);
		Expect(4);
		Expr();
		Expect(5);
		Expect(6);
		StatementList();
		Expect(7);
		Expect(14);
		Expect(6);
		StatementList();
		Expect(7);
	}

	void WhileStmt() {
		Expect(15);
		Expect(4);
		Expr();
		Expect(5);
		Expect(6);
		StatementList();
		Expect(7);
	}

	void AssignStmt() {
		Expect(1);
		Expect(16);
		RHS();
	}

	void DeclStmt() {
		Type();
		Expect(1);
		Expect(16);
		Expr();
	}

	void PrintStmt() {
		Expect(17);
		Expect(4);
		Expr();
		Expect(5);
	}

	void ReturnStmt() {
		Expect(18);
		Expr();
	}

	void SendStmt() {
		Expect(19);
		Expr();
		Expect(20);
		Expr();
	}

	void SkipStmt() {
		Expect(21);
	}

	void Expr() {
		OrExp();
	}

	void RHS() {
		if (la.kind == 22) {
			Get();
			Expect(4);
			Expect(5);
		} else if (la.kind == 23) {
			Get();
			Expect(1);
			Expect(4);
			Args();
			Expect(5);
		} else if (StartOf(2)) {
			Expr();
		} else SynErr(44);
	}

	void Args() {
		if (StartOf(2)) {
			Expr();
			while (la.kind == 8) {
				Get();
				Expr();
			}
		} else if (la.kind == 5) {
		} else SynErr(45);
	}

	void OrExp() {
		AndExp();
		while (la.kind == 24) {
			Get();
			AndExp();
		}
	}

	void AndExp() {
		EqExp();
		while (la.kind == 25) {
			Get();
			EqExp();
		}
	}

	void EqExp() {
		RelExp();
		if (la.kind == 26 || la.kind == 27) {
			if (la.kind == 26) {
				Get();
			} else {
				Get();
			}
			RelExp();
		}
	}

	void RelExp() {
		AddExp();
		if (StartOf(3)) {
			if (la.kind == 28) {
				Get();
			} else if (la.kind == 29) {
				Get();
			} else if (la.kind == 30) {
				Get();
			} else {
				Get();
			}
			AddExp();
		}
	}

	void AddExp() {
		MulExp();
		while (la.kind == 32 || la.kind == 33) {
			if (la.kind == 32) {
				Get();
			} else {
				Get();
			}
			MulExp();
		}
	}

	void MulExp() {
		UnaryExp();
		while (la.kind == 34 || la.kind == 35) {
			if (la.kind == 34) {
				Get();
			} else {
				Get();
			}
			UnaryExp();
		}
	}

	void UnaryExp() {
		if (la.kind == 36) {
			Get();
			UnaryExp();
		} else if (la.kind == 33) {
			Get();
			UnaryExp();
		} else if (StartOf(4)) {
			Primary();
		} else SynErr(46);
	}

	void Primary() {
		switch (la.kind) {
		case 2: {
			Get();
			break;
		}
		case 37: {
			Get();
			break;
		}
		case 38: {
			Get();
			break;
		}
		case 1: {
			Get();
			PrimaryTail();
			break;
		}
		case 39: {
			Get();
			break;
		}
		case 4: {
			Get();
			Expr();
			Expect(5);
			break;
		}
		default: SynErr(47); break;
		}
	}

	void PrimaryTail() {
		if (la.kind == 4) {
			Get();
			Args();
			Expect(5);
		} else if (StartOf(5)) {
		} else SynErr(48);
	}



	public void Parse() {
		la = new Token();
		la.val = "";		
		Get();
		MyLang();
		Expect(0);

	}
	
	static readonly bool[,] set = {
		{_T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x},
		{_x,_T,_x,_x, _x,_x,_x,_x, _x,_T,_T,_T, _x,_T,_x,_T, _x,_T,_T,_T, _x,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x},
		{_x,_T,_T,_x, _T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_T,_x,_x, _T,_T,_T,_T, _x,_x},
		{_x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _T,_T,_T,_T, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x},
		{_x,_T,_T,_x, _T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_T,_T,_T, _x,_x},
		{_x,_x,_x,_x, _x,_T,_x,_T, _T,_x,_x,_x, _T,_x,_x,_x, _x,_x,_x,_x, _T,_x,_x,_x, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _x,_x,_x,_x, _x,_x}

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
			case 24: s = "\"||\" expected"; break;
			case 25: s = "\"&&\" expected"; break;
			case 26: s = "\"==\" expected"; break;
			case 27: s = "\"!=\" expected"; break;
			case 28: s = "\"<\" expected"; break;
			case 29: s = "\">\" expected"; break;
			case 30: s = "\"<=\" expected"; break;
			case 31: s = "\">=\" expected"; break;
			case 32: s = "\"+\" expected"; break;
			case 33: s = "\"-\" expected"; break;
			case 34: s = "\"*\" expected"; break;
			case 35: s = "\"/\" expected"; break;
			case 36: s = "\"!\" expected"; break;
			case 37: s = "\"true\" expected"; break;
			case 38: s = "\"false\" expected"; break;
			case 39: s = "\"self\" expected"; break;
			case 40: s = "??? expected"; break;
			case 41: s = "invalid Type"; break;
			case 42: s = "invalid Params"; break;
			case 43: s = "invalid Statement"; break;
			case 44: s = "invalid RHS"; break;
			case 45: s = "invalid Args"; break;
			case 46: s = "invalid UnaryExp"; break;
			case 47: s = "invalid Primary"; break;
			case 48: s = "invalid PrimaryTail"; break;

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
