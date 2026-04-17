namespace RAL;

class Program {
    static void Main(string[] args) {
        if (args.Length == 0) {
            Console.WriteLine("Usage: dotnet run -- <inputfile>");
            return;
        }

        string filePath = args[0];
        if (!File.Exists(filePath)) {
            Console.WriteLine($"File not found: {filePath}");
            return;
        }

        Scanner scanner = new Scanner(filePath);
        Parser parser = new Parser(scanner);
        parser.Parse();

        if (parser.errors.count == 0) {
            Console.WriteLine("Parsing successful!");
        } else {
            Console.WriteLine($"Parsing failed with {parser.errors.count} error(s).");
        }
    }
}