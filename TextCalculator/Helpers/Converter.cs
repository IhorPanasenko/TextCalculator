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

        public static string ConvertToBase(double value, int numBase)
        {
            string digits = "0123456789ABCDEF";

            bool isNegative = value < 0;
            value = Math.Abs(value);

            long intPart = (long)value;
            double fracPart = value - intPart;

            string intStr = "";
            if (intPart == 0)
                intStr = "0";
            else
            {
                while (intPart > 0)
                {
                    intStr = digits[(int)(intPart % numBase)] + intStr;
                    intPart /= numBase;
                }
            }

            string fracStr = "";
            int maxDigits = 10;
            while (fracPart > 0 && fracStr.Length < maxDigits)
            {
                fracPart *= numBase;
                int digit = (int)fracPart;
                fracStr += digits[digit];
                fracPart -= digit;
            }

            string result = fracStr.Length > 0 ? $"{intStr}.{fracStr}" : intStr;
            return isNegative ? "-" + result : result;
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

        private static Tuple<int, int> AsRational(double value, int maxDenominator = 10000)
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

        private static IEnumerable<int> GetPrimeFactors(int number)
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