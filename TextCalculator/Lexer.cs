using System.Text.RegularExpressions;

namespace TextCalculator
{
    public static class Lexer
    {
        private static readonly Regex variablePattern = new(@"^[A-Za-z][A-Za-z0-9_]*$", RegexOptions.Compiled);
        private static readonly Regex numberPattern = new(@"^(0|[1-9]\d*)(\.\d+)?$", RegexOptions.Compiled);
        private static readonly Regex assignmentPattern = new(@"^\s*(\w+)\s*=\s*(.+?);?\s*$", RegexOptions.Compiled);
        private static readonly Regex queryPattern = new(@"^\?\s*([A-Za-z][A-Za-z0-9_]*)\s*=?\s*;?\s*$", RegexOptions.Compiled);

        public static void ValidateCharacters(string line)
        {
            if (!Regex.IsMatch(line, @"^[A-Za-z0-9\s\+\-\*\^/\(\)=\.\?_><;]+$"))
                throw new Exception("You have entered the symbols that are not allowed");
        }

        public static bool IsAssignment(string line)
        {
            var match = assignmentPattern.Match(line);
            if (!match.Success)
                return false;

            string varName = match.Groups[1].Value;

            if (!Regex.IsMatch(varName, @"^[A-Za-z][A-Za-z0-9_]*$"))
                throw new Exception($"Variable '{varName}' is invalid. Variable names must start with a letter and contain only letters, digits, or underscores.");

            return true;
        }

        public static (string varName, string expr) ParseAssignment(string line)
        {
            var match = assignmentPattern.Match(line);
            if (!match.Success)
                throw new Exception("Invalid assignment");

            string varName = match.Groups[1].Value;

            if (!Regex.IsMatch(varName, @"^[A-Za-z][A-Za-z0-9_]*$"))
                throw new Exception($"Variable '{varName}' is invalid. Variable names must start with a letter and contain only letters, digits, or underscores.");

            return (varName, match.Groups[2].Value);
        }

        public static bool IsQuery(string line)
        {
            return line.TrimStart().StartsWith("?");
        }

        public static string GetQueryVariable(string line)
        {
            var match = queryPattern.Match(line.Trim());
            if (!match.Success)
                throw new Exception("Request has invalid format");
            return match.Groups[1].Value;
        }

        public static bool IsValidVariable(string input)
        {
            return variablePattern.IsMatch(input);
        }

        public static bool IsValidNumber(string input)
        {
            return numberPattern.IsMatch(input);
        }
    }
}
