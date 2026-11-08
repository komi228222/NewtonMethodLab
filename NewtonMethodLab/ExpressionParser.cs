using System;
using System.Globalization;

namespace NewtonMethodLab {
    public class ExpressionParser {
        private const int MaxExpressionLength = 500;
        private const int MaxRecursionDepth = 50;

        private string expression;
        private int position;
        private int recursionDepth;
        private double variableValue;

        public ExpressionParser(string expression) {
            if (string.IsNullOrWhiteSpace(expression))
                throw new FormatException("Формула не может быть пустой.");

            if (expression.Length > MaxExpressionLength)
                throw new FormatException("Слишком длинная формула (максимум 500 символов).");

            // Убираем все невидимые символы (Ctrl+Backspace, табы и т.д.)
            string cleanExpression = expression.ToLower();
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (char c in cleanExpression) {
                if (!char.IsControl(c) && c != ' ') sb.Append(c);
            }

            this.expression = sb.ToString();
            CheckBracketBalance(this.expression);
        }

        public double Evaluate(double x) {
            this.variableValue = x;
            this.position = 0;
            this.recursionDepth = 0;

            double result = ParseExpression();

            if (position < expression.Length)
                throw new FormatException("Неожиданный символ '" + expression[position] + "' на позиции " + position + ".");

            if (double.IsNaN(result) || double.IsInfinity(result))
                throw new FormatException("Результат вычисления не является числом (NaN или ∞).");

            return result;
        }

        private static void CheckBracketBalance(string s) {
            int depth = 0;
            for (int i = 0; i < s.Length; i++) {
                if (s[i] == '(') depth++;
                else if (s[i] == ')') depth--;
                if (depth < 0) throw new FormatException("Лишняя закрывающая скобка на позиции " + i + ".");
            }
            if (depth > 0) throw new FormatException("Не закрыта скобка (открыто " + depth + " штук).");
        }

        private void EnterRecursion() {
            recursionDepth++;
            if (recursionDepth > MaxRecursionDepth)
                throw new FormatException("Слишком глубокая вложенность выражения (максимум 50 уровней).");
        }

        private void ExitRecursion() { recursionDepth--; }

        private double ParseExpression() {
            EnterRecursion();
            try {
                double result = ParseTerm();
                while (position < expression.Length) {
                    char c = expression[position];
                    if (c == '+') { position++; result += ParseTerm(); } else if (c == '-') { position++; result -= ParseTerm(); } else break;
                }
                return result;
            } finally { ExitRecursion(); }
        }

        private double ParseTerm() {
            EnterRecursion();
            try {
                double result = ParseFactor();
                while (position < expression.Length) {
                    char c = expression[position];
                    if (c == '*') { position++; result *= ParseFactor(); } else if (c == '/') {
                        position++;
                        double divisor = ParseFactor();
                        if (Math.Abs(divisor) < 1e-15) throw new DivideByZeroException("Деление на ноль.");
                        result /= divisor;
                    } else break;
                }
                return result;
            } finally { ExitRecursion(); }
        }

        private double ParseFactor() {
            EnterRecursion();
            try {
                double baseValue = ParseBase();
                if (position < expression.Length && expression[position] == '^') {
                    position++;
                    double exponent = ParseFactor();
                    if (Math.Abs(exponent) > 1000) throw new FormatException("Слишком большая степень (максимум 1000).");
                    return Math.Pow(baseValue, exponent);
                }
                return baseValue;
            } finally { ExitRecursion(); }
        }

        private double ParseBase() {
            if (position >= expression.Length)
                throw new FormatException("Неожиданный конец выражения. Ожидалось число, x или функция.");

            char c = expression[position];

            if (c == '(') {
                position++;
                double value = ParseExpression();
                if (position >= expression.Length || expression[position] != ')')
                    throw new FormatException("Не закрыта скобка.");
                position++;
                return value;
            }

            if (c == '-') { position++; return -ParseBase(); }
            if (c == '+') { position++; return ParseBase(); }

            if (char.IsLetter(c)) {
                int start = position;
                while (position < expression.Length && char.IsLetter(expression[position])) position++;
                string name = expression.Substring(start, position - start);

                switch (name) {
                    case "x": return variableValue;
                    case "pi": return Math.PI;
                    case "e": return Math.E;
                }

                if (position >= expression.Length || expression[position] != '(')
                    throw new FormatException("После функции '" + name + "' ожидается открывающая скобка (например: " + name + "(x)).");

                position++;
                double arg = ParseExpression();
                if (position >= expression.Length || expression[position] != ')')
                    throw new FormatException("Не закрыта скобка после функции '" + name + "'.");

                position++;
                switch (name) {
                    case "sin": return Math.Sin(arg);
                    case "cos": return Math.Cos(arg);
                    case "tan": return Math.Tan(arg);
                    case "exp":
                        if (arg > 700) throw new FormatException("Переполнение в exp (аргумент > 700).");
                        return Math.Exp(arg);
                    case "ln":
                        if (arg <= 0) throw new FormatException("Аргумент логарифма ln должен быть > 0.");
                        return Math.Log(arg);
                    case "sqrt":
                        if (arg < 0) throw new FormatException("Под корнем sqrt не может быть отрицательного числа.");
                        return Math.Sqrt(arg);
                    case "abs": return Math.Abs(arg);
                    default: throw new FormatException("Неизвестная функция: '" + name + "'. Доступны: sin, cos, tan, exp, ln, sqrt, abs.");
                }
            }

            if (char.IsDigit(c) || c == '.') {
                int start = position;
                while (position < expression.Length && (char.IsDigit(expression[position]) || expression[position] == '.')) position++;

                string numberStr = expression.Substring(start, position - start);
                int dotCount = 0;
                foreach (char ch in numberStr) if (ch == '.') dotCount++;
                if (dotCount > 1) throw new FormatException("Некорректное число: " + numberStr);

                return double.Parse(numberStr, CultureInfo.InvariantCulture);
            }

            throw new FormatException("Неожиданный символ '" + c + "' на позиции " + position + ".");
        }
    }
}