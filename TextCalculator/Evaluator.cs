using Spectre.Console;
using System.Data;
using System.Text.RegularExpressions;

namespace TextCalculator
{
    public class Evaluator
    {
        private readonly Dictionary<string, double> variables = new();

        public void Process(string line, bool highlightOutput = false)
        {
            Lexer.ValidateCharacters(line);

            if (Lexer.IsAssignment(line))
            {
                var (varName, expr) = Lexer.ParseAssignment(line);
                double result = EvaluateExpression(expr);
                variables[varName] = result;

                if (highlightOutput)
                    AnsiConsole.MarkupLine($"[green]{varName} = {result:0.######}[/]");
                else
                    Console.WriteLine($"{varName} = {result:0.######}");
            }
            else if (Lexer.IsQuery(line))
            {
                string varName = Lexer.GetQueryVariable(line);
                if (variables.ContainsKey(varName))
                {
                    if (highlightOutput)
                        AnsiConsole.MarkupLine($"[green]{varName} = {variables[varName]:0.######}[/]");
                    else
                        Console.WriteLine($"{varName} = {variables[varName]:0.######}");
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
                    AnsiConsole.MarkupLine($"Result: [green]{result:0.######}[/]");
                else
                    Console.WriteLine($"Result: {result:0.######}");
            }
            else
            {
                throw new Exception("Unknown instruction format");
            }
        }


        private double EvaluateExpression(string expression)
        {
            string normalized = NormalizeUnaryMinus(expression);
            string withVars = ReplaceVariables(normalized);
            string expanded = Converter.ConvertSpecialNotations(withVars);
            ValidateSyntax(expanded);
            return Compute(expanded);
        }

        private string NormalizeUnaryMinus(string expr)
        {
            return Regex.Replace(expr, @"(?<=^|\(|\+|\-|\*|\/)\-", "u");
        }

        private string ReplaceVariables(string expr)
        {
            foreach (var kvp in variables)
            {
                expr = Regex.Replace(expr, $@"\b{kvp.Key}\b", kvp.Value.ToString());
            }
            return expr;
        }

        private void ValidateSyntax(string expr)
        {
            if (!Regex.IsMatch(expr, @"^[\du\.\+\-\*/\(\)\s]+$"))
                throw new Exception("Syntax error in the expression");
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
                while (pos < expr.Length && (char.IsDigit(expr[pos]) || expr[pos] == '.'))
                    pos++;
                if (start == pos)
                    throw new Exception("Expected number");
                return double.Parse(expr.Substring(start, pos - start));
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
    }
}
