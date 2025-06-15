using Spectre.Console;
using System.Text.RegularExpressions;

namespace TextCalculator
{
    class Program
    {
        static void Main()
        {
            AnsiConsole.MarkupLine("[bold yellow]Text calculator with highlighting[/]");
            AnsiConsole.MarkupLine("[dim]Enter the instructions. The empty line = execute. 'close' = end program exceution.[/]\n");

            while (true)
            {
                var evaluator = new Evaluator();
                var instructions = new List<string>();
                bool shouldExit = false;

                AnsiConsole.MarkupLine("[blue]Enter the instructions block:[/]");

                while (true)
                {
                    string line = ReadLineWithHighlight();

                    if (line.Trim().Equals("close", StringComparison.OrdinalIgnoreCase))
                    {
                        shouldExit = true;
                        break;
                    }

                    if (string.IsNullOrWhiteSpace(line))
                        break;

                    if (line.EndsWith("close", StringComparison.OrdinalIgnoreCase))
                    {
                        shouldExit = true;
                        line = line[..^5].TrimEnd();
                        if (!string.IsNullOrWhiteSpace(line))
                            instructions.Add(line);
                        break;
                    }

                    instructions.Add(line);
                }

                try
                {
                    foreach (var instr in instructions)
                    {
                        evaluator.Process(instr, highlightOutput: true);
                    }
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[bold red]Eror:[/] {ex.Message}");
                }

                AnsiConsole.MarkupLine("[grey]Variables are resetted.[/]\n");

                if (shouldExit)
                    break;
            }

            AnsiConsole.MarkupLine("[green]The program has finished running.[/]");
        }

        static string ReadLineWithHighlight()
        {
            var input = string.Empty;
            ConsoleKeyInfo key;

            do
            {
                key = Console.ReadKey(intercept: true);

                if (key.Key == ConsoleKey.Backspace && input.Length > 0)
                {
                    input = input[..^1];
                }
                else if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    break;
                }
                else if (!char.IsControl(key.KeyChar))
                {
                    input += key.KeyChar;
                }

                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write(new string(' ', Console.WindowWidth));
                Console.SetCursorPosition(0, Console.CursorTop);
                HighlightSyntax(input);
            }
            while (true);

            return input;
        }

        static void HighlightSyntax(string input)
        {
            int bracketLevel = 0;
            var colorCycle = new[] { "red", "green", "blue", "magenta", "yellow" };

            foreach (var token in Regex.Split(input, @"(\s+|\b)"))
            {
                if (Regex.IsMatch(token, @"^[0-9]+(\.[0-9]+)?$"))
                    AnsiConsole.Markup($"[cyan]{token}[/]");
                else if (Regex.IsMatch(token, @"^[A-Z]$"))
                    AnsiConsole.Markup($"[green]{token}[/]");
                else if (token == "(")
                {
                    var color = colorCycle[bracketLevel % colorCycle.Length];
                    AnsiConsole.Markup($"[{color}]{token}[/]");
                    bracketLevel++;
                }
                else if (token == ")")
                {
                    bracketLevel = Math.Max(0, bracketLevel - 1);
                    var color = colorCycle[bracketLevel % colorCycle.Length];
                    AnsiConsole.Markup($"[{color}]{token}[/]");
                }
                else if (Regex.IsMatch(token, @"[+\-*/=;]"))
                    AnsiConsole.Markup($"[yellow]{token}[/]");
                else
                    AnsiConsole.Markup(token);
            }
        }
    }
}
