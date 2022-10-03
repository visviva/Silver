using Spectre.Console;
using System.Globalization;

namespace Silver
{
    internal class Program
    {
        private static bool hadError = false;

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

            // For now just print the tokens
            foreach (var token in tokens)
            {
                AnsiConsole.WriteLine(token.ToString());
            }
        }

        public static void Error(int line, string message)
        {
            Report(line, "", message);
        }

        private static void Report(int line, string where, string message)
        {
            AnsiConsole.MarkupLine($"[red][[line: {line}]] Error{where}: {message}[/]");
            hadError = true;
        }

    }

}