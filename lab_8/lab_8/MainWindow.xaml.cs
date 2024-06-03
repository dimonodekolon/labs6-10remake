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

namespace lab_8
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private double scale = 30;
        private double horizontalOffset = 0;
        private double verticalOffset = 0;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            DrawGraph();
        }

        private void DrawGraph()
        {
            if (!double.TryParse(StartTextBox.Text, out double start) ||
                !double.TryParse(EndTextBox.Text, out double end) ||
                !double.TryParse(StepTextBox.Text, out double step))
            {
                MessageBox.Show("Некорректные входные данные. Пожалуйста, введите корректные значения.");
                return;
            }

            string expression = ExpressionTextBox.Text;

            GraphCanvas.Children.Clear();

            // Draw axes
            DrawAxes();

            // Draw graph
            try
            {
                DrawFunctionGraph(expression, start, end, step);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при вычислении выражения: {ex.Message}");
            }
        }

        private void DrawAxes()
        {
            double width = GraphCanvas.ActualWidth;
            double height = GraphCanvas.ActualHeight;

            // X axis
            Line xAxis = new Line
            {
                X1 = 0,
                Y1 = height / 2 + verticalOffset,
                X2 = width,
                Y2 = height / 2 + verticalOffset,
                Stroke = Brushes.Black
            };
            GraphCanvas.Children.Add(xAxis);

            // Y axis
            Line yAxis = new Line
            {
                X1 = width / 2 + horizontalOffset,
                Y1 = 0,
                X2 = width / 2 + horizontalOffset,
                Y2 = height,
                Stroke = Brushes.Black
            };
            GraphCanvas.Children.Add(yAxis);
        }

        private void DrawFunctionGraph(string expression, double start, double end, double step)
        {
            double width = GraphCanvas.ActualWidth;
            double height = GraphCanvas.ActualHeight;
            double centerX = width / 2 + horizontalOffset;
            double centerY = height / 2 + verticalOffset;

            List<Point> points = new List<Point>();

            for (double x = start; x <= end; x += step)
            {
                double y = EvaluateExpression(expression, x);

                double screenX = centerX + x * scale;
                double screenY = centerY - y * scale;

                points.Add(new Point(screenX, screenY));
            }

            Polyline polyline = new Polyline
            {
                Stroke = Brushes.Blue,
                StrokeThickness = 2,
                Points = new PointCollection(points)
            };

            GraphCanvas.Children.Add(polyline);
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
                if (char.IsDigit(expression[i]) || expression[i] == '.' || (i == 0 && expression[i] == '-') || (i > 0 && expression[i] == '-' && !char.IsDigit(expression[i - 1]) && expression[i - 1] != ')'))
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
            string arguments = expression.Substring(startIndex, index - startIndex - 1);
            foreach (var arg in arguments.Split(','))
            {
                args.Add(ParseExpression(arg));
            }
            return args;
        }

        private double ApplyFunction(string functionName, List<double> args)
        {
            switch (functionName)
            {
                case "log":
                    if (args.Count != 1) throw new Exception("Функция log принимает 1 аргумент");
                    return Math.Log10(args[0]);
                case "sqrt":
                    if (args.Count != 1) throw new Exception("Функция sqrt принимает 1 аргумент");
                    return Math.Sqrt(args[0]);
                case "rt":
                    if (args.Count != 2) throw new Exception("Функция rt принимает 2 аргумента");
                    return Math.Pow(args[0], 1 / args[1]);
                case "sin":
                    if (args.Count != 1) throw new Exception("Функция sin принимает 1 аргумент");
                    return Math.Sin(args[0]);
                case "cos":
                    if (args.Count != 1) throw new Exception("Функция cos принимает 1 аргумент");
                    return Math.Cos(args[0]);
                case "tg":
                    if (args.Count != 1) throw new Exception("Функция tg принимает 1 аргумент");
                    return Math.Tan(args[0]);
                case "ctg":
                    if (args.Count != 1) throw new Exception("Функция ctg принимает 1 аргумент");
                    return 1 / Math.Tan(args[0]);
                default:
                    throw new Exception($"Неизвестная функция: {functionName}");
            }
        }
    }
}
