using Spectre.Console;
using System.Globalization;

namespace Silver
{
    internal static class Program
    {
        private static bool hadError = false;
        private static bool hadRuntimeError = false;

        private static readonly Interpreter interpreter = new();

        static void Main(string[] args)
        {
            System.Environment.ExitCode = 0;

            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            if (args.Length > 1)
            {
                AnsiConsole.Markup("[bold red]Usage:[/] silver [[script]]");
                System.Environment.ExitCode = 64;
            }
            else if (args.Length == 1)
            {
                RunFile(args[0]);
            }
            else
            {
                RunPrompt();
            }
        }

        private static void RunFile(string filepath)
        {
            byte[] bytes = File.ReadAllBytes(filepath);

            string program = bytes.ToString() ?? "";

            Run(program);

            if (hadError)
            {
                System.Environment.ExitCode = 65;
            }

            if(hadRuntimeError)
            {
                System.Environment.ExitCode = 70;
            }
        }

        private static void RunPrompt()
        {
            while (true)
            {
                AnsiConsole.Markup("[bold blue]>>>[/] ");
                string? nextToken = Console.ReadLine();

                if (nextToken == null)
                {
                    return;
                }

                Run(nextToken);

                hadError = false;
            }
        }

        private static void Run(string program)
        {
            var scanner = new Scanner(program);
            List<Token> tokens = scanner.ScanTokens();
            Parser parser = new(tokens);
            var statements = parser.Parse();

            if (hadError)
            {
                return;
            }

            var resolver = new Resolver(interpreter);
            resolver.Resolve(statements);

            if (hadError)
            {
                return;
            }

            interpreter.Interpret(statements);
        }

        public static void Error(int line, string message)
        {
            Report(line, "", message);
        }

        public static void Error(Token token, String message)
        {
            if (token.type == TokenType.EOF)
            {
                Report(token.line, " at end ", message);
            }
            else
            {
                Report(token.line, " at '" + token.lexeme + "'", message);
            }
        }

        public static void RuntimeError(RuntimeErrorException error)
        {
            AnsiConsole.MarkupLine($"[red]{error.Message}[/]");
            AnsiConsole.MarkupLine($"[red][[line: {error.token.line}]][/]");
            hadRuntimeError = true;
        }

        private static void Report(int line, string where, string message)
        {
            AnsiConsole.MarkupLine($"[red][[line: {line}]] Error{where}: {message}[/]");
            hadError = true;
        }
    }

}