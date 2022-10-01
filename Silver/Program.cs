using Spectre.Console;

namespace Silver
{
    internal class Program
    {
        static void Main(string[] args)
        {
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

            System.Environment.ExitCode = 0;
        }

        private static void RunFile(string filename)
        {

        }

        private static void RunPrompt()
        {

        }

    }

}