
using System;



public class Parser {
	public const int _EOF = 0;
	public const int _ident = 1;
	public const int _number = 2;
	public const int _stringLit = 3;
	public const int _dateLit = 4;
	public const int _timeLit = 5;
	public const int maxT = 67;

	const bool _T = true;
	const bool _x = false;
	const int minErrDist = 2;
	
	public Scanner scanner;
	public Errors  errors;

	public Token t;    // last recognized token
	public Token la;   // lookahead token
	int errDist = minErrDist;

bool IsParenBool() {
    scanner.ResetPeek();
    Token t = scanner.Peek();
    return t.kind == _lparen;;
}



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
		} else if (StartOf(3)) {
			Expr();
		} else if (la.kind == 1) {
			Get();
			Expect(7);
			Expect(8);
			Expect(1);
		} else if (la.kind == 9) {
			Get();
			Expect(1);
		} else if (la.kind == 10) {
			Get();
			BoolExpr();
			Expect(11);
			Statement();
			Expect(12);
			Statement();
		} else if (la.kind == 10) {
			Get();
			BoolExpr();
			Expect(11);
			Statement();
		} else SynErr(68);
	}

	void Decl() {
		if (StartOf(4)) {
			VarDecl();
		} else if (la.kind == 1) {
			ResourceDecl();
		} else if (la.kind == 25) {
			CategoryDecl();
		} else if (la.kind == 26) {
			TemplateDecl();
		} else SynErr(69);
	}

	void Expr() {
		if (isIndentBool()) {
			Expect(1);
			Expect(13);
			Expr();
		} else if (la.kind == 1 || la.kind == 2 || la.kind == 27) {
			ArithExpr();
		} else if (StartOf(5)) {
			BoolExpr();
		} else if (la.kind == 47) {
			TimeExpr();
		} else if (la.kind == 1 || la.kind == 4) {
			DateTime();
		} else if (la.kind == 1 || la.kind == 2) {
			Duration();
		} else if (la.kind == 1 || la.kind == 2 || la.kind == 27) {
			ResourceExpr();
		} else if (la.kind == 1 || la.kind == 2 || la.kind == 27) {
			AvailabilityExpr();
		} else if (la.kind == 27 || la.kind == 45) {
			ReservationExpr();
		} else if (la.kind == 30) {
			Get();
			Expect(1);
			TimeExpr();
		} else if (la.kind == 31) {
			Get();
			Expect(1);
			Expect(27);
			if (StartOf(3)) {
				ArgumentList();
			}
			Expect(28);
		} else SynErr(70);
	}

	void BoolExpr() {
		BoolTerm();
		while (la.kind == 36) {
			Get();
			BoolTerm();
		}
	}

	void VarDecl() {
		if (StartOf(4)) {
			Type();
			Expect(1);
		} else if (StartOf(4)) {
			Type();
			Expect(1);
			Expect(13);
			Expr();
		} else SynErr(71);
	}

	void ResourceDecl() {
		Expect(1);
		Expect(1);
		Expect(23);
		while (StartOf(4)) {
			VarDecl();
			Expect(6);
		}
		Expect(24);
	}

	void CategoryDecl() {
		Expect(25);
		Expect(1);
		if (la.kind == 7) {
			Get();
			Expect(8);
			Expect(1);
		}
	}

	void TemplateDecl() {
		Expect(26);
		Expect(1);
		Expect(27);
		if (StartOf(4)) {
			ParamList();
		}
		Expect(28);
		Expect(23);
		while (StartOf(1)) {
			Statement();
			Expect(6);
		}
		Expect(24);
	}

	void Type() {
		switch (la.kind) {
		case 19: case 20: case 21: case 22: {
			BaseType();
			break;
		}
		case 14: {
			Get();
			break;
		}
		case 15: {
			Get();
			break;
		}
		case 16: {
			Get();
			break;
		}
		case 17: {
			Get();
			break;
		}
		case 18: {
			Get();
			break;
		}
		default: SynErr(72); break;
		}
	}

	void BaseType() {
		if (la.kind == 19) {
			Get();
		} else if (la.kind == 20) {
			Get();
		} else if (la.kind == 21) {
			Get();
		} else if (la.kind == 22) {
			Get();
		} else SynErr(73);
	}

	void ParamList() {
		Param();
		while (la.kind == 29) {
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
		while (la.kind == 32 || la.kind == 33) {
			if (la.kind == 32) {
				Get();
			} else {
				Get();
			}
			ArithTerm();
		}
	}

	void TimeExpr() {
		Expect(47);
		DateTime();
		if (la.kind == 48) {
			Get();
			DateTime();
		} else if (la.kind == 49) {
			Get();
			Duration();
		} else SynErr(74);
	}

	void DateTime() {
		if (la.kind == 4) {
			Get();
			Expect(5);
		} else if (la.kind == 1) {
			Get();
		} else if (la.kind == 1 || la.kind == 4) {
			DateTime();
			Expect(32);
			Duration();
		} else if (la.kind == 1 || la.kind == 4) {
			DateTime();
			Expect(33);
			Duration();
		} else SynErr(75);
	}

	void Duration() {
		DurationAtom();
		while (la.kind == 1 || la.kind == 2) {
			DurationAtom();
		}
	}

	void ResourceExpr() {
		if (la.kind == 1) {
			Get();
		} else if (la.kind == 1 || la.kind == 2 || la.kind == 27) {
			ArithExpr();
			Expect(34);
			Expect(1);
		} else if (la.kind == 1 || la.kind == 2 || la.kind == 27) {
			ResourceExpr();
			Expect(37);
			ResourceExpr();
		} else SynErr(76);
	}

	void AvailabilityExpr() {
		ResourceExpr();
		TimeExpr();
		Constraint();
		Recurrence();
	}

	void ReservationExpr() {
		ReservationTerm();
		while (la.kind == 29 || la.kind == 36 || la.kind == 37) {
			if (la.kind == 37) {
				Get();
				ReservationTerm();
			} else if (la.kind == 36) {
				Get();
				ReservationTerm();
			} else {
				Get();
				ReservationTerm();
			}
		}
	}

	void ArgumentList() {
		Expr();
		while (la.kind == 29) {
			Get();
			Expr();
		}
	}

	void ArithTerm() {
		ArithFactor();
		while (la.kind == 34 || la.kind == 35) {
			if (la.kind == 34) {
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
		} else if (la.kind == 27) {
			Get();
			ArithExpr();
			Expect(28);
		} else SynErr(77);
	}

	void BoolTerm() {
		BoolFactor();
		while (la.kind == 37) {
			Get();
			BoolFactor();
		}
	}

	void BoolFactor() {
		if (la.kind == 38) {
			Get();
			BoolFactor();
		} else if (IsParenBool()) {
			Expect(27);
			BoolExpr();
			Expect(28);
		} else if (la.kind == 1 || la.kind == 2 || la.kind == 27) {
			Comparison();
		} else SynErr(78);
	}

	void Comparison() {
		ArithExpr();
		switch (la.kind) {
		case 39: {
			Get();
			ArithExpr();
			break;
		}
		case 40: {
			Get();
			ArithExpr();
			break;
		}
		case 41: {
			Get();
			ArithExpr();
			break;
		}
		case 42: {
			Get();
			ArithExpr();
			break;
		}
		case 43: {
			Get();
			ArithExpr();
			break;
		}
		case 44: {
			Get();
			ArithExpr();
			break;
		}
		default: SynErr(79); break;
		}
	}

	void Constraint() {
		if (la.kind == 46) {
			Get();
			Expect(27);
			IdentList();
			Expect(28);
			BoolExpr();
		}
	}

	void Recurrence() {
		if (la.kind == 62) {
			Get();
			if (la.kind == 65 || la.kind == 66) {
				RecurrenceMode();
			}
			Expect(63);
			Duration();
			if (la.kind == 64) {
				Get();
				DateTime();
			} else if (la.kind == 49) {
				Get();
				Duration();
			} else SynErr(80);
		}
	}

	void ReservationTerm() {
		if (la.kind == 45) {
			Get();
			AvailabilityExpr();
		} else if (la.kind == 27) {
			Get();
			ReservationExpr();
			Expect(28);
		} else SynErr(81);
	}

	void IdentList() {
		Expect(1);
		while (la.kind == 1) {
			Get();
		}
	}

	void DurationAtom() {
		if (la.kind == 2) {
			Get();
			DurationUnit();
		} else if (la.kind == 1) {
			Get();
		} else SynErr(82);
	}

	void DurationUnit() {
		switch (la.kind) {
		case 50: {
			Get();
			break;
		}
		case 51: {
			Get();
			break;
		}
		case 52: {
			Get();
			break;
		}
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
		default: SynErr(83); break;
		}
	}

	void RecurrenceMode() {
		if (la.kind == 65) {
			Get();
		} else if (la.kind == 66) {
			Get();
		} else SynErr(84);
	}



	public void Parse() {
		la = new Token();
		la.val = "";		
		Get();
		ResourceAvailability();
		Expect(0);

	}
	
	static readonly bool[,] set = {
		{_T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x},
		{_x,_T,_T,_x, _T,_x,_x,_x, _x,_T,_T,_x, _x,_x,_T,_T, _T,_T,_T,_T, _T,_T,_T,_x, _x,_T,_T,_T, _x,_x,_T,_T, _x,_x,_x,_x, _x,_x,_T,_x, _x,_x,_x,_x, _x,_T,_x,_T, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x},
		{_x,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_T,_T, _T,_T,_T,_T, _T,_T,_T,_x, _x,_T,_T,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x},
		{_x,_T,_T,_x, _T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_T, _x,_x,_T,_T, _x,_x,_x,_x, _x,_x,_T,_x, _x,_x,_x,_x, _x,_T,_x,_T, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x},
		{_x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_T,_T, _T,_T,_T,_T, _T,_T,_T,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x},
		{_x,_T,_T,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_T, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_T,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x}

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
			case 7: s = "\"is\" expected"; break;
			case 8: s = "\"a\" expected"; break;
			case 9: s = "\"cancel\" expected"; break;
			case 10: s = "\"if\" expected"; break;
			case 11: s = "\"then\" expected"; break;
			case 12: s = "\"else\" expected"; break;
			case 13: s = "\":=\" expected"; break;
			case 14: s = "\"Resource\" expected"; break;
			case 15: s = "\"Reservation\" expected"; break;
			case 16: s = "\"TimePeriod\" expected"; break;
			case 17: s = "\"DateTime\" expected"; break;
			case 18: s = "\"Duration\" expected"; break;
			case 19: s = "\"int\" expected"; break;
			case 20: s = "\"bool\" expected"; break;
			case 21: s = "\"string\" expected"; break;
			case 22: s = "\"float\" expected"; break;
			case 23: s = "\"{\" expected"; break;
			case 24: s = "\"}\" expected"; break;
			case 25: s = "\"category\" expected"; break;
			case 26: s = "\"template\" expected"; break;
			case 27: s = "\"(\" expected"; break;
			case 28: s = "\")\" expected"; break;
			case 29: s = "\",\" expected"; break;
			case 30: s = "\"reschedule\" expected"; break;
			case 31: s = "\"use\" expected"; break;
			case 32: s = "\"+\" expected"; break;
			case 33: s = "\"-\" expected"; break;
			case 34: s = "\"*\" expected"; break;
			case 35: s = "\"/\" expected"; break;
			case 36: s = "\"or\" expected"; break;
			case 37: s = "\"and\" expected"; break;
			case 38: s = "\"not\" expected"; break;
			case 39: s = "\"==\" expected"; break;
			case 40: s = "\"!=\" expected"; break;
			case 41: s = "\"<\" expected"; break;
			case 42: s = "\"<=\" expected"; break;
			case 43: s = "\">\" expected"; break;
			case 44: s = "\">=\" expected"; break;
			case 45: s = "\"reserve\" expected"; break;
			case 46: s = "\"where\" expected"; break;
			case 47: s = "\"from\" expected"; break;
			case 48: s = "\"to\" expected"; break;
			case 49: s = "\"for\" expected"; break;
			case 50: s = "\"weeks\" expected"; break;
			case 51: s = "\"week\" expected"; break;
			case 52: s = "\"w\" expected"; break;
			case 53: s = "\"days\" expected"; break;
			case 54: s = "\"day\" expected"; break;
			case 55: s = "\"d\" expected"; break;
			case 56: s = "\"hours\" expected"; break;
			case 57: s = "\"hour\" expected"; break;
			case 58: s = "\"h\" expected"; break;
			case 59: s = "\"minutes\" expected"; break;
			case 60: s = "\"minute\" expected"; break;
			case 61: s = "\"min\" expected"; break;
			case 62: s = "\"recurring\" expected"; break;
			case 63: s = "\"every\" expected"; break;
			case 64: s = "\"until\" expected"; break;
			case 65: s = "\"strict\" expected"; break;
			case 66: s = "\"flexible\" expected"; break;
			case 67: s = "??? expected"; break;
			case 68: s = "invalid Statement"; break;
			case 69: s = "invalid Decl"; break;
			case 70: s = "invalid Expr"; break;
			case 71: s = "invalid VarDecl"; break;
			case 72: s = "invalid Type"; break;
			case 73: s = "invalid BaseType"; break;
			case 74: s = "invalid TimeExpr"; break;
			case 75: s = "invalid DateTime"; break;
			case 76: s = "invalid ResourceExpr"; break;
			case 77: s = "invalid ArithFactor"; break;
			case 78: s = "invalid BoolFactor"; break;
			case 79: s = "invalid Comparison"; break;
			case 80: s = "invalid Recurrence"; break;
			case 81: s = "invalid ReservationTerm"; break;
			case 82: s = "invalid DurationAtom"; break;
			case 83: s = "invalid DurationUnit"; break;
			case 84: s = "invalid RecurrenceMode"; break;

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
