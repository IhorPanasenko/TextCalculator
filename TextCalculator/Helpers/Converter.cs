using System;
using System.Text.RegularExpressions;

namespace TextCalculator
{
    public static class Converter
    {
        public static string ConvertSpecialNotations(string expr)
        {
            // BIN(1010) → 10
            expr = Regex.Replace(expr, @"BIN\(([^)]+)\)", m =>
            {
                var val = Convert.ToInt32(m.Groups[1].Value, 2);
                return val.ToString();
            }, RegexOptions.IgnoreCase);

            // OCT(17) → 15
            expr = Regex.Replace(expr, @"OCT\(([^)]+)\)", m =>
            {
                var val = Convert.ToInt32(m.Groups[1].Value, 8);
                return val.ToString();
            }, RegexOptions.IgnoreCase);

            // DEC(42) → 42 (просто видаляємо мітку)
            expr = Regex.Replace(expr, @"DEC\(([^)]+)\)", m =>
            {
                return m.Groups[1].Value;
            }, RegexOptions.IgnoreCase);

            // HEX(1F) → 31
            expr = Regex.Replace(expr, @"HEX\(([^)]+)\)", m =>
            {
                var val = Convert.ToInt32(m.Groups[1].Value, 16);
                return val.ToString();
            }, RegexOptions.IgnoreCase);

            return expr;
        }

        // (опційно) Метод для перетворення числа назад у різні системи числення
        public static string ToBase(double value, string format)
        {
            int intValue = (int)Math.Round(value);

            return format.ToUpper() switch
            {
                "BIN" => Convert.ToString(intValue, 2),
                "OCT" => Convert.ToString(intValue, 8),
                "DEC" => intValue.ToString(),
                "HEX" => Convert.ToString(intValue, 16).ToUpper(),
                _ => throw new ArgumentException("Невідома система числення")
            };
        }
    }
}
