using System.Text.RegularExpressions;

namespace TextCalculator
{
    public static class Lexer
    {
        private static readonly Regex variablePattern = new(@"^[A-Z]$", RegexOptions.Compiled);
        private static readonly Regex numberPattern = new(@"^(0|[1-9]\d*)(\.\d+)?$", RegexOptions.Compiled);
        private static readonly Regex assignmentPattern = new(@"^\s*([A-Za-z]\w*)\s*=\s*(.+?);?\s*$", RegexOptions.Compiled);
        private static readonly Regex queryPattern = new(@"^\?\s*([A-Za-z]\w*)\s*$", RegexOptions.Compiled);

        public static void ValidateCharacters(string line)
        {
            if (!Regex.IsMatch(line, @"^[A-Za-z0-9\s\+\-\*/\(\)=\.\?]+$"))
                throw new Exception("You have entered the symbols that are not allowed");
        }

        public static bool IsAssignment(string line)
        {
            return assignmentPattern.IsMatch(line);
        }

        public static (string varName, string expr) ParseAssignment(string line)
        {
            var match = assignmentPattern.Match(line);
            if (!match.Success)
                throw new Exception("Invalid assignment");
            return (match.Groups[1].Value, match.Groups[2].Value);
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
