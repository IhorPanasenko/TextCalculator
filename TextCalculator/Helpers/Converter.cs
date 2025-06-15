using System.Text.RegularExpressions;
using System.Globalization;

namespace TextCalculator
{
    public static class Converter
    {
        // Matches: optional sign, digits (with optional .), underscore, base (2-16)
        private static readonly Regex BaseNumberRegex = new(
            @"(?<sign>[-+]?)((?<int>[0-9A-Fa-f]+)(\.(?<frac>[0-9A-Fa-f]+))?)_(?<base>[2-9]|1[0-6])\b",
            RegexOptions.Compiled);

        public static string ConvertSpecialNotations(string expr)
        {
            return BaseNumberRegex.Replace(expr, match =>
            {
                string sign = match.Groups["sign"].Value;
                string intPart = match.Groups["int"].Value;
                string fracPart = match.Groups["frac"].Success ? match.Groups["frac"].Value : "";
                int numBase = int.Parse(match.Groups["base"].Value, CultureInfo.InvariantCulture);

                double value = ConvertBaseNumber(intPart, fracPart, numBase);
                if (sign == "-") value = -value;
                // Use InvariantCulture to ensure dot as decimal separator
                return value.ToString("G17", CultureInfo.InvariantCulture);
            });
        }

        private static double ConvertBaseNumber(string intPart, string fracPart, int numBase)
        {
            // Integer part
            double result = 0;
            for (int i = 0; i < intPart.Length; i++)
            {
                int digit = ParseDigit(intPart[i]);
                if (digit >= numBase)
                    throw new Exception($"Digit '{intPart[i]}' is not valid for base {numBase}");
                result = result * numBase + digit;
            }

            // Fractional part
            if (!string.IsNullOrEmpty(fracPart))
            {
                double frac = 0;
                double basePow = numBase;
                for (int i = 0; i < fracPart.Length; i++)
                {
                    int digit = ParseDigit(fracPart[i]);
                    if (digit >= numBase)
                        throw new Exception($"Digit '{fracPart[i]}' is not valid for base {numBase}");
                    frac += digit / basePow;
                    basePow *= numBase;
                }
                result += frac;
            }

            return result;
        }

        private static int ParseDigit(char c)
        {
            if (c >= '0' && c <= '9') return c - '0';
            if (c >= 'A' && c <= 'F') return 10 + (c - 'A');
            if (c >= 'a' && c <= 'f') return 10 + (c - 'a');
            throw new Exception($"Invalid digit '{c}' in base number");
        }
    }
}