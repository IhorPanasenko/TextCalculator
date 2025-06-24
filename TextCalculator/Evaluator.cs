using Spectre.Console;
using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;

namespace TextCalculator
{
    public class Evaluator
    {
        private readonly Dictionary<string, double> variables = new();

        public void Process(string line, bool highlightOutput = false)
        {
            Lexer.ValidateCharacters(line);
            Lexer.DetectInvalidLeadingZerosGlobal(line);

            if (Lexer.IsAssignment(line))
            {
                var (varName, expr) = Lexer.ParseAssignment(line);
                double result = EvaluateExpression(expr);
                variables[varName] = result;

                if (highlightOutput)
                    AnsiConsole.MarkupLine($"[green]{varName} = {result.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture)}[/]");
                else
                    Console.WriteLine($"{varName} = {result.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture)}");
            }
            else if (Regex.IsMatch(line, @"^(.+?)=>\s*_([2-9]|1[0-6])\s*;?$"))
            {
                var match = Regex.Match(line, @"^(.+?)=>\s*_([2-9]|1[0-6])\s*;?$");
                string expr = match.Groups[1].Value.Trim();
                int targetBase = int.Parse(match.Groups[2].Value);

                // Підставляємо змінні та обчислюємо вираз
                string normalized = NormalizeUnaryMinus(expr);
                string withVars = ReplaceVariables(normalized);
                string expanded = Converter.ConvertSpecialNotations(withVars);
                Lexer.ValidateCharacters(expanded);
                double result = Compute(expanded);

                // Виводимо результат у потрібній системі числення
                string converted = ConvertToBase(result, targetBase);

                if (highlightOutput)
                    AnsiConsole.MarkupLine($"Result in base {targetBase}: [green]{converted}[/]");
                else
                    Console.WriteLine($"Result in base {targetBase}: {converted}");

                return;
            }
            else if (Lexer.IsQuery(line))
            {
                string varName = Lexer.GetQueryVariable(line);
                if (variables.ContainsKey(varName))
                {
                    if (highlightOutput)
                        AnsiConsole.MarkupLine($"[green]{varName} = {variables[varName].ToString("0.######", System.Globalization.CultureInfo.InvariantCulture)}[/]");
                    else
                        Console.WriteLine($"{varName} = {variables[varName].ToString("0.######", System.Globalization.CultureInfo.InvariantCulture)}");
                }
                else
                {
                    if (highlightOutput)
                        AnsiConsole.MarkupLine($"[red]Variable '{varName}' is not defined[/]");
                    else
                        Console.WriteLine($"Variable '{varName}' is not defined");
                }
            }
            else if (Regex.IsMatch(line, @"=>\s*_([2-9]|1[0-6])\s*$"))
            {
                var match = Regex.Match(line, @"^(.*)=>\s*_([2-9]|1[0-6])\s*$");
                string expr = match.Groups[1].Value.Trim();
                int targetBase = int.Parse(match.Groups[2].Value);

                double result = EvaluateExpression(expr);
                string converted = ConvertToBase(result, targetBase);

                if (highlightOutput)
                    AnsiConsole.MarkupLine($"Result in base {targetBase}: [green]{converted}[/]");
                else
                    Console.WriteLine($"Result in base {targetBase}: {converted}");
            }
            else if (line.Trim().EndsWith("="))
            {
                string expr = line.Trim().TrimEnd('=');
                double result = EvaluateExpression(expr);

                if (highlightOutput)
                    AnsiConsole.MarkupLine($"Result: [green]{result.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture)}[/]");
                else
                    Console.WriteLine($"Result: {result.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture)}");
            }
            else
            {
                throw new Exception("Unknown instruction format");
            }
        }

        private double EvaluateExpression(string expression)
        {
            expression = expression.Trim();
            if (expression.EndsWith(";"))
                expression = expression.Substring(0, expression.Length - 1).Trim();


            string withVars = ReplaceVariables(expression);
            string normalized = NormalizeUnaryMinus(withVars);
            string expanded = Converter.ConvertSpecialNotations(withVars);
            Lexer.ValidateCharacters(expanded);
            return Compute(expanded);
        }

        private string NormalizeUnaryMinus(string expr)
        {
            return Regex.Replace(expr, @"(?<=^|\(|\+|\-|\*|\/)\-", "u");
        }

        private string ReplaceVariables(string expr)
        {
            foreach (var kvp in variables.OrderByDescending(k => k.Key.Length))
            {
                // Handle unary minus: -A → -1*A
                expr = Regex.Replace(expr, $@"(?<=^|[^A-Za-z0-9_])-{Regex.Escape(kvp.Key)}\b",
                    match => $"-1*{kvp.Value.ToString(CultureInfo.InvariantCulture)}");

                // Handle normal variable usage
                expr = Regex.Replace(expr, $@"\b{Regex.Escape(kvp.Key)}\b",
                    kvp.Value.ToString(CultureInfo.InvariantCulture));
            }
            return expr;
        }

        private double Compute(string expr)
        {
            expr = expr.Replace("u", "-1*");
            int pos = 0;

            double ParseExpression()
            {
                double x = ParseTerm();
                while (true)
                {
                    if (Match('+')) x += ParseTerm();
                    else if (Match('-')) x -= ParseTerm();
                    else return x;
                }
            }

            double ParseTerm()
            {
                double x = ParseFactor();
                while (true)
                {
                    if (Match('*')) x *= ParseFactor();
                    else if (Match('/')) x /= ParseFactor();
                    else return x;
                }
            }

            double ParseFactor()
            {
                double x = ParsePower();
                return x;
            }

            double ParsePower()
            {
                double x = ParsePrimary();
                while (Match('^'))
                {
                    double exponent = ParsePower();
                    x = Math.Pow(x, exponent);
                }
                return x;
            }

            double ParsePrimary()
            {
                SkipWhitespace();
                if (Match('('))
                {
                    double x = ParseExpression();
                    if (!Match(')')) throw new Exception("Missing closing parenthesis");
                    return x;
                }
                if (Match('-')) // unary minus
                    return -ParsePrimary();
                return ParseNumber();
            }

            double ParseNumber()
            {
                SkipWhitespace();
                int start = pos;

                while (pos < expr.Length &&
                      (char.IsDigit(expr[pos]) || expr[pos] == '.' ||
                       expr[pos] == 'e' || expr[pos] == 'E' ||
                       (pos > start && (expr[pos] == '+' || expr[pos] == '-') &&
                        (expr[pos - 1] == 'e' || expr[pos - 1] == 'E'))))
                {
                    pos++;
                }

                if (start == pos)
                    throw new Exception("Expected number");

                string numStr = expr.Substring(start, pos - start);
                return double.Parse(numStr, System.Globalization.CultureInfo.InvariantCulture);
            }


            bool Match(char expected)
            {
                SkipWhitespace();
                if (pos < expr.Length && expr[pos] == expected)
                {
                    pos++;
                    return true;
                }
                return false;
            }

            void SkipWhitespace()
            {
                while (pos < expr.Length && char.IsWhiteSpace(expr[pos])) pos++;
            }

            return ParseExpression();
        }

        private static string ConvertToBase(double value, int numBase)
        {
            string digits = "0123456789ABCDEF";

            bool isNegative = value < 0;
            value = Math.Abs(value);

            long intPart = (long)value;
            double fracPart = value - intPart;

            string intStr = "";
            if (intPart == 0)
                intStr = "0";
            else
            {
                while (intPart > 0)
                {
                    intStr = digits[(int)(intPart % numBase)] + intStr;
                    intPart /= numBase;
                }
            }

            string fracStr = "";
            int maxDigits = 10;
            while (fracPart > 0 && fracStr.Length < maxDigits)
            {
                fracPart *= numBase;
                int digit = (int)fracPart;
                fracStr += digits[digit];
                fracPart -= digit;
            }

            string result = fracStr.Length > 0 ? $"{intStr}.{fracStr}" : intStr;
            return isNegative ? "-" + result : result;
        }
    }
}
