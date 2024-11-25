using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace LabIOD
{
    public partial class Form1 : Form
    {
        private List<double[]> ecgData = new List<double[]>(); // Дані для 12 каналів

        public Form1()
        {
            InitializeComponent();
        }
        private double[,] LoadData(string filePath)
        {
            try
            {
                var lines = File.ReadAllLines(filePath)
                                .Where(line => !string.IsNullOrWhiteSpace(line)) // Ігноруємо порожні рядки
                                .ToArray();

                if (lines.Length == 0)
                {
                    throw new Exception("Файл порожній або не містить коректних даних.");
                }

                int rowCount = lines.Length;
                int columnCount = 12; // Очікуємо, що завжди 12 каналів

                // Перевіряємо, чи всі рядки мають правильну кількість значень
                foreach (var line in lines.Select((value, index) => new { value, index }))
                {
                    var values = line.value.Split(',');
                    if (values.Length != columnCount)
                    {
                        throw new FormatException($"Рядок {line.index + 1} має неправильну кількість значень: {values.Length}, очікується {columnCount}.");
                    }
                }

                // Ініціалізуємо масив
                double[,] data = new double[rowCount, columnCount];

                for (int i = 0; i < rowCount; i++)
                {
                    var values = lines[i].Split(',');
                    for (int j = 0; j < columnCount; j++)
                    {
                        data[i, j] = double.Parse(values[j], CultureInfo.InvariantCulture);
                    }
                }
                return data;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при завантаженні даних: {ex.Message}", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        // TASK 1

        private void PlotECGGraphs(double[,] data)
        {
            if (data == null) return;

            int channels = data.GetLength(1); // 12 каналів
            int points = data.GetLength(0);   // Кількість рядків (точок)

            if (channels != 12)
            {
                MessageBox.Show($"Очікується 12 каналів, але знайдено {channels}.", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            for (int channel = 0; channel < channels; channel++)
            {
                var chart = this.Controls.Find($"chart{channel + 1}", true).FirstOrDefault() as Chart;
                if (chart != null)
                {
                    chart.Series.Clear();
                    var series = chart.Series.Add($"Канал {channel + 1}");
                    series.ChartType = SeriesChartType.Line;
                    series.IsVisibleInLegend = false;
                    for (int i = 0; i < points; i++)
                    {
                        series.Points.AddXY(i, data[i, channel]);
                    }

                    chart.Invalidate();

                }
                else
                {
                    MessageBox.Show($"Чарт для каналу {channel + 1} не знайдено.", "Попередження", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void CalculateStatistics(double[,] data)
        {
            if (data == null) return;

            int channels = data.GetLength(1); // Кількість каналів
            int points = data.GetLength(0);   // Кількість точок у кожному каналі

            listBoxStatistics.Items.Clear(); // Очищення перед додаванням нових даних

            for (int channel = 0; channel < channels; channel++)
            {
                // Отримуємо дані одного каналу
                double[] channelData = new double[points];
                for (int i = 0; i < points; i++)
                {
                    channelData[i] = data[i, channel];
                }

                // Обчислення статистичних параметрів
                double mean = channelData.Average();
                double harmonicMean = channelData.Where(x => x != 0).Select(x => 1 / x).DefaultIfEmpty(0).Average();
                //double geometricMean = channelData.Where(x => x > 0).Aggregate(1.0, (acc, x) => acc * x);
                //geometricMean = Math.Pow(geometricMean, (1.0 / channelData.Length));
                double variance = channelData.Average(x => Math.Pow(x - mean, 2));
                double median = channelData.OrderBy(x => x).ElementAt(channelData.Length / 2);
                double mode = channelData.GroupBy(x => x)
                                         .OrderByDescending(g => g.Count())
                                         .ThenBy(g => g.Key)
                                         .First().Key;
                double skewness = channelData.Average(x => Math.Pow(x - mean, 3)) / Math.Pow(variance, 1.5);
                double kurtosis = channelData.Average(x => Math.Pow(x - mean, 4)) / Math.Pow(variance, 2) - 3;

                // Середня різниця Джині
                double giniMeanDifference = channelData.SelectMany(x => channelData, (x, y) => Math.Abs(x - y)).Average() / (2 * channelData.Length);

                // Додавання результатів до ListBox
                listBoxStatistics.Items.Add($"Канал {channel + 1}:");
                listBoxStatistics.Items.Add($"  Середнє: {mean:F3}");
                listBoxStatistics.Items.Add($"  Гармонійне середнє: {harmonicMean:F3}");
                //listBoxStatistics.Items.Add($"  Геометричне середнє: {geometricMean:F3}");
                listBoxStatistics.Items.Add($"  Дисперсія: {variance:F3}");
                listBoxStatistics.Items.Add($"  Медіана: {median:F3}");
                listBoxStatistics.Items.Add($"  Мода: {mode:F3}");
                listBoxStatistics.Items.Add($"  Асиметрія: {skewness:F3}");
                listBoxStatistics.Items.Add($"  Ексцес: {kurtosis:F3}");
                listBoxStatistics.Items.Add($"  Середня різниця Джині: {giniMeanDifference:F3}");
                listBoxStatistics.Items.Add(""); // Порожній рядок для розділення між каналами
            }
        }

        private void btnAnalyze_Click_1(object sender, EventArgs e)
        {
            try
            {
                var data = LoadData("A5.txt");
                if (data == null) return;
                PlotECGGraphs(data);
                CalculateStatistics(data);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка: {ex.Message}");
            }
        }

        // TASK 2

        private void PerformAnova(double[,] data)
        {
            if (data == null) return;

            int channels = data.GetLength(1); // Кількість каналів (рівнів)
            int points = data.GetLength(0);   // Кількість точок у кожному каналі

            listBoxAnova.Items.Clear(); // Очищення перед додаванням результатів

            // Розрахунок середніх значень для кожного рівня (каналу)
            double[] means = new double[channels];
            for (int i = 0; i < channels; i++)
            {
                double sum = 0;
                for (int j = 0; j < points; j++)
                {
                    sum += data[j, i];
                }
                means[i] = sum / points; // Середнє для каналу i
            }

            // Загальне середнє значення
            double grandMean = means.Average();

            // Сума квадратів всередині груп (S_w)
            double S_w = 0;
            for (int i = 0; i < channels; i++)
            {
                for (int j = 0; j < points; j++)
                {
                    S_w += Math.Pow(data[j, i] - means[i], 2);
                }
            }

            // Сума квадратів між групами (S_b)
            double S_b = 0;
            for (int i = 0; i < channels; i++)
            {
                S_b += points * Math.Pow(means[i] - grandMean, 2);
            }

            // Вибіркова дисперсія всередині груп (σ^2_w)
            double sigmaSquared_w = S_w / (channels * (points - 1));

            // Вибіркова дисперсія між групами (σ^2_b)
            double sigmaSquared_b = S_b / (channels - 1);

            // F-критерій
            double F = sigmaSquared_b / sigmaSquared_w;

            // Виведення результатів
            listBoxAnova.Items.Add("Однофакторний дисперсійний аналіз:");
            listBoxAnova.Items.Add($"Загальне середнє: {grandMean:F3}");
            listBoxAnova.Items.Add($"Сума квадратів всередині груп (S_w): {S_w:F3}");
            listBoxAnova.Items.Add($"Сума квадратів між групами (S_b): {S_b:F3}");
            listBoxAnova.Items.Add($"Вибіркова дисперсія всередині груп (σ^2_w): {sigmaSquared_w:F3}");
            listBoxAnova.Items.Add($"Вибіркова дисперсія між групами (σ^2_b): {sigmaSquared_b:F3}");
            listBoxAnova.Items.Add($"F-критерій: {F:F3}");
        }

        // TASK 3

        private void PerformTwoFactorAnalysis(double[,] data)
        {
            int n = 1000; // Кількість точок в одній частині
            int k = 12;   // Кількість каналів (фактор A)
            int m = 5;    // Кількість частин (фактор B)

            // Розбиваємо дані на групи
            double[,,] groupedData = new double[k, m, n];
            for (int channel = 0; channel < k; channel++)
            {
                for (int part = 0; part < m; part++)
                {
                    for (int point = 0; point < n; point++)
                    {
                        groupedData[channel, part, point] = data[part * n + point, channel];
                    }
                }
            }

            // Обчислюємо основні статистики
            double Q1 = 0, Q2 = 0, Q3 = 0, Q4 = 0, Q5 = 0;
            for (int i = 0; i < k; i++)
            {
                for (int j = 0; j < m; j++)
                {
                    double sumX_ij = 0;
                    for (int l = 0; l < n; l++)
                    {
                        double x_ijl = groupedData[i, j, l];
                        Q1 += x_ijl;
                        sumX_ij += x_ijl;
                    }
                    Q2 += Math.Pow(sumX_ij, 2);
                }
                Q3 += Math.Pow(Q1, 2);
            }

            for (int j = 0; j < m; j++)
            {
                double sumX_j = 0;
                for (int i = 0; i < k; i++)
                {
                    for (int l = 0; l < n; l++)
                    {
                        sumX_j += groupedData[i, j, l];
                    }
                }
                Q4 += Math.Pow(sumX_j, 2);
            }

            Q5 = Math.Pow(Q1, 2) / (k * m * n);

            // Обчислюємо дисперсії
            double S0_2 = (Q1 - Q5) / (k * m * n - 1);
            double SA_2 = (Q3 - Q5) / (k - 1);
            double SB_2 = (Q4 - Q5) / (m - 1);
            double SAB_2 = (Q2 - Q3 - Q4 + Q5) / ((k - 1) * (m - 1));

            // Обчислюємо критерії F
            double FA = SA_2 / S0_2;
            double FB = SB_2 / S0_2;
            double FAB = SAB_2 / S0_2;

            // Ступені свободи
            int df1_A = k - 1;
            int df2_A = k * (m - 1);
            int df1_B = m - 1;
            int df2_B = k * (m - 1);
            int df1_AB = (k - 1) * (m - 1);
            int df2_AB = k * (m - 1);

            // Вивід результатів
            listBoxTwoFactor.Items.Clear();
            listBoxTwoFactor.Items.Add($"Дисперсія за фактором A: {SA_2:F2}");
            listBoxTwoFactor.Items.Add($"Дисперсія за фактором B: {SB_2:F2}");
            listBoxTwoFactor.Items.Add($"Додаткова дисперсія: {SAB_2:F2}");
            listBoxTwoFactor.Items.Add($"Загальна дисперсія: {S0_2:F2}");
            listBoxTwoFactor.Items.Add($"F-критерій для фактора A: {FA:F2} (df1={df1_A}, df2={df2_A})");
            listBoxTwoFactor.Items.Add($"F-критерій для фактора B: {FB:F2} (df1={df1_B}, df2={df2_B})");
            listBoxTwoFactor.Items.Add($"F-критерій для взаємодії AB: {FAB:F2} (df1={df1_AB}, df2={df2_AB})");
        }


        // TASK 4

        private void PerformCorrelationAnalysis(double[,] data)
        {
            int n = data.GetLength(0); // Кількість рядків (спостережень)
            int m = data.GetLength(1); // Кількість колонок (параметрів)

            // Крок 1: Нормалізація змінних
            double[,] normalizedData = NormalizeData(data, n, m);

            // Крок 2: Обчислення кореляційної матриці
            double[,] correlationMatrix = CalculateCorrelationMatrix(normalizedData, n, m);
            listBoxCorrelation.Items.Add("Кореляційна матриця:");
            DisplayMatrix(correlationMatrix, listBoxCorrelation);

            // Крок 3: Аналіз кореляційної матриці
            listBoxCorrelation.Items.Add("\nАналіз кореляційної матриці:");
            var significantPairs = FindHighlyCorrelatedPairs(correlationMatrix, 0.9); // Поріг 0.9
            foreach (var pair in significantPairs)
            {
                listBoxCorrelation.Items.Add($"Сильна кореляція між параметрами {pair.Item1 + 1} та {pair.Item2 + 1} (r = {pair.Item3:F2})");
            }

            // Кроки 4-7: Часткові та множинні коефіцієнти кореляції
            if (significantPairs.Count >= 3)
            {
                var (a, b, c) = (significantPairs[0].Item1, significantPairs[1].Item1, significantPairs[2].Item1); // Вибір трьох параметрів
                CalculatePartialAndMultipleCorrelation(correlationMatrix, a, b, c, listBoxCorrelation);
            }
            else
            {
                listBoxCorrelation.Items.Add("\nНедостатньо сильно корельованих параметрів для виконання часткового і множинного аналізу.");
            }

            // Крок 8: Висновки
            listBoxCorrelation.Items.Add("\nВисновки про кореляцію:");
            AnalyzeParameterIndependence(correlationMatrix, listBoxCorrelation);
        }

        private double[,] NormalizeData(double[,] data, int n, int m)
        {
            double[,] normalizedData = new double[n, m];
            for (int j = 0; j < m; j++)
            {
                double mean = 0, stddev = 0;
                for (int i = 0; i < n; i++) mean += data[i, j];
                mean /= n;

                for (int i = 0; i < n; i++) stddev += Math.Pow(data[i, j] - mean, 2);
                stddev = Math.Sqrt(stddev / n);

                for (int i = 0; i < n; i++) normalizedData[i, j] = (data[i, j] - mean) / stddev;
            }
            return normalizedData;
        }

        private double[,] CalculateCorrelationMatrix(double[,] normalizedData, int n, int m)
        {
            double[,] correlationMatrix = new double[m, m];
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < m; j++)
                {
                    double sum = 0;
                    for (int k = 0; k < n; k++)
                    {
                        sum += normalizedData[k, i] * normalizedData[k, j];
                    }
                    correlationMatrix[i, j] = sum / n;
                }
            }
            return correlationMatrix;
        }

        private List<(int, int, double)> FindHighlyCorrelatedPairs(double[,] correlationMatrix, double threshold)
        {
            var significantPairs = new List<(int, int, double)>();
            int m = correlationMatrix.GetLength(0);
            for (int i = 0; i < m; i++)
            {
                for (int j = i + 1; j < m; j++)
                {
                    if (Math.Abs(correlationMatrix[i, j]) > threshold)
                    {
                        significantPairs.Add((i, j, correlationMatrix[i, j]));
                    }
                }
            }
            return significantPairs;
        }

        private void CalculatePartialAndMultipleCorrelation(double[,] correlationMatrix, int a, int b, int c, ListBox resultsListBox)
        {
            // Частковий коефіцієнт кореляції r_ab(c)
            double r_ab = correlationMatrix[a, b];
            double r_ac = correlationMatrix[a, c];
            double r_bc = correlationMatrix[b, c];
            double r_ab_c = (r_ab - r_ac * r_bc) / Math.Sqrt((1 - r_ac * r_ac) * (1 - r_bc * r_bc));

            listBoxCorrelation.Items.Add($"\nЧастковий коефіцієнт кореляції r_ab(c): {r_ab_c:F2}");

            // Множинний коефіцієнт кореляції r_a(bc)
            double r_a_bc = Math.Sqrt((r_ab * r_ab + r_ac * r_ac - 2 * r_ab * r_ac * r_bc) / (1 - r_bc * r_bc));
            listBoxCorrelation.Items.Add($"Множинний коефіцієнт кореляції r_a(bc): {r_a_bc:F2}");
        }

        private void AnalyzeParameterIndependence(double[,] correlationMatrix, ListBox resultsListBox)
        {
            int m = correlationMatrix.GetLength(0);
            for (int i = 0; i < m; i++)
            {
                double sum = 0;
                for (int j = 0; j < m; j++) if (i != j) sum += Math.Abs(correlationMatrix[i, j]);
                double avgCorrelation = sum / (m - 1);
                listBoxCorrelation.Items.Add($"Параметр {i + 1}: Середня кореляція з іншими = {avgCorrelation:F2}");
            }
        }
        private void DisplayMatrix(double[,] matrix, ListBox listBox)
        {
            int rows = matrix.GetLength(0); // Количество строк
            int cols = matrix.GetLength(1); // Количество столбцов

            for (int i = 0; i < rows; i++)
            {
                string row = "";
                for (int j = 0; j < cols; j++)
                {
                    row += matrix[i, j].ToString("F2") + "\t"; // Форматируем до двух знаков после запятой
                }
                listBox.Items.Add(row.Trim()); // Добавляем строку в ListBox
            }
            listBox.Items.Add(""); // Пустая строка для разделения матриц
        }
        private void btnPerformAnova_Click(object sender, EventArgs e)
        {
            try
            {
                var data = LoadData("A5.txt"); // Завантаження даних
                if (data == null) return;

                PerformAnova(data); // Виконання дисперсійного аналізу
                PerformTwoFactorAnalysis(data);
                PerformCorrelationAnalysis(data);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка: {ex.Message}", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // TASK 5

        private void PerformFactorAnalysis(double[,] data)
        {
            // Нормалізація даних
            var normalizedData = NormalizeData(data);

            // Кореляційна матриця
            var correlationMatrix = CalculateCorrelationMatrix(normalizedData);

            // Пошук власних чисел та векторів
            var eigenResults = CalculateEigenValuesAndVectors(correlationMatrix);
            var eigenValues = eigenResults.Item1; // Власні чсила
            var eigenVectors = eigenResults.Item2; // Власні вектори

            // Частка дисперсії
            double totalVariance = eigenValues.Sum();
            var explainedVariance = eigenValues.Select(v => v / totalVariance).ToArray();

            listBoxFactorAnalysis.Items.Add("№\tВласні числа\tЧастка дисперсії\tСумарна дисперсія");
            double cumulativeVariance = 0;
            for (int i = 0; i < eigenValues.Length; i++)
            {
                cumulativeVariance += explainedVariance[i];
                listBoxFactorAnalysis.Items.Add($"{i + 1}\t{eigenValues[i]:F6}\t\t{explainedVariance[i] * 100:F2}%\t\t{cumulativeVariance * 100:F2}%");
            }
            // Побудова графіку частки дисперсії
            PlotExplainedVariance(explainedVariance);

            ValidateOrthogonality(eigenVectors);

            var principalComponents = CalculatePrincipalComponents(normalizedData, eigenVectors);
            ValidatePrincipalComponents(principalComponents, eigenValues);
            PlotPrincipalComponents(principalComponents);
        }
        private double[,] NormalizeData(double[,] data)
        {
            int rows = data.GetLength(0);
            int cols = data.GetLength(1);
            double[,] normalized = new double[rows, cols];

            for (int j = 0; j < cols; j++)
            {
                double mean = 0, stdDev = 0;

                for (int i = 0; i < rows; i++)
                    mean += data[i, j];
                mean /= rows;

                for (int i = 0; i < rows; i++)
                    stdDev += Math.Pow(data[i, j] - mean, 2);
                stdDev = Math.Sqrt(stdDev / rows);

                for (int i = 0; i < rows; i++)
                    normalized[i, j] = (data[i, j] - mean) / stdDev;
            }

            return normalized;
        }
        private double[,] CalculateCorrelationMatrix(double[,] data)
        {
            int rows = data.GetLength(0);
            int cols = data.GetLength(1);
            double[,] correlationMatrix = new double[cols, cols];

            for (int i = 0; i < cols; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    double sum = 0;
                    for (int k = 0; k < rows; k++)
                        sum += data[k, i] * data[k, j];

                    correlationMatrix[i, j] = sum / rows;
                }
            }

            return correlationMatrix;
        }
        private (double[], double[,]) CalculateEigenValuesAndVectors(double[,] matrix)
        {
            var matrixDense = MathNet.Numerics.LinearAlgebra.Matrix<double>.Build.DenseOfArray(matrix);
            var evd = matrixDense.Evd();

            double[] eigenValues = evd.EigenValues.Real().ToArray();
            double[,] eigenVectors = evd.EigenVectors.ToArray();

            return (eigenValues, eigenVectors);
        }
        private void PlotExplainedVariance(double[] explainedVariance)
        {
            chartFactorAnalysis.Series.Clear();
            var series = new System.Windows.Forms.DataVisualization.Charting.Series
            {
                Name = "Частка дисперсії",
                ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line
            };

            for (int i = 0; i < explainedVariance.Length; i++)
                series.Points.AddXY(i + 1, explainedVariance[i] * 100);

            chartFactorAnalysis.Series.Add(series);
            series.IsVisibleInLegend = false;
        }
        private void ValidateOrthogonality(double[,] eigenVectors)
        {
            int size = eigenVectors.GetLength(0);

            for (int i = 0; i < size; i++)
            {
                for (int j = i + 1; j < size; j++)
                {
                    double dotProduct = 0;

                    for (int k = 0; k < size; k++)
                        dotProduct += eigenVectors[k, i] * eigenVectors[k, j];

                    if (Math.Abs(dotProduct) > 1e-6)
                    {
                        listBoxFactorAnalysis.Items.Add($"Вектори {i + 1} и {j + 1} не ортогональні: скалярний добуток = {dotProduct:F6}");
                    }
                }
            }

            listBoxFactorAnalysis.Items.Add("Перевірка ортогональності завершена.");
        }
        private double[,] CalculatePrincipalComponents(double[,] normalizedData, double[,] eigenVectors)
        {
            int rows = normalizedData.GetLength(0);
            int cols = eigenVectors.GetLength(1);
            double[,] principalComponents = new double[rows, cols];

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    for (int k = 0; k < cols; k++)
                        principalComponents[i, j] += normalizedData[i, k] * eigenVectors[k, j];
                }
            }

            return principalComponents;
        }
        private void ValidatePrincipalComponents(double[,] principalComponents, double[] eigenValues)
        {
            int rows = principalComponents.GetLength(0);
            int cols = principalComponents.GetLength(1);

            for (int j = 0; j < cols; j++)
            {
                double sum = 0;
                for (int i = 0; i < rows; i++)
                    sum += principalComponents[i, j];

                if (Math.Abs(sum) > 1e-6)
                    listBoxFactorAnalysis.Items.Add($"Головна компонента {j + 1} не задовольняє умову суми = 0: сума = {sum:F6}");
            }

            for (int j = 0; j < cols; j++)
            {
                double normSquared = 0;
                for (int i = 0; i < rows; i++)
                    normSquared += Math.Pow(principalComponents[i, j], 2);

                double norm = normSquared / rows;
                if (Math.Abs(norm - eigenValues[j]) > 1e-6)
                    listBoxFactorAnalysis.Items.Add($"Головна компонента {j + 1} не задовольняє умову дисперсії: дисперсія = {norm:F6}, очікувалося = {eigenValues[j]:F6}");
            }

            listBoxFactorAnalysis.Items.Add("Перевірка головних компонент завершена.");
        }
        private void PlotPrincipalComponents(double[,] principalComponents)
        {
            // Очистимо всі графіки перед побудовою нових
            chartZ1.Series.Clear();
            chartZ2.Series.Clear();
            chartZ3.Series.Clear();

            int rows = principalComponents.GetLength(0);

            // Створюємо серії для кожної головної компоненти
            for (int componentIndex = 0; componentIndex < 3; componentIndex++)
            {
                Series series = new Series($"Компонента {componentIndex + 1}");
                series.ChartType = SeriesChartType.Line; // Тип графіка - лінійний
                series.BorderWidth = 2;
                series.IsVisibleInLegend = false;
                for (int i = 0; i < rows; i++)
                {
                    series.Points.AddXY(i + 1, principalComponents[i, componentIndex]); // Додаємо точку
                }

                // Додаємо серію в відповідний Chart в залежності від компоненти
                if (componentIndex == 0)
                {
                    chartZ1.Series.Add(series);
                }
                else if (componentIndex == 1)
                {
                    chartZ2.Series.Add(series);
                }
                else
                {
                    chartZ3.Series.Add(series);
                }
            }

            // Настроїмо осі для всіх графіків
            foreach (var chart in new[] { chartZ1, chartZ2, chartZ3 })
            {
                chart.ChartAreas[0].AxisX.Title = "";
                chart.ChartAreas[0].AxisY.Title = "Значення компоненти";
                chart.ChartAreas[0].RecalculateAxesScale();
            }
        }

        private void btnPerformFactorAnalysis_Click(object sender, EventArgs e)
        {
            try
            {
                var data = LoadData("A5.txt"); // Завантаження даних
                if (data == null) return;

                PerformFactorAnalysis(data);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка: {ex.Message}", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // TASK 6

        private List<int>[] KMeansClustering(double[,] data, int k, int maxIterations = 100, double tolerance = 1e-4)
        {
            int nPoints = data.GetLength(0); // Кількість точок
            int dimensions = data.GetLength(1); // Розмірність простору
            Random rand = new Random();

            // Ініціалізація початкових центрів мас випадковими точками
            double[,] centroids = new double[k, dimensions];
            for (int i = 0; i < k; i++)
            {
                int randomIndex = rand.Next(nPoints);
                for (int d = 0; d < dimensions; d++)
                    centroids[i, d] = data[randomIndex, d];
            }

            // Масив для зберігання приналежності кожної точки до кластеру
            int[] clusterAssignments = new int[nPoints];
            double[,] newCentroids = new double[k, dimensions];

            for (int iteration = 0; iteration < maxIterations; iteration++)
            {
                bool clusterChanged = false;

                // Крок 2: Віднести кожну точку до найближчого центру
                for (int i = 0; i < nPoints; i++)
                {
                    int closestCluster = -1;
                    double minDistance = double.MaxValue;

                    for (int j = 0; j < k; j++)
                    {
                        double distance = 0;
                        for (int d = 0; d < dimensions; d++)
                        {
                            distance += Math.Pow(data[i, d] - centroids[j, d], 2);
                        }
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            closestCluster = j;
                        }
                    }

                    if (clusterAssignments[i] != closestCluster)
                    {
                        clusterChanged = true;
                        clusterAssignments[i] = closestCluster;
                    }
                }

                // Крок 3: Перерахувати центри мас
                int[] clusterSizes = new int[k];
                Array.Clear(newCentroids, 0, newCentroids.Length);

                for (int i = 0; i < nPoints; i++)
                {
                    int cluster = clusterAssignments[i];
                    clusterSizes[cluster]++;
                    for (int d = 0; d < dimensions; d++)
                        newCentroids[cluster, d] += data[i, d];
                }

                for (int j = 0; j < k; j++)
                {
                    if (clusterSizes[j] > 0)
                    {
                        for (int d = 0; d < dimensions; d++)
                            newCentroids[j, d] /= clusterSizes[j];
                    }
                    else
                    {
                        // Якщо кластер пустий, повторно ініціалізуємо його
                        int randomIndex = rand.Next(nPoints);
                        for (int d = 0; d < dimensions; d++)
                            newCentroids[j, d] = data[randomIndex, d];
                    }
                }

                // Крок 4: Критерій зупинки
                double maxShift = 0;
                for (int j = 0; j < k; j++)
                {
                    double shift = 0;
                    for (int d = 0; d < dimensions; d++)
                        shift += Math.Pow(newCentroids[j, d] - centroids[j, d], 2);

                    maxShift = Math.Max(maxShift, Math.Sqrt(shift));
                }

                if (!clusterChanged || maxShift < tolerance)
                    break;

                Array.Copy(newCentroids, centroids, centroids.Length);
            }

            // Повертаємо кластери
            List<int>[] clusters = new List<int>[k];
            for (int i = 0; i < k; i++)
                clusters[i] = new List<int>();

            for (int i = 0; i < nPoints; i++)
                clusters[clusterAssignments[i]].Add(i);

            return clusters;
        }
        private void PlotClusters(double[,] data, List<int>[] clusters, Chart chart, string title)
        {
            chart.Series.Clear();
            chart.Titles.Clear();
            chart.Titles.Add(title);

            Random rand = new Random();
            for (int i = 0; i < clusters.Length; i++)
            {
                Series series = new Series($"Кластер {i + 1}")
                {
                    ChartType = SeriesChartType.Point
                };

                // Випадковий колір для кластеру
                series.Color = Color.FromArgb(rand.Next(256), rand.Next(256), rand.Next(256));

                foreach (int index in clusters[i])
                {
                    series.Points.AddXY(data[index, 0], data[index, 1]); // Візуалізуємо перші дві координати
                }

                chart.Series.Add(series);
            }
        }

        private void PerformClusterAnalysis(double[,] data)
        {

            var normalizedData = NormalizeData(data);

            // Кореляційна матриця
            var correlationMatrix = CalculateCorrelationMatrix(normalizedData);

            // Пошук власних чисел та векторів
            var eigenResults = CalculateEigenValuesAndVectors(correlationMatrix);
            var eigenValues = eigenResults.Item1; // Власні чсила
            var eigenVectors = eigenResults.Item2; // Власні вектори
            // Завантаження даних
            double[,] principalComponentData = CalculatePrincipalComponents(normalizedData, eigenVectors); // Головні фактори (3 розміри)

            // Кластеризація для всіх каналів
            List<int>[] clustersAll11 = KMeansClustering(data, 11);
            List<int>[] clustersAll7 = KMeansClustering(data, 7);

            // Кластеризація для головних компонент
            List<int>[] clustersPC11 = KMeansClustering(principalComponentData, 11);
            List<int>[] clustersPC7 = KMeansClustering(principalComponentData, 7);

            // Побудова графіків
            PlotClusters(data, clustersAll11, chartAllChannels11, "ЕКГ: 11 кластерів");
            PlotClusters(data, clustersAll7, chartAllChannels7, "ЕКГ: 7 кластерів");
            PlotClusters(principalComponentData, clustersPC11, chartPrincipal11, "Фактори: 11 кластерів");
            PlotClusters(principalComponentData, clustersPC7, chartPrincipal7, "Фактори: 7 кластерів");
        }

        private void btnCluster_Click(object sender, EventArgs e)
        {
            try
            {
                var data = LoadData("A5.txt"); // Завантаження даних
                if (data == null) return;

                PerformClusterAnalysis(data);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка: {ex.Message}", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // TASK 7

        private double[] CalculateFourierA(double[] data, int N)
        {
            double[] A = new double[N / 2 + 1];
            for (int j = 0; j <= N / 2; j++)
            {
                for (int i = 0; i < N; i++)
                {
                    A[j] += data[i] * Math.Cos(2 * Math.PI * i * j / N);
                }
                A[j] /= N;
            }
            return A;
        }

        private double[] CalculateFourierB(double[] data, int N)
        {
            double[] B = new double[N / 2 + 1];
            for (int j = 1; j <= N / 2; j++) // Починаємо з j = 1
            {
                for (int i = 0; i < N; i++)
                {
                    B[j] += data[i] * Math.Sin(2 * Math.PI * i * j / N);
                }
                B[j] *= 2.0 / N;
            }
            return B;
        }
        private double[] CalculateSpectrum(double[] A, double[] B)
        {
            int N = A.Length;
            double[] C = new double[N];
            for (int j = 0; j < N; j++)
            {
                C[j] = Math.Sqrt(A[j] * A[j] + B[j] * B[j]);
            }
            return C;
        }
        private double[] CalculateInverseFourier(double[] A, double[] B, int N)
        {
            double[] d1 = new double[N];
            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j <= N / 2; j++)
                {
                    d1[i] += A[j] * Math.Cos(2 * Math.PI * j * i / N);
                    if (j != 0) // Не враховуємо j = 0 для B
                    {
                        d1[i] += B[j] * Math.Sin(2 * Math.PI * j * i / N);
                    }
                }
            }
            return d1;
        }

        private void PerformFourierAnalysis(double[,] allChannelsData)
        {
            int numChannels = allChannelsData.GetLength(1);
            int numPoints = allChannelsData.GetLength(0);

            for (int channel = 0; channel < numChannels; channel++)
            {
                double[] data = new double[numPoints];
                for (int i = 0; i < numPoints; i++)
                {
                    data[i] = allChannelsData[i, channel];
                }

                // Перетворення Фур'є
                double[] A = CalculateFourierA(data, numPoints);
                double[] B = CalculateFourierB(data, numPoints);
                double[] C = CalculateSpectrum(A, B);

                // Отримуємо доступ до Chart для спектру
                Chart spectrumChart = this.Controls.Find($"fourierChart{channel + 1}", true).FirstOrDefault() as Chart;

                if (spectrumChart != null)
                {
                    spectrumChart.Series.Clear();
                    Series spectrumSeries = new Series
                    {
                        ChartType = SeriesChartType.Line,
                        Name = $"Spectrum {channel + 1}"
                    };
                    for (int i = 0; i < C.Length; i++)
                    {
                        spectrumSeries.Points.AddXY(i, C[i]);
                        spectrumSeries.IsVisibleInLegend = false;
                    }
                    spectrumChart.Series.Add(spectrumSeries);

                }
            }
        }
        private void PerformInverseFourierAnalysis(double[,] allChannelsData)
        {
            int numChannels = allChannelsData.GetLength(1);
            int numPoints = allChannelsData.GetLength(0);

            for (int channel = 0; channel < numChannels; channel++)
            {
                double[] data = new double[numPoints];
                for (int i = 0; i < numPoints; i++)
                {
                    data[i] = allChannelsData[i, channel];
                }

                // Перетворення Фур'є
                double[] A = CalculateFourierA(data, numPoints);
                double[] B = CalculateFourierB(data, numPoints);

                // Обернене перетворення
                double[] restoredData = CalculateInverseFourier(A, B, numPoints);

                // Отримуємо доступ до Chart для оберненого перетворення
                Chart inverseChart = this.Controls.Find($"inverseChart{channel + 1}", true).FirstOrDefault() as Chart;

                if (inverseChart != null)
                {
                    inverseChart.Series.Clear();
                    Series restoredSeries = new Series
                    {
                        ChartType = SeriesChartType.Line,
                        Name = $"Restored {channel + 1}"
                    };
                    for (int i = 0; i < restoredData.Length; i++)
                    {
                        restoredSeries.Points.AddXY(i, restoredData[i]);
                        restoredSeries.IsVisibleInLegend = false;
                    }
                    inverseChart.Series.Add(restoredSeries);
                }
            }
        }

        private void btnFourier_Click(object sender, EventArgs e)
        {
            try
            {
                var data = LoadData("A5.txt"); // Завантаження даних
                if (data == null) return;

                PerformFourierAnalysis(data);
                PerformInverseFourierAnalysis(data);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка: {ex.Message}", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
