using Spectre.Console;
using System.Globalization;
using System.Text.RegularExpressions;

namespace TextCalculator
{
    public class Evaluator
    {
        private readonly Dictionary<string, ExpressionValue> variables = new();

        public void Process(string line, bool highlightOutput = false)
        {
            Lexer.ValidateCharacters(line);
            Lexer.DetectInvalidLeadingZerosGlobal(line);

            if (Lexer.IsAssignment(line))
            {
                var (varName, expr) = Lexer.ParseAssignment(line);
                try
                {
                    string withVars = ReplaceVariables(expr);
                    string normalized = NormalizeUnaryMinus(withVars);
                    string expanded = Converter.ConvertSpecialNotations(normalized);
                    Lexer.ValidateCharacters(expanded);
                    double result = Compute(expanded);
                    variables[varName] = new ExpressionValue { Expression = expr, NumericValue = result };

                    PrintAssignment(varName, result, highlightOutput);
                }
                catch
                {
                    // Save unresolved expression
                    variables[varName] = new ExpressionValue { Expression = expr };
                }
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
            }
            else if (Lexer.IsQuery(line))
            {
                string varName = Lexer.GetQueryVariable(line);
                if (variables.TryGetValue(varName, out var value))
                {
                    if (value.IsResolved)
                    {
                        PrintAssignment(varName, value.NumericValue!.Value, highlightOutput);
                        PrintBetterRepresentation(value.NumericValue.Value, highlightOutput);
                    }
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
                    AnsiConsole.MarkupLine($"Result: [green]{result.ToString("0.######", CultureInfo.InvariantCulture)}[/]");
                else
                    Console.WriteLine($"Result: {result.ToString("0.######", CultureInfo.InvariantCulture)}");

                PrintBetterRepresentation(result, highlightOutput);
            }
            else
            {
                throw new Exception("Unknown instruction format");
            }
        }

        public void ProcessBlock(IEnumerable<string> lines, bool highlightOutput = false)
        {
            foreach (var line in lines.Where(l => !string.IsNullOrWhiteSpace(l)))
            {
                try
                {
                    Process(line, highlightOutput);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to process line: {line}\nReason: {ex.Message}", ex);
                }
            }

            TryEvaluatePending(highlightOutput);

            var unresolved = variables.Where(kvp => !kvp.Value.IsResolved);
            if (unresolved.Any())
            {
                var list = string.Join("\n", unresolved.Select(kvp => $"[red]{kvp.Key} = {kvp.Value}[/]"));
                throw new Exception("Unresolved instructions due to undefined variables:\n" + list);
            }
        }

        public void TryEvaluatePending(bool highlightOutput = false)
        {
            bool changed;
            do
            {
                changed = false;
                foreach (var key in variables.Keys.ToList())
                {
                    var val = variables[key];
                    if (val.IsResolved) continue;

                    try
                    {
                        double result = EvaluateExpression(val.Expression);
                        variables[key] = new ExpressionValue { Expression = val.Expression, NumericValue = result };
                        PrintAssignment(key, result, highlightOutput);
                        PrintBetterRepresentation(result, highlightOutput);
                        changed = true;
                    }
                    catch
                    {
                        // still unresolved
                    }
                }
            }
            while (changed);
        }

        private void PrintAssignment(string name, double value, bool highlightOutput)
        {
            if (highlightOutput)
                AnsiConsole.MarkupLine($"[green]{name} = {value.ToString("0.######", CultureInfo.InvariantCulture)}[/]");
            else
                Console.WriteLine($"{name} = {value.ToString("0.######", CultureInfo.InvariantCulture)}");
        }

        private double EvaluateExpression(string expression)
        {
            expression = expression.Trim();
            if (expression.EndsWith(";"))
                expression = expression[..^1].Trim();

            string withVars = ReplaceVariables(expression);
            string normalized = NormalizeUnaryMinus(withVars);
            string expanded = Converter.ConvertSpecialNotations(normalized);
            Lexer.ValidateCharacters(expanded);
            return Compute(expanded);
        }

        private string ReplaceVariables(string expr)
        {
            foreach (var kvp in variables.OrderByDescending(k => k.Key.Length))
            {
                if (kvp.Value.IsResolved)
                {
                    expr = Regex.Replace(expr, $@"(?<=^|[^A-Za-z0-9_])-{Regex.Escape(kvp.Key)}\b",
                        $"-1*{kvp.Value.NumericValue!.Value.ToString(CultureInfo.InvariantCulture)}");

                    expr = Regex.Replace(expr, $@"\b{Regex.Escape(kvp.Key)}\b",
                        kvp.Value.NumericValue!.Value.ToString(CultureInfo.InvariantCulture));
                }
            }
            return expr;
        }

        private string NormalizeUnaryMinus(string expr)
        {
            return Regex.Replace(expr, "(?<=^|\\(|\\+|\\-|\\*|\\/)-", "u");
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
                return ParsePower();
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

                string numStr = expr[start..pos];
                return double.Parse(numStr, CultureInfo.InvariantCulture);
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
                        AnsiConsole.MarkupLine($"[yellow]Note:[/] Better representation in base {betterBase}: [green]{alt}_{betterBase}[/]");
                    }
                    else
                    {
                        Console.WriteLine($"Note: Better representation in base {betterBase}: {alt}_{betterBase}");
                    }
                }
            }
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
    }

    public class ExpressionValue
    {
        public string Expression { get; set; } = "";
        public double? NumericValue { get; set; }
        public bool IsResolved => NumericValue.HasValue;

        public override string ToString() => IsResolved
            ? NumericValue.Value.ToString("G17", CultureInfo.InvariantCulture)
            : Expression;
    }
}
