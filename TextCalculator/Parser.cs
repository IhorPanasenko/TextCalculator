using System.Text.RegularExpressions;
namespace TextCalculator
{
    public static class Parser
    {
        /// <summary>
        /// Розбиває вираз на токени: числа, змінні, оператори, дужки
        /// </summary>
        public static List<string> Tokenize(string expression)
        {
            var tokens = new List<string>();
            var pattern = @"\d+(\.\d+)?|[A-Z]|\+|\-|\*|\/|\(|\)";
            foreach (Match match in Regex.Matches(expression, pattern))
            {
                tokens.Add(match.Value);
            }
            return tokens;
        }

        /// <summary>
        /// Приводить подібні доданки в простому вигляді, наприклад:
        /// A + A + A -> 3A
        /// </summary>
        public static string Simplify(string expression)
        {
            var tokens = Tokenize(expression);
            var varCounts = new Dictionary<string, int>();
            var others = new List<string>();

            for (int i = 0; i < tokens.Count; i++)
            {
                string token = tokens[i];

                if (Regex.IsMatch(token, @"^[A-Z]$")) // змінна
                {
                    int sign = 1;

                    // Перевірка на унарний мінус перед змінною
                    if (i > 0 && tokens[i - 1] == "-")
                    {
                        sign = -1;
                        tokens[i - 1] = "+"; // замінимо на '+' для простоти
                    }

                    if (!varCounts.ContainsKey(token))
                        varCounts[token] = 0;

                    varCounts[token] += sign;
                }
                else
                {
                    others.Add(token);
                }
            }

            string simplified = "";
            foreach (var kvp in varCounts)
            {
                string part = kvp.Value switch
                {
                    0 => "",
                    1 => kvp.Key,
                    -1 => "-" + kvp.Key,
                    _ => $"{kvp.Value}{kvp.Key}"
                };
                if (!string.IsNullOrEmpty(part))
                {
                    if (!string.IsNullOrEmpty(simplified) && kvp.Value > 0)
                        simplified += "+";
                    simplified += part;
                }
            }

            // Додаємо решту елементів (наприклад, числа, дужки)
            foreach (var token in others)
            {
                simplified += token;
            }

            return simplified;
        }
    }
}
