using System;
using System.Text.RegularExpressions;

namespace TextCalculator
{
    public static class Lexer
    {
        private static readonly Regex variablePattern = new(@"^[A-Z]$", RegexOptions.Compiled);
        private static readonly Regex numberPattern = new(@"^(0|[1-9]\d*)(\.\d+)?$", RegexOptions.Compiled);
        private static readonly Regex assignmentPattern = new(@"^\s*([A-Za-z]\w*)\s*=\s*(.+?);?\s*$", RegexOptions.Compiled);
        private static readonly Regex queryPattern = new(@"^\?\s*([A-Za-z]\w*)\s*$", RegexOptions.Compiled);
        private static readonly Regex formatQueryPattern = new(@"^\?\s*(BIN|OCT|DEC|HEX)\s+(.+)$", RegexOptions.Compiled);
        private static readonly Regex formatPrefixPattern = new(@"^\?\s*(BIN|OCT|DEC|HEX)\b", RegexOptions.Compiled);

        public static void ValidateCharacters(string line)
        {
            if (!Regex.IsMatch(line, @"^[A-Za-z0-9\s\+\-\*/\(\)=\.\?]+$"))
                throw new Exception("Недопустимі символи у введенні");
        }

        public static bool IsAssignment(string line)
        {
            return assignmentPattern.IsMatch(line);
        }

        public static (string varName, string expr) ParseAssignment(string line)
        {
            var match = assignmentPattern.Match(line);
            if (!match.Success)
                throw new Exception("Неправильне присвоєння");
            return (match.Groups[1].Value, match.Groups[2].Value);
        }

        public static bool IsQuery(string line)
        {
            return line.TrimStart().StartsWith("?");
        }

        public static bool IsFormatQuery(string line)
        {
            return formatPrefixPattern.IsMatch(line);
        }

        public static (string format, string expression) ParseFormatQuery(string line)
        {
            var match = formatQueryPattern.Match(line.Trim());
            if (!match.Success)
                throw new Exception("Неправильний формат запиту на конвертацію");
            return (match.Groups[1].Value.ToUpper(), match.Groups[2].Value.Trim());
        }

        public static string GetQueryVariable(string line)
        {
            var match = queryPattern.Match(line.Trim());
            if (!match.Success)
                throw new Exception("Неправильний формат запиту");
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
