using System.Text.RegularExpressions;
using System.Globalization;

namespace TextCalculator
{
    public static class Converter
    {
        private static readonly Regex BaseNumberRegex = new(
            @"(?<sign>[-+]?)((?<int>[0-9A-Fa-f]+)(\.(?<frac>[0-9A-Fa-f]+))?)_(?<base>[2-9]|1[0-6])\b",
            RegexOptions.Compiled);

        private static readonly Regex RepeatingDecimalRegex = new(
            @"(?<int>\d*)\.(?<nonrep>\d*)\((?<rep>\d+)\)", RegexOptions.Compiled);


        public static string ConvertSpecialNotations(string expr)
        {
            expr = RepeatingDecimalRegex.Replace(expr, match =>
            {
                string intPart = match.Groups["int"].Value;
                string nonRepPart = match.Groups["nonrep"].Value;
                string repPart = match.Groups["rep"].Value;

                double result = ConvertRepeatingDecimal(intPart, nonRepPart, repPart);
                return result.ToString("G17", CultureInfo.InvariantCulture);
            });

            expr = BaseNumberRegex.Replace(expr, match =>
            {
                string sign = match.Groups["sign"].Value;
                string intPart = match.Groups["int"].Value;
                string fracPart = match.Groups["frac"].Success ? match.Groups["frac"].Value : "";
                int numBase = int.Parse(match.Groups["base"].Value, CultureInfo.InvariantCulture);

                double value = ConvertBaseNumber(intPart, fracPart, numBase);
                if (sign == "-") value = -value;

                return value.ToString("G17", CultureInfo.InvariantCulture);
            });

            return expr;
        }

        private static double ConvertRepeatingDecimal(string intPart, string nonRepPart, string repPart)
        {
            string fullNumber = $"{intPart}.{nonRepPart}{repPart}{repPart}{repPart}";
            double approx = double.Parse(fullNumber, CultureInfo.InvariantCulture);

            int d = repPart.Length;
            int k = nonRepPart.Length;

            double pow10_k = Math.Pow(10, k);
            double pow10_d = Math.Pow(10, d);
            double numerator = double.Parse($"{intPart}{nonRepPart}{repPart}", CultureInfo.InvariantCulture) -
                               double.Parse($"{intPart}{nonRepPart}", CultureInfo.InvariantCulture);
            double denominator = pow10_k * (pow10_d - 1);

            return numerator / denominator + (string.IsNullOrEmpty(intPart) ? 0 : int.Parse(intPart));
        }


        private static double ConvertBaseNumber(string intPart, string fracPart, int numBase)
        {
            double result = 0;
            for (int i = 0; i < intPart.Length; i++)
            {
                int digit = ParseDigit(intPart[i]);
                if (digit >= numBase)
                    throw new Exception($"Digit '{intPart[i]}' is not valid for base {numBase}");
                result = result * numBase + digit;
            }

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