namespace Sharplox;

public static class Lox
{
    readonly static Interpreter Interpreter = new();

    public static int Main(string[] args)
    {
        switch (args.Length)
        {
            case > 1:
                Console.WriteLine("Invalid arguments provided");
                return 64;
            case 1:
                return RunFile(args[0]);
            default:
                RunPrompt();
                return 0;
        }
    }

    static int RunFile(string path)
    {
        var source = File.ReadAllText(path);
        Run(source);
        if (_hadError) return 65;
        if (_hadRuntimeError) return 70;

        return 0;
    }

    static void RunPrompt()
    {
        while (true)
        {
            var line = Console.ReadLine();
            if (line is null) break;
            Run(line);
            _hadError = false;
        }
    }

    static void Run(string source)
    {
        Lexer lexer = new(source);
        var tokens = lexer.ScanTokens();
        Parser parser = new(tokens);
        var statements = parser.Parse();
        if (_hadError) return;
        
        Interpreter.Interpret(statements);
    }

    static bool _hadError;
    static bool _hadRuntimeError;

    public static void Error(int line, string message) => ReportError(line, "", message);

    public static void Error(Token token, string message)
    {
        var where = token.Type == TokenType.Eof ? " at end" : $" at '{token.Lexeme}'";
        ReportError(token.Line, where, message);
    }

    public static void RuntimeError(RuntimeError error)
    {
        Console.WriteLine($"{error.Message}\n[line {error.Token.Line}]");
        _hadRuntimeError = true;
    }

    static void ReportError(int line, string where,
        String message)
    {
        Console.WriteLine($"[line {line}] Error {where}: {message}");
        _hadError = true;
    }
}
