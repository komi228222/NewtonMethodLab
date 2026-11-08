using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace NewtonMethodLab {
    public partial class Form1 : Form {
        private MenuStrip mainMenu;
        private ToolStripMenuItem plotMenuItem;
        private ToolStripMenuItem solveMenuItem;
        private ToolStripMenuItem stepMenuItem;
        private ToolStripMenuItem clearMenuItem;

        private Label functionLabel;
        private TextBox functionTextBox;
        private Label aLabel;
        private TextBox aTextBox;
        private Label bLabel;
        private TextBox bTextBox;
        private Label eLabel;
        private TextBox eTextBox;

        private Panel graphPanel;
        private RichTextBox logBox;

        private double currentA = 0;
        private double currentB = 5;
        private bool graphBuilt = false;

        private List<double> stepPoints = new List<double>();
        private Timer animationTimer;
        private int animationIndex = 0;

        private double? foundPointX = null;
        private double foundPointY = 0;
        private string foundPointLabel = "";

        public Form1() {
            InitializeComponent();
            BuildUserInterface();
            SetupAnimation();
        }

        private void BuildUserInterface() {
            mainMenu = new MenuStrip { Location = new Point(0, 0), Size = new Size(this.ClientSize.Width, 24) };
            plotMenuItem = new ToolStripMenuItem("Построить график");
            solveMenuItem = new ToolStripMenuItem("Найти экстремум (Ньютон)");
            stepMenuItem = new ToolStripMenuItem("Пошаговый просмотр");
            clearMenuItem = new ToolStripMenuItem("Очистить");

            mainMenu.Items.Add(plotMenuItem);
            mainMenu.Items.Add(solveMenuItem);
            mainMenu.Items.Add(stepMenuItem);
            mainMenu.Items.Add(clearMenuItem);

            plotMenuItem.Click += PlotButton_Click;
            solveMenuItem.Click += SolveButton_Click;
            stepMenuItem.Click += StepButton_Click;
            clearMenuItem.Click += ClearButton_Click;

            this.MainMenuStrip = mainMenu;
            this.Controls.Add(mainMenu);

            functionLabel = new Label { Text = "f(x) =", Location = new Point(12, 42), Size = new Size(50, 22) };
            this.Controls.Add(functionLabel);
            functionTextBox = new TextBox { Text = "x^3 - 3*x^2 + 4", Location = new Point(70, 40), Size = new Size(300, 22), Font = new Font("Consolas", 10) };
            this.Controls.Add(functionTextBox);

            aLabel = new Label { Text = "Начало x₀ =", Location = new Point(390, 42), Size = new Size(70, 22) };
            this.Controls.Add(aLabel);
            aTextBox = new TextBox { Text = "0", Location = new Point(460, 40), Size = new Size(70, 22) };
            this.Controls.Add(aTextBox);

            bLabel = new Label { Text = "Конец b =", Location = new Point(550, 42), Size = new Size(60, 22) };
            this.Controls.Add(bLabel);
            bTextBox = new TextBox { Text = "5", Location = new Point(610, 40), Size = new Size(70, 22) };
            this.Controls.Add(bTextBox);

            eLabel = new Label { Text = "e =", Location = new Point(700, 42), Size = new Size(30, 22) };
            this.Controls.Add(eLabel);
            eTextBox = new TextBox { Text = "0.0001", Location = new Point(730, 40), Size = new Size(100, 22) };
            this.Controls.Add(eTextBox);

            graphPanel = new Panel { Location = new Point(12, 75), Size = new Size(960, 400), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            graphPanel.Paint += GraphPanel_Paint;
            this.Controls.Add(graphPanel);

            logBox = new RichTextBox { Location = new Point(12, 485), Size = new Size(960, 170), ReadOnly = true, Font = new Font("Consolas", 9), BackColor = Color.WhiteSmoke };
            this.Controls.Add(logBox);

            logBox.AppendText("Введите f(x) и нажмите «Построить график».\n");
        }

        private void SetupAnimation() {
            animationTimer = new Timer();
            animationTimer.Interval = 700;
            animationTimer.Tick += (s, e) =>
            {
                if (animationIndex < stepPoints.Count) {
                    foundPointX = stepPoints[animationIndex];
                    try { foundPointY = new ExpressionParser(functionTextBox.Text).Evaluate(foundPointX.Value); } catch { foundPointY = 0; }
                    logBox.AppendText($"Шаг {animationIndex + 1}: x = {foundPointX.Value:F5}\n");
                    graphPanel.Invalidate();
                    animationIndex++;
                } else {
                    animationTimer.Stop();
                    logBox.AppendText("Анимация завершена.\n");
                }
            };
        }

        private void PlotButton_Click(object sender, EventArgs e) {
            if (!TryParseInputs(out double a, out double b, out _, out string error)) {
                MessageBox.Show(error, "Ошибка ввода", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Защита от дурака: проверяем формулу до отрисовки
            try {
                var testParser = new ExpressionParser(functionTextBox.Text);
                for (int i = 0; i <= 10; i++) {
                    double testX = a + (b - a) * i / 10.0;
                    double y = testParser.Evaluate(testX);
                    if (double.IsNaN(y) || double.IsInfinity(y))
                        throw new Exception("Функция уходит в бесконечность (разрыв графика).");
                }
            } catch (Exception ex) {
                MessageBox.Show("Ошибка в формуле:\n" + ex.Message + "\n\nПодсказка: умножение пишется через * (например, 4*x).",
                                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            currentA = a;
            currentB = b;
            graphBuilt = true;
            foundPointX = null;
            stepPoints.Clear();
            animationTimer.Stop();

            logBox.Clear();
            logBox.AppendText("График построен.\n");
            graphPanel.Invalidate();
        }

        private void SolveButton_Click(object sender, EventArgs e) {
            if (!Prepare(out double a, out double b, out double eps, out ExpressionParser parser)) return;

            Func<double, double> f = x => parser.Evaluate(x);
            double startX = (a + b) / 2;

            try {
                var result = NewtonMethodSolver.FindExtremum(f, startX, eps);

                if (!result.Success) {
                    MessageBox.Show(result.ErrorMessage, "Ошибка метода Ньютона", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                foundPointX = result.Root;
                foundPointY = result.FunctionValue;
                foundPointLabel = result.IsMinimum ? "Минимум" : "Максимум";

                logBox.AppendText($"\n=== ЭКСТРЕМУМ ===\n");
                logBox.AppendText($"Тип: {foundPointLabel}\n");
                logBox.AppendText($"x* = {result.Root:F6}\n");
                logBox.AppendText($"f(x*) = {result.FunctionValue:F6}\n");
                logBox.AppendText($"f'(x*) = {result.FirstDerivative:F6}\n");
                logBox.AppendText($"f''(x*) = {result.SecondDerivative:F6}\n");
                logBox.AppendText($"Итераций: {result.Iterations}\n");

                graphPanel.Invalidate();
            } catch (Exception ex) {
                MessageBox.Show("Ошибка при вычислении: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void StepButton_Click(object sender, EventArgs e) {
            if (!Prepare(out double a, out double b, out _, out ExpressionParser parser)) return;

            Func<double, double> f = x => parser.Evaluate(x);
            double currentX = (a + b) / 2;

            stepPoints.Clear();
            stepPoints.Add(currentX);

            try {
                for (int i = 0; i < 15; i++) {
                    double df = NewtonMethodSolver.FirstDerivative(f, currentX);
                    double d2f = NewtonMethodSolver.SecondDerivative(f, currentX);
                    if (Math.Abs(d2f) < 1e-10) break;

                    currentX = currentX - df / d2f;
                    if (double.IsNaN(currentX) || double.IsInfinity(currentX)) break;

                    stepPoints.Add(currentX);
                }

                animationIndex = 0;
                logBox.Clear();
                logBox.AppendText("=== ПОШАГОВЫЙ ПРОСМОТР ===\n");
                animationTimer.Start();
            } catch (Exception ex) {
                MessageBox.Show("Ошибка в формуле: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool Prepare(out double a, out double b, out double eps, out ExpressionParser parser) {
            a = b = eps = 0; parser = null;
            if (!graphBuilt) { MessageBox.Show("Сначала постройте график."); return false; }
            if (!TryParseInputs(out a, out b, out eps, out string err)) { MessageBox.Show(err); return false; }
            try { parser = new ExpressionParser(functionTextBox.Text); } catch { return false; }
            return true;
        }

        private bool TryParseInputs(out double a, out double b, out double eps, out string error) {
            a = b = eps = 0; error = "";
            if (!double.TryParse(aTextBox.Text.Replace(',', '.'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out a)) { error = "Некорректное значение x₀."; return false; }
            if (!double.TryParse(bTextBox.Text.Replace(',', '.'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out b)) { error = "Некорректное значение b."; return false; }
            if (!double.TryParse(eTextBox.Text.Replace(',', '.'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out eps)) { error = "Некорректное значение e."; return false; }
            if (a >= b) { error = "Начало интервала x₀ должно быть меньше конца b."; return false; }
            if (eps <= 0) { error = "Точность e должна быть больше нуля."; return false; }
            return true;
        }

        private void GraphPanel_Paint(object sender, PaintEventArgs e) {
            if (!graphBuilt) return;
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            int w = graphPanel.Width, h = graphPanel.Height, margin = 45;
            int plotW = w - 2 * margin, plotH = h - 2 * margin;
            if (plotW <= 0 || plotH <= 0) return;

            ExpressionParser parser; try { parser = new ExpressionParser(functionTextBox.Text); } catch { return; }

            double xMin = currentA - (currentB - currentA) * 0.05;
            double xMax = currentB + (currentB - currentA) * 0.05;
            int samples = Math.Min(600, plotW); // Ограничиваем количество точек для скорости

            double[] xs = new double[samples + 1], ys = new double[samples + 1];
            double yMin = double.MaxValue, yMax = double.MinValue;

            for (int i = 0; i <= samples; i++) {
                xs[i] = xMin + (xMax - xMin) * i / samples;
                try { ys[i] = parser.Evaluate(xs[i]); } catch { ys[i] = double.NaN; }
                if (!double.IsNaN(ys[i]) && !double.IsInfinity(ys[i])) { yMin = Math.Min(yMin, ys[i]); yMax = Math.Max(yMax, ys[i]); }
            }

            if (yMin == double.MaxValue) { yMin = -1; yMax = 1; }
            double yLim = Math.Max(Math.Abs(yMin), Math.Abs(yMax)) * 3;
            if (yLim < 10) yLim = 10;
            yMin = Math.Max(yMin, -yLim); yMax = Math.Min(yMax, yLim);
            double yPad = (yMax - yMin) * 0.1; yMin -= yPad; yMax += yPad;
            if (yMax - yMin < 1e-9) yMax = yMin + 1;

            Func<double, float> toX = x => (float)(margin + (x - xMin) / (xMax - xMin) * plotW);
            Func<double, float> toY = y => (float)(h - margin - (y - yMin) / (yMax - yMin) * plotH);

            using (Pen gp = new Pen(Color.FromArgb(230, 230, 230)))
                for (int i = 0; i <= 10; i++) {
                    g.DrawLine(gp, margin + plotW * i / 10f, margin, margin + plotW * i / 10f, h - margin);
                    g.DrawLine(gp, margin, margin + plotH * i / 10f, w - margin, margin + plotH * i / 10f);
                }

            using (Pen ap = new Pen(Color.Black, 2)) {
                if (yMin <= 0 && yMax >= 0) g.DrawLine(ap, margin, toY(0), w - margin, toY(0));
                if (xMin <= 0 && xMax >= 0) g.DrawLine(ap, toX(0), margin, toX(0), h - margin);
            }

            using (Pen cp = new Pen(Color.RoyalBlue, 2))
                for (int i = 0; i < samples; i++) {
                    if (double.IsNaN(ys[i]) || double.IsNaN(ys[i + 1]) || double.IsInfinity(ys[i]) || double.IsInfinity(ys[i + 1])) continue;

                    float sx1 = toX(xs[i]), sy1 = toY(ys[i]);
                    float sx2 = toX(xs[i + 1]), sy2 = toY(ys[i + 1]);

                    if (Math.Abs(sy1 - sy2) > plotH || sy1 < -plotH || sy1 > 2 * plotH || sy2 < -plotH || sy2 > 2 * plotH) continue;

                    g.DrawLine(cp, sx1, sy1, sx2, sy2);
                }

            if (foundPointX.HasValue) {
                float px = toX(foundPointX.Value), py = toY(foundPointY);
                using (SolidBrush b = new SolidBrush(Color.Red)) g.FillEllipse(b, px - 5, py - 5, 10, 10);
                using (Font f = new Font("Segoe UI", 9, FontStyle.Bold)) g.DrawString(foundPointLabel + ": x = " + foundPointX.Value.ToString("0.###"), f, Brushes.Red, px + 10, py - 20);
            }
        }

        private void ClearButton_Click(object sender, EventArgs e) {
            functionTextBox.Text = "x^3 - 3*x^2 + 4"; aTextBox.Text = "0"; bTextBox.Text = "5"; eTextBox.Text = "0.0001";
            logBox.Clear(); graphBuilt = false; foundPointX = null; stepPoints.Clear(); animationTimer.Stop(); graphPanel.Invalidate();
        }
    }
}