
using System;



public class Parser {
	public const int _EOF = 0;
	public const int _ident = 1;
	public const int _number = 2;
	public const int _stringLit = 3;
	public const int _dateLit = 4;
	public const int _timeLit = 5;
	public const int maxT = 70;

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

	
	void ResourceAvailability() {
		while (StartOf(1)) {
			Statement();
			Expect(6);
		}
		Expect(0);
	}

	void Statement() {
		if (StartOf(2)) {
			Decl();
		} else if (la.kind == 7) {
			Get();
			Expect(1);
		} else if (la.kind == 8) {
			Get();
			BoolExpr();
			Expect(9);
			Statement();
			if (la.kind == 10) {
				Get();
				Statement();
			}
		} else if (la.kind == 1) {
			Get();
			Expect(11);
			Expect(12);
			Expect(1);
		} else if (la.kind == 1) {
			Get();
			Expect(13);
			Expr();
		} else if (la.kind == 14) {
			Get();
			Expect(1);
			TimeExpr();
		} else if (la.kind == 15) {
			Get();
			Expect(1);
			Expect(16);
			if (StartOf(3)) {
				ArgumentList();
			}
			Expect(17);
		} else if (la.kind == 18) {
			Get();
			AvailabilityExpr();
			while (la.kind == 24 || la.kind == 25 || la.kind == 26) {
				ReservationOp();
				AvailabilityExpr();
			}
		} else if (la.kind == 19) {
			Get();
			AvailabilityExpr();
		} else SynErr(71);
	}

	void Decl() {
		if (StartOf(4)) {
			Type();
			Expect(1);
			if (la.kind == 13) {
				Get();
				Expr();
			}
		} else if (la.kind == 20) {
			Get();
			Expect(1);
			if (la.kind == 11) {
				Get();
				Expect(12);
				Expect(1);
			}
		} else if (la.kind == 1) {
			Get();
			Expect(1);
			Expect(21);
			while (StartOf(4)) {
				VarDecl();
				Expect(6);
			}
			Expect(22);
		} else if (la.kind == 23) {
			Get();
			Expect(1);
			Expect(16);
			if (StartOf(4)) {
				ParamList();
			}
			Expect(17);
			Expect(21);
			while (StartOf(1)) {
				Statement();
				Expect(6);
			}
			Expect(22);
		} else SynErr(72);
	}

	void BoolExpr() {
		BoolTerm();
		while (la.kind == 25) {
			Get();
			BoolTerm();
		}
	}

	void Expr() {
		if (la.kind == 1 || la.kind == 2 || la.kind == 16) {
			ArithExpr();
			if (StartOf(5)) {
				if (la.kind == 13) {
					AssignTail();
				} else if (StartOf(6)) {
					BoolTail();
				} else {
					TimeTail();
				}
			}
		} else if (la.kind == 36) {
			Get();
		} else if (la.kind == 37) {
			Get();
		} else if (la.kind == 3) {
			Get();
		} else SynErr(73);
	}

	void TimeExpr() {
		Expect(44);
		DateTimeExpr();
		if (la.kind == 45) {
			Get();
			DateTimeExpr();
		} else if (la.kind == 46) {
			Get();
			Duration();
		} else SynErr(74);
	}

	void ArgumentList() {
		Expr();
		while (la.kind == 26) {
			Get();
			Expr();
		}
	}

	void AvailabilityExpr() {
		ResourceExpr();
		TimeExpr();
		Constraint();
		Recurrence();
	}

	void ReservationOp() {
		if (la.kind == 24) {
			Get();
		} else if (la.kind == 25) {
			Get();
		} else if (la.kind == 26) {
			Get();
		} else SynErr(75);
	}

	void Type() {
		switch (la.kind) {
		case 27: {
			Get();
			break;
		}
		case 28: {
			Get();
			break;
		}
		case 29: {
			Get();
			break;
		}
		case 30: {
			Get();
			break;
		}
		case 31: {
			Get();
			break;
		}
		case 32: {
			Get();
			break;
		}
		case 33: {
			Get();
			break;
		}
		case 34: {
			Get();
			break;
		}
		case 35: {
			Get();
			break;
		}
		default: SynErr(76); break;
		}
	}

	void VarDecl() {
		Type();
		Expect(1);
		if (la.kind == 13) {
			Get();
			Expr();
		}
	}

	void ParamList() {
		Param();
		while (la.kind == 26) {
			Get();
			Param();
		}
	}

	void Param() {
		Type();
		Expect(1);
	}

	void ArithExpr() {
		ArithTerm();
		while (la.kind == 47 || la.kind == 48) {
			if (la.kind == 47) {
				Get();
			} else {
				Get();
			}
			ArithTerm();
		}
	}

	void AssignTail() {
		Expect(13);
		Expr();
	}

