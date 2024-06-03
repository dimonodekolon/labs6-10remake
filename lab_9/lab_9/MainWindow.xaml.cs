using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace lab_9
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnCalc_Click(object sender, RoutedEventArgs e)
        {
            string input = tbExpression.Text;

            if (!double.TryParse(tbValueX.Text, out double xValue))
            {
                Console.WriteLine("Некорректное значение для переменной x.");
                return;
            }

            try
            {
                double result = EvaluateExpression(input, xValue);
                lbResult.Content = $"Результат: {result}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при вычислении выражения: {ex.Message}");
            }
        }

        private double EvaluateExpression(string expression, double xValue)
        {
            // Замена переменной x на ее значение
            expression = expression.Replace("x", xValue.ToString(CultureInfo.InvariantCulture));

            // Убираем лишние пробелы из входной строки
            expression = Regex.Replace(expression, @"\s+", "");

            // Парсим и вычисляем выражение
            return ParseExpression(expression);
        }

        private double ParseExpression(string expression)
        {
            Stack<double> numbers = new Stack<double>();
            Stack<char> operations = new Stack<char>();

            int i = 0;
            while (i < expression.Length)
            {
                if (char.IsDigit(expression[i]) || expression[i] == '.' || (i > 0 && expression[i] == '-' && !char.IsDigit(expression[i - 1])))
                {
                    string number = "";
                    while (i < expression.Length && (char.IsDigit(expression[i]) || expression[i] == '.' || (number == "" && expression[i] == '-')))
                    {
                        number += expression[i];
                        i++;
                    }
                    numbers.Push(double.Parse(number, CultureInfo.InvariantCulture));
                }
                else if (expression[i] == '(')
                {
                    operations.Push(expression[i]);
                    i++;
                }
                else if (expression[i] == ')')
                {
                    while (operations.Peek() != '(')
                    {
                        numbers.Push(ApplyOperation(numbers.Pop(), numbers.Pop(), operations.Pop()));
                    }
                    operations.Pop();
                    i++;
                }
                else if (IsOperator(expression[i]))
                {
                    while (operations.Count > 0 && Priority(operations.Peek()) >= Priority(expression[i]))
                    {
                        numbers.Push(ApplyOperation(numbers.Pop(), numbers.Pop(), operations.Pop()));
                    }
                    operations.Push(expression[i]);
                    i++;
                }
                else if (IsFunction(expression, i, out string functionName, out int functionLength))
                {
                    i += functionLength;
                    var args = ParseFunctionArguments(expression, ref i);
                    numbers.Push(ApplyFunction(functionName, args));
                }
                else
                {
                    throw new Exception($"Неизвестный символ в выражении: {expression[i]}");
                }
            }

            while (operations.Count > 0)
            {
                numbers.Push(ApplyOperation(numbers.Pop(), numbers.Pop(), operations.Pop()));
            }

            return numbers.Pop();
        }

        private bool IsOperator(char c)
        {
            return c == '+' || c == '-' || c == '*' || c == '/' || c == '^';
        }

        static int Priority(char c)
        {
            if (c == '+' || c == '-')
                return 1;
            if (c == '*' || c == '/')
                return 2;
            if (c == '^')
                return 3;
            return 0;
        }

        private double ApplyOperation(double b, double a, char operation)
        {
            switch (operation)
            {
                case '+':
                    return a + b;
                case '-':
                    return a - b;
                case '*':
                    return a * b;
                case '/':
                    if (a == 0)
                    {
                        throw new DivideByZeroException("Деление на ноль.");
                    }

                    return a / b;
                case '^':
                    return Math.Pow(a, b);

                default:
                    throw new Exception($"Неизвестная операция: {operation}");
            }
        }

        private bool IsFunction(string expression, int index, out string functionName, out int length)
        {
            functionName = "";
            length = 0;
            string[] functions = { "log", "sqrt", "rt", "sin", "cos", "tg", "ctg" };
            foreach (var func in functions)
            {
                if (expression.Substring(index).StartsWith(func))
                {
                    functionName = func;
                    length = func.Length;
                    return true;
                }
            }
            return false;
        }

        private List<double> ParseFunctionArguments(string expression, ref int index)
        {
            List<double> args = new List<double>();
            if (expression[index] != '(')
            {
                throw new Exception("Ожидалась открывающая скобка для функции");
            }
            index++;
            int startIndex = index;
            int bracketsCount = 1;
            while (index < expression.Length && bracketsCount > 0)
            {
                if (expression[index] == '(')
                {
                    bracketsCount++;
                }
                else if (expression[index] == ')')
                {
                    bracketsCount--;
                }
                index++;
            }

            if (bracketsCount != 0)
            {
                throw new Exception("Несоответствие скобок в аргументах функции");
            }

            string argsExpression = expression.Substring(startIndex, index - startIndex - 1);
            string[] argsParts = argsExpression.Split(',');
            foreach (var part in argsParts)
            {
                args.Add(ParseExpression(part));
            }

            return args;
        }

        private double ApplyFunction(string functionName, List<double> args)
        {
            switch (functionName)
            {
                case "log":
                    return Math.Log(args[1], args[0]);
                case "sqrt":
                    return Math.Sqrt(args[0]);
                case "rt":
                    return Math.Pow(args[1], 1 / args[0]);
                case "sin":
                    return Math.Sin(args[0]);
                case "cos":
                    return Math.Cos(args[0]);
                case "tg":
                    return Math.Tan(args[0]);
                case "ctg":
                    return 1 / Math.Tan(args[0]);

                default:
                    throw new Exception($"Неизвестная функция: {functionName}");
            }
        }
    }
}
