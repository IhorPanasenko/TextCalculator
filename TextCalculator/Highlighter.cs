using System;
using System.Text.RegularExpressions;

namespace TextCalculator
{
    public static class Highlighter
    {
        public static void Highlight(string line)
        {
            var tokens = Regex.Matches(line, @"\d+(\.\d+)?|[A-Z]|\+|\-|\*|\/|\=|\(|\)|\?|\;|\S");

            foreach (Match token in tokens)
            {
                string value = token.Value;

                if (Regex.IsMatch(value, @"^\d+(\.\d+)?$")) // число
                    PrintColored(value, ConsoleColor.Green);
                else if (Regex.IsMatch(value, @"^[A-Z]$")) // змінна
                    PrintColored(value, ConsoleColor.Cyan);
                else if ("+-*/()".Contains(value)) // оператори
                    PrintColored(value, ConsoleColor.Yellow);
                else if ("=?;".Contains(value)) // спеціальні символи
                    PrintColored(value, ConsoleColor.Red);
                else
                    PrintColored(value, ConsoleColor.Gray); // інше (наприклад пробіл)
            }

            Console.WriteLine(); // новий рядок після підсвічування
        }

        private static void PrintColored(string text, ConsoleColor color)
        {
            var prev = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(text);
            Console.ForegroundColor = prev;
        }
    }
}