	void BoolTail() {
		switch (la.kind) {
		case 38: {
			Get();
			break;
		}
		case 39: {
			Get();
			break;
		}
		case 40: {
			Get();
			break;
		}
		case 41: {
			Get();
			break;
		}
		case 42: {
			Get();
			break;
		}
		case 43: {
			Get();
			break;
		}
		default: SynErr(77); break;
		}
		ArithExpr();
		while (la.kind == 24 || la.kind == 25) {
			if (la.kind == 24) {
				Get();
			} else {
				Get();
			}
			BoolFactor();
		}
	}

	void TimeTail() {
		Expect(44);
		DateTimeExpr();
		if (la.kind == 45) {
			Get();
			DateTimeExpr();
		} else if (la.kind == 46) {
			Get();
			Duration();
		} else SynErr(78);
	}

	void BoolFactor() {
		if (la.kind == 51) {
			Get();
			BoolFactor();
		} else if (la.kind == 16) {
			Get();
			BoolExpr();
			Expect(17);
		} else if (la.kind == 1 || la.kind == 2 || la.kind == 16) {
			ArithExpr();
			switch (la.kind) {
			case 38: {
				Get();
				break;
			}
			case 39: {
				Get();
				break;
			}
			case 40: {
				Get();
				break;
			}
			case 41: {
				Get();
				break;
			}
			case 42: {
				Get();
				break;
			}
			case 43: {
				Get();
				break;
			}
			default: SynErr(79); break;
			}
			ArithExpr();
		} else SynErr(80);
	}

	void DateTimeExpr() {
		DateTimeBase();
		while (la.kind == 47 || la.kind == 48) {
			if (la.kind == 47) {
				Get();
			} else {
				Get();
			}
			Duration();
		}
	}

	void Duration() {
		DurationAtom();
		while (la.kind == 1 || la.kind == 2) {
			DurationAtom();
		}
	}

	void ArithTerm() {
		ArithFactor();
		while (la.kind == 49 || la.kind == 50) {
			if (la.kind == 49) {
				Get();
			} else {
				Get();
			}
			ArithFactor();
		}
	}

	void ArithFactor() {
		if (la.kind == 1) {
			Get();
		} else if (la.kind == 2) {
			Get();
		} else if (la.kind == 16) {
			Get();
			ArithExpr();
			Expect(17);
		} else SynErr(81);
	}

	void BoolTerm() {
		BoolFactor();
		while (la.kind == 24) {
			Get();
			BoolFactor();
		}
	}

	void ResourceExpr() {
		ResourceTerm();
		while (la.kind == 24) {
			Get();
			ResourceTerm();
		}
	}

	void Constraint() {
		if (la.kind == 52) {
			Get();
			Expect(16);
			IdentList();
			Expect(17);
			BoolExpr();
		}
	}

	void Recurrence() {
		if (la.kind == 65) {
			Get();
			if (la.kind == 68 || la.kind == 69) {
				RecurrenceMode();
			}
			Expect(66);
			Duration();
			if (la.kind == 67) {
				Get();
				DateTimeExpr();
			} else if (la.kind == 46) {
				Get();
				Duration();
			} else SynErr(82);
		}
	}

	void ResourceTerm() {
		if (la.kind == 2) {
			Get();
			Expect(49);
			Expect(1);
		} else if (la.kind == 1) {
			Get();
		} else SynErr(83);
	}

	void IdentList() {
		Expect(1);
		while (la.kind == 1) {
			Get();
		}
	}

	void DateTimeBase() {
		if (la.kind == 4) {
			Get();
			Expect(5);
		} else if (la.kind == 1) {
			Get();
		} else SynErr(84);
	}

	void DurationAtom() {
		if (la.kind == 2) {
			Get();
			DurationUnit();
		} else if (la.kind == 1) {
			Get();
		} else SynErr(85);
	}

	void DurationUnit() {
		switch (la.kind) {
		case 53: {
			Get();
			break;
		}
		case 54: {
			Get();
			break;
		}
		case 55: {
			Get();
			break;
		}
		case 56: {
			Get();
			break;
		}
		case 57: {
			Get();
			break;
		}
		case 58: {
			Get();
			break;
		}
		case 59: {
			Get();
			break;
		}
		case 60: {
			Get();
			break;
		}
		case 61: {
			Get();
			break;
		}
		case 62: {
			Get();
			break;
		}
		case 63: {
			Get();
			break;
		}
		case 64: {
			Get();
			break;
		}
		default: SynErr(86); break;
		}
	}

	void RecurrenceMode() {
		if (la.kind == 68) {
			Get();
		} else if (la.kind == 69) {
			Get();
		} else SynErr(87);
	}



	public void Parse() {
		la = new Token();
		la.val = "";		
		Get();
		ResourceAvailability();
		Expect(0);

	}
	
