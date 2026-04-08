
using System;



public class Parser {
	public const int _EOF = 0;
	public const int _ident = 1;
	public const int _number = 2;
	public const int _func = 3;
	public const int _if = 4;
	public const int _else = 5;
	public const int _while = 6;
	public const int _return = 7;
	public const int _skip = 8;
	public const int _print = 9;
	public const int _send = 10;
	public const int _to = 11;
	public const int _spawn = 12;
	public const int _receive = 13;
	public const int _self = 14;
	public const int _Int = 15;
	public const int _Bool = 16;
	public const int _Pid = 17;
	public const int maxT = 38;

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
		Expect(18);
		Params();
		Expect(19);
		Expect(20);
		StatementList();
		Expect(21);
	}

	void Type() {
		if (la.kind == 15) {
			Get();
		} else if (la.kind == 16) {
			Get();
		} else if (la.kind == 17) {
			Get();
		} else SynErr(39);
	}

	void Params() {
		if (la.kind == 15 || la.kind == 16 || la.kind == 17) {
			Param();
			while (la.kind == 22) {
				Get();
				Param();
			}
		} else if (la.kind == 19) {
		} else SynErr(40);
	}

	void StatementList() {
		Statement();
		while (la.kind == 23) {
			Get();
			Statement();
		}
	}

	void Param() {
		Type();
		Expect(1);
	}

	void Statement() {
		switch (la.kind) {
		case 4: {
			IfStmt();
			break;
		}
		case 6: {
			WhileStmt();
			break;
		}
		case 1: {
			AssignStmt();
			break;
		}
		case 15: case 16: case 17: {
			DeclStmt();
			break;
		}
		case 9: {
			PrintStmt();
			break;
		}
		case 7: {
			ReturnStmt();
			break;
		}
		case 10: {
			SendStmt();
			break;
		}
		case 8: {
			SkipStmt();
			break;
		}
		default: SynErr(41); break;
		}
	}

	void IfStmt() {
		Expect(4);
		Expect(18);
		Expr();
		Expect(19);
		Expect(20);
		StatementList();
		Expect(21);
		Expect(5);
		Expect(20);
		StatementList();
		Expect(21);
	}

	void WhileStmt() {
		Expect(6);
		Expect(18);
		Expr();
		Expect(19);
		Expect(20);
		StatementList();
		Expect(21);
	}

	void AssignStmt() {
		Expect(1);
		Expect(24);
		RHS();
	}

	void DeclStmt() {
		Type();
		Expect(1);
		Expect(24);
		Expr();
	}

	void PrintStmt() {
		Expect(9);
		Expect(18);
		Expr();
		Expect(19);
	}

	void ReturnStmt() {
		Expect(7);
		Expr();
	}

	void SendStmt() {
		Expect(10);
		Expr();
		Expect(11);
		Expr();
	}

	void SkipStmt() {
		Expect(8);
	}

	void Expr() {
		OrExp();
	}

	void RHS() {
		if (la.kind == 13) {
			Get();
			Expect(18);
			Expect(19);
		} else if (la.kind == 12) {
			Get();
			Expect(1);
			Expect(18);
			Args();
			Expect(19);
		} else if (StartOf(1)) {
			Expr();
		} else SynErr(42);
	}

	void Args() {
		if (StartOf(1)) {
			Expr();
			while (la.kind == 22) {
				Get();
				Expr();
			}
		} else if (la.kind == 19) {
		} else SynErr(43);
	}

	void OrExp() {
		AndExp();
		while (la.kind == 25) {
			Get();
			AndExp();
		}
	}

	void AndExp() {
		EqExp();
		while (la.kind == 26) {
			Get();
			EqExp();
		}
	}

	void EqExp() {
		RelExp();
		if (la.kind == 27 || la.kind == 28) {
			if (la.kind == 27) {
				Get();
			} else {
				Get();
			}
			RelExp();
		}
	}

	void RelExp() {
		AddExp();
		if (StartOf(2)) {
			if (la.kind == 29) {
				Get();
			} else if (la.kind == 30) {
				Get();
			} else if (la.kind == 31) {
				Get();
			} else {
				Get();
			}
			AddExp();
		}
	}

	void AddExp() {
		MulExp();
		while (la.kind == 33 || la.kind == 34) {
			if (la.kind == 33) {
				Get();
			} else {
				Get();
			}
			MulExp();
		}
	}

	void MulExp() {
		UnaryExp();
		while (la.kind == 35 || la.kind == 36) {
			if (la.kind == 35) {
				Get();
			} else {
				Get();
			}
			UnaryExp();
		}
	}

	void UnaryExp() {
		if (la.kind == 37) {
			Get();
			UnaryExp();
		} else if (la.kind == 34) {
			Get();
			UnaryExp();
		} else if (StartOf(3)) {
			Primary();
		} else SynErr(44);
	}

	void Primary() {
		if (la.kind == 2) {
			Get();
		} else if (la.kind == 1) {
			Get();
			PrimaryTail();
		} else if (la.kind == 14) {
			Get();
		} else if (la.kind == 18) {
			Get();
			Expr();
			Expect(19);
		} else SynErr(45);
	}

	void PrimaryTail() {
		if (la.kind == 18) {
			Get();
			Args();
			Expect(19);
		} else if (StartOf(4)) {
		} else SynErr(46);
	}



	public void Parse() {
		la = new Token();
		la.val = "";		
		Get();
		MyLang();
		Expect(0);

	}
	
	static readonly bool[,] set = {
		{_T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x},
		{_x,_T,_T,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_T,_x, _x,_x,_T,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_T,_x, _x,_T,_x,_x},
		{_x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_T,_T,_T, _T,_x,_x,_x, _x,_x,_x,_x},
		{_x,_T,_T,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_T,_x, _x,_x,_T,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x},
		{_x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_T, _x,_x,_x,_x, _x,_x,_x,_T, _x,_T,_T,_T, _x,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_x,_x,_x}

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
			case 3: s = "func expected"; break;
			case 4: s = "if expected"; break;
			case 5: s = "else expected"; break;
			case 6: s = "while expected"; break;
			case 7: s = "return expected"; break;
			case 8: s = "skip expected"; break;
			case 9: s = "print expected"; break;
			case 10: s = "send expected"; break;
			case 11: s = "to expected"; break;
			case 12: s = "spawn expected"; break;
			case 13: s = "receive expected"; break;
			case 14: s = "self expected"; break;
			case 15: s = "Int expected"; break;
			case 16: s = "Bool expected"; break;
			case 17: s = "Pid expected"; break;
			case 18: s = "\"(\" expected"; break;
			case 19: s = "\")\" expected"; break;
			case 20: s = "\"{\" expected"; break;
			case 21: s = "\"}\" expected"; break;
			case 22: s = "\",\" expected"; break;
			case 23: s = "\";\" expected"; break;
			case 24: s = "\"=\" expected"; break;
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
			case 38: s = "??? expected"; break;
			case 39: s = "invalid Type"; break;
			case 40: s = "invalid Params"; break;
			case 41: s = "invalid Statement"; break;
			case 42: s = "invalid RHS"; break;
			case 43: s = "invalid Args"; break;
			case 44: s = "invalid UnaryExp"; break;
			case 45: s = "invalid Primary"; break;
			case 46: s = "invalid PrimaryTail"; break;

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
