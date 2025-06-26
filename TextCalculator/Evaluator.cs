using Spectre.Console;
using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;

namespace TextCalculator
{
    public class Evaluator
    {
        private readonly Dictionary<string, double> variables = new();

        public void ProcessBlock(IEnumerable<string> lines, bool highlightOutput = false)
        {
            var pending = new Queue<string>(lines.Where(l => !string.IsNullOrWhiteSpace(l)));
            var processedLines = new HashSet<string>();
            bool progressMade;

            do
            {
                int initialCount = pending.Count;
                var retryQueue = new Queue<string>();
                progressMade = false;

                while (pending.Count > 0)
                {
                    var line = pending.Dequeue();
                    try
                    {
                        if (CanEvaluate(line))
                        {
                            Process(line, highlightOutput);
                            processedLines.Add(line);
                            progressMade = true;
                        }
                        else
                        {
                            retryQueue.Enqueue(line);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Re-throw unexpected errors
                        throw new Exception($"Failed to process line: {line}\nReason: {ex.Message}", ex);
                    }
                }

                pending = retryQueue;

            } while (progressMade && pending.Count > 0);

            if (pending.Count > 0)
            {
                var unresolved = string.Join("\n", pending.Select(l => $"[red]{l}[/]"));
                throw new Exception("Unresolved instructions due to undefined variables:\n" + unresolved);
            }
        }

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
                    AnsiConsole.MarkupLine($"[green]{varName} = {result.ToString("0.######", CultureInfo.InvariantCulture)}[/]");
                else
                    Console.WriteLine($"{varName} = {result.ToString("0.######", CultureInfo.InvariantCulture)}");


                PrintBetterRepresentation(result, highlightOutput);
            }
            else if (Regex.IsMatch(line, @"^(.+?)=>\s*_([2-9]|1[0-6])\s*;?$"))
            {
                var match = Regex.Match(line, @"^(.+?)=>\s*_([2-9]|1[0-6])\s*;?$");
                string expr = match.Groups[1].Value.Trim();
                int targetBase = int.Parse(match.Groups[2].Value);

                string normalized = NormalizeUnaryMinus(expr);
                string withVars = ReplaceVariables(normalized);
                string expanded = Converter.ConvertSpecialNotations(withVars);
                Lexer.ValidateCharacters(expanded);
                double result = Compute(expanded);

                string converted = Converter.ConvertToBase(result, targetBase);

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
                    double value = variables[varName];
                    if (highlightOutput)
                        AnsiConsole.MarkupLine($"[green]{varName} = {value.ToString("0.######", CultureInfo.InvariantCulture)}[/]");
                    else
                        Console.WriteLine($"{varName} = {value.ToString("0.######", CultureInfo.InvariantCulture)}");

                    PrintBetterRepresentation(value, highlightOutput);
                }
                else
                {
                    if (highlightOutput)
                        AnsiConsole.MarkupLine($"[red]Variable '{varName}' is not defined[/]");
                    else
                        Console.WriteLine($"Variable '{varName}' is not defined");
                }
            }
            else if (line.Trim().EndsWith("="))
            {
                string expr = line.Trim().TrimEnd('=');
                double result = EvaluateExpression(expr);

                if (highlightOutput)
                    AnsiConsole.MarkupLine($"Result: [green]{result.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture)}[/]");
                else
                    Console.WriteLine($"Result: {result.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture)}");

                PrintBetterRepresentation(result, highlightOutput);
            }
            else
            {
                throw new Exception("Unknown instruction format");
            }
        }

        private void PrintBetterRepresentation(double value, bool highlightOutput)
        {
            if (HasRepeatingDecimal(value))
            {
                int? betterBase = Converter.FindBestFiniteBase(value);
                if (betterBase != null)
                {
                    string alt = Converter.ConvertToBase(value, betterBase.Value);
                    if (highlightOutput)
                    {
                        AnsiConsole.MarkupLine($"[yellow]Note:[/] This is a repeating decimal in base 10. Better representation found in base {betterBase}: [green]{alt}_{betterBase}[/]");
                    }
                    else
                    {
                        Console.WriteLine($"Note: This is a repeating decimal in base 10. Better representation found in base {betterBase}: {alt}_{betterBase}");
                    }
                }
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
                expr = Regex.Replace(expr, $@"(?<=^|[^A-Za-z0-9_])-{Regex.Escape(kvp.Key)}\b",
                    match => $"-1*{kvp.Value.ToString(CultureInfo.InvariantCulture)}");

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
                if (Match('-'))
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
        private bool HasRepeatingDecimal(double value)
        {
            if (value == Math.Floor(value)) return false;

            var frac = Converter.AsRational(value);
            int denominator = frac.Item2;

            foreach (var prime in Converter.GetPrimeFactors(denominator))
            {
                if (prime != 2 && prime != 5)
                    return true;
            }

            return false;
        }

        private bool CanEvaluate(string line)
        {
            // Queries are always safe if the variable exists
            if (Lexer.IsQuery(line))
                return variables.ContainsKey(Lexer.GetQueryVariable(line));

            // Expressions with base conversion (e.g., A + B => _2)
            var baseConvMatch = Regex.Match(line, @"^(.+?)=>\s*_([2-9]|1[0-6])\s*;?$");
            string expr = baseConvMatch.Success ? baseConvMatch.Groups[1].Value.Trim() : line;

            // If it's an assignment, extract only the right-hand expression
            if (Lexer.IsAssignment(line))
                expr = Lexer.ParseAssignment(line).expr;

            // Replace known variables with 1, then check for any unknowns
            string temp = expr;
            foreach (var kvp in variables.OrderByDescending(k => k.Key.Length))
            {
                temp = Regex.Replace(temp, $@"(?<=^|[^A-Za-z0-9_]){Regex.Escape(kvp.Key)}\b", "1");
            }

            // Now check if there are any variable-like tokens left
            var tokens = Regex.Matches(temp, @"\b[A-Za-z_][A-Za-z0-9_]*\b");
            foreach (Match token in tokens)
            {
                if (!variables.ContainsKey(token.Value))
                    return false;
            }

            return true;
        }

    }
}