	static readonly bool[,] set = {
		{_T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x},
		{_x,_T,_x,_x, _x,_x,_x,_T, _T,_x,_x,_x, _x,_x,_T,_T, _x,_x,_T,_T, _T,_x,_x,_T, _x,_x,_x,_T, _T,_T,_T,_T, _T,_T,_T,_T, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x},
		{_x,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _T,_x,_x,_T, _x,_x,_x,_T, _T,_T,_T,_T, _T,_T,_T,_T, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x},
		{_x,_T,_T,_T, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _T,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x},
		{_x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_T, _T,_T,_T,_T, _T,_T,_T,_T, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x},
		{_x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_T,_T, _T,_T,_T,_T, _T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x},
		{_x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_T,_T, _T,_T,_T,_T, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x}

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
			case 3: s = "stringLit expected"; break;
			case 4: s = "dateLit expected"; break;
			case 5: s = "timeLit expected"; break;
			case 6: s = "\";\" expected"; break;
			case 7: s = "\"cancel\" expected"; break;
			case 8: s = "\"if\" expected"; break;
			case 9: s = "\"then\" expected"; break;
			case 10: s = "\"else\" expected"; break;
			case 11: s = "\"is\" expected"; break;
			case 12: s = "\"a\" expected"; break;
			case 13: s = "\":=\" expected"; break;
			case 14: s = "\"reschedule\" expected"; break;
			case 15: s = "\"use\" expected"; break;
			case 16: s = "\"(\" expected"; break;
			case 17: s = "\")\" expected"; break;
			case 18: s = "\"reserve\" expected"; break;
			case 19: s = "\"available\" expected"; break;
			case 20: s = "\"category\" expected"; break;
			case 21: s = "\"{\" expected"; break;
			case 22: s = "\"}\" expected"; break;
			case 23: s = "\"template\" expected"; break;
			case 24: s = "\"and\" expected"; break;
			case 25: s = "\"or\" expected"; break;
			case 26: s = "\",\" expected"; break;
			case 27: s = "\"Resource\" expected"; break;
			case 28: s = "\"Reservation\" expected"; break;
			case 29: s = "\"TimePeriod\" expected"; break;
			case 30: s = "\"DateTime\" expected"; break;
			case 31: s = "\"Duration\" expected"; break;
			case 32: s = "\"int\" expected"; break;
			case 33: s = "\"bool\" expected"; break;
			case 34: s = "\"string\" expected"; break;
			case 35: s = "\"float\" expected"; break;
			case 36: s = "\"true\" expected"; break;
			case 37: s = "\"false\" expected"; break;
			case 38: s = "\"==\" expected"; break;
			case 39: s = "\"!=\" expected"; break;
			case 40: s = "\"<\" expected"; break;
			case 41: s = "\"<=\" expected"; break;
			case 42: s = "\">\" expected"; break;
			case 43: s = "\">=\" expected"; break;
			case 44: s = "\"from\" expected"; break;
			case 45: s = "\"to\" expected"; break;
			case 46: s = "\"for\" expected"; break;
			case 47: s = "\"+\" expected"; break;
			case 48: s = "\"-\" expected"; break;
			case 49: s = "\"*\" expected"; break;
			case 50: s = "\"/\" expected"; break;
			case 51: s = "\"not\" expected"; break;
			case 52: s = "\"where\" expected"; break;
			case 53: s = "\"weeks\" expected"; break;
			case 54: s = "\"week\" expected"; break;
			case 55: s = "\"w\" expected"; break;
			case 56: s = "\"days\" expected"; break;
			case 57: s = "\"day\" expected"; break;
			case 58: s = "\"d\" expected"; break;
			case 59: s = "\"hours\" expected"; break;
			case 60: s = "\"hour\" expected"; break;
			case 61: s = "\"h\" expected"; break;
			case 62: s = "\"minutes\" expected"; break;
			case 63: s = "\"minute\" expected"; break;
			case 64: s = "\"min\" expected"; break;
			case 65: s = "\"recurring\" expected"; break;
			case 66: s = "\"every\" expected"; break;
			case 67: s = "\"until\" expected"; break;
			case 68: s = "\"strict\" expected"; break;
			case 69: s = "\"flexible\" expected"; break;
			case 70: s = "??? expected"; break;
			case 71: s = "invalid Statement"; break;
			case 72: s = "invalid Decl"; break;
			case 73: s = "invalid Expr"; break;
			case 74: s = "invalid TimeExpr"; break;
			case 75: s = "invalid ReservationOp"; break;
			case 76: s = "invalid Type"; break;
			case 77: s = "invalid BoolTail"; break;
			case 78: s = "invalid TimeTail"; break;
			case 79: s = "invalid BoolFactor"; break;
			case 80: s = "invalid BoolFactor"; break;
			case 81: s = "invalid ArithFactor"; break;
			case 82: s = "invalid Recurrence"; break;
			case 83: s = "invalid ResourceTerm"; break;
			case 84: s = "invalid DateTimeBase"; break;
			case 85: s = "invalid DurationAtom"; break;
			case 86: s = "invalid DurationUnit"; break;
			case 87: s = "invalid RecurrenceMode"; break;

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
