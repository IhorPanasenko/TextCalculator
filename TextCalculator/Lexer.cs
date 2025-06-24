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
            if (line.Contains("=>")) return false;
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

        public static void DetectInvalidLeadingZerosGlobal(string line)
        {
            var decimalMatches = Regex.Matches(line, @"(?<![_A-Za-z0-9])(?<full>\d+)(\.\d+)?(?![\w_])");
            foreach (Match match in decimalMatches)
            {
                string full = match.Groups["full"].Value;

                if (full.Length > 1 && full.StartsWith("0"))
                {
                    throw new Exception($"Invalid number '{match.Value}': leading zeros are not allowed in decimal numbers");
                }
            }

            var baseMatches = Regex.Matches(line, @"(?<sign>[-+]?)(?<int>[0-9A-Fa-f]+)(\.(?<frac>[0-9A-Fa-f]+))?_(?<base>[2-9]|1[0-6])");
            foreach (Match match in baseMatches)
            {
                string intPart = match.Groups["int"].Value;
                string baseStr = match.Groups["base"].Value;
                if (intPart.Length > 1 && intPart.StartsWith("0"))
                    throw new Exception($"Invalid base-{baseStr} number '{match.Value}': leading zeros are not allowed in integer part");
            }
        }
    }
}
