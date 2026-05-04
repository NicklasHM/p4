using RAL.AST;
using RAL.TC;

namespace RAL;

class Program {
    static void Main(string[] args) {
        
        //If no argument is supplied upon running main, args.length = 1 & args[0] == file name
        if (args.Length < 2) {
            //Fix
            Console.WriteLine("Usage: dotnet run -- <inputfile>");
            Console.WriteLine("       dotnet run -- --run-tests");
            return;
        }

        string filePath = args[1];
        if (!File.Exists(filePath)) {
            Console.WriteLine($"File not found: {filePath}");
            return;
        }

        Scanner scanner = new Scanner(filePath);
        Parser parser = new Parser(scanner);
        parser.Parse();

        //Check parsing errors
        if (parser.errors.count == 0) {
            Console.WriteLine("Parsing successful!");

            //Extract AST
            Stmt program = parser.mainNode ?? throw new NullReferenceException();

            TypeChecker typeChecker = new TypeChecker();

            typeChecker.StmtType(program, new EnvV(), new EnvC(), new EnvH(), new EnvT(), new EnvR());

            Console.WriteLine("\n\n\n\nProgram:\n"+ program.ToString() + "\n\n\n\n\n");

            foreach (string error in typeChecker.errors)
            {
                Console.WriteLine("\n" + error + "\n");
            }


        } else {
            Console.WriteLine($"Parsing failed with {parser.errors.count} error(s).");
        }
    }
    
}