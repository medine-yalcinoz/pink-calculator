using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace PinkCalculator
{
    public partial class MainWindow : Window
    {
        // Ekrandaki sayı
        private string currentDisplay = "0";

        // İlk sayı
        private double? firstOperand = null;

        // Bekleyen işlem
        private string? pendingOperator = null;

        // Yeni sayı girilecek mi?
        private bool startNewNumber = true;


        public MainWindow()
        {
            InitializeComponent();

            UpdateDisplay();
        }


        // ==================================================
        // RAKAMLAR
        // ==================================================

        private void Number_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;

            string digit = button.Tag?.ToString() ?? "0";

            AddDigit(digit);
        }


        private void AddDigit(string digit)
        {
            if (currentDisplay == "Hata")
            {
                AC_Click(this, new RoutedEventArgs());
            }

            if (startNewNumber)
            {
                currentDisplay = digit;
                startNewNumber = false;
            }
            else
            {
                if (currentDisplay == "0")
                {
                    currentDisplay = digit;
                }
                else
                {
                    currentDisplay += digit;
                }
            }

            UpdateExpression();
            UpdateDisplay();
        }


        // ==================================================
        // ONDALIK
        // ==================================================

        private void Decimal_Click(object sender, RoutedEventArgs e)
        {
            AddDecimal();
        }


        private void AddDecimal()
        {
            if (currentDisplay == "Hata")
            {
                AC_Click(this, new RoutedEventArgs());
            }

            if (startNewNumber)
            {
                currentDisplay = "0.";
                startNewNumber = false;
            }
            else if (!currentDisplay.Contains("."))
            {
                currentDisplay += ".";
            }

            UpdateExpression();
            UpdateDisplay();
        }


        // ==================================================
        // AC
        // ==================================================

        private void AC_Click(object sender, RoutedEventArgs e)
        {
            currentDisplay = "0";

            firstOperand = null;

            pendingOperator = null;

            startNewNumber = true;

            ExpressionText.Text = "";

            ConfettiCanvas.Children.Clear();

            UpdateDisplay();
        }


        // ==================================================
        // BACKSPACE
        // ==================================================

        private void Backspace_Click(object sender, RoutedEventArgs e)
        {
            if (currentDisplay == "Hata")
            {
                AC_Click(sender, e);
                return;
            }

            if (currentDisplay.Length > 1)
            {
                currentDisplay =
                    currentDisplay.Substring(
                        0,
                        currentDisplay.Length - 1
                    );
            }
            else
            {
                currentDisplay = "0";
            }

            if (currentDisplay == "-")
            {
                currentDisplay = "0";
            }

            startNewNumber = false;

            UpdateExpression();
            UpdateDisplay();
        }


        // ==================================================
        // ±
        // ==================================================

        private void PlusMinus_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (currentDisplay == "Hata")
                return;

            if (currentDisplay == "0")
                return;

            if (currentDisplay.StartsWith("-"))
            {
                currentDisplay =
                    currentDisplay.Substring(1);
            }
            else
            {
                currentDisplay =
                    "-" + currentDisplay;
            }

            UpdateExpression();
            UpdateDisplay();
        }


        // ==================================================
        // %
        // ==================================================

        private void Percent_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (currentDisplay == "Hata")
                return;

            double value =
                ParseCurrentDisplay();

            value = value / 100.0;

            currentDisplay =
                FormatNumber(value);

            startNewNumber = true;

            UpdateExpression();
            UpdateDisplay();
        }


        // ==================================================
        // İŞLEM BUTONLARI
        // ==================================================

        private void Operator_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (currentDisplay == "Hata")
                return;

            Button button = (Button)sender;

            string operation =
                button.Tag?.ToString() ?? "+";

            PerformOperator(operation);
        }


        private void PerformOperator(string operation)
        {
            double currentValue =
                ParseCurrentDisplay();

            if (pendingOperator != null &&
                firstOperand != null &&
                !startNewNumber)
            {
                bool success =
                    TryCalculate(
                        firstOperand.Value,
                        pendingOperator,
                        currentValue,
                        out double result
                    );

                if (!success)
                {
                    ShowError();
                    return;
                }

                firstOperand = result;

                currentDisplay =
                    FormatNumber(result);
            }
            else
            {
                firstOperand = currentValue;
            }

            pendingOperator = operation;

            startNewNumber = true;

            ExpressionText.Text =
                $"{FormatNumber(firstOperand.Value)} " +
                $"{GetOperatorSymbol(operation)}";

            UpdateDisplay();
        }


        // ==================================================
        // =
        // ==================================================

        private void Equals_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (currentDisplay == "Hata")
                return;

            if (firstOperand == null ||
                pendingOperator == null)
            {
                return;
            }

            double secondOperand =
                ParseCurrentDisplay();

            string expression =
                $"{FormatNumber(firstOperand.Value)} " +
                $"{GetOperatorSymbol(pendingOperator)} " +
                $"{FormatNumber(secondOperand)} =";

            bool success =
                TryCalculate(
                    firstOperand.Value,
                    pendingOperator,
                    secondOperand,
                    out double result
                );

            if (!success)
            {
                ShowError();
                return;
            }

            ExpressionText.Text = expression;

            currentDisplay =
                FormatNumber(result);

            firstOperand = null;

            pendingOperator = null;

            startNewNumber = true;

            UpdateDisplay();

            // 🎉 HESAPLAMA BAŞARILI OLUNCA KONFETİ
            LaunchConfetti();
        }


        // ==================================================
        // HESAPLAMA
        // ==================================================

        private bool TryCalculate(
            double first,
            string operation,
            double second,
            out double result)
        {
            result = 0;

            switch (operation)
            {
                case "+":
                    result = first + second;
                    return true;

                case "-":
                    result = first - second;
                    return true;

                case "*":
                    result = first * second;
                    return true;

                case "/":

                    if (second == 0)
                    {
                        return false;
                    }

                    result = first / second;
                    return true;

                default:
                    return false;
            }
        }


        // ==================================================
        // HATA
        // ==================================================

        private void ShowError()
        {
            currentDisplay = "Hata";

            firstOperand = null;

            pendingOperator = null;

            startNewNumber = true;

            ExpressionText.Text = "";

            UpdateDisplay();
        }


        // ==================================================
        // STRING → DOUBLE
        // ==================================================

        private double ParseCurrentDisplay()
        {
            double.TryParse(
                currentDisplay,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double value
            );

            return value;
        }


        // ==================================================
        // SAYI FORMATLAMA
        // ==================================================

        private string FormatNumber(double value)
        {
            value = Math.Round(value, 10);

            return value.ToString(
                "0.##########",
                CultureInfo.InvariantCulture
            );
        }


        // ==================================================
        // İŞLEM SEMBOLLERİ
        // ==================================================

        private string GetOperatorSymbol(
            string operation)
        {
            switch (operation)
            {
                case "+":
                    return "+";

                case "-":
                    return "−";

                case "*":
                    return "×";

                case "/":
                    return "÷";

                default:
                    return operation;
            }
        }


        // ==================================================
        // İŞLEMİ EKRANIN ÜSTÜNDE GÖSTER
        // ==================================================

        private void UpdateExpression()
        {
            if (firstOperand != null &&
                pendingOperator != null)
            {
                ExpressionText.Text =
                    $"{FormatNumber(firstOperand.Value)} " +
                    $"{GetOperatorSymbol(pendingOperator)} " +
                    $"{currentDisplay}";
            }
        }


        // ==================================================
        // KLAVYE
        // ==================================================

        private void Window_PreviewKeyDown(
            object sender,
            KeyEventArgs e)
        {
            // --------------------------
            // RAKAMLAR
            // --------------------------

            if (e.Key >= Key.D0 &&
                e.Key <= Key.D9)
            {
                string digit =
                    (e.Key - Key.D0).ToString();

                AddDigit(digit);

                e.Handled = true;

                return;
            }


            // --------------------------
            // NUMPAD
            // --------------------------

            if (e.Key >= Key.NumPad0 &&
                e.Key <= Key.NumPad9)
            {
                string digit =
                    (e.Key - Key.NumPad0).ToString();

                AddDigit(digit);

                e.Handled = true;

                return;
            }


            // --------------------------
            // TOPLAMA
            // --------------------------

            if (e.Key == Key.Add ||
                e.Key == Key.OemPlus)
            {
                PerformOperator("+");

                e.Handled = true;

                return;
            }


            // --------------------------
            // ÇIKARMA
            // --------------------------

            if (e.Key == Key.Subtract ||
                e.Key == Key.OemMinus)
            {
                PerformOperator("-");

                e.Handled = true;

                return;
            }


            // --------------------------
            // ÇARPMA
            // --------------------------

            if (e.Key == Key.Multiply)
            {
                PerformOperator("*");

                e.Handled = true;

                return;
            }


            // --------------------------
            // BÖLME
            // --------------------------

            if (e.Key == Key.Divide ||
                e.Key == Key.Oem2)
            {
                PerformOperator("/");

                e.Handled = true;

                return;
            }


            // --------------------------
            // ENTER
            // --------------------------

            if (e.Key == Key.Enter ||
                e.Key == Key.Return)
            {
                Equals_Click(
                    this,
                    new RoutedEventArgs()
                );

                e.Handled = true;

                return;
            }


            // --------------------------
            // BACKSPACE
            // --------------------------

            if (e.Key == Key.Back ||
                e.Key == Key.Delete)
            {
                Backspace_Click(
                    this,
                    new RoutedEventArgs()
                );

                e.Handled = true;

                return;
            }


            // --------------------------
            // ESC = AC
            // --------------------------

            if (e.Key == Key.Escape)
            {
                AC_Click(
                    this,
                    new RoutedEventArgs()
                );

                e.Handled = true;

                return;
            }


            // --------------------------
            // NOKTA
            // --------------------------

            if (e.Key == Key.Decimal ||
                e.Key == Key.OemPeriod ||
                e.Key == Key.OemComma)
            {
                AddDecimal();

                e.Handled = true;

                return;
            }


            // --------------------------
            // YÜZDE
            // Shift + 5
            // --------------------------

            if (e.Key == Key.D5 &&
                Keyboard.Modifiers == ModifierKeys.Shift)
            {
                Percent_Click(
                    this,
                    new RoutedEventArgs()
                );

                e.Handled = true;

                return;
            }


            // --------------------------
            // SOL OK
            // --------------------------

            if (e.Key == Key.Left)
            {
                DisplayScrollViewer.LineLeft();

                e.Handled = true;

                return;
            }


            // --------------------------
            // SAĞ OK
            // --------------------------

            if (e.Key == Key.Right)
            {
                DisplayScrollViewer.LineRight();

                e.Handled = true;

                return;
            }
        }


        // ==================================================
        // EKRANI GÜNCELLE
        // ==================================================

        private void UpdateDisplay()
        {
            DisplayText.Text = currentDisplay;

            Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    DisplayScrollViewer.ScrollToRightEnd();
                }),
                DispatcherPriority.Background
            );
        }


        // ==================================================
        // 🎉 KONFETİ
        // ==================================================

        private void LaunchConfetti()
        {
            // Önce eski konfetileri temizle
            ConfettiCanvas.Children.Clear();

            Random random = new Random();

            string[] symbols =
            {
                "♥",
                "★",
                "✦",
                "♡",
                "✧",
                "❤"
            };


            // Çok fazla olmasın.
            // Sonucun görünmesini engellememesi için 22 tane.
            for (int i = 0; i < 22; i++)
            {
                TextBlock confetti =
                    new TextBlock();

                confetti.Text =
                    symbols[random.Next(symbols.Length)];

                confetti.FontSize =
                    random.Next(14, 23);

                confetti.FontWeight =
                    FontWeights.Bold;

                confetti.Foreground =
                    GetConfettiBrush(i);


                // Konfetiler sadece üst bölümden başlasın.
                double canvasWidth =
                    Math.Max(1, ConfettiCanvas.ActualWidth);

                double startX =
                    random.NextDouble() *
                    Math.Max(1, canvasWidth - 20);

                double startY =
                    random.Next(-40, 10);


                Canvas.SetLeft(
                    confetti,
                    startX);

                Canvas.SetTop(
                    confetti,
                    startY);


                ConfettiCanvas.Children.Add(
                    confetti);


                // Hareket için transform
                TranslateTransform transform =
                    new TranslateTransform();

                confetti.RenderTransform =
                    transform;


                // Hafif sağa/sola savrulsun
                double endX =
                    random.NextDouble() * 100 - 50;


                // Sadece ekranın orta-alt kısmına kadar.
                // Böylece sonuç tamamen kapanmaz.
                double endY =
                    Math.Min(
                        ConfettiCanvas.ActualHeight * 0.70,
                        330
                    );


                // Her konfeti biraz farklı hızda düşsün
                double fallTime =
                    1.5 +
                    random.NextDouble() * 0.4;


                Duration duration =
                    new Duration(
                        TimeSpan.FromSeconds(
                            fallTime
                        ));


                // Aşağı düşme
                DoubleAnimation fallAnimation =
                    new DoubleAnimation
                    {
                        From = 0,
                        To = endY,
                        Duration = duration
                    };


                // Sağa/sola hareket
                DoubleAnimation sidewaysAnimation =
                    new DoubleAnimation
                    {
                        From = 0,
                        To = endX,
                        Duration = duration
                    };


                // Sonlara doğru yavaşça kaybolsun
                DoubleAnimation opacityAnimation =
                    new DoubleAnimation
                    {
                        From = 1,
                        To = 0,
                        BeginTime =
                            TimeSpan.FromSeconds(1.2),
                        Duration =
                            TimeSpan.FromSeconds(0.8)
                    };


                transform.BeginAnimation(
                    TranslateTransform.YProperty,
                    fallAnimation);


                transform.BeginAnimation(
                    TranslateTransform.XProperty,
                    sidewaysAnimation);


                confetti.BeginAnimation(
                    OpacityProperty,
                    opacityAnimation);
            }


            // ==================================================
            // TAM 2 SANİYE SONRA HEPSİNİ SİL
            // ==================================================

            DispatcherTimer timer =
                new DispatcherTimer();

            timer.Interval =
                TimeSpan.FromSeconds(2);

            timer.Tick += (sender, e) =>
            {
                ConfettiCanvas.Children.Clear();

                timer.Stop();
            };

            timer.Start();
        }


        // ==================================================
        // KONFETİ RENKLERİ
        // ==================================================

        private Brush GetConfettiBrush(int index)
        {
            switch (index % 5)
            {
                case 0:
                    return Brushes.Red;

                case 1:
                    return Brushes.HotPink;

                case 2:
                    return Brushes.Gold;

                case 3:
                    return Brushes.White;

                default:
                    return Brushes.DeepPink;
            }
        }
    }
}