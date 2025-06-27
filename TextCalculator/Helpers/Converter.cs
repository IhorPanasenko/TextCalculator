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

        public static string ConvertToBase(double number, int baseN, int maxFractionDigits = 12)
        {
            if (baseN < 2 || baseN > 16)
                throw new ArgumentException("Base must be between 2 and 16");

            string digits = "0123456789ABCDEF";

            bool isNegative = number < 0;
            number = Math.Abs(number);

            long integerPart = (long)Math.Floor(number);
            double fractionPart = number - integerPart;

            // Convert integer part
            string intStr = "";
            if (integerPart == 0)
                intStr = "0";
            else
            {
                while (integerPart > 0)
                {
                    int digit = (int)(integerPart % baseN);
                    intStr = digits[digit] + intStr;
                    integerPart /= baseN;
                }
            }

            // Convert fractional part
            string fracStr = "";
            int count = 0;
            HashSet<double> seen = new();
            while (fractionPart > 0 && count < maxFractionDigits)
            {
                fractionPart *= baseN;
                int digit = (int)Math.Floor(fractionPart);
                fracStr += digits[digit];
                fractionPart -= digit;

                // Optional: break repeating cycle
                if (!seen.Add(fractionPart))
                    break;

                count++;
            }

            string result = isNegative ? "-" : "";
            result += intStr;
            if (fracStr.Length > 0)
                result += "." + fracStr;

            return result;
        }

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
                string sign = match.Groups["sign"].Value; // може бути "-" або "" (порожнє)
                string intPart = match.Groups["int"].Value;
                string fracPart = match.Groups["frac"].Success ? match.Groups["frac"].Value : "";
                int numBase = int.Parse(match.Groups["base"].Value, CultureInfo.InvariantCulture);

                double value = ConvertBaseNumber(intPart, fracPart, numBase);
                if (sign == "-") value = -value;

                return value.ToString("G17", CultureInfo.InvariantCulture);
            });

            return expr;
        }

        public static double ConvertRepeatingDecimal(string intPart, string nonRepPart, string repPart)
        {
            int integer = string.IsNullOrEmpty(intPart) ? 0 : int.Parse(intPart);
            double nonRepeating = string.IsNullOrEmpty(nonRepPart) ? 0.0 :
                double.Parse("0." + nonRepPart, CultureInfo.InvariantCulture);

            double numerator = double.Parse(repPart, CultureInfo.InvariantCulture);
            double denominator = Math.Pow(10, repPart.Length) - 1;
            denominator *= Math.Pow(10, nonRepPart.Length);

            double repeating = numerator / denominator;

            return integer + nonRepeating + repeating;
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

        public static int? FindBestFiniteBase(double value)
        {
            if (value == Math.Floor(value)) return null;

            var frac = AsRational(value);
            int denom = frac.Item2;

            for (int baseN = 2; baseN <= 16; baseN++)
            {
                var baseFactors = GetPrimeFactors(baseN).ToHashSet();
                bool ok = true;
                foreach (var d in GetPrimeFactors(denom))
                {
                    if (!baseFactors.Contains(d))
                    {
                        ok = false;
                        break;
                    }
                }
                if (ok)
                    return baseN;
            }
            return null;
        }

        public static Tuple<int, int> AsRational(double value, int maxDenominator = 10000)
        {
            int sign = Math.Sign(value);
            value = Math.Abs(value);
            int bestDen = 1;
            double bestError = double.MaxValue;
            int bestNum = 0;

            for (int d = 1; d <= maxDenominator; d++)
            {
                int n = (int)Math.Round(value * d);
                double err = Math.Abs(value - (double)n / d);
                if (err < bestError)
                {
                    bestError = err;
                    bestDen = d;
                    bestNum = n;
                }
            }

            return Tuple.Create(sign * bestNum, bestDen);
        }

        public static IEnumerable<int> GetPrimeFactors(int number)
        {
            int n = number;
            for (int i = 2; i <= n / i; i++)
            {
                while (n % i == 0)
                {
                    yield return i;
                    n /= i;
                }
            }
            if (n > 1) yield return n;
        }
    }
}